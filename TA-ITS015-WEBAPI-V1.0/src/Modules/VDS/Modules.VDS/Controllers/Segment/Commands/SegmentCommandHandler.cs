using Microsoft.Extensions.Logging;
using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.CfgSystem.Core.Refresh;
using Modules.TMS.Core.Entities;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Core.Dto;
using Modules.VDS.Controllers.Segment.Dto;
using Modules.VDS.Infrastructure;
using Modules.VDS.Infrastructure.Services.Segment;
using Shared.Infrastructure.Extensions;
using Shared.DTO.Enums;

namespace Modules.VDS.Controllers.Segment.Commands;

/// <summary>
/// Author: KienLT
/// Description: Command handler thêm/sửa/xóa đoạn tuyến VDS.
///
/// <para><b>Tách Add và Update từ 22/09/2026.</b> Trước đó một nhánh <c>Save</c> duy nhất phân biệt bằng
/// <c>ID</c> rỗng hay không, nên không tầng nào nói được "ID bắt buộc khi sửa" và nhánh sửa chỉ báo lỗi khi
/// truy DB không thấy bản ghi.</para>
///
/// <para><b>Từ 23/09/2026 map bằng <c>Adapt</c> nguyên vẹn</b> thay cho hàm đổ field tường minh: input đã kế
/// thừa entity nên không còn gì để đổ tay. Cột hệ thống do <c>VdsSystemColumns</c> giữ, không do DTO cắt
/// field.</para>
///
/// <para>Luật NGHIỆP VỤ dùng chung cho cả hai nhánh nằm ở <see cref="EnsureUsableAsync"/>: camera đầu khác
/// camera cuối, thời gian di chuyển min ≤ max, hai camera tồn tại, và đoạn A→B chưa có. Giữ ở handler vì cần
/// DB và vì handler còn được gọi qua message bus, nơi FluentValidation không chạy.</para>
/// Created date: 16/09/2026
/// </summary>
public class SegmentCommandHandler : IWolverineHandler
{
    private readonly BaseRepository<TmsTrafficSegment> _repo;
    private readonly BaseRepository<TmsEquipment> _equipment;
    private readonly BaseRepository<VdsConfig> _defaultsRepo;
    private readonly RefreshModifyService _refreshModify;
    private readonly ILogger<SegmentCommandHandler> _logger;
    private readonly VdsSegmentGenerateService _generate;

    public SegmentCommandHandler(BaseRepository<TmsTrafficSegment> repo, BaseRepository<TmsEquipment> equipment,
        BaseRepository<VdsConfig> defaultsRepo, RefreshModifyService refreshModify,
        ILogger<SegmentCommandHandler> logger, VdsSegmentGenerateService generate)
    {
        _repo = repo;
        _equipment = equipment;
        _defaultsRepo = defaultsRepo;
        _refreshModify = refreshModify;
        _logger = logger;
        _generate = generate;
    }

    /// <summary>Handler chỉ khai báo cái gì vừa đổi; route 'catalog:segment' trong SysConfigData quyết định mã refresh.</summary>
    private async Task<Dictionary<string, bool>> NotifyAsync(string? key, string action)
        => (await _refreshModify.NotifyAsync(new RefreshModifyRequest("catalog", "segment", key, action))).Transport;

    /// <summary>Thêm một đoạn tuyến.</summary>
    public async Task<SegmentSaveOutput> HandleAsync(AddSegmentInput command)
    {
        var row = await NormalizeAsync(VdsSystemColumns.Reset(command.Adapt<TmsTrafficSegment>()));
        await EnsureUsableAsync(row, currentId: null);
        await _repo.InsertWithDiffLogAsync(row);
        return new SegmentSaveOutput { ID = row.ID, Saved = true, Refresh = await NotifyAsync(row.ID, "add"), Applied = null };
    }

    /// <summary>Cập nhật một đoạn tuyến.</summary>
    public async Task<SegmentSaveOutput> HandleAsync(UpdateSegmentInput command)
    {
        // ID rỗng đã bị UpdateSegmentValidator chặn ở đường HTTP; kiểm lại ở đây để đường message bus không hở.
        if (string.IsNullOrWhiteSpace(command.ID)) throw Oops.Oh(BaseMsg.BaseValidation.IdRequired);

        var row = await _repo.AsQueryable().Where(x => x.ID == command.ID && x.IsDelete == null).FirstAsync()
            ?? throw Oops.Oh(BaseMsg.Vds.Exception.SegmentNotFound);

        var stored = VdsSystemColumns.Snapshot(row);
        command.Adapt(row);
        await NormalizeAsync(VdsSystemColumns.KeepFrom(row, stored));

        await EnsureUsableAsync(row, row.ID);
        await _repo.UpdateWithDiffLogAsync(row);
        return new SegmentSaveOutput { ID = row.ID, Saved = true, Refresh = await NotifyAsync(row.ID, "update"), Applied = null };
    }

    /// <summary>Xóa mềm một đoạn tuyến.</summary>
    public async Task<VdsSaveOutput> HandleAsync(DeleteSegmentInput command)
    {
        var row = await _repo.AsQueryable().Where(x => x.ID == command.ID && x.IsDelete == null).FirstAsync()
            ?? throw Oops.Oh(BaseMsg.Vds.Exception.SegmentNotFound);
        await _repo.SoftDeleteAsync<TmsTrafficSegment>(row);
        return new VdsSaveOutput { Saved = true, Refresh = await NotifyAsync(command.ID, "delete"), Applied = null };
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Ghi kết quả tự sinh đoạn A→B vào TmsTrafficSegment và gửi refresh (refresh do
    /// <see cref="VdsSegmentGenerateService.SyncAsync"/> tự bắn). Chuyển từ SegmentGenerateHandler ngày 13/10/2026.
    /// Created date: 29/09/2026
    /// </summary>
    public Task<SegmentGenerateOutput> HandleAsync(SegmentSyncInput command) => _generate.SyncAsync();

    /// <summary>
    /// Chuẩn hoá entity đã map xong: cắt khoảng trắng, điền biên mặc định từ VdsConfig, khép
    /// <c>Status</c> về đúng hai giá trị của cột.
    ///
    /// <para>Chạy cho CẢ đường HTTP và đường message bus — FluentValidation chỉ tự chạy cho tham số action.</para>
    /// </summary>
    private async Task<TmsTrafficSegment> NormalizeAsync(TmsTrafficSegment row)
    {
        row.Name = row.Name?.Trim();
        if (row.MinTravelSeconds is null || row.MaxTravelSeconds is null)
        {
            var defaults = await ReadDefaultsAsync();
            row.MinTravelSeconds ??= defaults.MinSeconds;
            row.MaxTravelSeconds ??= defaults.MaxSeconds;
        }
        row.Status = row.Status switch
        {
            null or "" => TmsTrafficSegment.StatusEnabled,
            TmsTrafficSegment.StatusEnabled => TmsTrafficSegment.StatusEnabled,
            TmsTrafficSegment.StatusDisabled => TmsTrafficSegment.StatusDisabled,
            _ => throw Oops.Oh(BaseMsg.BaseException.IncorrectAcceptValue),
        };
        return row;
    }

    private async Task<(int MinSeconds, int MaxSeconds)> ReadDefaultsAsync()
    {
        try
        {
            var rows = await _defaultsRepo.AsQueryable()
                .Where(x => x.IsDelete == null && x.GroupCode == VdsConfigConst.Group.ConfigDefault
                    && x.Status == BaseEnums.StatusEnum.Enable
                    && (x.Code == VdsConfigConst.Code.JourneyMinTravelDefault
                        || x.Code == VdsConfigConst.Code.JourneyMaxTravelDefault))
                .Select(x => new { x.Code, x.Value })
                .ToListAsync();
            var minRaw = rows.FirstOrDefault(x => x.Code == VdsConfigConst.Code.JourneyMinTravelDefault)?.Value;
            var maxRaw = rows.FirstOrDefault(x => x.Code == VdsConfigConst.Code.JourneyMaxTravelDefault)?.Value;
            var min = VdsJourneyTravelConst.ParseSeconds(minRaw);
            var max = VdsJourneyTravelConst.ParseSeconds(maxRaw);
            if ((minRaw is not null && min is null)
                || (maxRaw is not null && max is null)
                || (min ?? VdsJourneyTravelConst.CodeMinSeconds)
                    > (max ?? VdsJourneyTravelConst.CodeMaxSeconds))
                _logger.LogWarning("[Segment] Biên thời gian mặc định trong VdsConfig không hợp lệ; dùng giá trị dự phòng của code.");
            return VdsJourneyTravelConst.Resolve(minRaw, maxRaw);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Segment] Không đọc được biên thời gian mặc định từ VdsConfig; dùng giá trị dự phòng của code.");
            return (VdsJourneyTravelConst.CodeMinSeconds, VdsJourneyTravelConst.CodeMaxSeconds);
        }
    }

    /// <summary>
    /// Luật nghiệp vụ dùng chung cho thêm và sửa.
    /// </summary>
    /// <param name="row">Entity đã chuẩn hoá — so trên giá trị SẼ GHI, không phải trên body thô.</param>
    /// <param name="currentId">ID bản ghi đang sửa; <c>null</c> khi thêm. Dùng để tự loại mình khỏi phép trùng.</param>
    private async Task EnsureUsableAsync(TmsTrafficSegment row, string? currentId)
    {
        if (row.FromEquipmentId == row.ToEquipmentId || row.MinTravelSeconds > row.MaxTravelSeconds
            || row.MinTravelSeconds is < VdsJourneyTravelConst.CodeMinSeconds or > VdsJourneyTravelConst.MaxSupportedSeconds
            || row.MaxTravelSeconds is < VdsJourneyTravelConst.CodeMinSeconds or > VdsJourneyTravelConst.MaxSupportedSeconds)
            throw Oops.Oh(BaseMsg.Vds.Exception.SegmentEndpointsInvalid);

        // Khoảng cách là dữ liệu bắt buộc; đường HTTP đã chặn ở validator, đường bus chặn ở đây.
        if (row.DistanceMeter is null or <= 0) throw Oops.Oh(BaseMsg.Vds.Exception.SegmentEndpointsInvalid);

        var ids = new[] { row.FromEquipmentId, row.ToEquipmentId };
        if (await _equipment.AsQueryable().Where(x => x.IsDelete == null && ids.Contains(x.ID)).CountAsync() != 2)
            throw Oops.Oh(BaseMsg.Vds.Exception.CameraNotFound);

        if (await _repo.AsQueryable().Where(x => x.IsDelete == null && x.FromEquipmentId == row.FromEquipmentId
                && x.ToEquipmentId == row.ToEquipmentId && x.ID != currentId).AnyAsync())
            throw Oops.Oh(BaseMsg.Vds.Exception.SegmentExist);
    }
}
