USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-don-trung-TmsTrafficData.sql
-- BẢNG GHI: [dbo].[TmsTrafficData] trên CSDL nhận (*_Inbound) — chỉ bỏ dòng trùng gói 106 do Worker nhận ghi; không bảng nào khác (Rule 19.103).
-- DESCRIPTION: Dọn dòng trùng gói 106 (cân động WIM) mà câu ghi mẫu trước 13/11/2026 để lại và gộp 1-1 dòng gói 106 mồ côi
--              sang dòng gói 103 (VDS) cùng (DetectTime, Lane, Location) và tốc độ khi gói 106 đã tới trước gói 103.
--              Câu cũ so DetectTime (datetime) với thời gian JSON (datetime2): mili-giây lẻ không bao giờ khớp nên mỗi lần cùng bản ghi
--              tới lại thì chèn thêm một dòng. Còn khi 106 tới trước 103, 106 tạo dòng NEWID() và 103 tạo thêm dòng theo ID nguồn.
--              Nhóm trùng: cùng DetectTime, Lane, Location (trống coi như nhau), đúng khóa so khớp của câu ghi gói 106.
--              Chỉ xét dòng gói 106 chèn: Worker nhận ghi (CreateUId = SHAREDATA_INBOUND), chưa xóa mềm, ID là GUID do câu ghi sinh,
--              không có biển số, loại xe, hướng, thiết bị (các cột gói 103 mới ghi). Dòng gói 103 và dòng do nơi khác ghi giữ nguyên;
--              khi cặp 103–106 khớp duy nhất 1-1 (chỉ 1 dòng 103 cùng khóa và khớp tốc độ), gộp Height/Width/Length/Speed sang dòng 103 rồi xóa dòng 106 mồ côi.
--              Mỗi nhóm 106 giữ dòng ghi sau cùng (UpdateTime, không có thì CreateTime), bỏ các dòng còn lại.
--              Cách làm: dòng trùng chiếm phần lớn bảng và nằm rải rác (khóa chính ID là GUID), DELETE từng dòng trên HDD chỉ được
--              ~15.000 dòng/phút (đo 14/11/2026). Tệp chép các dòng cần giữ ra bảng tạm, gộp 106 sang 103 trên bảng tạm, TRUNCATE rồi nạp lại,
--              trong MỘT giao dịch giữ khóa bảng (TABLOCKX) từ đầu tới cuối: không ai ghi chen được; lỗi giữa chừng hoặc số dòng nạp lại lệch thì hoàn tác
--              toàn bộ, bảng về như trước. Chỉ mục, ràng buộc của bảng giữ nguyên (không tạo lại bảng).
-- THỨ TỰ CHẠY: Sau D10 và/hoặc bản Tệp 2 ngày 19/11/2026 trên CSDL chính. TẮT các dịch vụ có chiều nhận (ShareDataWorker, ShareDataWorker_Inbound_*) và dừng lần chạy
--              tệp này đang dở (nếu có) trước khi chạy: bảng bị khóa suốt lúc chạy. Chạy xong bật lại, gói đến trong lúc tắt nằm chờ
--              và được xử lý tiếp.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL nhận *_Inbound (dòng USE đầu tệp trỏ DEV_ITS015_Inbound; môi trường khác đổi tên CSDL). Chạy trên CSDL không có hậu tố _Inbound thì tệp tự bỏ qua.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): hết trùng thì không làm gì. Muốn xem trước: đặt @ChiXem = 1
--           (chỉ đếm rồi hoàn tác, vẫn khóa bảng trong lúc đếm).
-- CẬP NHẬT LẦN CUỐI: 19/11/2026 (bổ sung gộp 1-1 dòng gói 106 mồ côi sang dòng 103 khi 106 tới trước 103); 14/11/2026 (chép phần giữ rồi nạp lại thay cho xóa từng khung thời gian, bản cũ ước lượng 2–3 giờ trên HDD)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-don-trung-TmsTrafficData.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF DB_NAME() NOT LIKE N'%[_]Inbound'
   OR OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NULL
BEGIN
    PRINT N'⛔ Tệp này chỉ chạy trên CSDL nhận (*_Inbound) có bảng TmsTrafficData, đang kết nối ' + DB_NAME() + N'. Bỏ qua toàn bộ tệp.';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @ChiXem BIT = 0;                                  -- 1 = chỉ đếm rồi hoàn tác, không xóa
DECLARE @Operator NVARCHAR(64) = N'SHAREDATA_INBOUND';    -- người ghi của Worker nhận

SET XACT_ABORT ON;

DECLARE @Bat DATETIME2 = SYSDATETIME();
DECLARE @Truoc BIGINT;
DECLARE @Giu BIGINT;
DECLARE @Bo BIGINT;
DECLARE @Sau BIGINT;
DECLARE @Giay INT;

-- Cột được ghi (bỏ cột tính toán, rowversion) để nạp lại đúng cột
DECLARE @Cot NVARCHAR(MAX) =
(
    SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), QUOTENAME(c.[name])), N', ') WITHIN GROUP (ORDER BY c.[column_id])
    FROM sys.columns c
    WHERE c.[object_id] = OBJECT_ID(N'[dbo].[TmsTrafficData]')
      AND c.[is_computed] = 0
      AND TYPE_NAME(c.[system_type_id]) <> N'timestamp'
);

IF OBJECT_ID(N'tempdb..#GiuId') IS NOT NULL
    DROP TABLE #GiuId;
IF OBJECT_ID(N'tempdb..#Giu') IS NOT NULL
    DROP TABLE #Giu;
IF OBJECT_ID(N'tempdb..#Gop106') IS NOT NULL
    DROP TABLE #Gop106;

BEGIN TRANSACTION;

-- Khóa bảng tới cuối giao dịch: không ai ghi chen giữa lúc chép phần giữ và lúc nạp lại
SELECT @Truoc = COUNT_BIG(*)
FROM [dbo].[TmsTrafficData] WITH (TABLOCKX, HOLDLOCK);
RAISERROR(N'   - Đã khóa bảng, đang có %I64d dòng. Đang tìm dòng cần giữ...', 0, 1, @Truoc) WITH NOWAIT;

-- Dòng dạng gói 106: mỗi nhóm giữ dòng ghi sau cùng
SELECT n.[ID]
INTO #GiuId
FROM
(
    SELECT t.[ID],
           ROW_NUMBER() OVER (PARTITION BY t.[DetectTime], ISNULL(t.[Lane], N''), ISNULL(t.[Location], N'')
                              ORDER BY ISNULL(t.[UpdateTime], t.[CreateTime]) DESC, t.[CreateTime] DESC, t.[ID] DESC) AS [ThuTu]
    FROM [dbo].[TmsTrafficData] t
    WHERE t.[CreateUId] = @Operator
      AND t.[IsDelete] IS NULL
      AND t.[DetectTime] IS NOT NULL
      AND TRY_CONVERT(uniqueidentifier, t.[ID]) IS NOT NULL
      AND t.[LicensePlate] IS NULL
      AND t.[Type] IS NULL
      AND t.[Direction] IS NULL
      AND t.[EquipmentId] IS NULL
) n
WHERE n.[ThuTu] = 1;

CREATE UNIQUE CLUSTERED INDEX [IX_GiuId_ID] ON #GiuId ([ID]);

-- Bảng tạm cùng cột với bảng gốc (cột tính toán thành cột thường, không nạp lại)
SELECT *
INTO #Giu
FROM [dbo].[TmsTrafficData]
WHERE 1 = 0;

-- Mọi dòng không phải dạng gói 106 giữ nguyên
INSERT INTO #Giu
SELECT t.*
FROM [dbo].[TmsTrafficData] t
WHERE CASE
          WHEN t.[CreateUId] = @Operator
           AND t.[IsDelete] IS NULL
           AND t.[DetectTime] IS NOT NULL
           AND TRY_CONVERT(uniqueidentifier, t.[ID]) IS NOT NULL
           AND t.[LicensePlate] IS NULL
           AND t.[Type] IS NULL
           AND t.[Direction] IS NULL
           AND t.[EquipmentId] IS NULL THEN 1
          ELSE 0
      END = 0;

-- Dòng giữ lại của từng nhóm gói 106
INSERT INTO #Giu
SELECT t.*
FROM [dbo].[TmsTrafficData] t
JOIN #GiuId g
    ON g.[ID] = t.[ID];

-- Gộp cặp 1-1 dòng gói 106 mồ côi sang dòng gói 103 cùng (DetectTime, Lane, Location) khi 106 đã tới trước 103
SELECT
    v.[ID] AS [Id103],
    w.[ID] AS [Id106]
INTO #Gop106
FROM
(
    SELECT g.[ID],
           g.[DetectTime],
           ISNULL(g.[Lane], N'') AS [LaneKey],
           ISNULL(g.[Location], N'') AS [LocationKey],
           g.[Speed],
           COUNT(*) OVER (PARTITION BY g.[DetectTime], ISNULL(g.[Lane], N''), ISNULL(g.[Location], N'')) AS [SoDong103]
    FROM #Giu g
    WHERE g.[CreateUId] = @Operator
      AND g.[IsDelete] IS NULL
      AND g.[DetectTime] IS NOT NULL
      AND (g.[LicensePlate] IS NOT NULL
           OR g.[Type] IS NOT NULL
           OR g.[Direction] IS NOT NULL
           OR g.[EquipmentId] IS NOT NULL)
) v
JOIN #Giu w
    ON w.[DetectTime] = v.[DetectTime]
   AND ISNULL(w.[Lane], N'') = v.[LaneKey]
   AND ISNULL(w.[Location], N'') = v.[LocationKey]
JOIN #GiuId wId
    ON wId.[ID] = w.[ID]
WHERE v.[SoDong103] = 1
  AND (v.[Speed] = w.[Speed] OR v.[Speed] IS NULL OR w.[Speed] IS NULL);

DECLARE @DaGop106 BIGINT = @@ROWCOUNT;

IF @DaGop106 > 0
BEGIN
    UPDATE v
    SET v.[Height]     = ISNULL(v.[Height], w.[Height]),
        v.[Width]      = ISNULL(v.[Width], w.[Width]),
        v.[Length]     = ISNULL(v.[Length], w.[Length]),
        v.[Speed]      = ISNULL(v.[Speed], w.[Speed]),
        v.[UpdateTime] = ISNULL(v.[UpdateTime], ISNULL(w.[UpdateTime], w.[CreateTime])),
        v.[UpdateUId]  = ISNULL(v.[UpdateUId], @Operator)
    FROM #Giu v
    JOIN #Gop106 g
        ON g.[Id103] = v.[ID]
    JOIN #Giu w
        ON w.[ID] = g.[Id106];

    DELETE w
    FROM #Giu w
    JOIN #Gop106 g
        ON g.[Id106] = w.[ID];
END;

SET @Giu = (SELECT COUNT_BIG(*) FROM #Giu);
SET @Bo = @Truoc - @Giu;
SET @Giay = DATEDIFF(SECOND, @Bat, SYSDATETIME());
RAISERROR(N'   - Giữ %I64d dòng, bỏ %I64d dòng trùng (gộp %I64d dòng 106 sang 103, tìm mất %d giây).', 0, 1, @Giu, @Bo, @DaGop106, @Giay) WITH NOWAIT;

IF @ChiXem = 1
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'ℹ️ @ChiXem = 1: chỉ đếm, không xóa dữ liệu.';
END
ELSE IF @Bo = 0
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'✅ Không còn dòng trùng, không làm gì.';
END
ELSE
BEGIN
    RAISERROR(N'   - Đang xóa trắng bảng và nạp lại phần giữ...', 0, 1) WITH NOWAIT;
    TRUNCATE TABLE [dbo].[TmsTrafficData];

    DECLARE @Nap NVARCHAR(MAX) = N'INSERT INTO [dbo].[TmsTrafficData] WITH (TABLOCK) (' + @Cot + N') SELECT ' + @Cot + N' FROM #Giu;';
    EXEC sys.sp_executesql @Nap;

    SET @Sau = (SELECT COUNT_BIG(*) FROM [dbo].[TmsTrafficData]);
    IF @Sau <> @Giu
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, N'Số dòng nạp lại không khớp số dòng cần giữ, đã hoàn tác toàn bộ, bảng về như trước.', 1;
    END;

    COMMIT TRANSACTION;
    SET @Giay = DATEDIFF(SECOND, @Bat, SYSDATETIME());
    RAISERROR(N'   - Đã bỏ %I64d dòng trùng, bảng còn %I64d dòng (tổng %d giây).', 0, 1, @Bo, @Sau, @Giay) WITH NOWAIT;
END;

-- Hoàn tác (xem trước, không còn trùng) cũng hủy luôn bảng tạm tạo trong giao dịch
DROP TABLE IF EXISTS #Gop106;
DROP TABLE IF EXISTS #Giu;
DROP TABLE IF EXISTS #GiuId;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-don-trung-TmsTrafficData.sql';
PRINT N'=============================================================================';
GO
