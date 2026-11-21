using Services.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.DTO.Enums;
using Services.Vds.Shared.Persistence;

namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Truy vấn DANH MỤC trên SQL Server cho các manager giữ cache trong RAM.
///
/// SqlSugar chỉ xuất hiện trong repository, không lộ ra cho manager — manager nhận về record thuần.
/// Nhờ vậy đổi cách đọc (view khác, API khác) không ảnh hưởng logic cache.
///
/// Phạm vi lọc: chỉ loại bản ghi xoá mềm (<c>IsDelete IS NULL</c>), <b>KHÔNG</b> lọc <c>TenantId</c> —
/// giữ đúng hành vi bản cũ. Danh mục thiết bị dùng chung cho mọi tenant.
/// Created date: 24/08/2026
/// </summary>
public sealed class VdsCatalogRepository : VdsRepositoryBase
{
    // KienLT - 23/10/2026: truy vấn chuyển sang IncidentPositionHelper.QueryAsync để WebAPI dùng chung.
    public Task<(string? ZoneIds, string? EquipmentIds)> QueryIncidentPositionAsync(
        int? km, int? met, string? location, string? direction, string cctvModuleId, CancellationToken ct) =>
        Modules.TMS.Core.Helper.IncidentPositionHelper.QueryAsync(Client, km, met, location, direction, cctvModuleId, ct);

    private readonly IVdsConfigManager _config;

    public VdsCatalogRepository(DatabaseManager databaseManager, IVdsConfigManager config)
        : base(databaseManager, config)
    {
        _config = config;
    }

    /// <summary>
    /// Một dòng camera VDS trong danh mục thiết bị.
    ///
    /// <c>Direction</c> và <c>Location</c> trả về dạng CHUỖI đúng như tầng trên cần — kiểu cột gốc
    /// (<c>DirectionId</c> số, <c>Location</c> số) không lộ ra ngoài repository, để tầng trên không phải
    /// biết cột nào là số cột nào là chuỗi.
    /// </summary>
    public readonly record struct CameraRow(
        string Id, string? Code, string? Name, string? Ip,
        int? KmNumber, int? MetNumber, string? Direction, string? LaneId, string? Location);

    /// <summary>
    /// Camera VDS theo <c>EquipmentTypeId</c>, giới hạn theo <paramref name="ids"/> (TmsEquipment.ID) nếu
    /// có khai. Danh sách nạp lấy từ <c>SysConfigData</c> nhóm <c>vds_list_equipment</c> (con lưu
    /// EquipmentId).
    ///
    /// Lọc theo ID ĐÁNG TIN hơn theo Code: ID là khoá chính, không đổi; Code người vận hành sửa được và
    /// có thể trùng. Vẫn KHÔNG lọc IP trong câu lệnh để tầng trên đếm được camera thiếu IP rồi cảnh báo.
    /// </summary>
    public async Task<List<CameraRow>> QueryVdsCamerasByIdsAsync(
        IReadOnlyCollection<string> ids, CancellationToken ct = default)
    {
        var typeId = _config.Current.Catalog.VdsEquipmentTypeId;
        var idList = ids.ToList();

        if (string.IsNullOrWhiteSpace(typeId) || idList.Count == 0) return new List<CameraRow>();

        var rows = await Client.Queryable<TmsEquipment>()
            .Where(e => e.EquipmentTypeId == typeId && e.IsDelete == null && e.TenantId == TenantId)
            .Where(e => idList.Contains(e.ID))
            .Select(e => new
            {
                e.ID, e.Code, e.Name, e.Ip,
                e.KmNumber, e.MetNumber, e.DirectionId, e.LaneId, e.Location,
            })
            .ToListAsync();

        return rows
            .Select(r => new CameraRow(
                r.ID, r.Code, r.Name, r.Ip, r.KmNumber, r.MetNumber,
                r.DirectionId?.ToString(), r.LaneId, r.Location?.ToString()))
            .ToList();
    }

    /// <summary>Một dòng cấu hình vận hành <c>VdsConfig</c>.</summary>
    public sealed class OpConfigRow
    {
        public string? Code { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>Cấu hình vận hành đang bật, chưa xoá, đúng nhóm và tenant của worker.</summary>
    public Task<List<OpConfigRow>> QueryOpConfigAsync(string groupCode, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(groupCode)
            ? Task.FromResult(new List<OpConfigRow>())
            : VdsCatalogQueries.OpConfig(Client, groupCode, TenantId).ToListAsync(ct);

    // KienLT - 31/10/2026: tham số QuestDB HA thuộc tenant sở hữu cấu hình HA của host, có thể khác tenant của worker.
    /// <summary>Cấu hình vận hành đang bật, chưa xoá của nhóm và tenant chỉ định.</summary>
    public Task<List<OpConfigRow>> QueryOpConfigAsync(string groupCode, string tenantId, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(groupCode) || string.IsNullOrWhiteSpace(tenantId)
            ? Task.FromResult(new List<OpConfigRow>())
            : VdsCatalogQueries.OpConfig(Client, groupCode, tenantId).ToListAsync(ct);

    /// <summary>Liên kết camera ↔ vùng từ <c>TmsZoneEquipment</c> (quan hệ n-n, không có UNIQUE).</summary>
    /// <summary>Tên vùng theo danh sách ID (rỗng ⇒ lấy hết). Dùng cho harness, KHÔNG dùng trong pipeline.</summary>
    public async Task<List<ZoneRow>> QueryZonesAsync(
        IReadOnlyCollection<string> zoneIds, CancellationToken ct = default)
    {
        var ids = zoneIds.ToList();
        var rows = await Client.Queryable<TmsZone>()
            .Where(z => z.IsDelete == null)
            .WhereIF(ids.Count > 0, z => ids.Contains(z.ID))
            .Select(z => new { z.ID, z.Name, z.Code })
            .ToListAsync();

        return rows.Select(r => new ZoneRow(r.ID, r.Name, r.Code)).ToList();
    }

    public readonly record struct ZoneLinkRow(string? ZoneId, string? EquipmentId, DateTime? CreateTime);

    /// <summary>
    /// Toàn bộ liên kết camera ↔ vùng. Giữ đúng phạm vi lọc của camera: chỉ loại bản ghi xoá mềm,
    /// KHÔNG lọc <c>TenantId</c>.
    /// </summary>
    public async Task<List<ZoneLinkRow>> QueryZoneLinksAsync(CancellationToken ct = default)
    {
        var rows = await Client.Queryable<TmsZoneEquipment>()
            .Where(z => z.IsDelete == null)
            .Select(z => new { z.ZoneId, z.EquipmentId, z.CreateTime })
            .ToListAsync();

        return rows.Select(r => new ZoneLinkRow(r.ZoneId, r.EquipmentId, r.CreateTime)).ToList();
    }

    /// <summary>
    /// Ngưỡng tốc độ khai trên một vùng (km/h; null = vùng không khai giá trị đó) kèm hướng mặc định của vùng
    /// (<c>TmsZone.LaneId</c> — cột này dùng cho direction). <paramref name="LaneId"/> để cuối, có mặc định,
    /// để snapshot cũ (chưa có trường này) vẫn đọc được.
    /// </summary>
    public readonly record struct ZoneSpeedRow(
        string? ZoneId, double? MinSpeed, double? MaxSpeed, string? LaneId = null);

    /// <summary>
    /// Ngưỡng và hướng của MỌI vùng chưa xoá. Đọc cả vùng không khai min/max vì hướng của vùng
    /// (<c>LaneId</c>) quyết định mặc định <c>vds_route_{hướng}_*</c> cho các giá trị vùng không khai.
    /// </summary>
    public async Task<List<ZoneSpeedRow>> QueryZoneSpeedsAsync(CancellationToken ct = default)
    {
        var rows = await Client.Queryable<TmsZone>()
            .Where(z => z.IsDelete == null)
            .Select(z => new { z.ID, z.LaneId, z.MinSpeed, z.MaxSpeed })
            .ToListAsync(ct);

        return rows
            .Select(r => new ZoneSpeedRow(r.ID, (double?)r.MinSpeed, (double?)r.MaxSpeed, r.LaneId))
            .ToList();
    }

    /// <summary>Một camera XEM ĐƯỢC của một vùng: liên kết vùng ↔ thiết bị đã lọc theo module.</summary>
    /// <summary>Một vùng trong danh mục <c>TmsZone</c> — chỉ để hiển thị tên cho người dùng.</summary>
    public readonly record struct ZoneRow(string? Id, string? Name, string? Code);

    public readonly record struct ZoneCameraRow(string? ZoneId, string? EquipmentId, string? Code);

    /// <summary>
    /// Camera thuộc MODULE <paramref name="moduleId"/> gán vào từng vùng — dùng để điền thiết bị
    /// stream được cho sự cố thay cho camera VDS.
    ///
    /// Lọc theo module chứ không theo <c>EquipmentTypeId</c>: module CCTV gồm nhiều loại
    /// (<c>CCTV</c>/<c>FIX</c>/<c>PTZ</c>) và <c>TmsEquipment</c> KHÔNG có cột <c>ModuleId</c>, nên phải
    /// join <c>TmsEquipmentType</c>. Chỉ lấy thiết bị đang bật: thiết bị tắt thì FE cũng không mở hình được.
    /// </summary>
    public async Task<List<ZoneCameraRow>> QueryZoneCamerasByModuleAsync(
        string moduleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(moduleId)) return new List<ZoneCameraRow>();

        var rows = await Client.Queryable<TmsZoneEquipment>()
            .InnerJoin<TmsEquipment>((ze, e) => ze.EquipmentId == e.ID)
            .InnerJoin<TmsEquipmentType>((ze, e, t) => e.EquipmentTypeId == t.ID)
            .Where((ze, e, t) => ze.IsDelete == null && e.IsDelete == null && t.IsDelete == null)
            .Where((ze, e, t) => t.ModuleId == moduleId)
            .Where((ze, e, t) => e.Status == BaseEnums.StatusEnum.Enable)
            .Select((ze, e, t) => new { ze.ZoneId, EquipmentId = e.ID, e.Code })
            .ToListAsync();

        return rows.Select(r => new ZoneCameraRow(r.ZoneId, r.EquipmentId, r.Code)).ToList();
    }

    /// <summary>Một giá trị trong danh mục cấu hình <c>SysConfigData</c>.</summary>
    public sealed class ConfigDataRow
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>Một cấu hình tự động TMS còn hiệu lực: mã loại sự kiện và danh sách nguồn cho phép.</summary>
    public sealed class AutomationConfigRow
    {
        public string? EventTypeCode { get; set; }
        public string? AllowedSources { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Loại sự kiện VDS cần gửi sang TMS (lập lịch tự động hoặc tự kết thúc khi không ai nhận), SUY TỪ
    ///              cấu hình tự động của TMS thay cho nhóm VdsConfig <c>vds_automation_event</c> khai tay (bỏ
    ///              17/10/2026 — hai nơi cấu hình phải khớp nhau, lệch là mất tự động hoặc mất tự kết thúc).
    ///
    ///              Lấy RỘNG hơn điều kiện thật của TMS (chỉ cần cấu hình còn hiệu lực, bật một trong hai chức năng và
    ///              cho nguồn VDS): TMS vẫn là nơi quyết định cuối, ở đây chỉ bỏ các loại chắc chắn không cần gọi để
    ///              khỏi tốn một transaction khoá dòng sự cố cho mỗi sự cố VDS.
    /// Created date: 17/10/2026
    /// </summary>
    public async Task<List<string>> QueryAutomationEventTypesAsync(CancellationToken ct = default)
    {
        var rows = await VdsCatalogQueries.AutomationConfigs(Client, TenantId).ToListAsync(ct).ConfigureAwait(false);
        return rows
            .Where(r => Modules.TMS.Core.Constants.IncidentAutomationSourceConst.IsAllowed(
                r.AllowedSources, Modules.TMS.Core.Constants.IncidentAutomationSourceConst.Vds))
            .Select(r => (r.EventTypeCode ?? string.Empty).Trim())
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Nhóm vds_ lấy cấu hình đang bật theo tenant; nhóm khác giữ danh mục dùng chung,
    /// không thêm điều kiện tenant/status ngoài contract cũ.
    /// </summary>
    public Task<List<ConfigDataRow>> QueryConfigDataAsync(string configTypeCode, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(configTypeCode)
            ? Task.FromResult(new List<ConfigDataRow>())
            : configTypeCode.StartsWith("vds_", StringComparison.OrdinalIgnoreCase)
                ? VdsCatalogQueries.VdsConfigData(Client, configTypeCode, TenantId).ToListAsync(ct)
                : VdsCatalogQueries.SharedConfigData(Client, configTypeCode).ToListAsync(ct);

    /// <summary>
    /// Một dòng đoạn A→B trong danh mục <c>TmsTrafficSegment</c> (khoảng cách khai tay).
    /// </summary>
    public readonly record struct SegmentRow(
        string Id, string? Name, string? FromEquipmentId, string? ToEquipmentId,
        string? Direction, int? DistanceMeter, int? MinTravelSeconds, int? MaxTravelSeconds, string? Status);

    /// <summary>
    /// Danh mục đoạn A→B đang hoạt động: chỉ bản ghi chưa xoá mềm và đủ hai đầu camera. Việc loại bỏ đoạn
    /// <c>DistanceMeter &lt;= 0</c> để cho <c>SegmentManager</c> làm (cùng chỗ ghi cảnh báo), giữ repository
    /// thuần đọc.
    ///
    /// Giữ đúng phạm vi lọc của các danh mục khác: chỉ <c>IsDelete IS NULL</c>, KHÔNG lọc <c>TenantId</c>.
    /// </summary>
    public async Task<List<SegmentRow>> QuerySegmentsAsync(CancellationToken ct = default)
    {
        var rows = await Client.Queryable<TmsTrafficSegment>()
            .Where(s => s.IsDelete == null && s.Status == "1")
            .Where(s => s.FromEquipmentId != null && s.ToEquipmentId != null)
            .Select(s => new
            {
                s.ID, s.Name, s.FromEquipmentId, s.ToEquipmentId,
                s.Direction, s.DistanceMeter, s.MinTravelSeconds, s.MaxTravelSeconds, s.Status,
            })
            .ToListAsync();

        return rows
            .Select(s => new SegmentRow(
                s.ID, s.Name, s.FromEquipmentId, s.ToEquipmentId,
                s.Direction, s.DistanceMeter, s.MinTravelSeconds, s.MaxTravelSeconds, s.Status))
            .ToList();
    }
}
