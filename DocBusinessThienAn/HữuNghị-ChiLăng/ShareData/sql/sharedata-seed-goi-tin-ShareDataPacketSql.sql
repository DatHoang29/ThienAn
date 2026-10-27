USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-goi-tin-ShareDataPacketSql.sql
-- BẢNG GHI: [dbo].[ShareDataPacketSql] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Nạp khối câu ghi chiều nhận chuẩn Catalog vào ShareDataPacketSql cho các gói (101 - 109),
--              mỗi gói một dòng (OrderNo = 1) kèm danh sách bảng bị ảnh hưởng (AffectedTables),
--              chống trùng theo PacketCode kèm IsDelete IS NULL, ID sinh động, CreateUId = SHAREDATA-SEED-02.
-- THỨ TỰ CHẠY: 3 / 3 (độc lập với hai tệp gói tin còn lại) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 26/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-goi-tin-ShareDataPacketSql.sql';
PRINT N'=============================================================================';

IF OBJECT_ID('dbo.ShareDataPacketSql', 'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPacketSql] chưa tồn tại (chưa chạy CodeFirst)! Bỏ qua nạp câu ghi chiều nhận.';
    SET NOEXEC ON;
END;
GO

IF COL_LENGTH('dbo.ShareDataPacketSql', 'AffectedTables') IS NULL
BEGIN
    ALTER TABLE [dbo].[ShareDataPacketSql] ADD [AffectedTables] NVARCHAR(1024) NULL;
    PRINT N'   + Đã thêm cột [AffectedTables] vào [dbo].[ShareDataPacketSql].';
END;
GO

-- -----------------------------------------------------------------------------------------
-- 4. NẠP KHỐI CÂU GHI CHIỀU NHẬN (ShareDataPacketSql — mỗi gói 1 dòng)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 4. Nạp khối câu ghi chiều nhận chuẩn Catalog vào ShareDataPacketSql...';

DECLARE @TenantIdPacketSql NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdPacketSql IS NULL
    SET @TenantIdPacketSql = N'Default';

-- Dọn mềm các dòng seed cũ (OrderNo > 1 do bản seed trước tách nhiều dòng) chưa bị sửa tay
UPDATE [dbo].[ShareDataPacketSql]
SET [IsDelete] = 1,
    [UpdateTime] = GETDATE()
WHERE [CreateUId] = 'SHAREDATA-SEED-02'
  AND [UpdateUId] IS NULL
  AND [OrderNo] > 1
  AND [IsDelete] IS NULL;

DECLARE @SeedPackets TABLE (
    [PacketCode]     NVARCHAR(64)   NOT NULL,
    [TableName]      NVARCHAR(128)  NOT NULL,
    [AffectedTables] NVARCHAR(1024) NOT NULL,
    [WriteSql]       NVARCHAR(MAX)  NOT NULL
);

-- 4.1 Gói 101: CommonData (3 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('101_commonData', 'dbo.TmsZone', 'dbo.TmsZone, dbo.TmsTrafficStatistic, dbo.TmsZoneStatus', N'MERGE dbo.TmsZone WITH (HOLDLOCK) AS t
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
OUTPUT $action;

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
OUTPUT $action;

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
OUTPUT $action;');

-- 4.2 Gói 102: CCTV (2 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('102_cctvData', 'dbo.TmsEquipment', 'dbo.TmsEquipment, dbo.CctvDevice', N'MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
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
OUTPUT $action;

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
OUTPUT $action;');

-- 4.3 Gói 103: VDS (2 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('103_vdsData', 'dbo.TmsEquipment', 'dbo.TmsEquipment, dbo.TmsTrafficData', N'MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
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
OUTPUT $action;

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
OUTPUT $action;');

-- 4.4 Gói 104: Weather (1 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('104_weatherData', 'dbo.TmsWeather', 'dbo.TmsWeather', N'MERGE dbo.TmsWeather WITH (HOLDLOCK) AS t
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
OUTPUT $action;');

-- 4.5 Gói 105: RfidData (2 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('105_rfidData', 'dbo.TmsVehicleRegistration', 'dbo.TmsVehicleRegistration, dbo.TollTransactionOut', N'MERGE dbo.TmsVehicleRegistration WITH (HOLDLOCK) AS t
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
OUTPUT $action;

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
OUTPUT $action;');

-- 4.6 Gói 106: WimData (1 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('106_wimData', 'dbo.TmsTrafficData', 'dbo.TmsTrafficData', N'MERGE dbo.TmsTrafficData WITH (HOLDLOCK) AS t
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
OUTPUT $action;');

-- 4.7 Gói 107: IncidentData (2 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('107_incidentData', 'dbo.TmsEventType', 'dbo.TmsEventType, dbo.TmsIncident', N'MERGE dbo.TmsEventType WITH (HOLDLOCK) AS t
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
OUTPUT $action;

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
OUTPUT $action;');

-- 4.8 Gói 108: VmsInfo (2 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('108_vmsInfo', 'dbo.TmsEquipment', 'dbo.TmsEquipment, dbo.VmsCurrent', N'MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
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
OUTPUT $action;

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
OUTPUT $action;');

-- 4.9 Gói 109: EtcData (3 bảng)
INSERT INTO @SeedPackets ([PacketCode], [TableName], [AffectedTables], [WriteSql])
VALUES ('109_etcData', 'dbo.TollStation', 'dbo.TollStation, dbo.TollLane, dbo.TollTransactionOut', N'MERGE dbo.TollStation WITH (HOLDLOCK) AS t
USING (
    SELECT stationId, MAX(stationName) AS stationName
    FROM OPENJSON(@records) WITH (stationId nvarchar(64), stationName nvarchar(64))
    WHERE stationId IS NOT NULL AND stationName IS NOT NULL
    GROUP BY stationId
) AS s
   ON t.StationId = s.stationId
WHEN MATCHED THEN
    UPDATE SET t.Name = s.stationName, t.UpdateTime = @now, t.UpdateUId = @operator
OUTPUT $action;

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
OUTPUT $action;

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
OUTPUT $action;');

-- Cập nhật các dòng seed OrderNo = 1 chưa có AffectedTables (nếu chưa bị sửa tay)
UPDATE target
SET target.[TableName]      = src.[TableName],
    target.[AffectedTables] = src.[AffectedTables],
    target.[WriteSql]       = src.[WriteSql],
    target.[UpdateTime]     = GETDATE()
FROM [dbo].[ShareDataPacketSql] target
INNER JOIN @SeedPackets src ON src.[PacketCode] = target.[PacketCode]
WHERE target.[IsDelete] IS NULL
  AND target.[OrderNo] = 1
  AND target.[CreateUId] = 'SHAREDATA-SEED-02'
  AND target.[UpdateUId] IS NULL
  AND (target.[AffectedTables] IS NULL OR target.[AffectedTables] <> src.[AffectedTables]);

-- Thêm mới cho các gói chưa có dòng hoạt động nào
INSERT INTO [dbo].[ShareDataPacketSql] ([ID], [PacketCode], [TableName], [AffectedTables], [OrderNo], [WriteSql], [Status], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())),
       src.[PacketCode],
       src.[TableName],
       src.[AffectedTables],
       1,
       src.[WriteSql],
       1,
       @TenantIdPacketSql,
       GETDATE(),
       'SHAREDATA-SEED-02'
FROM @SeedPackets src
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[ShareDataPacketSql] existing
    WHERE existing.[PacketCode] = src.[PacketCode]
      AND existing.[IsDelete] IS NULL
);

DECLARE @TotalWriteSqlInserted INT = @@ROWCOUNT;
PRINT N'   -> Đã nạp mới ' + CAST(@TotalWriteSqlInserted AS NVARCHAR(10)) + N' khối câu ghi chiều nhận.';

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-goi-tin-ShareDataPacketSql.sql';
PRINT N'=============================================================================';
GO
