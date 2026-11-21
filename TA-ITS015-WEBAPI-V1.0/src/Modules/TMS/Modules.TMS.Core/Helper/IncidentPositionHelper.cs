using System.Linq.Expressions;
using Modules.TMS.Core.Entities;
using SqlSugar;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Triển khai IncidentPositionHelper.
/// Created date: 02/10/2026
/// </summary>
public static class IncidentPositionHelper
{
    // Preserve the manual form's half-open zone boundaries, including descending kilometre ranges.
    public static Expression<Func<TmsZone, bool>> Zones(int? km, int? met, string? location, string? direction)
    {
        var position = (km ?? 0) * 1000 + (met ?? 0);
        var anyLocation = string.IsNullOrEmpty(location);
        var anyDirection = string.IsNullOrEmpty(direction) || direction == "0";
        return z => z.IsDelete == null && z.Status == BaseEnums.StatusEnum.Enable
            && (anyLocation || z.Location == location)
            && (anyDirection || z.LaneId == null || z.LaneId == direction)
            && ((SqlFunc.IsNull(z.FromKmNumber, 0) * 1000 + SqlFunc.IsNull(z.FromMetNumber, 0) <= position
                && SqlFunc.IsNull(z.ToKmNumber, 0) * 1000 + SqlFunc.IsNull(z.ToMetNumber, 0) > position)
                || (SqlFunc.IsNull(z.FromKmNumber, 0) * 1000 + SqlFunc.IsNull(z.FromMetNumber, 0) > position
                && SqlFunc.IsNull(z.ToKmNumber, 0) * 1000 + SqlFunc.IsNull(z.ToMetNumber, 0) <= position));
    }

    /// <summary>
    /// KienLT - 23/10/2026: Vùng (cấp nhỏ nhất) chứa vị trí sự cố và camera XEM ĐƯỢC (thuộc module CCTV) gắn với
    /// các vùng đó. Camera VDS phát hiện sự cố nhưng không stream được, nên sự cố trỏ sang camera CCTV cùng vùng.
    /// Dùng chung cho sự cố VDS tự tạo (Services.Vds) và sự cố tạo từ sự kiện cũ (WebAPI).
    /// <paramref name="cctvModuleId"/> rỗng ⇒ chỉ trả vùng, để trống camera xem được.
    /// </summary>
    public static async Task<(string? ZoneIds, string? EquipmentIds)> QueryAsync(ISqlSugarClient db,
        int? km, int? met, string? location, string? direction, string? cctvModuleId, CancellationToken ct = default)
    {
        var zones = await db.Queryable<TmsZone>()
            .Where(Zones(km, met, location, direction))
            .OrderBy(z => z.FromKmNumber).OrderBy(z => z.FromMetNumber).ToListAsync(ct);
        var parentIds = zones.Select(z => z.Pid).ToHashSet(StringComparer.Ordinal);
        var ids = zones.Where(z => !parentIds.Contains(z.ID)).Select(z => z.ID).ToList();
        if (ids.Count == 0) return (null, null);
        if (string.IsNullOrWhiteSpace(cctvModuleId)) return (string.Join(',', ids), null);
        var equipment = await db.Queryable<TmsZoneEquipment>()
            .InnerJoin<TmsEquipment>((ze, e) => ze.EquipmentId == e.ID)
            .InnerJoin<TmsZone>((ze, e, z) => ze.ZoneId == z.ID)
            .Where((ze, e, z) => ids.Contains(ze.ZoneId))
            .Where(MappedEquipment)
            .InnerJoin<TmsEquipmentType>((ze, e, z, t) => e.EquipmentTypeId == t.ID)
            .Where((ze, e, z, t) => t.ModuleId == cctvModuleId && t.IsDelete == null)
            .OrderBy((ze, e, z, t) => e.KmNumber).OrderBy((ze, e, z, t) => e.MetNumber)
            .Select((ze, e, z, t) => e.ID).ToListAsync(ct);
        return (string.Join(',', ids), equipment.Count == 0 ? null : string.Join(',', equipment.Distinct()));
    }

    public static Expression<Func<TmsZoneEquipment, TmsEquipment, TmsZone, bool>> MappedEquipment =>
        (ze, e, z) => ze.IsDelete == null && e.IsDelete == null && z.IsDelete == null
            && (ze.Status == null || ze.Status == BaseEnums.StatusEnum.Enable)
            && z.Status == BaseEnums.StatusEnum.Enable && e.Status == BaseEnums.StatusEnum.Enable
            && ze.TenantId == e.TenantId && e.TenantId == z.TenantId && e.Location == z.Location;
}
