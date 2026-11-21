using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.CfgSystem.Core.Refresh;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Controllers.Configuration.Dto;
using Modules.VDS.Infrastructure;
using Modules.VDS.Infrastructure.Services.Config;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Modules.VDS.Core.Constants;
using Shared.DTO.Constants.Application;
using Modules.VDS.Infrastructure.Services.Config.Rules;

namespace Modules.VDS.Controllers.Configuration.Commands;

/// <summary>
/// Author: KienLT
/// Description: Command handler lưu tham số cấu hình VDS. Giữ nguyên toàn bộ
/// validation (speed pair, chu kỳ thống kê, thư mục media, danh sách camera...).
/// Created date: 16/09/2026
/// </summary>
public class ConfigurationCommandHandler : IWolverineHandler
{
    private readonly BaseRepository<VdsConfig> _repo;
    private readonly BaseCacheService _cache;
    private readonly RefreshModifyService _refreshModify;
    private readonly VdsConfigWriteService _writeRules;

    public ConfigurationCommandHandler(BaseRepository<VdsConfig> repo, BaseCacheService cache,
        RefreshModifyService refreshModify, VdsConfigWriteService writeRules)
    {
        _repo = repo;
        _cache = cache;
        _refreshModify = refreshModify;
        _writeRules = writeRules;
    }

    /// <summary>
    /// Handler chỉ khai báo cái gì vừa đổi; route 'vdsconfig:{groupCode}' trong SysConfigData quyết định mã refresh.
    /// Scope tách vds_config (tham số vận hành) và vds_config_default (giá trị rơi về) để hai nhóm có thể có mã riêng.
    /// </summary>
    private async Task<Dictionary<string, bool>> NotifyAsync(string groupCode, string? key, string action)
        => (await _refreshModify.NotifyAsync(new RefreshModifyRequest("vdsconfig", groupCode, key, action))).Transport;

    /// <summary>
    /// Phát lại bản tin refresh theo mã đã khai — dùng khi lần phát tự động sau khi ghi bị lỗi transport.
    ///
    /// <para>Đây là phát THẲNG theo mã, không đi qua rule <c>source:scope</c>, nên giữ ở command handler
    /// riêng chứ không gộp vào <see cref="NotifyAsync"/>. Payload NATS vẫn là số như trước.</para>
    /// </summary>
    public async Task<ConfigurationRefreshOutput> HandleAsync(ConfigurationRefreshInput command)
    {
        var tenantId = await _repo.AsQueryable().Select(x => x.TenantId).FirstAsync();
        if (string.IsNullOrWhiteSpace(tenantId))
            return new ConfigurationRefreshOutput { Refresh = ItsRefresh.Send(command.Code), Applied = null };
        var result = await new VdsRefreshReceiptService().SendAsync(command.Code, tenantId);
        return new ConfigurationRefreshOutput { Refresh = result.Refresh, Applied = result.Applied, Services = result.Services };
    }

    public async Task<ConfigurationSaveOutput> HandleAsync(VdsConfigValueInput command)
    {
        var code = command.Code.Trim(); var value = command.Value.Trim();

        // Feature này CỐ Ý chỉ nhận một tập mã hẹp — đó là phạm vi của mặt tiền dành cho người vận hành, không
        // phải một luật kiểm tra. Mã ngoài tập bị từ chối thẳng; CRUD mọi nhóm nằm ở API vdsConfig.
        var group = ResolveGroup(code) ?? throw Oops.Oh(BaseMsg.Vds.Exception.ParameterNotSupported);

        var row = await _repo.AsQueryable().Where(x => x.IsDelete == null && x.GroupCode == group && x.Code == code).FirstAsync();

        // Toàn bộ LUẬT nằm ở service dùng chung với API vdsConfig. Trước 22/09/2026 chỗ này tự kiểm và lệch
        // ba điểm: thời lượng chỉ khớp regex nên 31d lưu được rồi bên đọc im lặng rơi về mặc định; tốc độ là
        // int 0..300 trong khi đường kia là double ≥ 0; và cặp min/max không lọc Status nên dòng đã tắt vẫn
        // chặn được cấu hình đang chạy.
        var name = row?.Name ?? code;
        value = await _writeRules.ValidateAsync(group, code, value, name,
            row?.Status ?? BaseEnums.StatusEnum.Enable, row?.ID) ?? value;

        if (row == null)
        {
            row = new VdsConfig { Code = code, Name = name, GroupCode = group, Value = value, Status = BaseEnums.StatusEnum.Enable };
            await _repo.InsertWithDiffLogAsync(row);
        }
        else { row.Value = value; await _repo.UpdateWithDiffLogAsync(row); }
        _cache.RemoveByPrefixKey(CacheConst.Vds.OpConfig);
        return new ConfigurationSaveOutput { Saved = true, Refresh = await NotifyAsync(group, code, "save"), Applied = null };
    }

    /// <summary>
    /// Nhóm cấu hình của một mã mà mặt tiền vận hành cho phép sửa, hoặc <c>null</c> nếu mã ngoài phạm vi.
    ///
    /// <para>Chỉ quyết định PHẠM VI và NHÓM. Việc giá trị có hợp lệ hay không do
    /// <c>VdsConfigWriteService</c> trả lời, nên hai API không thể lệch luật nữa.</para>
    /// </summary>
    private static string? ResolveGroup(string code)
    {
        if (code is VdsConfigConst.Code.SkipDefault or VdsConfigConst.Code.DedupDefault
            or VdsConfigConst.Code.JourneyMinTravelDefault
            or VdsConfigConst.Code.JourneyMaxTravelDefault)
            return VdsConfigConst.Group.ConfigDefault;

        if (VdsConfigRules.IsRouteSpeedCode(code)
            || code == VdsConfigConst.Code.StatisticPeriodMinutes
            || code == VdsConfigConst.Code.MediaStorageDirectory
            || code.StartsWith(VdsConfigConst.Pattern.CountDirectionEquipmentPrefix, StringComparison.Ordinal)
            || code.StartsWith(VdsConfigConst.Pattern.SpeedDirectionEquipmentPrefix, StringComparison.Ordinal))
            return VdsConfigConst.Group.Config;

        return null;
    }
}
