USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 20261014-seed-packet-template-sql-101-110.sql
-- DESCRIPTION: Khởi tạo bảng ShareDataPacketExtractSql (nếu chưa có) và nạp dữ liệu mẫu
--              (Seed Templates) chuẩn Catalog cho các gói tin 101-110 (PartnerCode = NULL)
--              theo kiến trúc Truy vấn động ShareData (Dynamic Query) ngày 14/10/2026.
-- GHI CHÚ:
--   - File kịch bản để lập trình viên kiểm tra và chạy trên môi trường CSDL DEV/Staging.
--   - Hỗ trợ phân cấp Fallback: Đối tác cấu hình riêng -> Template chung (file này) -> Catalog C#.
-- =========================================================================================

-- 1. TẠO BẢNG ShareDataPacketExtractSql NẾU CHƯA CÓ
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataPacketExtractSql')
BEGIN
    PRINT N'  -> Đang tạo bảng ShareDataPacketExtractSql...';
    CREATE TABLE [dbo].[ShareDataPacketExtractSql] (
        [ID]                 NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [TenantId]           BIGINT         NULL,
        [PacketCode]         NVARCHAR(128)  NOT NULL,
        [PartnerCode]        NVARCHAR(128)  NULL,
        [ExtractSql]         NVARCHAR(MAX)  NOT NULL,
        [SyncKind]           INT            NULL,       -- 1 = Snapshot, 2 = Incremental
        [CursorColumn]       NVARCHAR(128)  NULL,
        [WindowDays]         INT            NULL,
        [Status]             INT            NULL DEFAULT 1, -- 1 = Enable, 2 = Disable
        [Remark]             NVARCHAR(256)  NULL,
        [CreateTime]         DATETIME       NULL,
        [CreateUId]          NVARCHAR(64)   NULL,
        [UpdateTime]         DATETIME       NULL,
        [UpdateUId]          NVARCHAR(64)   NULL,
        [IsDelete]           DATETIME       NULL
    );

    CREATE NONCLUSTERED INDEX [index_ShareDataPacketExtractSql_PC] 
        ON [dbo].[ShareDataPacketExtractSql] ([PacketCode] ASC);

    CREATE NONCLUSTERED INDEX [index_ShareDataPacketExtractSql_PT] 
        ON [dbo].[ShareDataPacketExtractSql] ([PartnerCode] ASC);

    PRINT N'  -> Tạo bảng ShareDataPacketExtractSql thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng ShareDataPacketExtractSql đã tồn tại, kiểm tra nạp dữ liệu template...';
END
GO

-- 2. SEED TEMPLATE MẪU CHO TỪNG GÓI TIN 101 - 110 (PartnerCode = NULL)

-- Gói 101: TrafficFlow (Snapshot)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '101' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '101',
        NULL,
        N'SELECT
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
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId',
        1, 1, N'Template chuẩn Catalog gói 101 (TrafficFlow Snapshot)', GETDATE()
    );
END

-- Gói 102: CctvImage (Snapshot)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '102' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '102',
        NULL,
        N'SELECT
    e.Code AS cameraCode,
    c.Name AS cameraName,
    NULL AS snapshot,
    c.SnapshotTime AS snapshotTime,
    c.DeviceState AS deviceState,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction
FROM CctvDevice c
LEFT JOIN TmsEquipment e ON c.Ip = e.Ip',
        1, 1, N'Template chuẩn Catalog gói 102 (CctvImage Snapshot)', GETDATE()
    );
END

-- Gói 103: VehicleDetection (Incremental)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '103' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [CursorColumn], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '103',
        NULL,
        N'SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)',
        2, N'UpdateTime', 1, N'Template chuẩn Catalog gói 103 (VehicleDetection Incremental)', GETDATE()
    );
END

-- Gói 104: Weather (Incremental)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '104' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [CursorColumn], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '104',
        NULL,
        N'SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)',
        2, N'UpdateTime', 1, N'Template chuẩn Catalog gói 104 (Weather Incremental)', GETDATE()
    );
END

-- Gói 105: VehicleIdentification (Snapshot)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '105' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '105',
        NULL,
        N'SELECT
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
ORDER BY i.TransactionDateTime DESC',
        1, 1, N'Template chuẩn Catalog gói 105 (VehicleIdentification Snapshot)', GETDATE()
    );
END

-- Gói 106: WeighInMotion (Incremental)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '106' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [CursorColumn], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '106',
        NULL,
        N'SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)',
        2, N'UpdateTime', 1, N'Template chuẩn Catalog gói 106 (WeighInMotion Incremental)', GETDATE()
    );
END

-- Gói 107: TrafficIncident (Incremental)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '107' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [CursorColumn], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '107',
        NULL,
        N'SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)',
        2, N'UpdateTime', 1, N'Template chuẩn Catalog gói 107 (TrafficIncident Incremental)', GETDATE()
    );
END

-- Gói 108: VmsDisplay (Snapshot)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '108' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '108',
        NULL,
        N'SELECT
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
LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID',
        1, 1, N'Template chuẩn Catalog gói 108 (VmsDisplay Snapshot)', GETDATE()
    );
END

-- Gói 109: TollCollection (Incremental)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '109' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [CursorColumn], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '109',
        NULL,
        N'SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)',
        2, N'UpdateTime', 1, N'Template chuẩn Catalog gói 109 (TollCollection Incremental)', GETDATE()
    );
END

-- Gói 110: PublicMessaging (Snapshot)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketExtractSql] WHERE [PacketCode] = '110' AND ([PartnerCode] IS NULL OR [PartnerCode] = ''))
BEGIN
    INSERT INTO [dbo].[ShareDataPacketExtractSql] (
        [ID], [PacketCode], [PartnerCode], [ExtractSql], [SyncKind], [Status], [Remark], [CreateTime]
    ) VALUES (
        REPLACE(NEWID(), '-', ''),
        '110',
        NULL,
        N'SELECT
    CONCAT(ISNULL(i.Name, ''''), '' - '', ISNULL(i.Description, '''')) AS incidentMessage,
    v.RowData AS guidanceContent,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.StartDate AS publishedTime
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
WHERE (i.State IS NULL OR (
    i.State != ''FINISHED'' AND i.State != ''CANCELED''
    AND i.State != ''Closed'' AND i.State != ''Cancelled''
))',
        1, 1, N'Template chuẩn Catalog gói 110 (PublicMessaging Snapshot)', GETDATE()
    );
END
GO

PRINT N'✅ Đã khởi tạo và nạp thành công các Template câu truy vấn trích xuất cho gói tin 101 - 110.';
GO

