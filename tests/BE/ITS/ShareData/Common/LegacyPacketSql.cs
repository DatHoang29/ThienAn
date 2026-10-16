namespace Tests.ShareData.Common;

/// <summary>
/// Description: Chứa các chuỗi truy vấn SQL mẫu nguyên văn từ commit 56bb4bc3 của repo TA-ITS015-WEBAPI-V1.0
///              (tương ứng các hàm QueryPacket101..QueryPacket109 trong DataOutboundExtractionProcess.cs).
///              Chỉ dùng làm chuẩn đối chiếu trong kiểm thử hồi quy, không sửa.
/// </summary>
internal static class LegacyPacketSql
{
    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket101 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet101 = """
        SELECT
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
        LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId
        """;

    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket102 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet102 = """
        SELECT
            e.Code AS cameraCode,
            c.Name AS cameraName,
            NULL AS snapshot,
            c.SnapshotTime AS snapshotTime,
            c.DeviceState AS deviceState,
            e.KmNumber AS locationKm,
            e.MetNumber AS locationMet,
            e.DirectionId AS direction
        FROM CctvDevice c
        LEFT JOIN TmsEquipment e ON c.Ip = e.Ip
        """;

    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket103 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet103 = """
        SELECT TOP (@pageSize)
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
        OPTION (RECOMPILE)
        """;

    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket104 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet104 = """
        SELECT TOP (@pageSize)
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
        OPTION (RECOMPILE)
        """;

    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket105 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet105 = """
        SELECT TOP (@snapshotTop)
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
        ORDER BY i.TransactionDateTime DESC
        """;

    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket106 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet106 = """
        SELECT TOP (@pageSize)
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
        OPTION (RECOMPILE)
        """;

    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket107 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet107 = """
        SELECT TOP (@pageSize)
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
        OPTION (RECOMPILE)
        """;

    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket108 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet108 = """
        SELECT
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
        LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
        """;

    /// <summary>
    /// Chuỗi SQL trích xuất nguyên văn từ hàm QueryPacket109 tại commit 56bb4bc3.
    /// Chỉ dùng làm chuẩn đối chiếu, không sửa.
    /// </summary>
    public const string Packet109 = """
        SELECT TOP (@pageSize)
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
        OPTION (RECOMPILE)
        """;

    /// <summary>
    /// Description: Lấy chuỗi SQL trích xuất cũ theo mã gói tin (101..109).
    /// </summary>
    public static string GetLegacySql(string packetCode)
    {
        if (string.IsNullOrWhiteSpace(packetCode))
        {
            throw new ArgumentException("Mã gói tin không được để trống.", nameof(packetCode));
        }

        return packetCode.Trim() switch
        {
            "101" => Packet101,
            "102" => Packet102,
            "103" => Packet103,
            "104" => Packet104,
            "105" => Packet105,
            "106" => Packet106,
            "107" => Packet107,
            "108" => Packet108,
            "109" => Packet109,
            _ => throw new ArgumentOutOfRangeException(nameof(packetCode), $"Không tìm thấy SQL đối chiếu cũ cho gói '{packetCode}'.")
        };
    }
}
