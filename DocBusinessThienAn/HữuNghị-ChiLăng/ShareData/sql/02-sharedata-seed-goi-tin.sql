USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 02-sharedata-seed-goi-tin.sql
-- DESCRIPTION: Nạp Template câu truy vấn SQL trích xuất chuẩn Catalog (gói 101-111),
--              chuẩn hóa chế độ gửi (SyncMode), số thứ tự (OrderNo) và trạng thái gói (Status) trong ShareDataPacket:
--              0. Khởi tạo bản ghi cho 11 gói tin (101 - 111) nếu chưa tồn tại (idempotent,
--                 chống trùng theo Code + IsDelete IS NULL, ID sinh động bằng LOWER(CONVERT(NVARCHAR(64), NEWID()))).
--              0.2 Chuẩn hóa OrderNo về đúng 101 - 111 cho các gói khớp mã chuẩn.
--              1. Nạp câu truy vấn SQL trích xuất mẫu chuẩn cho các gói tin 101 - 111:
--                 - TemplateSql được cập nhật bằng câu truy vấn mẫu gốc chuẩn Catalog khi đang trống hoặc @GhiDeMauGoiTin = 1 (cách duy nhất làm mới TemplateSql sau khi tạo).
--                 - ExtractSql = TemplateSql chỉ khi ExtractSql đang trống hoặc @GhiDeMauGoiTin = 1.
--              2. Chuẩn hóa SyncMode mặc định theo nghiệp vụ cho các gói còn NULL:
--                 - Snapshot (1): 101, 102, 105, 108, 110
--                 - Incremental (2): 103, 104, 106, 107, 109, 111
--              3. Cập nhật Status = 1 (Enable) cho các gói 110* và Status = 0 (Disable) cho các gói 111*.
--              4. Nạp câu ghi chiều nhận chuẩn Catalog (ShareDataPacketSql) cho các gói đã có câu ghi (101-109,
--                 chống trùng theo PacketCode + TableName kèm IsDelete IS NULL, ID sinh động bằng LOWER(CONVERT(NVARCHAR(64), NEWID()))).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent, chống trùng theo khóa nghiệp vụ, không ghi đè dữ liệu quản trị đã sửa).
-- CẬP NHẬT LẦN CUỐI: 20/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN SEED GÓI TIN SHAREDATA (02-sharedata-seed-goi-tin.sql)';
PRINT N'=============================================================================';

IF OBJECT_ID('dbo.ShareDataPacket', 'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPacket] chưa tồn tại (chưa chạy CodeFirst)! Vui lòng chạy CodeFirst trước khi nạp dữ liệu gói tin.';
    RETURN;
END;
GO

-- -----------------------------------------------------------------------------------------
-- 0. CẤU HÌNH GHI ĐÈ TEMPLATE
-- -----------------------------------------------------------------------------------------
-- @GhiDeMauGoiTin = 0 (Mặc định): Chỉ nạp TemplateSql và ExtractSql nếu đang NULL hoặc rỗng.
-- @GhiDeMauGoiTin = 1: Buộc ghi đè toàn bộ câu truy vấn TemplateSql và ExtractSql về mẫu chuẩn Catalog (cách duy nhất đổi TemplateSql sau khi tạo).
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

DECLARE @GhiDeMauGoiTin BIT = 0;
DECLARE @UpdatedRows INT = 0;
DECLARE @TotalUpdated INT = 0;

PRINT N'-> 1. Bắt đầu nạp Template câu lệnh SQL trích xuất vào bảng ShareDataPacket (101 - 111)...';

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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '101' OR [Code] LIKE '101[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 101 (TrafficFlow): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '102' OR [Code] LIKE '102[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 102 (CctvImage): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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
    td.ID AS __rowid,
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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '103' OR [Code] LIKE '103[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 103 (VehicleDetection): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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
    w.ID AS __rowid,
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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '104' OR [Code] LIKE '104[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 104 (Weather): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '105' OR [Code] LIKE '105[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 105 (VehicleIdentification): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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
    td.ID AS __rowid,
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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '106' OR [Code] LIKE '106[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 106 (WeighInMotion): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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
    i.ID AS __rowid,
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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '107' OR [Code] LIKE '107[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 107 (TrafficIncident): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '108' OR [Code] LIKE '108[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 108 (VmsDisplay): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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
    t.ID AS __rowid,
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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '109' OR [Code] LIKE '109[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 109 (TollCollection): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '110' OR [Code] LIKE '110[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 110 (PublicMessaging): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

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
    sl.ID AS __rowid,
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

UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = [TemplateSql],
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1,
    [Status] = 0
WHERE ([Code] = '111' OR [Code] LIKE '111[_]%')
  AND [IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 111 (InterCenter): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

PRINT N'   -> Tổng số bản ghi template SQL đã cập nhật: ' + CAST(@TotalUpdated AS NVARCHAR(10)) + N' dòng.';

-- -----------------------------------------------------------------------------------------
-- 2. CHUẨN HÓA SyncMode MẶC ĐỊNH CHO CÁC GÓI CÒN NULL
-- -----------------------------------------------------------------------------------------
PRINT N'-> 2. Chuẩn hóa SyncMode mặc định khi còn NULL...';

-- Gói Snapshot (101, 102, 105, 108, 110)
UPDATE [dbo].[ShareDataPacket]
SET [SyncMode] = 1
WHERE [SyncMode] IS NULL
  AND ([Code] IN ('101', '102', '105', '108', '110')
       OR [Code] LIKE '101[_]%'
       OR [Code] LIKE '102[_]%'
       OR [Code] LIKE '105[_]%'
       OR [Code] LIKE '108[_]%'
       OR [Code] LIKE '110[_]%');
PRINT N'   - Cập nhật SyncMode = 1 (Snapshot) cho các gói 101, 102, 105, 108, 110 còn NULL: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

-- Gói Incremental (103, 104, 106, 107, 109, 111)
UPDATE [dbo].[ShareDataPacket]
SET [SyncMode] = 2
WHERE [SyncMode] IS NULL
  AND ([Code] IN ('103', '104', '106', '107', '109', '111')
       OR [Code] LIKE '103[_]%'
       OR [Code] LIKE '104[_]%'
       OR [Code] LIKE '106[_]%'
       OR [Code] LIKE '107[_]%'
       OR [Code] LIKE '109[_]%'
       OR [Code] LIKE '111[_]%');
PRINT N'   - Cập nhật SyncMode = 2 (Incremental) cho các gói 103, 104, 106, 107, 109, 111 còn NULL: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

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
-- 4. NẠP CÂU GHI CHIỀU NHẬN (ShareDataPacketSql)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 4. Nạp câu ghi chiều nhận chuẩn Catalog vào ShareDataPacketSql...';

IF OBJECT_ID('dbo.ShareDataPacketSql', 'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPacketSql] chưa tồn tại (chưa chạy CodeFirst)! Bỏ qua nạp câu ghi chiều nhận.';
    RETURN;
END;

DECLARE @TenantIdPacketSql NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdPacketSql IS NULL
    SET @TenantIdPacketSql = N'Default';

DECLARE @TotalWriteSqlInserted INT = 0;
DECLARE @InsertedWriteSql INT = 0;

-- 4.1 Gói 101: CommonData (3 câu ghi)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '101_commonData'
      AND [TableName] = 'dbo.TmsZone'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '101_commonData', 'dbo.TmsZone', 1, N'
MERGE dbo.TmsZone WITH (HOLDLOCK) AS t
USING (
    SELECT  zoneId,
            MAX(zoneName)        AS zoneName,
            MAX(fromLocationKm)  AS fromLocationKm,
            MAX(fromLocationMet) AS fromLocationMet,
            MAX(toLocationKm)    AS toLocationKm,
            MAX(toLocationMet)   AS toLocationMet,
            MAX(laneId)          AS laneId,
            MAX(speedLimit)      AS speedLimit
    FROM OPENJSON(@records) WITH (
            zoneId          nvarchar(64),
            zoneName        nvarchar(64),
            fromLocationKm  int,
            fromLocationMet int,
            toLocationKm    int,
            toLocationMet   int,
            laneId          nvarchar(32),
            speedLimit      float)
    WHERE zoneId IS NOT NULL
    GROUP BY zoneId
) AS s
   ON t.ID = s.zoneId
WHEN MATCHED THEN
    UPDATE SET t.Name          = ISNULL(s.zoneName, t.Name),
               t.FromKmNumber  = ISNULL(s.fromLocationKm, t.FromKmNumber),
               t.FromMetNumber = ISNULL(s.fromLocationMet, t.FromMetNumber),
               t.ToKmNumber    = ISNULL(s.toLocationKm, t.ToKmNumber),
               t.ToMetNumber   = ISNULL(s.toLocationMet, t.ToMetNumber),
               t.LaneId        = ISNULL(s.laneId, t.LaneId),
               t.MaxSpeed      = ISNULL(s.speedLimit, t.MaxSpeed),
               t.UpdateTime    = @now,
               t.UpdateUId     = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, Name, FromKmNumber, FromMetNumber, ToKmNumber, ToMetNumber, LaneId, MaxSpeed,
            TenantId, CreateTime, CreateUId)
    VALUES (s.zoneId, s.zoneName, s.fromLocationKm, s.fromLocationMet, s.toLocationKm, s.toLocationMet,
            s.laneId, s.speedLimit, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '101_commonData'
      AND [TableName] = 'dbo.TmsTrafficStatistic'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '101_commonData', 'dbo.TmsTrafficStatistic', 2, N'
MERGE dbo.TmsTrafficStatistic WITH (HOLDLOCK) AS t
USING (
    SELECT  zoneId,
            MAX(vehicleCount) AS vehicleCount
    FROM OPENJSON(@records) WITH (
            zoneId       nvarchar(64),
            vehicleCount int)
    WHERE zoneId IS NOT NULL AND vehicleCount IS NOT NULL
    GROUP BY zoneId
) AS s
   ON t.ZoneId = s.zoneId
WHEN MATCHED THEN
    UPDATE SET t.TotalVehicleNumber = s.vehicleCount,
               t.UpdateTime         = @now,
               t.UpdateUId          = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, ZoneId, TotalVehicleNumber, VendorEventId, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.zoneId, s.vehicleCount, 0, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '101_commonData'
      AND [TableName] = 'dbo.TmsZoneStatus'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '101_commonData', 'dbo.TmsZoneStatus', 3, N'
MERGE dbo.TmsZoneStatus WITH (HOLDLOCK) AS t
USING (
    SELECT  zoneStatusId,
            MAX(zoneId)           AS zoneId,
            MAX(averageSpeed)     AS averageSpeed,
            MAX(trafficCondition) AS trafficCondition,
            MAX(dataTime)         AS dataTime
    FROM OPENJSON(@records) WITH (
            zoneStatusId     nvarchar(64),
            zoneId           nvarchar(64),
            averageSpeed     decimal(18, 2),
            trafficCondition nvarchar(64),
            dataTime         datetime2)
    WHERE zoneStatusId IS NOT NULL
    GROUP BY zoneStatusId
) AS s
   ON t.ID = s.zoneStatusId
WHEN MATCHED THEN
    UPDATE SET t.ZoneId       = ISNULL(s.zoneId, t.ZoneId),
               t.AverageSpeed = ISNULL(CONVERT(nvarchar(16), s.averageSpeed), t.AverageSpeed),
               t.Condition    = ISNULL(s.trafficCondition, t.Condition),
               t.UpdateTime   = ISNULL(s.dataTime, @now),
               t.UpdateUId    = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, ZoneId, AverageSpeed, Condition, UpdateTime, TenantId, CreateTime, CreateUId)
    VALUES (s.zoneStatusId, s.zoneId, CONVERT(nvarchar(16), s.averageSpeed), s.trafficCondition,
            ISNULL(s.dataTime, @now), @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 101 (CommonData): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

-- 4.2 Gói 102: CCTV (2 câu ghi)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '102_cctvData'
      AND [TableName] = 'dbo.TmsEquipment'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '102_cctvData', 'dbo.TmsEquipment', 1, N'
MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
USING (
    SELECT cameraCode, MAX(cameraName) AS cameraName, MAX(locationKm) AS locationKm,
           MAX(locationMet) AS locationMet, MAX(direction) AS direction
    FROM OPENJSON(@records) WITH (
        cameraCode nvarchar(64), cameraName nvarchar(64),
        locationKm int, locationMet int, direction int)
    WHERE cameraCode IS NOT NULL
    GROUP BY cameraCode
) AS s
   ON t.Code = s.cameraCode
WHEN MATCHED THEN
    UPDATE SET t.KmNumber    = ISNULL(s.locationKm, t.KmNumber),
               t.MetNumber   = ISNULL(s.locationMet, t.MetNumber),
               t.DirectionId = ISNULL(s.direction, t.DirectionId),
               t.UpdateTime  = @now,
               t.UpdateUId   = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, Code, Name, KmNumber, MetNumber, DirectionId, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.cameraCode, s.cameraName, s.locationKm, s.locationMet,
            s.direction, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '102_cctvData'
      AND [TableName] = 'dbo.CctvDevice'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '102_cctvData', 'dbo.CctvDevice', 2, N'
MERGE dbo.CctvDevice WITH (HOLDLOCK) AS t
USING (
    SELECT cameraCode, MAX(cameraName) AS cameraName, MAX(snapshotTime) AS snapshotTime,
           MAX(deviceState) AS deviceState
    FROM OPENJSON(@records) WITH (
        cameraCode nvarchar(64), cameraName nvarchar(64),
        snapshotTime datetime2, deviceState int)
    WHERE cameraCode IS NOT NULL
    GROUP BY cameraCode
) AS s
   ON t.Code = s.cameraCode
WHEN MATCHED THEN
    UPDATE SET t.Name         = ISNULL(s.cameraName, t.Name),
               t.SnapshotTime = ISNULL(s.snapshotTime, t.SnapshotTime),
               t.DeviceState  = ISNULL(s.deviceState, t.DeviceState),
               t.UpdateTime   = @now,
               t.UpdateUId    = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, Code, DeviceId, Name, SnapshotTime, DeviceState, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.cameraCode, s.cameraCode, s.cameraName, s.snapshotTime,
            s.deviceState, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 102 (CctvData): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

-- 4.3 Gói 103: VDS (2 câu ghi)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '103_vdsData'
      AND [TableName] = 'dbo.TmsEquipment'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '103_vdsData', 'dbo.TmsEquipment', 1, N'
MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
USING (
    SELECT equipmentId, MAX(locationKm) AS locationKm, MAX(locationMet) AS locationMet
    FROM OPENJSON(@records) WITH (equipmentId nvarchar(64), locationKm int, locationMet int)
    WHERE equipmentId IS NOT NULL
    GROUP BY equipmentId
) AS s
   ON t.ID = s.equipmentId
WHEN MATCHED THEN
    UPDATE SET t.KmNumber   = ISNULL(s.locationKm, t.KmNumber),
               t.MetNumber  = ISNULL(s.locationMet, t.MetNumber),
               t.UpdateTime = @now,
               t.UpdateUId  = @operator
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '103_vdsData'
      AND [TableName] = 'dbo.TmsTrafficData'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '103_vdsData', 'dbo.TmsTrafficData', 2, N'
MERGE dbo.TmsTrafficData WITH (HOLDLOCK) AS t
USING (
    SELECT detectionId, MAX(detectTime) AS detectTime, MAX(vehicleType) AS vehicleType,
           MAX(licensePlate) AS licensePlate, MAX(speed) AS speed, MAX(lane) AS lane,
           MAX(direction) AS direction, MAX(locationRoute) AS locationRoute, MAX(equipmentId) AS equipmentId
    FROM OPENJSON(@records) WITH (
        detectionId nvarchar(64), detectTime datetime2, vehicleType nvarchar(32),
        licensePlate nvarchar(32), speed float, lane nvarchar(32),
        direction nvarchar(32), locationRoute nvarchar(32), equipmentId nvarchar(64))
    WHERE detectionId IS NOT NULL
    GROUP BY detectionId
) AS s
   ON t.ID = s.detectionId
WHEN MATCHED THEN
    UPDATE SET t.DetectTime   = ISNULL(s.detectTime, t.DetectTime),
               t.Type         = ISNULL(s.vehicleType, t.Type),
               t.LicensePlate = ISNULL(s.licensePlate, t.LicensePlate),
               t.Speed        = ISNULL(s.speed, t.Speed),
               t.Lane         = ISNULL(s.lane, t.Lane),
               t.Direction    = ISNULL(s.direction, t.Direction),
               t.Location     = ISNULL(s.locationRoute, t.Location),
               t.EquipmentId  = ISNULL(s.equipmentId, t.EquipmentId),
               t.UpdateTime   = @now,
               t.UpdateUId    = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, DetectTime, Type, LicensePlate, Speed, Lane, Direction, Location, EquipmentId,
            VendorEventId, TenantId, CreateTime, CreateUId)
    VALUES (s.detectionId, s.detectTime, s.vehicleType, s.licensePlate, s.speed, s.lane, s.direction,
            s.locationRoute, s.equipmentId, 0, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 103 (VdsData): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

-- 4.4 Gói 104: Weather (1 câu ghi)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '104_weatherData'
      AND [TableName] = 'dbo.TmsWeather'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '104_weatherData', 'dbo.TmsWeather', 1, N'
MERGE dbo.TmsWeather WITH (HOLDLOCK) AS t
USING (
    SELECT weatherStationId, detectTime,
           MAX(locationDetail) AS locationDetail, MAX(temperature) AS temperature, MAX(humidity) AS humidity,
           MAX(windSpeed) AS windSpeed, MAX(windDirection) AS windDirection, MAX(rainfall) AS rainfall,
           MAX(rainfallHour) AS rainfallHour, MAX(visibility) AS visibility,
           MAX(weatherDescription) AS weatherDescription, MAX(weatherCode) AS weatherCode
    FROM OPENJSON(@records) WITH (
        weatherStationId nvarchar(64), detectTime datetime2, locationDetail nvarchar(128),
        temperature float, humidity float, windSpeed float, windDirection nvarchar(32),
        rainfall float, rainfallHour float, visibility float,
        weatherDescription nvarchar(128), weatherCode nvarchar(32))
    WHERE weatherStationId IS NOT NULL AND detectTime IS NOT NULL
    GROUP BY weatherStationId, detectTime
) AS s
   ON t.RefId = s.weatherStationId AND t.TimeDetect = s.detectTime
WHEN MATCHED THEN
    UPDATE SET t.LocationDetail   = ISNULL(s.locationDetail, t.LocationDetail),
               t.Temperature      = ISNULL(s.temperature, t.Temperature),
               t.Hudmidity        = ISNULL(s.humidity, t.Hudmidity),
               t.WindSpeed        = ISNULL(s.windSpeed, t.WindSpeed),
               t.WindDirection    = ISNULL(s.windDirection, t.WindDirection),
               t.Rain             = ISNULL(s.rainfall, t.Rain),
               t.RainHour         = ISNULL(s.rainfallHour, t.RainHour),
               t.Foresight        = ISNULL(s.visibility, t.Foresight),
               t.Description      = ISNULL(s.weatherDescription, t.Description),
               t.ShortDescription = ISNULL(s.weatherCode, t.ShortDescription),
               t.UpdateTime       = @now,
               t.UpdateUId        = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, RefId, TimeDetect, LocationDetail, Temperature, Hudmidity, WindSpeed, WindDirection,
            Rain, RainHour, Foresight, Description, ShortDescription, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.weatherStationId, s.detectTime, s.locationDetail, s.temperature,
            s.humidity, s.windSpeed, s.windDirection, s.rainfall, s.rainfallHour, s.visibility,
            s.weatherDescription, s.weatherCode, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 104 (WeatherData): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

-- 4.5 Gói 105: RfidData (2 câu ghi - ánh xạ chuẩn từ danh mục 105_rfidData)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '105_rfidData'
      AND [TableName] = 'dbo.TmsVehicleRegistration'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '105_rfidData', 'dbo.TmsVehicleRegistration', 1, N'
MERGE dbo.TmsVehicleRegistration WITH (HOLDLOCK) AS t
USING (
    SELECT licensePlate, MAX(vehicleBrand) AS vehicleBrand, MAX(vehicleOwner) AS vehicleOwner
    FROM OPENJSON(@records) WITH (licensePlate nvarchar(32), vehicleBrand nvarchar(64), vehicleOwner nvarchar(64))
    WHERE licensePlate IS NOT NULL AND (vehicleBrand IS NOT NULL OR vehicleOwner IS NOT NULL)
    GROUP BY licensePlate
) AS s
   ON t.LicensePlate = s.licensePlate
WHEN MATCHED THEN
    UPDATE SET t.Brand      = ISNULL(s.vehicleBrand, t.Brand),
               t.Owner      = ISNULL(s.vehicleOwner, t.Owner),
               t.UpdateTime = @now,
               t.UpdateUId  = @operator
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '105_rfidData'
      AND [TableName] = 'dbo.TollTransactionOut'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '105_rfidData', 'dbo.TollTransactionOut', 2, N'
MERGE dbo.TollTransactionOut WITH (HOLDLOCK) AS t
USING (
    SELECT transactionId, MAX(tagId) AS tagId, MAX(licensePlate) AS licensePlate,
           MAX(vehicleTypeId) AS vehicleTypeId, MAX(entryTime) AS entryTime, MAX(exitTime) AS exitTime,
           MAX(laneId) AS laneId, MAX(stationId) AS stationId
    FROM OPENJSON(@records) WITH (
        transactionId nvarchar(128), tagId nvarchar(64), licensePlate nvarchar(32),
        vehicleTypeId nvarchar(64), entryTime datetime2, exitTime datetime2,
        laneId nvarchar(32), stationId nvarchar(32))
    WHERE transactionId IS NOT NULL
    GROUP BY transactionId
) AS s
   ON t.TransactionId = s.transactionId
WHEN MATCHED THEN
    UPDATE SET t.TagId                 = ISNULL(s.tagId, t.TagId),
               t.Plate                 = ISNULL(s.licensePlate, t.Plate),
               t.PlateEdit             = ISNULL(s.licensePlate, t.PlateEdit),
               t.VehicleTypeId         = ISNULL(s.vehicleTypeId, t.VehicleTypeId),
               t.TransactionDateTimeIn = ISNULL(s.entryTime, t.TransactionDateTimeIn),
               t.TransactionDateTime   = ISNULL(s.exitTime, t.TransactionDateTime),
               t.LaneId                = ISNULL(s.laneId, t.LaneId),
               t.StationId             = ISNULL(s.stationId, t.StationId),
               t.UpdateTime            = @now,
               t.UpdateUId             = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, TransactionId, TagId, Plate, PlateEdit, VehicleTypeId, TransactionDateTimeIn,
            TransactionDateTime, LaneId, StationId, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.transactionId, s.tagId, s.licensePlate, s.licensePlate,
            s.vehicleTypeId, s.entryTime, s.exitTime, s.laneId, s.stationId, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 105 (RfidData): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

-- 4.6 Gói 106: WimData (1 câu ghi)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '106_wimData'
      AND [TableName] = 'dbo.TmsTrafficData'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '106_wimData', 'dbo.TmsTrafficData', 1, N'
MERGE dbo.TmsTrafficData WITH (HOLDLOCK) AS t
USING (
    SELECT detectTime, lane, locationCode,
           MAX(speed) AS speed, MAX(height) AS height, MAX(width) AS width, MAX(length) AS length
    FROM OPENJSON(@records) WITH (
        detectTime datetime2, lane nvarchar(32), locationCode nvarchar(32),
        speed float, height int, width int, length int)
    WHERE detectTime IS NOT NULL
    GROUP BY detectTime, lane, locationCode
) AS s
   ON t.DetectTime = s.detectTime
  AND ISNULL(t.Lane, N'''') = ISNULL(s.lane, N'''')
  AND ISNULL(t.Location, N'''') = ISNULL(s.locationCode, N'''')
WHEN MATCHED THEN
    UPDATE SET t.Speed      = ISNULL(s.speed, t.Speed),
               t.Height     = ISNULL(s.height, t.Height),
               t.Width      = ISNULL(s.width, t.Width),
               t.Length     = ISNULL(s.length, t.Length),
               t.UpdateTime = @now,
               t.UpdateUId  = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, DetectTime, Lane, Location, Speed, Height, Width, Length,
            VendorEventId, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.detectTime, s.lane, s.locationCode, s.speed, s.height,
            s.width, s.length, 0, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 106 (WimData): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

-- 4.7 Gói 107: IncidentData (2 câu ghi)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '107_incidentData'
      AND [TableName] = 'dbo.TmsEventType'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '107_incidentData', 'dbo.TmsEventType', 1, N'
MERGE dbo.TmsEventType WITH (HOLDLOCK) AS t
USING (
    SELECT eventTypeId, MAX(eventTypeName) AS eventTypeName
    FROM OPENJSON(@records) WITH (eventTypeId nvarchar(64), eventTypeName nvarchar(64))
    WHERE eventTypeId IS NOT NULL AND eventTypeName IS NOT NULL
    GROUP BY eventTypeId
) AS s
   ON t.ID = s.eventTypeId
WHEN MATCHED THEN
    UPDATE SET t.Name       = s.eventTypeName,
               t.UpdateTime = @now,
               t.UpdateUId  = @operator
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '107_incidentData'
      AND [TableName] = 'dbo.TmsIncident'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '107_incidentData', 'dbo.TmsIncident', 2, N'
MERGE dbo.TmsIncident WITH (HOLDLOCK) AS t
USING (
    SELECT incidentCode, MAX(incidentName) AS incidentName, MAX(eventTypeId) AS eventTypeId,
           MAX(occurredTime) AS occurredTime, MAX(locationKm) AS locationKm, MAX(locationMet) AS locationMet,
           MAX(locationRoute) AS locationRoute, MAX(direction) AS direction,
           MAX(injuredCount) AS injuredCount, MAX(vehicleCount) AS vehicleCount,
           MAX(incidentState) AS incidentState, MAX(description) AS description, MAX(source) AS source
    FROM OPENJSON(@records) WITH (
        incidentCode nvarchar(64), incidentName nvarchar(128), eventTypeId nvarchar(64),
        occurredTime datetime2, locationKm int, locationMet int, locationRoute nvarchar(32),
        direction nvarchar(32), injuredCount int, vehicleCount int, incidentState nvarchar(32),
        description nvarchar(512), source nvarchar(32))
    WHERE incidentCode IS NOT NULL
    GROUP BY incidentCode
) AS s
   ON t.Code = s.incidentCode
WHEN MATCHED THEN
    UPDATE SET t.Name           = ISNULL(s.incidentName, t.Name),
               t.EventTypeId    = ISNULL(s.eventTypeId, t.EventTypeId),
               t.StartDate      = ISNULL(s.occurredTime, t.StartDate),
               t.KmNumber       = ISNULL(s.locationKm, t.KmNumber),
               t.MetNumber      = ISNULL(s.locationMet, t.MetNumber),
               t.Location       = ISNULL(s.locationRoute, t.Location),
               t.InfluenceScope = ISNULL(s.direction, t.InfluenceScope),
               t.InjuredNumber  = ISNULL(s.injuredCount, t.InjuredNumber),
               t.VehicleNumber  = ISNULL(s.vehicleCount, t.VehicleNumber),
               t.State          = ISNULL(s.incidentState, t.State),
               t.Description    = ISNULL(s.description, t.Description),
               t.Source         = ISNULL(s.source, t.Source),
               t.UpdateTime     = @now,
               t.UpdateUId      = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, Code, Name, EventTypeId, StartDate, KmNumber, MetNumber, Location, InfluenceScope,
            InjuredNumber, VehicleNumber, State, Description, Source, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.incidentCode, s.incidentName, s.eventTypeId, s.occurredTime,
            s.locationKm, s.locationMet, s.locationRoute, s.direction, s.injuredCount, s.vehicleCount,
            s.incidentState, s.description, s.source, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 107 (IncidentData): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

-- 4.8 Gói 108: VmsInfo (2 câu ghi)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '108_vmsInfo'
      AND [TableName] = 'dbo.TmsEquipment'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '108_vmsInfo', 'dbo.TmsEquipment', 1, N'
MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
USING (
    SELECT equipmentCode, MAX(locationKm) AS locationKm, MAX(locationMet) AS locationMet,
           MAX(direction) AS direction, MAX(laneId) AS laneId
    FROM OPENJSON(@records) WITH (
        equipmentCode nvarchar(64), locationKm int, locationMet int, direction int, laneId nvarchar(32))
    WHERE equipmentCode IS NOT NULL
    GROUP BY equipmentCode
) AS s
   ON t.Code = s.equipmentCode
WHEN MATCHED THEN
    UPDATE SET t.KmNumber    = ISNULL(s.locationKm, t.KmNumber),
               t.MetNumber   = ISNULL(s.locationMet, t.MetNumber),
               t.DirectionId = ISNULL(s.direction, t.DirectionId),
               t.LaneId      = ISNULL(s.laneId, t.LaneId),
               t.UpdateTime  = @now,
               t.UpdateUId   = @operator
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '108_vmsInfo'
      AND [TableName] = 'dbo.VmsCurrent'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '108_vmsInfo', 'dbo.VmsCurrent', 2, N'
MERGE dbo.VmsCurrent WITH (HOLDLOCK) AS t
USING (
    SELECT e.ID AS equipmentId, MAX(r.vmsName) AS vmsName, MAX(r.displayContent) AS displayContent,
           MAX(r.displayImageUrl) AS displayImageUrl, MAX(r.displaySize) AS displaySize,
           MAX(r.priority) AS priority, MAX(r.executedTime) AS executedTime
    FROM OPENJSON(@records) WITH (
        equipmentCode nvarchar(64), vmsName nvarchar(64), displayContent nvarchar(1024),
        displayImageUrl nvarchar(512), displaySize nvarchar(32), priority int, executedTime datetime2) AS r
    INNER JOIN dbo.TmsEquipment e ON e.Code = r.equipmentCode
    GROUP BY e.ID
) AS s
   ON t.EquipmentId = s.equipmentId
WHEN MATCHED THEN
    UPDATE SET t.Name         = ISNULL(s.vmsName, t.Name),
               t.RowData      = s.displayContent,
               t.Url          = s.displayImageUrl,
               t.Size         = ISNULL(s.displaySize, t.Size),
               t.Priority     = ISNULL(s.priority, t.Priority),
               t.ExecutedDate = ISNULL(s.executedTime, t.ExecutedDate),
               t.UpdateTime   = @now,
               t.UpdateUId    = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, EquipmentId, Name, RowData, Url, Size, Priority, ExecutedDate, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.equipmentId, s.vmsName, s.displayContent, s.displayImageUrl,
            s.displaySize, s.priority, s.executedTime, @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 108 (VmsInfo): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

-- 4.9 Gói 109: EtcData (3 câu ghi)
SET @InsertedWriteSql = 0;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '109_etcData'
      AND [TableName] = 'dbo.TollStation'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '109_etcData', 'dbo.TollStation', 1, N'
MERGE dbo.TollStation WITH (HOLDLOCK) AS t
USING (
    SELECT stationId, MAX(stationName) AS stationName
    FROM OPENJSON(@records) WITH (stationId nvarchar(64), stationName nvarchar(64))
    WHERE stationId IS NOT NULL AND stationName IS NOT NULL
    GROUP BY stationId
) AS s
   ON t.StationId = s.stationId
WHEN MATCHED THEN
    UPDATE SET t.Name = s.stationName, t.UpdateTime = @now, t.UpdateUId = @operator
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '109_etcData'
      AND [TableName] = 'dbo.TollLane'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '109_etcData', 'dbo.TollLane', 2, N'
MERGE dbo.TollLane WITH (HOLDLOCK) AS t
USING (
    SELECT laneId, MAX(laneName) AS laneName
    FROM OPENJSON(@records) WITH (laneId nvarchar(64), laneName nvarchar(64))
    WHERE laneId IS NOT NULL AND laneName IS NOT NULL
    GROUP BY laneId
) AS s
   ON t.LaneId = s.laneId
WHEN MATCHED THEN
    UPDATE SET t.Name = s.laneName, t.UpdateTime = @now, t.UpdateUId = @operator
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql]
    WHERE [PacketCode] = '109_etcData'
      AND [TableName] = 'dbo.TollTransactionOut'
      AND [IsDelete] IS NULL
)
BEGIN
    INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
    VALUES (LOWER(CONVERT(NVARCHAR(64), NEWID())), '109_etcData', 'dbo.TollTransactionOut', 3, N'
MERGE dbo.TollTransactionOut WITH (HOLDLOCK) AS t
USING (
    SELECT transactionId, MAX(entryTime) AS entryTime, MAX(exitTime) AS exitTime,
           MAX(vehicleTypeId) AS vehicleTypeId, MAX(licensePlate) AS licensePlate, MAX(tagId) AS tagId,
           MAX(laneId) AS laneId, MAX(stationId) AS stationId, MAX(tollPrice) AS tollPrice,
           MAX(syncTime) AS syncTime
    FROM OPENJSON(@records) WITH (
        transactionId nvarchar(128), entryTime datetime2, exitTime datetime2,
        vehicleTypeId nvarchar(64), licensePlate nvarchar(32), tagId nvarchar(64),
        laneId nvarchar(32), stationId nvarchar(32), tollPrice decimal(18, 2), syncTime datetime2)
    WHERE transactionId IS NOT NULL
    GROUP BY transactionId
) AS s
   ON t.TransactionId = s.transactionId
WHEN MATCHED THEN
    UPDATE SET t.TransactionDateTimeIn = ISNULL(s.entryTime, t.TransactionDateTimeIn),
               t.TransactionDateTime   = ISNULL(s.exitTime, t.TransactionDateTime),
               t.VehicleTypeId         = ISNULL(s.vehicleTypeId, t.VehicleTypeId),
               t.Plate                 = ISNULL(s.licensePlate, t.Plate),
               t.PlateEdit             = ISNULL(s.licensePlate, t.PlateEdit),
               t.TagId                 = ISNULL(s.tagId, t.TagId),
               t.LaneId                = ISNULL(s.laneId, t.LaneId),
               t.StationId             = ISNULL(s.stationId, t.StationId),
               t.Price                 = ISNULL(s.tollPrice, t.Price),
               t.SyncTime              = ISNULL(s.syncTime, t.SyncTime),
               t.UpdateTime            = @now,
               t.UpdateUId             = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, TransactionId, TransactionDateTimeIn, TransactionDateTime, VehicleTypeId, Plate, PlateEdit,
            TagId, LaneId, StationId, Price, SyncTime, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.transactionId, s.entryTime, s.exitTime, s.vehicleTypeId,
            s.licensePlate, s.licensePlate, s.tagId, s.laneId, s.stationId, s.tollPrice, s.syncTime,
            @tenantId, @now, @operator)
OUTPUT $action;', 1, @TenantIdPacketSql, GETDATE(), 'SHAREDATA-SEED-02');
    SET @InsertedWriteSql = @InsertedWriteSql + 1;
END;

PRINT N'   - Gói 109 (EtcData): đã nạp ' + CAST(@InsertedWriteSql AS NVARCHAR(10)) + N' câu ghi mới.';
SET @TotalWriteSqlInserted = @TotalWriteSqlInserted + @InsertedWriteSql;

PRINT N'   -> Tổng số câu ghi chiều nhận đã nạp mới: ' + CAST(@TotalWriteSqlInserted AS NVARCHAR(10)) + N' dòng.';


PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN SEED GÓI TIN SHAREDATA (02-sharedata-seed-goi-tin.sql)';
PRINT N'=============================================================================';
GO
