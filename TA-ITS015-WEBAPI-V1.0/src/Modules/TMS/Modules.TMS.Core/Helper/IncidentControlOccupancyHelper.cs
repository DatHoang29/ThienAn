using Modules.TMS.Core.Entities;

namespace Modules.TMS.Core.Helper;

/// <summary>
/// Quy tắc quyền giữ thiết bị giữa các sự cố (dùng chung VMS + LCS, TMS + VMS module). Hàm thuần, không truy cập DB:
/// đầu vào là các bản ghi TmsIncidentControl của sự cố còn hoạt động (đã lọc sẵn).
/// Quy ước: số Priority càng nhỏ ưu tiên càng cao; Priority null coi như defaultPriority.
/// Author: ChuongLP
/// Created date: 19/10/2026
/// </summary>
public static class IncidentControlOccupancyHelper
{
    /// <summary>
    /// Thiết bị bị chặn khi có ít nhất một sự cố khác đang giữ với ưu tiên CAO hơn sự cố mới (DEC-09).
    /// Cùng mức ưu tiên thì không chặn: sự cố mới được hiển thị.
    /// </summary>
    public static HashSet<string> GetOverrideBlockedEquipmentIds(
        IEnumerable<TmsIncidentControl>? occupancy, int? newPriority, int defaultPriority)
    {
        var curPriority = newPriority ?? defaultPriority;
        return (occupancy ?? Enumerable.Empty<TmsIncidentControl>())
            .Where(c => !string.IsNullOrWhiteSpace(c.EquipmentId)
                && (c.Priority ?? defaultPriority) < curPriority)
            .Select(c => c.EquipmentId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Thiết bị chỉ hiển thị một nội dung (LCS, VMS không có playlist): chọn sự cố được hiển thị trong các sự cố đang giữ.
    /// Ưu tiên cao nhất trước, cùng mức thì sự cố cập nhật gần nhất.
    /// </summary>
    public static TmsIncidentControl? PickDisplayOwner(IEnumerable<TmsIncidentControl>? controls, int defaultPriority)
        => (controls ?? Enumerable.Empty<TmsIncidentControl>())
            .Where(c => !string.IsNullOrEmpty(c.IncidentId))
            .OrderBy(c => c.Priority ?? defaultPriority)
            .ThenByDescending(c => c.UpdateTime ?? c.CreateTime)
            .FirstOrDefault();
}
