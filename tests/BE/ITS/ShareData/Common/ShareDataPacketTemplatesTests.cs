namespace Tests.ShareData.Common;

using Module.ShareData.Core.Entities;

/// <summary>
/// Description: Cung cấp câu truy vấn SQL trích xuất chuẩn cho các gói tin ShareData (101 - 110)
///              dùng trong môi trường kiểm thử xUnit.
///              Nội dung đồng bộ 100% với script seed chuẩn: 02-sharedata-seed-goi-tin.sql.
/// Created date: 16/10/2026
/// </summary>
public static class ShareDataPacketTemplatesTests
{
    public const string Packet101_TrafficFlow = @"SELECT
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
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId";

    public const string Packet102_CctvImage = @"SELECT
    e.Code AS cameraCode,
    c.Name AS cameraName,
    NULL AS snapshot,
    c.SnapshotTime AS snapshotTime,
    c.DeviceState AS deviceState,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction
FROM CctvDevice c
LEFT JOIN TmsEquipment e ON c.Ip = e.Ip";

    public const string Packet103_VehicleDetection = @"SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)";

    public const string Packet104_Weather = @"SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)";

    public const string Packet105_VehicleIdentification = @"SELECT
    i.TransactionId AS transactionId,
    i.TagId AS tagId,
    NULLIF(ISNULL(NULLIF(i.PlateEdit, ''), NULLIF(i.PlateLpr, '')), '') AS licensePlate,
    i.VehicleTypeId AS vehicleTypeId,
    i.TransactionDateTime AS entryTime,
    CAST(NULL AS DATETIME) AS exitTime,
    i.LaneId AS laneId,
    i.StationId AS stationId,
    vr.Brand AS vehicleBrand,
    vr.Owner AS vehicleOwner
FROM TollTransactionIn i
LEFT JOIN TmsVehicleRegistration vr
       ON NULLIF(ISNULL(NULLIF(i.PlateEdit, ''), NULLIF(i.PlateLpr, '')), '') = vr.LicensePlate
      AND vr.IsDelete IS NULL
WHERE i.IsDelete IS NULL
ORDER BY i.TransactionDateTime DESC";

    public const string Packet106_WeighInMotion = @"SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)";

    public const string Packet107_TrafficIncident = @"SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)";

    public const string Packet108_VmsDisplay = @"SELECT
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
LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID";

    public const string Packet109_TollCollection = @"SELECT TOP (@pageSize)
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
OPTION (RECOMPILE)";

    public const string Packet110_PublicMessaging = @"SELECT
    CAST(i.ID AS NVARCHAR(64)) AS messageId,
    CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, '')) AS incidentMessage,
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
    i.State != 'FINISHED' AND i.State != 'CANCELED'
    AND i.State != 'Closed' AND i.State != 'Cancelled'
))";

    public static string? GetExtractSql(string? packetCode)
    {
        if (string.IsNullOrWhiteSpace(packetCode)) return null;

        var normalized = packetCode.Trim();
        if (normalized.StartsWith("101")) return Packet101_TrafficFlow;
        if (normalized.StartsWith("102")) return Packet102_CctvImage;
        if (normalized.StartsWith("103")) return Packet103_VehicleDetection;
        if (normalized.StartsWith("104")) return Packet104_Weather;
        if (normalized.StartsWith("105")) return Packet105_VehicleIdentification;
        if (normalized.StartsWith("106")) return Packet106_WeighInMotion;
        if (normalized.StartsWith("107")) return Packet107_TrafficIncident;
        if (normalized.StartsWith("108")) return Packet108_VmsDisplay;
        if (normalized.StartsWith("109")) return Packet109_TollCollection;
        if (normalized.StartsWith("110")) return Packet110_PublicMessaging;

        return null;
    }

    public static ShareDataPacket ApplyTemplate(ShareDataPacket packet)
    {
        if (packet == null) return null!;
        if (string.IsNullOrWhiteSpace(packet.ExtractSql))
        {
            packet.ExtractSql = GetExtractSql(packet.Code);
        }
        return packet;
    }
}
