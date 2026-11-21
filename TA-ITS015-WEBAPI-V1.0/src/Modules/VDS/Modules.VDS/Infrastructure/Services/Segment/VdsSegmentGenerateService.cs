using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.CfgSystem.Core.Refresh;
using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.Segment.Dto;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Helper;
using Modules.VDS.Infrastructure.Services.Segment.Rules;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using BaseMsg = Modules.VDS.Core.Exceptions.BaseMsg;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Infrastructure.Services.Segment;

/// <summary>
/// Author: KienLT
/// Description: Tự sinh đoạn A→B từ km + tuyến + hướng của camera VDS và cấu hình hình học
/// (nhóm <c>vds_segment</c>: <c>vds_km_dir_…</c>, <c>vds_junction_…</c>, <c>vds_turn_…</c>), rồi đồng bộ vào <c>TmsTrafficSegment</c>.
/// Luật tính nằm ở <see cref="VdsSegmentTopologyRules"/>; lớp này chỉ đọc DB và ghi kết quả.
/// Worker không đổi: nó vẫn đọc <c>TmsTrafficSegment</c> sau khi nhận refresh.
/// Created date: 29/09/2026
/// </summary>
public class VdsSegmentGenerateService : IScoped
{
    private static readonly string[] TopologyGroups =
        { VdsConfigConst.Group.Segment };

    private readonly BaseRepository<TmsTrafficSegment> _segments;
    private readonly BaseRepository<VdsConfig> _config;
    private readonly IMessageBus _messageBus;
    private readonly IStringLocalizer _localizer;
    private readonly RefreshModifyService _refreshModify;

    public VdsSegmentGenerateService(BaseRepository<TmsTrafficSegment> segments, BaseRepository<VdsConfig> config,
        IMessageBus messageBus, RefreshModifyService refreshModify, IStringLocalizer localizer)
    {
        _segments = segments;
        _config = config;
        _messageBus = messageBus;
        _localizer = localizer;
        _refreshModify = refreshModify;
    }

    /// <summary>Xem trước: tính đoạn và so với DB, không ghi gì. Lỗi cấu hình vẫn trả về để người vận hành sửa.</summary>
    public async Task<SegmentGenerateOutput> PreviewAsync()
    {
        var (items, plan, warnings) = await BuildAsync();
        return ToOutput(items, plan, warnings, saved: false);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Sơ đồ tuyến cho trang tốc độ trung bình (<c>GET api/vds/vdsSegment/Topology</c>). Chỉ đọc.
    /// <para>Camera và cấu hình đọc bằng ĐÚNG luật của bộ sinh đoạn (<see cref="ReadCamerasAsync"/> →
    /// <see cref="VdsSegmentTopologyRules.SelectCameras"/>, <see cref="ReadTopologyConfigAsync"/> theo
    /// <see cref="TopologyGroups"/>) nên sơ đồ và đoạn tự sinh hiểu cấu hình như nhau. Đoạn là mọi
    /// <c>TmsTrafficSegment</c> chưa xoá — kể cả đang tắt, vì hành trình cũ vẫn trỏ tới — theo thứ tự của <c>GetList</c>.
    /// Tên tuyến theo danh mục <see cref="VdsConfigConst.ZoneLocationTypeCode"/>, hỏi CfgSystem qua bus. Luật dựng: <see cref="VdsSegmentTopologyRules.BuildTopology"/>.</para>
    /// <para>Bổ sung đoạn worker tự sinh chưa có trong DB — hành trình của chúng mang
    /// <c>SegmentId</c> = mã tất định. Tính bằng <see cref="VdsSegmentTopologyRules.Plan"/> trên CHÍNH camera/cấu hình vừa
    /// đọc, như <c>AutoSegmentManager</c> của worker.</para>
    /// Created date: 20/10/2026
    /// </summary>
    public async Task<SegmentTopologyOutput> TopologyAsync()
    {
        var warnings = new List<string>();
        var cameraNames = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var cameras = await ReadCamerasAsync(warnings, cameraNames);
        var config = await ReadTopologyConfigAsync();
        var segments = await _segments.AsQueryable().Where(x => x.IsDelete == null)
            .OrderBy(x => x.OrderNo).OrderBy(x => x.ID)
            .ToListAsync();
        var routeCatalog = (await _messageBus.InvokeAsync<List<CfgConfigDataNameOutput>>(
                new CfgConfigDataNamesInput { TypeCode = VdsConfigConst.ZoneLocationTypeCode }))
            .Select(x => new VdsSegmentTopologyRules.NameRow(x.Code, x.Name)).ToList();
        var generated = VdsSegmentTopologyRules.Plan(cameras, config);
        return VdsSegmentTopologyRules.BuildTopology(cameras, cameraNames, config, segments, routeCatalog, warnings, generated, Localize);
    }

    /// <summary>
    /// Đồng bộ: ghi insert/update/disable trong một transaction rồi gửi refresh một lần.
    ///
    /// <para><b>Có lỗi cấu hình thì KHÔNG ghi gì</b> (<see cref="VdsBaseHelper.SegmentTopologyHp.TopoPlan.HasErrors"/>): kết quả khi đó có thể
    /// thiếu đoạn (ví dụ nút J01 hỏng làm mất 4 đoạn rẽ), và Diff sẽ coi những đoạn thiếu là cần TẮT.</para>
    ///
    /// <para><b>Đoạn thêm mới lấy ID = mã tất định</b> (<c>S:VDS-01&gt;VDS-03</c>) — đúng giá trị worker đang dùng
    /// làm SegmentId khi tự sinh. Nhờ vậy chuyển từ đoạn tự sinh sang đoạn trong DB không đổi khoá hành trình,
    /// <c>EventId</c> sự cố hay cursor đối soát. ID là khoá chính nên hai request Sync cùng thêm một cặp thì
    /// request sau lỗi khoá, không sinh hai dòng.</para>
    /// </summary>
    public async Task<SegmentGenerateOutput> SyncAsync()
    {
        var (items, plan, warnings) = await BuildAsync();
        if (plan.HasErrors) return ToOutput(items, plan, warnings, saved: false);

        var changes = items.Where(x => x.Action is VdsSegmentTopologyRules.SyncAction.Insert
            or VdsSegmentTopologyRules.SyncAction.Update or VdsSegmentTopologyRules.SyncAction.Disable).ToList();

        var db = _segments.Context;
        var insertIds = changes.Where(x => x.Action == VdsSegmentTopologyRules.SyncAction.Insert).Select(x => x.Target.ID).ToList();
        // Đoạn máy sinh đã từng bị XOÁ MỀM vẫn giữ khoá chính = mã tất định ⇒ không INSERT lại được; khôi phục
        // chính bản ghi đó (cùng ID ⇒ cùng khoá hành trình với lịch sử).
        var deleted = insertIds.Count == 0 ? new List<TmsTrafficSegment>()
            : await db.Queryable<TmsTrafficSegment>().Where(x => insertIds.Contains(x.ID) && x.IsDelete != null).ToListAsync();
        await db.Ado.BeginTranAsync();
        try
        {
            foreach (var item in changes)
            {
                if (item.Action == VdsSegmentTopologyRules.SyncAction.Insert)
                {
                    var id = item.Target.ID;
                    var restore = deleted.FirstOrDefault(x => x.ID == id);
                    if (restore is not null)
                    {
                        restore.IsDelete = null;
                        restore.Code = item.Target.Code;
                        restore.Name = item.Target.Name;
                        restore.FromEquipmentId = item.Target.FromEquipmentId;
                        restore.ToEquipmentId = item.Target.ToEquipmentId;
                        restore.Direction = item.Target.Direction;
                        restore.DistanceMeter = item.Target.DistanceMeter;
                        restore.MinTravelSeconds = item.Target.MinTravelSeconds;
                        restore.MaxTravelSeconds = item.Target.MaxTravelSeconds;
                        restore.Status = item.Target.Status;
                        restore.OrderNo = item.Target.OrderNo;
                        await _segments.UpdateWithDiffLogAsync(restore);
                        continue;
                    }
                    var row = VdsSystemColumns.Reset(item.Target);
                    row.ID = id;
                    await _segments.InsertWithDiffLogAsync(row);
                    // Tầng persistence chỉ được cấp ID khi ID rỗng; nếu nó ghi đè thì định danh đoạn lệch với
                    // worker — dừng hẳn còn hơn ghi dữ liệu sai.
                    if (row.ID != id)
                        throw Oops.Oh(BaseMsg.Vds.Exception.SegmentIdOverwritten, id, row.ID);
                    continue;
                }

                // Sửa trên chính entity đọc từ DB để giữ nguyên cột hệ thống/audit.
                var stored = item.Existing!;
                stored.Code = item.Target.Code;
                stored.Name = item.Target.Name;
                stored.Direction = item.Target.Direction;
                stored.DistanceMeter = item.Target.DistanceMeter;
                stored.MinTravelSeconds = item.Target.MinTravelSeconds;
                stored.MaxTravelSeconds = item.Target.MaxTravelSeconds;
                stored.Status = item.Target.Status;
                stored.OrderNo = item.Target.OrderNo;
                await _segments.UpdateWithDiffLogAsync(stored);
            }
            await db.Ado.CommitTranAsync();
        }
        catch
        {
            await db.Ado.RollbackTranAsync();
            throw;
        }

        var output = ToOutput(items, plan, warnings, saved: true);
        if (changes.Count > 0)
            output.Refresh = (await _refreshModify.NotifyAsync(
                new RefreshModifyRequest("catalog", "segment", null, "sync"))).Transport;
        return output;
    }

    /// <returns>Kết quả so khớp, kết quả sinh, và cảnh báo đã dịch (cảnh báo chọn camera đứng trước cảnh báo của bộ sinh đoạn).</returns>
    private async Task<(List<SegmentSyncItem> Items, VdsBaseHelper.SegmentTopologyHp.TopoPlan Plan, List<string> Warnings)> BuildAsync()
    {
        var warnings = new List<string>();
        var cameras = await ReadCamerasAsync(warnings);
        var config = await ReadTopologyConfigAsync();
        var plan = VdsSegmentTopologyRules.Plan(cameras, config);
        warnings.AddRange(plan.Warnings.Select(Localize));

        var existing = await _segments.AsQueryable().Where(x => x.IsDelete == null).ToListAsync();
        var items = VdsSegmentTopologyRules.Diff(plan.Segments, existing, await ReadMaxTravelDefaultAsync(),
            plan.PlausibleMaxSpeedKmh);
        return (items, plan, warnings);
    }

    /// <summary>
    /// Camera VDS theo ĐÚNG luật nạp của worker (<see cref="VdsSegmentTopologyRules.SelectCameras"/>), để Preview
    /// và đoạn worker tự sinh cho cùng kết quả.
    /// </summary>
    /// <param name="names">Khác null ⇒ điền thêm ID → <c>TmsEquipment.Name</c> (cho sơ đồ tuyến); không đổi luật chọn camera.</param>
    private async Task<List<VdsBaseHelper.SegmentTopologyHp.TopoCamera>> ReadCamerasAsync(List<string> warnings,
        Dictionary<string, string?>? names = null)
    {
        var listed = await _config.AsQueryable()
            .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable
                && x.GroupCode == VdsConfigConst.Group.ListEquipment)
            .Select(x => x.Code).ToListAsync();

        var rows = await _segments.Context.Queryable<TmsEquipment>()
            .InnerJoin<TmsEquipmentType>((e, t) => e.EquipmentTypeId == t.ID)
            .Where((e, t) => e.IsDelete == null && t.IsDelete == null && t.Code == "VDS")
            .Select((e, t) => new { e.ID, e.Code, e.Name, e.Ip, e.Location, e.DirectionId, e.KmNumber, e.MetNumber })
            .ToListAsync();
        if (names is not null)
            foreach (var r in rows.Where(r => !string.IsNullOrWhiteSpace(r.ID)))
                names.TryAdd(r.ID.Trim(), r.Name);

        return VdsSegmentTopologyRules.SelectCameras(
            rows.Select(r => new VdsSegmentTopologyRules.CameraRow(r.ID, r.Code ?? r.Name ?? r.ID, r.Ip, r.Location,
                r.DirectionId, r.KmNumber, r.MetNumber)).ToList(),
            listed, warnings, Localize);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Dịch mã thông báo và các thông báo lồng trong tham số theo ngôn ngữ request.
    /// Created date: 22/10/2026
    /// </summary>
    private string Localize(VdsSegmentTopologyMessageOutput message)
        => _localizer[message.Key, message.Arguments
            .Select(argument => argument is VdsSegmentTopologyMessageOutput nested ? Localize(nested) : argument)
            .ToArray()].Value;

    /// <summary>
    /// Dòng cấu hình hình học tuyến (nhóm <see cref="TopologyGroups"/>, đang bật, chưa xoá) — một truy vấn cho cả bộ sinh
    /// đoạn và sơ đồ tuyến.
    /// <para>Chép <see cref="TopologyGroups"/> ra biến cục bộ — SqlSugar không đọc được trường
    /// <c>private static</c> trong biểu thức (lỗi "Field TopologyGroups can't be private", làm hỏng cả Preview/Sync).</para>
    /// </summary>
    private async Task<List<VdsBaseHelper.SegmentTopologyHp.TopoConfigRow>> ReadTopologyConfigAsync()
    {
        var groups = TopologyGroups.ToList();
        return (await _config.AsQueryable()
                .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable && groups.Contains(x.GroupCode))
                .Select(x => new { x.GroupCode, x.Code, x.Value })
                .ToListAsync())
            .Select(x => new VdsBaseHelper.SegmentTopologyHp.TopoConfigRow(x.GroupCode, x.Code, x.Value)).ToList();
    }

    private async Task<int> ReadMaxTravelDefaultAsync()
    {
        var raw = await _config.AsQueryable()
            .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable
                && x.GroupCode == VdsConfigConst.Group.ConfigDefault
                && x.Code == VdsConfigConst.Code.JourneyMaxTravelDefault)
            .Select(x => x.Value).FirstAsync();
        return VdsJourneyTravelConst.Resolve(null, raw).MaxSeconds;
    }

    /// <param name="warnings">Cảnh báo đã dịch (<see cref="BuildAsync"/>); lỗi cấu hình của <paramref name="plan"/> dịch tại đây.</param>
    private SegmentGenerateOutput ToOutput(List<SegmentSyncItem> items, VdsBaseHelper.SegmentTopologyHp.TopoPlan plan,
        List<string> warnings, bool saved)
        => new()
        {
            Saved = saved,
            Warnings = warnings,
            Errors = plan.Errors.Select(Localize).ToList(),
            Items = items.Select(x => new SegmentGenerateItemOutput
            {
                Action = x.Action,
                SegmentId = string.IsNullOrEmpty(x.Target.ID) ? x.Existing?.ID : x.Target.ID,
                Code = x.Target.Code,
                Name = x.Target.Name,
                Kind = x.Plan?.Kind,
                Turn = x.Plan?.Turn,
                FromCode = x.Plan?.FromCode,
                ToCode = x.Plan?.ToCode,
                Direction = x.Target.Direction,
                DistanceMeter = x.Target.DistanceMeter,
                OldDistanceMeter = x.Existing?.DistanceMeter,
                MinTravelSeconds = x.Target.MinTravelSeconds,
                MaxTravelSeconds = x.Target.MaxTravelSeconds,
                Status = x.Target.Status,
                Formula = x.Plan?.Formula,
            }).ToList(),
        };
}
