using Modules.VDS.Core.Helper;
using Modules.TMS.Core.Constants;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Parsing;
using Services.Vds.Pipeline.Contracts;
using Services.Vds.Catalog;

namespace Services.Vds.Pipeline.Mapping;

/// <summary>
/// Author: KienLT
/// Description: Ánh xạ envelope đã chuẩn hoá + camera cache sang đúng schema QuestDB VdsTrafficData.
/// Created date: 25/08/2026
/// </summary>
public static class VdsTrafficDataMapper
{
    /// <summary>
    /// DetectTime chỉ lấy từ timestamp ổn định trong envelope. Không dùng giờ ingest hay
    /// metadata.device_time không có timezone vì replay phải sinh lại đúng DEDUP key.
    /// </summary>
    /// <param name="zoneId">
    /// Vùng chính của camera, do <c>ZoneManager</c> tra. Truyền vào thay vì để mapper tự tra: mapper là
    /// hàm thuần nên test được mà không cần dựng danh mục.
    /// </param>
    /// <param name="sizeResolver">
    /// Nơi duy nhất quy đổi đơn vị chiều dài và phân cỡ xe. Truyền vào chứ không tự tính tại đây, vì
    /// ngưỡng và đơn vị nằm trong cấu hình đổi được lúc chạy.
    /// </param>
    /// <param name="mappedEventTypeCode">
    /// <c>TmsEventType.Code</c> ánh xạ cho loại AID của bản tin (<c>EventOptions.MappedEventTypeCode</c>), do bên gọi tra.
    /// null ⇒ chưa khai ánh xạ, cột lưu unknown.
    /// </param>
    /// <param name="speedRange">
    /// Ngưỡng min/max hiệu lực của camera lúc nhận bản tin (<c>SpeedRangeResolver.ForCamera</c>), do bên gọi tra —
    /// cùng lý do với <paramref name="zoneId"/>. null ⇒ camera chưa có ngưỡng, hai cột để trống.
    /// </param>
    public static VdsTrafficData? Map(
        VdsSseEnvelope envelope,
        VdsVehicleMetadata? metadata,
        CameraInfo camera,
        string? zoneId,
        VehicleSizeResolver sizeResolver,
        out string? error,
        SpeedRange? speedRange = null,
        string? mappedEventTypeCode = null)
    {
        error = null;
        var detectTime = ResolveDetectTime(envelope);
        if (detectTime is null)
        {
            error = "thiếu occurred_utc/created_at";
            return null;
        }

        var lengthMm = sizeResolver.LengthMillimeter(
            envelope.VehicleLengthMm, metadata?.VehicleLengthRaw);

        return new VdsTrafficData
        {
            DetectTime = detectTime.Value,
            VendorEventId = envelope.Id,
            CameraIp = camera.CameraIp,
            EquipmentId = camera.EquipmentId,
            Direction = NullIfDash(camera.Direction),
            ZoneId = NullIfDash(zoneId),
            Lane = metadata?.Lane,
            EventType = StorageEventType(NullIfDash(envelope.Type), mappedEventTypeCode),
            AidEventType = NullIfDash(envelope.Type),
            LicensePlate = NullIfDash(metadata?.LicensePlate),
            VehicleType = NullIfDash(metadata?.VehicleType),
            VehicleColor = NullIfDash(metadata?.VehicleColor),
            VehicleSize = sizeResolver.Resolve(envelope.Id, lengthMm),
            Speed = VdsParse.SpeedKmh(metadata?.Speed),
            LengthMm = lengthMm,
            KmNumber = camera.Km,
            MetNumber = camera.Met,
            // Ngưỡng TẠI THỜI ĐIỂM phát hiện. Min ưu tiên giá trị luật chạy chậm đã dùng (có trong payload).
            SpeedMin = PositiveOrNull(VdsParse.Int(metadata?.LaneSpeedLimitMinKmh))
                       ?? (speedRange is { IsValid: true } ? speedRange.Min : null),
            SpeedMax = speedRange is { IsValid: true } ? speedRange.Max : null,
            PlateColor = NullIfDash(metadata?.PlateColor),
        };
    }

    public static DateTime? ResolveDetectTime(VdsSseEnvelope envelope)
    {
        var timestamp = envelope.OccurredUtc ?? envelope.CreatedAt;
        if (timestamp is null) return null;

        return timestamp.Value.Kind switch
        {
            DateTimeKind.Utc => timestamp.Value,
            DateTimeKind.Local => timestamp.Value.ToUniversalTime(),
            // Hai field đều được định nghĩa là UTC. Nếu vendor bỏ hậu tố Z thì vẫn coi đúng nghĩa field,
            // không phụ thuộc timezone của Windows Service đang chạy.
            _ => DateTime.SpecifyKind(timestamp.Value, DateTimeKind.Utc),
        };
    }

    /// <summary>
    /// Giá trị cột <c>eventType</c>: mã TMS theo ánh xạ, chưa ánh xạ thì lưu unknown. Lượt xe thường (<c>Normal</c>,
    /// cả tên cũ <c>Regular</c>) LUÔN lưu <c>Normal</c> dù có ai khai ánh xạ: đếm lưu lượng (<c>CountedEventTypes</c>)
    /// và nhánh lưu ảnh (<c>VdsBaseHelper.MediaPathHp.Branch</c>) dựa vào đúng giá trị này. Hàm thuần.
    /// </summary>
    public static string? StorageEventType(string? aidType, string? mappedEventTypeCode)
    {
        if (string.IsNullOrWhiteSpace(aidType)) return null;
        if (string.Equals(VdsEventTypes.Canonical(aidType), VdsEventTypes.Normal, StringComparison.OrdinalIgnoreCase))
            return VdsEventTypes.Normal;
        return string.IsNullOrWhiteSpace(mappedEventTypeCode) ? IncidentEventTypeConst.Unknown : mappedEventTypeCode.Trim();
    }

    private static int? PositiveOrNull(int? value) => value is > 0 ? value : null;

    private static string? NullIfDash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        return trimmed == "-" ? null : trimmed;
    }
}
