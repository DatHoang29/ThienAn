using Modules.TMS.Controllers.Zone.Dto;

namespace Modules.TMS.Infrastructure.Services.Zone.Rules;

/// <summary>
/// Author: KienLT
/// Description: Kiểm tra danh sách cập nhật ngưỡng tốc độ vùng và thứ tự ngưỡng tối thiểu, tối đa.
/// Created date: 19/10/2026
/// </summary>
internal static class TmsZoneSpeedBatchRules
{
    // Cùng giới hạn với VdsConfigRules.MaxSpeedKmh; module TMS không phụ thuộc implementation VDS.
    internal const int MaxSpeedKmh = 300;
    internal const int MaxItems = 500;

    internal static bool IsValid(BatchUpdateZoneSpeedInput? input)
    {
        if (input?.Items is not { Count: >= 1 and <= MaxItems } items) return false;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id.Trim())) return false;
            if (!IsValidSpeed(item.MinSpeed) || !IsValidSpeed(item.MaxSpeed)) return false;
            if (!IsOrdered(item.MinSpeed, item.MaxSpeed)) return false;
        }
        return true;
    }

    internal static bool IsValidSpeed(double? speed)
        => speed is null || (double.IsFinite(speed.Value) && speed is >= 1 and <= MaxSpeedKmh
                             && speed.Value == Math.Floor(speed.Value));

    internal static bool IsOrdered(double? min, double? max)
        => min is null || max is null || min.Value <= max.Value;
}
