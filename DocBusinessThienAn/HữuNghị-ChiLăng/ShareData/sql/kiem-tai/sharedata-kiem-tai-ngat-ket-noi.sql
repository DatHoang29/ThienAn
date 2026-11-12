USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-ngat-ket-noi.sql
-- BẢNG GHI: [dbo].[ShareDataSession], [dbo].[ShareDataPartner], [dbo].[ShareDataSubscription] — chỉ SỬA dữ liệu,
--           không thêm / xóa. Ngoại lệ Rule 19.103 (một tệp nhiều bảng): đây là đúng một thao tác "Ngắt kết nối" của
--           màn hình, ba bảng phải đổi cùng một giao dịch để không có đối tác Đã ngắt mà đăng ký vẫn chạy.
-- DESCRIPTION: Ngắt kết nối hàng loạt đối tác kiểm tải từ số @TuSo tới @DenSo (mặc định KT011 tới hết), làm giống
--              PartnerCommandHandler.HandleAsync(ShareDataDisconnectPartnerInput):
--                1) ShareDataSession: phiên đang hoạt động (State = 5 Active) -> 8 Closed, EndedAt = giờ hiện tại,
--                   EndReason = @LyDo (mặc định 'clientRequested', giống màn hình);
--                2) ShareDataPartner: SessionState -> 0 (Đã ngắt kết nối);
--                3) ShareDataSubscription: mọi đăng ký đang chạy (State = 1 Active) của đối tác, CẢ HAI CHIỀU -> 2 Paused.
--              Hệ quả:
--                - Worker chiều gửi bỏ qua đối tác ngay vòng quét sau (chỉ quét đối tác SessionState = 1);
--                - gói tin chiều nhận của các đối tác này đang chờ / đến sau sẽ được GIỮ ở Pending (đăng ký nhận Paused),
--                  không bị xử lý cho tới khi kết nối và bật lại đăng ký.
--              Khác màn hình: tệp KHÔNG ghi nhật ký hoạt động (ShareDataActivityLog) và không xóa bộ nhớ đệm của WebAPI;
--              màn hình có thể hiện trạng thái cũ cho tới khi bộ nhớ đệm hết hạn hoặc khởi động lại WebAPI.
-- THỨ TỰ CHẠY: K5, chạy độc lập bất cứ lúc nào sau K1–K4. Muốn xem trước danh sách: đặt @ChiXem = 1.
--              MỞ LẠI: chạy K6 (sharedata-kiem-tai-ket-noi-lai.sql), hoặc kết nối lại từng đối tác và bật lại đăng ký trên màn hình.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging (CSDL có dữ liệu kiểm tải KTxxx). ⛔ Không chạy trên CSDL vận hành thật.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): chỉ đổi dòng đang Active / đang kết nối; lỗi thì hoàn tác cả ba bảng.
-- CẬP NHẬT LẦN CUỐI: 12112026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-kiem-tai-ngat-ket-noi.sql trên DB: ' + DB_NAME();
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
DECLARE @TuSo INT = 11;                               -- từ số (KT011)
DECLARE @DenSo INT = NULL;                            -- tới số; NULL = tới hết
DECLARE @LyDo NVARCHAR(64) = N'clientRequested';      -- lý do đóng phiên (giống màn hình)
DECLARE @ChiXem BIT = 0;                              -- 1 = chỉ liệt kê, không ghi

DECLARE @Now DATETIME = GETDATE();

IF OBJECT_ID('tempdb..#DoiTac') IS NOT NULL DROP TABLE #DoiTac;
SELECT p.[ID], p.[Code], p.[SessionState]
INTO #DoiTac
FROM [dbo].[ShareDataPartner] p
WHERE p.[IsDelete] IS NULL
  AND p.[Code] LIKE @TienToMa + N'%'
  AND TRY_CAST(SUBSTRING(p.[Code], LEN(@TienToMa) + 1, 16) AS INT) >= @TuSo
  AND (@DenSo IS NULL OR TRY_CAST(SUBSTRING(p.[Code], LEN(@TienToMa) + 1, 16) AS INT) <= @DenSo);

DECLARE @SoDoiTac INT = (SELECT COUNT(*) FROM #DoiTac);
DECLARE @SoDangKetNoi INT = (SELECT COUNT(*) FROM #DoiTac WHERE [SessionState] = 1);
DECLARE @SoPhien INT = (SELECT COUNT(*) FROM [dbo].[ShareDataSession] s
                        WHERE s.[IsDelete] IS NULL AND s.[State] = 5 AND s.[PartnerId] IN (SELECT [ID] FROM #DoiTac));
DECLARE @SoDangKy INT = (SELECT COUNT(*) FROM [dbo].[ShareDataSubscription] s
                         WHERE s.[IsDelete] IS NULL AND s.[State] = 1 AND s.[PartnerId] IN (SELECT [ID] FROM #DoiTac));

PRINT N'-> Đối tác trong khoảng: ' + CAST(@SoDoiTac AS NVARCHAR(12))
    + N' (đang kết nối ' + CAST(@SoDangKetNoi AS NVARCHAR(12)) + N'), phiên đang mở: ' + CAST(@SoPhien AS NVARCHAR(12))
    + N', đăng ký đang chạy: ' + CAST(@SoDangKy AS NVARCHAR(12)) + N'.';

IF @SoDoiTac = 0
    PRINT N'⚠️ Không có đối tác nào trong khoảng, không làm gì.';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ liệt kê, không ghi dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;

        -- 1. Đóng phiên đang hoạt động (Active -> Closed)
        UPDATE s
        SET s.[State] = 8, s.[EndedAt] = @Now, s.[EndReason] = @LyDo
        FROM [dbo].[ShareDataSession] s
        WHERE s.[IsDelete] IS NULL AND s.[State] = 5 AND s.[PartnerId] IN (SELECT [ID] FROM #DoiTac);
        PRINT N'-> Đã đóng ' + CAST(@@ROWCOUNT AS NVARCHAR(12)) + N' phiên.';

        -- 2. Đối tác về Đã ngắt kết nối
        UPDATE p
        SET p.[SessionState] = 0
        FROM [dbo].[ShareDataPartner] p
        WHERE p.[ID] IN (SELECT [ID] FROM #DoiTac) AND ISNULL(p.[SessionState], 0) <> 0;
        PRINT N'-> Đã ngắt kết nối ' + CAST(@@ROWCOUNT AS NVARCHAR(12)) + N' đối tác.';

        -- 3. Tạm dừng đăng ký đang chạy, cả hai chiều (Active -> Paused)
        UPDATE s
        SET s.[State] = 2, s.[UpdateTime] = @Now
        FROM [dbo].[ShareDataSubscription] s
        WHERE s.[IsDelete] IS NULL AND s.[State] = 1 AND s.[PartnerId] IN (SELECT [ID] FROM #DoiTac);
        PRINT N'-> Đã tạm dừng ' + CAST(@@ROWCOUNT AS NVARCHAR(12)) + N' đăng ký.';

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
PRINT N'KẾT THÚC KỊCH BẢN sharedata-kiem-tai-ngat-ket-noi.sql';
PRINT N'=============================================================================';
GO
