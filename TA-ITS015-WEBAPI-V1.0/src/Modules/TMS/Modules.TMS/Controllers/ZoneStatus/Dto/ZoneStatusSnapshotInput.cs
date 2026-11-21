namespace Modules.TMS.Controllers.ZoneStatus.Dto;

/// <summary>
/// Author: KienLT
/// Description: Bộ lọc snapshot trạng thái vùng kèm ngưỡng quá hạn do người gọi chỉ định.
///
/// <para>DTO này KHÔNG lộ ra swagger: hai action <c>Stream</c>/<c>Snapshot</c> giữ nguyên tham số
/// <c>zoneId</c>/<c>staleAfterSeconds</c> trên query string để contract HTTP không đổi, controller tự dựng
/// DTO rồi dispatch qua message bus.</para>
/// Created date: 21/09/2026
/// </summary>
public class ZoneStatusSnapshotInput
{
    public string? ZoneId { get; set; }

    /// <summary>Ngưỡng quá hạn; bỏ trống thì lấy theo cấu hình <c>vds_zone_stale_seconds</c>.</summary>
    [Range(1, 86400)] public int? StaleAfterSeconds { get; set; }
}
