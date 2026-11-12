USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-ket-noi-lai.sql
-- BẢNG GHI: [dbo].[ShareDataSession] (THÊM phiên), [dbo].[ShareDataPartner], [dbo].[ShareDataSubscription] (SỬA) —
--           không xóa. Ngoại lệ Rule 19.103 (một tệp nhiều bảng): đây là đúng một thao tác "Kết nối" của màn hình kèm
--           bật lại đăng ký, ba bảng phải đổi cùng một giao dịch; là chiều ngược của K5 (sharedata-kiem-tai-ngat-ket-noi.sql).
-- DESCRIPTION: Kết nối lại hàng loạt đối tác kiểm tải từ số @TuSo tới @DenSo (mặc định KT001 tới hết = toàn bộ), làm giống
--              PartnerCommandHandler.HandleAsync(ShareDataConnectPartnerInput), rồi bật lại đăng ký:
--                1) ShareDataSession: đối tác chưa có phiên đang hoạt động thì mở phiên mới State = 5 (Active),
--                   StartedAt = AcceptedAt = LastHeartbeatAt = giờ hiện tại, PacketsSent = PacketsRecv = 0;
--                   đã có phiên Active thì giữ phiên đó, không mở thêm;
--                2) ShareDataPartner: SessionState -> 1 (Đã kết nối);
--                3) ShareDataSubscription: mọi đăng ký đang tạm dừng (State = 2 Paused) của đối tác, CẢ HAI CHIỀU -> 1 Active.
--              Đối tác đang Tắt (Status <> 1) được bỏ qua, giống màn hình chặn kết nối đối tác bị tắt.
--              Hệ quả:
--                - Worker chiều gửi quét lại các đối tác này từ vòng quét sau;
--                - gói chiều nhận đang giữ ở Pending của các đối tác này được xử lý tiếp.
--              Khác màn hình: tệp KHÔNG ghi nhật ký hoạt động (ShareDataActivityLog) và không xóa bộ nhớ đệm của WebAPI;
--              màn hình có thể hiện trạng thái cũ cho tới khi bộ nhớ đệm hết hạn hoặc khởi động lại WebAPI.
-- THỨ TỰ CHẠY: K6, chạy độc lập bất cứ lúc nào sau K1–K4 (thường sau K5). Muốn xem trước số lượng: đặt @ChiXem = 1.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging (CSDL có dữ liệu kiểm tải KTxxx). ⛔ Không chạy trên CSDL vận hành thật.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): chỉ mở phiên cho đối tác chưa có phiên Active, chỉ đổi dòng đang
--           ngắt / đang Paused; lỗi thì hoàn tác cả ba bảng.
-- CẬP NHẬT LẦN CUỐI: 12112026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-kiem-tai-ket-noi-lai.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataPartner]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[ShareDataSession]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[ShareDataSubscription]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Thiếu bảng ShareDataPartner / ShareDataSession / ShareDataSubscription trên DB ' + DB_NAME() + N'!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @TienToMa NVARCHAR(16) = N'KT';               -- tiền tố mã đối tác kiểm tải
DECLARE @TuSo INT = 1;                                -- từ số (KT001)
DECLARE @DenSo INT = NULL;                            -- tới số; NULL = tới hết
DECLARE @ChiXem BIT = 0;                              -- 1 = chỉ liệt kê, không ghi

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-KIEM-TAI';
DECLARE @Now DATETIME = GETDATE();

IF OBJECT_ID('tempdb..#DoiTac') IS NOT NULL DROP TABLE #DoiTac;
SELECT p.[ID], p.[Code], p.[Status], p.[SessionState], p.[TenantId]
INTO #DoiTac
FROM [dbo].[ShareDataPartner] p
WHERE p.[IsDelete] IS NULL
  AND p.[Code] LIKE @TienToMa + N'%'
  AND TRY_CAST(SUBSTRING(p.[Code], LEN(@TienToMa) + 1, 16) AS INT) >= @TuSo
  AND (@DenSo IS NULL OR TRY_CAST(SUBSTRING(p.[Code], LEN(@TienToMa) + 1, 16) AS INT) <= @DenSo);

DECLARE @SoDoiTac INT = (SELECT COUNT(*) FROM #DoiTac);
DECLARE @SoDangTat INT = (SELECT COUNT(*) FROM #DoiTac WHERE ISNULL([Status], 0) <> 1);
DECLARE @SoDaNgat INT = (SELECT COUNT(*) FROM #DoiTac WHERE ISNULL([Status], 0) = 1 AND ISNULL([SessionState], 0) <> 1);
DECLARE @SoCanMoPhien INT = (SELECT COUNT(*) FROM #DoiTac d
                             WHERE ISNULL(d.[Status], 0) = 1
                               AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSession] s
                                               WHERE s.[IsDelete] IS NULL AND s.[State] = 5 AND s.[PartnerId] = d.[ID]));
DECLARE @SoDangKy INT = (SELECT COUNT(*) FROM [dbo].[ShareDataSubscription] s
                         WHERE s.[IsDelete] IS NULL AND s.[State] = 2
                           AND s.[PartnerId] IN (SELECT [ID] FROM #DoiTac WHERE ISNULL([Status], 0) = 1));

PRINT N'-> Đối tác trong khoảng: ' + CAST(@SoDoiTac AS NVARCHAR(12))
    + N' (đang tắt, bỏ qua: ' + CAST(@SoDangTat AS NVARCHAR(12)) + N'; đang ngắt: ' + CAST(@SoDaNgat AS NVARCHAR(12))
    + N'), cần mở phiên: ' + CAST(@SoCanMoPhien AS NVARCHAR(12))
    + N', đăng ký đang tạm dừng: ' + CAST(@SoDangKy AS NVARCHAR(12)) + N'.';

IF @SoDoiTac = 0
    PRINT N'⚠️ Không có đối tác nào trong khoảng, không làm gì.';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ liệt kê, không ghi dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;

        -- 1. Mở phiên Active cho đối tác đang bật chưa có phiên đang hoạt động
        INSERT INTO [dbo].[ShareDataSession]
            ([ID], [PartnerId], [State], [StartedAt], [AcceptedAt], [PacketsSent], [PacketsRecv], [LastHeartbeatAt],
             [TenantId], [CreateTime], [CreateUId])
        SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), d.[ID], 5, @Now, @Now, 0, 0, @Now,
               d.[TenantId], @Now, @SeedUser
        FROM #DoiTac d
        WHERE ISNULL(d.[Status], 0) = 1
          AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSession] s
                          WHERE s.[IsDelete] IS NULL AND s.[State] = 5 AND s.[PartnerId] = d.[ID]);
        PRINT N'-> Đã mở ' + CAST(@@ROWCOUNT AS NVARCHAR(12)) + N' phiên.';

        -- 2. Đối tác về Đã kết nối
        UPDATE p
        SET p.[SessionState] = 1
        FROM [dbo].[ShareDataPartner] p
        WHERE p.[ID] IN (SELECT [ID] FROM #DoiTac WHERE ISNULL([Status], 0) = 1) AND ISNULL(p.[SessionState], 0) <> 1;
        PRINT N'-> Đã kết nối ' + CAST(@@ROWCOUNT AS NVARCHAR(12)) + N' đối tác.';

        -- 3. Bật lại đăng ký đang tạm dừng, cả hai chiều (Paused -> Active)
        UPDATE s
        SET s.[State] = 1, s.[UpdateTime] = @Now
        FROM [dbo].[ShareDataSubscription] s
        WHERE s.[IsDelete] IS NULL AND s.[State] = 2
          AND s.[PartnerId] IN (SELECT [ID] FROM #DoiTac WHERE ISNULL([Status], 0) = 1);
        PRINT N'-> Đã bật lại ' + CAST(@@ROWCOUNT AS NVARCHAR(12)) + N' đăng ký.';

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        PRINT N'❌ Lỗi ' + CAST(ERROR_NUMBER() AS NVARCHAR(12)) + N' — ' + ERROR_MESSAGE() + N' (đã hoàn tác cả ba bảng).';
    END CATCH;
END;

-- -----------------------------------------------------------------------------------------
-- KẾT QUẢ: trạng thái từng đối tác trong khoảng
-- -----------------------------------------------------------------------------------------
SELECT d.[Code] AS [DoiTac],
       p.[Status] AS [TrangThaiBat],
       p.[SessionState] AS [TrangThaiKetNoi],
       (SELECT COUNT(*) FROM [dbo].[ShareDataSession] s
         WHERE s.[IsDelete] IS NULL AND s.[State] = 5 AND s.[PartnerId] = d.[ID]) AS [PhienDangMo],
       (SELECT COUNT(*) FROM [dbo].[ShareDataSubscription] s
         WHERE s.[IsDelete] IS NULL AND s.[State] = 1 AND s.[PartnerId] = d.[ID]) AS [DangKyDangChay],
       (SELECT COUNT(*) FROM [dbo].[ShareDataSubscription] s
         WHERE s.[IsDelete] IS NULL AND s.[State] = 2 AND s.[PartnerId] = d.[ID]) AS [DangKyTamDung]
FROM #DoiTac d
JOIN [dbo].[ShareDataPartner] p ON p.[ID] = d.[ID]
ORDER BY d.[Code];

DROP TABLE #DoiTac;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-kiem-tai-ket-noi-lai.sql';
PRINT N'=============================================================================';
GO
