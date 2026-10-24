USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-goi-tin-ShareDataPacket.sql
-- BẢNG GHI: [dbo].[ShareDataPacket] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Khởi tạo và chuẩn hóa bản ghi gói tin Catalog (101 - 111) trong ShareDataPacket:
--              0.1 Thêm các gói còn thiếu (chống trùng theo Code + IsDelete IS NULL, ID sinh động).
--              0.2 Chuẩn hóa OrderNo về đúng 101 - 111 cho các gói khớp mã chuẩn.
--              1. Nạp TemplateSql (câu mẫu gốc) khi đang trống hoặc @GhiDeMauGoiTin = 1.
--              3. Chuẩn hóa Status (bật 110*, tắt 111*).
--              5. Đổi khóa con trỏ __rowid -> __lastKey trong TemplateSql.
-- THỨ TỰ CHẠY: 1 / 3 (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-goi-tin-ShareDataPacket.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPacket] chưa tồn tại (chưa chạy CodeFirst)! Vui lòng chạy CodeFirst trước khi nạp dữ liệu gói tin.';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- 0. CẤU HÌNH GHI ĐÈ TEMPLATE
-- -----------------------------------------------------------------------------------------
-- @GhiDeMauGoiTin = 0 (Mặc định): Chỉ nạp TemplateSql nếu đang NULL hoặc rỗng.
-- @GhiDeMauGoiTin = 1: Buộc ghi đè TemplateSql về mẫu chuẩn Catalog (cách duy nhất đổi TemplateSql sau khi tạo).
-- Câu SQL đang chạy (ShareDataSql) có cờ riêng cùng tên trong sharedata-seed-goi-tin-ShareDataSql.sql.
DECLARE @GhiDeMauGoiTin BIT = 0;

IF @GhiDeMauGoiTin = 1
    PRINT N'⚠️ CHẾ ĐỘ: GHI ĐÈ TOÀN BỘ (@GhiDeMauGoiTin = 1) - Các câu lệnh trích xuất và TemplateSql sẽ được đặt lại theo Catalog!';
ELSE
    PRINT N'ℹ️ CHẾ ĐỘ: AN TOÀN (@GhiDeMauGoiTin = 0) - Chỉ nạp mẫu cho gói chưa có TemplateSql/ExtractSql, giữ nguyên cấu hình đã sửa.';
GO

-- -----------------------------------------------------------------------------------------
-- 0.1 KHỞI TẠO BẢN GHI CHO CÁC GÓI TIN CHƯA TỒN TẠI (IDEMPOTENT)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 0.1. Kiểm tra và khởi tạo các gói tin còn thiếu trong bảng ShareDataPacket...';

DECLARE @InsertedPackets INT = 0;
DECLARE @ExistingPackets INT = 0;

-- Gói 101: CommonData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '101' OR [Code] LIKE '101[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'101_commonData', N'Dữ liệu thông tin chung / luồng giao thông', N'1.0', N'Gói 101: Dữ liệu thông tin chung / luồng giao thông', 101, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 102: CctvData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '102' OR [Code] LIKE '102[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'102_cctvData', N'Dữ liệu hình ảnh giao thông CCTV', N'1.0', N'Gói 102: Dữ liệu hình ảnh giao thông CCTV', 102, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 103: VdsData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '103' OR [Code] LIKE '103[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'103_vdsData', N'Dữ liệu đếm xe VDS', N'1.0', N'Gói 103: Dữ liệu đếm xe VDS', 103, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 104: WeatherData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '104' OR [Code] LIKE '104[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'104_weatherData', N'Dữ liệu khí tượng / thời tiết', N'1.0', N'Gói 104: Dữ liệu khí tượng / thời tiết', 104, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 105: RfidData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '105' OR [Code] LIKE '105[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'105_rfidData', N'Dữ liệu định danh phương tiện AVI / RFID', N'1.0', N'Gói 105: Dữ liệu định danh phương tiện AVI / RFID', 105, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 106: WimData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '106' OR [Code] LIKE '106[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'106_wimData', N'Dữ liệu cân tải trọng xe tự động WIM', N'1.0', N'Gói 106: Dữ liệu cân tải trọng xe tự động WIM', 106, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 107: IncidentData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '107' OR [Code] LIKE '107[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'107_incidentData', N'Dữ liệu thông tin sự cố / sự kiện giao thông', N'1.0', N'Gói 107: Dữ liệu thông tin sự cố / sự kiện giao thông', 107, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 108: VmsInfo
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '108' OR [Code] LIKE '108[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'108_vmsInfo', N'Dữ liệu thông tin biển báo điện tử VMS', N'1.0', N'Gói 108: Dữ liệu thông tin biển báo điện tử VMS', 108, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 109: EtcData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '109' OR [Code] LIKE '109[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'109_etcData', N'Dữ liệu giao dịch thu phí ETC', N'1.0', N'Gói 109: Dữ liệu giao dịch thu phí ETC', 109, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 110: WpData
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '110' OR [Code] LIKE '110[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'110_wpData', N'Dữ liệu trao đổi thông tin với người tham gia giao thông', N'1.0', N'Gói 110: Dữ liệu trao đổi thông tin với người tham gia giao thông', 110, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

-- Gói 111: InterCenter (Status = 0: Disable)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE ([Code] = '111' OR [Code] LIKE '111[_]%') AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [CreateTime], [IsDelete])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), N'111', N'Dữ liệu trao đổi thông tin giữa các TT QLĐHGT tuyến', N'1.0', N'Gói 111: Dữ liệu trao đổi thông tin giữa các TT QLĐHGT tuyến', 111, 0, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData (tạm thời chưa kích hoạt)', GETDATE(), 0);
    SET @InsertedPackets = @InsertedPackets + 1;
END
ELSE
BEGIN
    SET @ExistingPackets = @ExistingPackets + 1;
END

PRINT N'   -> Đã khởi tạo mới: ' + CAST(@InsertedPackets AS NVARCHAR(10)) + N' gói, đã có sẵn: ' + CAST(@ExistingPackets AS NVARCHAR(10)) + N' gói.';
GO

-- -----------------------------------------------------------------------------------------
-- 0.2 CHUẨN HÓA OrderNo CHO CÁC GÓI TIN CHUẨN CATALOG (101 - 111)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 0.2. Chuẩn hóa OrderNo về mã chuẩn 101 - 111 cho các gói khớp mã...';

UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 101 WHERE ([Code] = '101' OR [Code] LIKE '101[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 101);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 102 WHERE ([Code] = '102' OR [Code] LIKE '102[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 102);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 103 WHERE ([Code] = '103' OR [Code] LIKE '103[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 103);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 104 WHERE ([Code] = '104' OR [Code] LIKE '104[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 104);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 105 WHERE ([Code] = '105' OR [Code] LIKE '105[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 105);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 106 WHERE ([Code] = '106' OR [Code] LIKE '106[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 106);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 107 WHERE ([Code] = '107' OR [Code] LIKE '107[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 107);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 108 WHERE ([Code] = '108' OR [Code] LIKE '108[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 108);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 109 WHERE ([Code] = '109' OR [Code] LIKE '109[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 109);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 110 WHERE ([Code] = '110' OR [Code] LIKE '110[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 110);
UPDATE [dbo].[ShareDataPacket] SET [OrderNo] = 111 WHERE ([Code] = '111' OR [Code] LIKE '111[_]%') AND [IsDelete] IS NULL AND ([OrderNo] IS NULL OR [OrderNo] <> 111);
GO

-- -----------------------------------------------------------------------------------------
-- 1. NẠP CÂU MẪU GỐC (TemplateSql) CHO GÓI 101 - 111
-- -----------------------------------------------------------------------------------------
DECLARE @GhiDeMauGoiTin BIT = 0;
DECLARE @UpdatedRows INT = 0;
DECLARE @TotalUpdated INT = 0;

PRINT N'-> 1. Bắt đầu nạp TemplateSql vào bảng ShareDataPacket (101 - 111)...';

-- Gói 101: TrafficFlow (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT
    zs.ID AS zoneStatusId,
    zs.ZoneId AS zoneId,
    z.Name AS zoneName,
    z.FromKmNumber AS fromLocationKm,
    z.FromMetNumber AS fromLocationMet,
    z.ToKmNumber AS toLocationKm,
    z.ToMetNumber AS toLocationMet,
    z.LaneId AS laneId,
    CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed,
    zs.Condition AS trafficCondition,
    zs.UpdateTime AS dataTime,
    z.MaxSpeed AS speedLimit,
    ts.TotalVehicleNumber AS vehicleCount
FROM TmsZoneStatus zs
LEFT JOIN TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId'
WHERE ([Code] = '101' OR [Code] LIKE '101[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 101 (TrafficFlow): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 102: CctvImage (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT
    e.Code AS cameraCode,
    c.Name AS cameraName,
    NULL AS snapshot,
    c.SnapshotTime AS snapshotTime,
    c.DeviceState AS deviceState,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction
FROM CctvDevice c
LEFT JOIN TmsEquipment e ON c.Ip = e.Ip'
WHERE ([Code] = '102' OR [Code] LIKE '102[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 102 (CctvImage): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 103: VehicleDetection (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT TOP (@pageSize)
    td.ID AS detectionId,
    td.DetectTime AS detectTime,
    td.Type AS vehicleType,
    td.LicensePlate AS licensePlate,
    td.Speed AS speed,
    td.Lane AS lane,
    td.Direction AS direction,
    td.Location AS locationRoute,
    td.EquipmentId AS equipmentId,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) AS __lastTime,
    td.ID AS __lastKey,
    td.UpdateTime AS __auditUpdate,
    td.CreateTime AS __auditCreate
FROM TmsTrafficData td
LEFT JOIN TmsEquipment e ON td.EquipmentId = e.ID
WHERE td.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) > @lastTime
      OR (COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) = @lastTime AND td.ID > @lastKey)
  )
ORDER BY COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) ASC, td.ID ASC
OPTION (RECOMPILE)'
WHERE ([Code] = '103' OR [Code] LIKE '103[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 103 (VehicleDetection): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 104: Weather (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT TOP (@pageSize)
    w.RefId AS weatherStationId,
    w.LocationDetail AS locationDetail,
    w.Temperature AS temperature,
    w.Hudmidity AS humidity,
    w.WindSpeed AS windSpeed,
    w.WindDirection AS windDirection,
    w.Rain AS rainfall,
    w.RainHour AS rainfallHour,
    w.Foresight AS visibility,
    w.Description AS weatherDescription,
    w.ShortDescription AS weatherCode,
    w.TimeDetect AS detectTime,
    COALESCE(w.UpdateTime, w.CreateTime, w.TimeDetect) AS __lastTime,
    w.ID AS __lastKey,
    w.UpdateTime AS __auditUpdate,
    w.CreateTime AS __auditCreate
FROM TmsWeather w
WHERE w.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(w.UpdateTime, w.CreateTime, w.TimeDetect) > @lastTime
      OR (COALESCE(w.UpdateTime, w.CreateTime, w.TimeDetect) = @lastTime AND w.ID > @lastKey)
  )
ORDER BY COALESCE(w.UpdateTime, w.CreateTime, w.TimeDetect) ASC, w.ID ASC
OPTION (RECOMPILE)'
WHERE ([Code] = '104' OR [Code] LIKE '104[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 104 (Weather): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 105: VehicleIdentification (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT
    i.TransactionId AS transactionId,
    i.TagId AS tagId,
    NULLIF(ISNULL(NULLIF(i.PlateEdit, ''''), NULLIF(i.PlateLpr, '''')), '''') AS licensePlate,
    i.VehicleTypeId AS vehicleTypeId,
    i.TransactionDateTime AS entryTime,
    CAST(NULL AS DATETIME) AS exitTime,
    i.LaneId AS laneId,
    i.StationId AS stationId,
    vr.Brand AS vehicleBrand,
    vr.Owner AS vehicleOwner
FROM TollTransactionIn i
LEFT JOIN TmsVehicleRegistration vr
       ON NULLIF(ISNULL(NULLIF(i.PlateEdit, ''''), NULLIF(i.PlateLpr, '''')), '''') = vr.LicensePlate
      AND vr.IsDelete IS NULL
WHERE i.IsDelete IS NULL
ORDER BY i.TransactionDateTime DESC'
WHERE ([Code] = '105' OR [Code] LIKE '105[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 105 (VehicleIdentification): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 106: WeighInMotion (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT TOP (@pageSize)
    td.DetectTime AS detectTime,
    td.Lane AS lane,
    td.Location AS locationCode,
    td.Speed AS speed,
    td.Height AS height,
    td.Width AS width,
    td.Length AS length,
    COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) AS __lastTime,
    td.ID AS __lastKey,
    td.UpdateTime AS __auditUpdate,
    td.CreateTime AS __auditCreate
FROM TmsTrafficData td
WHERE td.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) > @lastTime
      OR (COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) = @lastTime AND td.ID > @lastKey)
  )
ORDER BY COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) ASC, td.ID ASC
OPTION (RECOMPILE)'
WHERE ([Code] = '106' OR [Code] LIKE '106[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 106 (WeighInMotion): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 107: TrafficIncident (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT TOP (@pageSize)
    i.Code AS incidentCode,
    i.Name AS incidentName,
    i.EventTypeId AS eventTypeId,
    et.Name AS eventTypeName,
    i.StartDate AS occurredTime,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.Location AS locationRoute,
    i.InfluenceScope AS direction,
    i.InjuredNumber AS injuredCount,
    i.VehicleNumber AS vehicleCount,
    i.State AS incidentState,
    i.Description AS description,
    i.Source AS source,
    COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) AS __lastTime,
    i.ID AS __lastKey,
    i.UpdateTime AS __auditUpdate,
    i.CreateTime AS __auditCreate
FROM TmsIncident i
LEFT JOIN TmsEventType et ON i.EventTypeId = et.ID
WHERE i.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) > @lastTime
      OR (COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) = @lastTime AND i.ID > @lastKey)
  )
ORDER BY COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) ASC, i.ID ASC
OPTION (RECOMPILE)'
WHERE ([Code] = '107' OR [Code] LIKE '107[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 107 (TrafficIncident): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 108: VmsDisplay (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT
    e.Code AS equipmentCode,
    v.Name AS vmsName,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction,
    e.LaneId AS laneId,
    v.RowData AS displayContent,
    v.Url AS displayImageUrl,
    v.Size AS displaySize,
    v.Priority AS priority,
    v.ExecutedDate AS executedTime
FROM VmsCurrent v
LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID'
WHERE ([Code] = '108' OR [Code] LIKE '108[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 108 (VmsDisplay): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 109: TollCollection (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT TOP (@pageSize)
    t.TransactionId AS transactionId,
    t.TransactionDateTimeIn AS entryTime,
    t.TransactionDateTime AS exitTime,
    t.VehicleTypeId AS vehicleTypeId,
    ISNULL(t.PlateEdit, t.PlateLpr) AS licensePlate,
    t.TagId AS tagId,
    t.LaneId AS laneId,
    l.Name AS laneName,
    t.StationId AS stationId,
    s.Name AS stationName,
    CAST(NULL AS DECIMAL(18, 2)) AS tollPrice,
    t.SyncTime AS syncTime,
    COALESCE(t.UpdateTime, t.CreateTime, t.TransactionDateTime) AS __lastTime,
    t.ID AS __lastKey,
    t.UpdateTime AS __auditUpdate,
    t.CreateTime AS __auditCreate
FROM TollTransactionOut t
LEFT JOIN TollLane l ON t.LaneId = l.LaneId
LEFT JOIN TollStation s ON t.StationId = s.StationId
WHERE t.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(t.UpdateTime, t.CreateTime, t.TransactionDateTime) > @lastTime
      OR (COALESCE(t.UpdateTime, t.CreateTime, t.TransactionDateTime) = @lastTime AND t.ID > @lastKey)
  )
ORDER BY COALESCE(t.UpdateTime, t.CreateTime, t.TransactionDateTime) ASC, t.ID ASC
OPTION (RECOMPILE)'
WHERE ([Code] = '109' OR [Code] LIKE '109[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 109 (TollCollection): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 110: PublicMessaging (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT
    CAST(i.ID AS NVARCHAR(64)) AS messageId,
    CONCAT(ISNULL(i.Name, ''''), '' - '', ISNULL(i.Description, '''')) AS incidentMessage,
    w.Description AS weatherMessage,
    v.RowData AS guidanceContent,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.StartDate AS publishedTime,
    CAST(NULL AS NVARCHAR(64)) AS channel,
    CAST(NULL AS NVARCHAR(64)) AS deliveryState
FROM TmsIncident i
OUTER APPLY (
    SELECT TOP 1 e.ID
    FROM TmsEquipment e
    WHERE e.KmNumber = i.KmNumber
    ORDER BY e.ID DESC
) eq
OUTER APPLY (
    SELECT TOP 1 v.RowData
    FROM VmsCurrent v
    WHERE v.EquipmentId = eq.ID
      AND v.RowData IS NOT NULL
    ORDER BY v.ExecutedDate DESC
) v
OUTER APPLY (
    SELECT TOP 1 w.Description
    FROM TmsWeather w
    WHERE w.Description IS NOT NULL
    ORDER BY COALESCE(w.TimeDetect, w.CreateTime) DESC
) w
WHERE (i.State IS NULL OR (
    i.State != ''FINISHED'' AND i.State != ''CANCELED''
    AND i.State != ''Closed'' AND i.State != ''Cancelled''
))'
WHERE ([Code] = '110' OR [Code] LIKE '110[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 110 (PublicMessaging): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 111: InterCenter (Incremental, Status = 0: Disable)
UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = N'SELECT TOP (@pageSize)
    CAST(NULL AS NVARCHAR(16)) AS packetType,
    CAST(NULL AS NVARCHAR(MAX)) AS payload,
    GETDATE() AS createdTime,
    sl.NewData AS controlCommand,
    sl.State AS controlState,
    CAST(NULL AS NVARCHAR(64)) AS connectionState,
    CAST(NULL AS DATETIME) AS ackTime,
    CAST(NULL AS INT) AS retryCount,
    CAST(NULL AS NVARCHAR(512)) AS error,
    CAST(NULL AS NVARCHAR(64)) AS partnerId,
    COALESCE(sl.UpdateTime, sl.CreateTime) AS __lastTime,
    sl.ID AS __lastKey,
    sl.UpdateTime AS __auditUpdate,
    sl.CreateTime AS __auditCreate
FROM TmsSignalLog sl
WHERE sl.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(sl.UpdateTime, sl.CreateTime) > @lastTime
      OR (COALESCE(sl.UpdateTime, sl.CreateTime) = @lastTime AND sl.ID > @lastKey)
  )
ORDER BY COALESCE(sl.UpdateTime, sl.CreateTime) ASC, sl.ID ASC
OPTION (RECOMPILE)'
WHERE ([Code] = '111' OR [Code] LIKE '111[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [TemplateSql] IS NULL OR LTRIM(RTRIM([TemplateSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 111 (InterCenter): đã cập nhật TemplateSql ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

PRINT N'   -> Tổng số bản ghi TemplateSql đã cập nhật: ' + CAST(@TotalUpdated AS NVARCHAR(10)) + N' dòng.';

-- -----------------------------------------------------------------------------------------
-- 3. CHUẨN HÓA TRẠNG THÁI GÓI (Status = 1 CHO 110*, Status = 0 CHO 111*)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 3. Chuẩn hóa trạng thái gói tin (Bật 110*, Tắt 111*)...';

-- Bật gói 110* (Enable = 1) khi đang NULL hoặc 0 do cấu hình cũ
UPDATE [dbo].[ShareDataPacket]
SET [Status] = 1
WHERE ([Status] IS NULL OR [Status] = 0)
  AND ([Code] = '110' OR [Code] LIKE '110[_]%');
PRINT N'   - Cập nhật Status = 1 (Enable) cho các gói 110*: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

-- Giữ gói 111* (Disable = 0) do chưa hoàn thiện
UPDATE [dbo].[ShareDataPacket]
SET [Status] = 0
WHERE ([Status] IS NULL OR [Status] <> 0)
  AND ([Code] = '111' OR [Code] LIKE '111[_]%');
PRINT N'   - Cập nhật Status = 0 (Disable) cho các gói 111*: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

-- -----------------------------------------------------------------------------------------
-- 5. CHUYỂN ĐỔI KHÓA CON TRỎ __rowid -> __lastKey TRONG TemplateSql (IDEMPOTENT)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 5. Đồng bộ chuyển đổi cột con trỏ __rowid -> __lastKey trong TemplateSql...';

UPDATE [dbo].[ShareDataPacket]
SET [TemplateSql] = REPLACE([TemplateSql], '__rowid', '__lastKey')
WHERE [IsDelete] IS NULL
  AND [TemplateSql] LIKE '%[_][_]rowid%';
PRINT N'   -> Đã chuyển đổi __rowid -> __lastKey: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' câu mẫu gốc.';

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-goi-tin-ShareDataPacket.sql';
PRINT N'=============================================================================';
GO
