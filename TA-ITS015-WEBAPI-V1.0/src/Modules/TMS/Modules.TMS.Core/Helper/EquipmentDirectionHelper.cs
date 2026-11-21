namespace Modules.TMS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Hướng lắp đặt và phạm vi PTZ theo zone là hai thông tin riêng biệt.
/// Created date: 02/10/2026
/// </summary>
public static class EquipmentDirectionHelper
{
    public static bool CanServe(string? target, string? installed, string? zoneDirection, bool mappedPtz)
    {
        var requested = (string.IsNullOrWhiteSpace(target) || target == "0") && !string.IsNullOrWhiteSpace(zoneDirection)
            ? zoneDirection : target;
        if (mappedPtz && (string.IsNullOrWhiteSpace(requested) || requested == "0" || requested == zoneDirection))
            return true;
        return requested == "0" || installed == "0" || (!string.IsNullOrWhiteSpace(requested) && requested == installed);
    }
}
