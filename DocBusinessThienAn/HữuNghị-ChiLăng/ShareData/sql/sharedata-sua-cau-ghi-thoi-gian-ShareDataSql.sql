-- =========================================================================================
-- FILE: sharedata-sua-cau-ghi-thoi-gian-ShareDataSql.sql
-- BẢNG GHI: [dbo].[ShareDataSql] — chỉ SỬA cột WriteSql (kèm UpdateTime) của dòng câu mẫu chung gói 104, 106; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Sửa câu ghi chiều nhận của gói 104 (TmsWeather) và 106 (TmsTrafficData) trên CSDL đã nạp seed 2 trước 13/11/2026.
--              Câu cũ so cột thời gian kiểu datetime với datetime2 đọc từ JSON: mili-giây lẻ (vd .613) không bao giờ khớp, nên mỗi lần
--              cùng bản ghi tới lại (gửi lại, nhiều đối tác) MERGE chèn thêm một dòng thay vì cập nhật dòng cũ.
--              Tệp đổi thời gian về datetime trong USING (một dòng SELECT, một dòng GROUP BY mỗi gói), đúng như seed 2 bản 13/11/2026.
--              Chỉ sửa câu mẫu chung (PartnerId trống) còn nguyên văn đoạn lỗi; câu đã sửa tay khác đi thì giữ nguyên, liệt kê để xem tay.
--              Không đụng câu trích (ExtractSql) nên công thức cột mốc và mốc gửi của đối tác không đổi.
--              Không dọn dòng đã trùng ở CSDL nhận.
-- THỨ TỰ CHẠY: Chạy bất cứ lúc nào sau seed 2 (sharedata-seed-goi-tin-ShareDataSql.sql); Worker dùng câu mới từ lượt nhận kế tiếp.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính đã nạp seed 2 trước 13/11/2026 (chọn đúng CSDL trước khi chạy). CSDL mới nạp seed 2 bản mới thì không cần.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): câu đã sửa không còn đoạn lỗi nên không bị sửa lần nữa. Muốn xem trước: đặt @ChiXem = 1.
-- CẬP NHẬT LẦN CUỐI: 13/11/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-sua-cau-ghi-thoi-gian-ShareDataSql.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataSql]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Thiếu bảng ShareDataSql / ShareDataPacket trên DB ' + DB_NAME() + N'!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @ChiXem BIT = 0;                     -- 1 = chỉ liệt kê, không ghi

-- Hiện trạng câu ghi mẫu chung của gói 104, 106 trước khi sửa
SELECT p.[Code] AS [MaGoi],
       CASE
           WHEN CHARINDEX(N'CONVERT(datetime, detectTime)', s.[WriteSql]) > 0 THEN N'Đã sửa'
           WHEN (p.[Code] = N'104' OR p.[Code] LIKE N'104[_]%')
                AND CHARINDEX(N'SELECT weatherStationId, detectTime,', s.[WriteSql]) > 0
                AND CHARINDEX(N'GROUP BY weatherStationId, detectTime', s.[WriteSql]) > 0 THEN N'Bản lỗi, sẽ sửa'
           WHEN (p.[Code] = N'106' OR p.[Code] LIKE N'106[_]%')
                AND CHARINDEX(N'SELECT detectTime, lane, locationCode,', s.[WriteSql]) > 0
                AND CHARINDEX(N'GROUP BY detectTime, lane, locationCode', s.[WriteSql]) > 0 THEN N'Bản lỗi, sẽ sửa'
           ELSE N'Khác bản seed, xem tay'
       END AS [TinhTrang],
       s.[ID]
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p
    ON p.[ID] = s.[PacketId]
   AND p.[IsDelete] IS NULL
WHERE s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND NULLIF(LTRIM(RTRIM(s.[WriteSql])), N'') IS NOT NULL
  AND (p.[Code] IN (N'104', N'106') OR p.[Code] LIKE N'104[_]%' OR p.[Code] LIKE N'106[_]%')
ORDER BY p.[Code];

DECLARE @Sua104 INT = 0;
DECLARE @Sua106 INT = 0;

IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ liệt kê, không ghi dữ liệu.';
ELSE
BEGIN
    -- Gói 104: thời gian trạm thời tiết là khóa so khớp TmsWeather.TimeDetect (datetime)
    UPDATE s
    SET s.[WriteSql] = REPLACE(REPLACE(s.[WriteSql],
            N'SELECT weatherStationId, detectTime,', N'SELECT weatherStationId, CONVERT(datetime, detectTime) AS detectTime,'),
            N'GROUP BY weatherStationId, detectTime', N'GROUP BY weatherStationId, CONVERT(datetime, detectTime)'),
        s.[UpdateTime] = GETDATE()
    FROM [dbo].[ShareDataSql] s
    JOIN [dbo].[ShareDataPacket] p
        ON p.[ID] = s.[PacketId]
       AND p.[IsDelete] IS NULL
    WHERE (p.[Code] = N'104' OR p.[Code] LIKE N'104[_]%')
      AND s.[PartnerId] IS NULL
      AND s.[IsDelete] IS NULL
      AND CHARINDEX(N'SELECT weatherStationId, detectTime,', s.[WriteSql]) > 0
      AND CHARINDEX(N'GROUP BY weatherStationId, detectTime', s.[WriteSql]) > 0;
    SET @Sua104 = @@ROWCOUNT;

    -- Gói 106: thời gian phát hiện là khóa so khớp TmsTrafficData.DetectTime (datetime)
    UPDATE s
    SET s.[WriteSql] = REPLACE(REPLACE(s.[WriteSql],
            N'SELECT detectTime, lane, locationCode,', N'SELECT CONVERT(datetime, detectTime) AS detectTime, lane, locationCode,'),
            N'GROUP BY detectTime, lane, locationCode', N'GROUP BY CONVERT(datetime, detectTime), lane, locationCode'),
        s.[UpdateTime] = GETDATE()
    FROM [dbo].[ShareDataSql] s
    JOIN [dbo].[ShareDataPacket] p
        ON p.[ID] = s.[PacketId]
       AND p.[IsDelete] IS NULL
    WHERE (p.[Code] = N'106' OR p.[Code] LIKE N'106[_]%')
      AND s.[PartnerId] IS NULL
      AND s.[IsDelete] IS NULL
      AND CHARINDEX(N'SELECT detectTime, lane, locationCode,', s.[WriteSql]) > 0
      AND CHARINDEX(N'GROUP BY detectTime, lane, locationCode', s.[WriteSql]) > 0;
    SET @Sua106 = @@ROWCOUNT;

    PRINT N'✅ Đã sửa câu ghi mẫu chung: gói 104 ' + CAST(@Sua104 AS NVARCHAR(12)) + N' dòng, gói 106 ' + CAST(@Sua106 AS NVARCHAR(12)) + N' dòng.';
END;
GO

SET NOEXEC OFF;
GO
