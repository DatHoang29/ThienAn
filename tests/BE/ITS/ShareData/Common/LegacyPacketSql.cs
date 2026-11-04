namespace Tests.ShareData.Common;

/// <summary>
/// Description: Chứa các chuỗi truy vấn SQL gửi toàn bộ nguyên văn từ commit 56bb4bc3 của repo TA-ITS015-WEBAPI-V1.0
///              (tương ứng các hàm QueryPacket101, 102, 105, 108 trong DataOutboundExtractionProcess.cs).
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
}
