using Modules.VDS.Infrastructure.Services.Filters.Mvc;
using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.CfgSystem.Core.Refresh;
using Modules.VDS.Infrastructure.Services.Config;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Controllers.Config.Dto;
using Shared.DTO.Constants.Application;
using Modules.VDS.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Modules.VDS.Core.Constants;
using Modules.VDS.Infrastructure.Services.Config.Rules;

namespace Modules.VDS.Controllers.Config.Commands;

/// <summary>
/// Author: KienLT
/// Description: Command handler thêm/sửa/xóa cấu hình VDS. Giữ nguyên logic
/// validation nghiệp vụ (ràng buộc secret, cặp min/max speed, mapping cỡ xe...).
///
/// <para><b>Từ 21/09/2026 đây là đường GHI DUY NHẤT của bảng <c>VdsConfig</c> cho mọi nhóm danh mục</b>
/// (API <c>vdsCatalog</c> đã bỏ). Vì vậy handler phải gánh thêm hai việc mà trước đây chỉ Catalog làm:
/// xóa cache cấu hình, và phát refresh với scope là ĐÚNG <c>GroupCode</c> của bản ghi.</para>
///
/// <para><b>Từ 23/09/2026 map bằng <c>Adapt</c> nguyên vẹn</b> thay cho hàm đổ field tường minh: input đã kế
/// thừa entity nên không còn gì để đổ tay. Cột hệ thống do <c>VdsSystemColumns</c> giữ, không do DTO cắt
/// field.</para>
/// Created date: 16/09/2026
/// </summary>
public class ConfigCommandHandler : IWolverineHandler
{
    private readonly BaseRepository<VdsConfig> _settings;
    private readonly BaseCacheService _cache;
    private readonly RefreshModifyService _refreshModify;
    private readonly Modules.VDS.Infrastructure.Services.Config.VdsConfigWriteService _writeRules;
    private readonly Modules.VDS.Infrastructure.Services.QuestHa.VdsQuestHaSettingsService _questHaSettings;

    // KienLT - 31/10/2026: thêm questHaSettings để WebAPI áp lại tham số router QuestDB HA ngay sau khi lưu.
    public ConfigCommandHandler(BaseRepository<VdsConfig> settings, BaseCacheService cache,
        RefreshModifyService refreshModify, Modules.VDS.Infrastructure.Services.Config.VdsConfigWriteService writeRules,
        Modules.VDS.Infrastructure.Services.QuestHa.VdsQuestHaSettingsService questHaSettings)
    {
        _questHaSettings = questHaSettings;
        _settings = settings;
        _cache = cache;
        _refreshModify = refreshModify;
        _writeRules = writeRules;
    }

    /// <summary>Thêm cấu hình VDS.</summary>
    public async Task<VdsConfigAddResult> HandleAsync(AddVdsConfigInput command)
    {
        var row = VdsSystemColumns.Reset(command.Adapt<VdsConfig>());
        await NormalizeAsync(row, storedCode: null, storedValue: null);
        await _settings.InsertWithDiffLogAsync(row);
        await AfterWriteAsync(row, "add");
        return new VdsConfigAddResult { Id = row.ID };
    }

    /// <summary>Cập nhật cấu hình VDS.</summary>
    public async Task<VdsConfigWriteResult> HandleAsync(UpdateVdsConfigInput command)
    {
        var row = await _settings.GetFirstAsync(x => x.ID == command.ID && x.IsDelete == null)
            ?? throw Oops.Oh(BaseMsg.Vds.Exception.ConfigNotFound);

        // Chụp trước khi Adapt: mã và giá trị cũ còn dùng cho luật "để trống thì giữ nguyên giá trị bí mật",
        // các cột audit thì phải khôi phục sau khi map.
        var storedCode = row.Code;
        var storedValue = row.Value;
        // KienLT - 31/10/2026: nhóm cũ để hook QuestDB HA biết dòng router bị đổi mã/nhóm đi nơi khác.
        var storedGroup = row.GroupCode;
        var stored = VdsSystemColumns.Snapshot(row);

        command.Adapt(row);
        VdsSystemColumns.KeepFrom(row, stored);

        await NormalizeAsync(row, storedCode, storedValue);
        var success = await _settings.UpdateWithDiffLogAsync(row) > 0;
        if (success) await AfterWriteAsync(row, "update", storedGroup, storedCode);
        return new VdsConfigWriteResult { Success = success };
    }

    /// <summary>Xóa mềm cấu hình VDS.</summary>
    public async Task HandleAsync(DeleteVdsConfigInput command)
    {
        var row = await _settings.GetFirstAsync(x => x.ID == command.ID && x.IsDelete == null)
            ?? throw Oops.Oh(BaseMsg.Vds.Exception.ConfigNotFound);
        if (row.IsSysConfig == true) throw Oops.Oh(BaseMsg.Vds.Exception.SysConfigDeleteLocked);
        // KienLT - 31/10/2026: xoá một dòng QuestDB HA không được để lại bộ tham số mà Manager sẽ từ chối.
        await _writeRules.EnsureQuestHaDeleteAsync(row.ID);
        await _settings.SoftDeleteAsync<VdsConfig>(row);
        await AfterWriteAsync(row, "delete");
    }

    /// <summary>
    /// Xóa cache và phát refresh sau khi ghi thành công.
    ///
    /// <para>Scope là <c>GroupCode</c> của chính bản ghi, không phải chuỗi cứng <c>vds_config</c> như
    /// <c>VdsRefreshAfterSaveFilter</c> làm trước đây — nhờ vậy mỗi nhóm có thể khai mã refresh riêng trong
    /// <c>SysConfigData</c> mà không phải build lại. Hiện DB chỉ có route wildcard <c>vdsconfig:*</c> nên mã
    /// ra vẫn là 2 cho mọi nhóm; đổi scope KHÔNG đổi hành vi hôm nay, chỉ mở đường cho route theo nhóm.</para>
    ///
    /// <para>Tiền tố cache chia theo nhóm giống hai handler cũ: nhóm tham số vận hành dùng
    /// <c>OpConfig</c>, các nhóm danh mục dùng <c>ConfigData</c>.</para>
    /// </summary>
    private async Task AfterWriteAsync(VdsConfig row, string action, string? storedGroup = null, string? storedCode = null)
    {
        var group = row.GroupCode ?? "";
        _cache.RemoveByPrefixKey(group is VdsConfigConst.Group.Config or VdsConfigConst.Group.ConfigDefault
            ? CacheConst.Vds.OpConfig
            : CacheConst.Vds.ConfigData);
        // Các nhóm này là nguồn danh mục snapshot sự kiện: xoá để snapshot nhận cấu hình mới ngay, không chờ hết TTL.
        if (group is VdsConfigConst.Group.EventDisplay or VdsConfigConst.Group.MappingEventType or VdsConfigConst.Group.ConfigDefault)
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot);
        await _refreshModify.NotifyAsync(new RefreshModifyRequest("vdsconfig", group, row.ID, action));
        // KienLT - 31/10/2026: Worker nhận qua refresh NATS ở trên; WebAPI tự áp phần router cho đường đọc của nó. Xét cả mã CŨ:
        // đổi một dòng router sang mã khác thì router phải quay về mặc định.
        await _questHaSettings.AfterWriteAsync(group, row.Code, storedGroup, storedCode, row.TenantId);
    }

    /// <summary>
    /// Chuẩn hoá và kiểm luật nghiệp vụ trên entity đã map xong.
    ///
    /// <para>Chạy cho CẢ đường HTTP và đường message bus — FluentValidation chỉ tự chạy cho tham số action,
    /// nên đây là lớp bảo vệ duy nhất khi handler được gọi qua bus.</para>
    /// </summary>
    /// <param name="storedCode">Mã đang lưu trong DB; <c>null</c> khi thêm mới.</param>
    /// <param name="storedValue">Giá trị đang lưu trong DB; <c>null</c> khi thêm mới.</param>
    private async Task NormalizeAsync(VdsConfig row, string? storedCode, string? storedValue)
    {
        row.GroupCode = row.GroupCode?.Trim();
        row.Code = row.Code?.Trim();
        row.Name = row.Name?.Trim();
        if (row.Code != VdsConfigConst.Code.MediaStoragePassword) row.Value = row.Value?.Trim();
        // Cột Status nullable: không gửi nghĩa là bật, giữ đúng hành vi trước khi input kế thừa entity.
        row.Status ??= BaseEnums.StatusEnum.Enable;

        // Sửa một mã bí mật mà để trống giá trị: hiểu là "không đổi", giữ nguyên giá trị đang lưu.
        if (VdsConfigRules.IsSecret(row.Code) && string.IsNullOrEmpty(row.Value) && storedCode == row.Code)
            row.Value = storedValue;

        var error = VdsConfigRules.Validate(row.GroupCode, row.Code, row.Value, row.Name, (int)row.Status);
        if (error != null) throw Oops.Oh(error);
        if (row.Remark?.Length > 1024) throw Oops.Oh(BaseMsg.Vds.Exception.RemarkTooLong);

        // Toàn bộ luật cần DB — trùng mã, tham chiếu tồn tại, chuẩn hoá danh sách camera, và so cặp min/max
        // trên bộ cấu hình CÓ HIỆU LỰC — nằm ở service dùng chung với API vdsConfiguration. Xem
        // VdsConfigWriteService để biết ba chỗ hai đường ghi từng lệch nhau.
        row.Value = await _writeRules.ValidateAsync(row.GroupCode, row.Code, row.Value, row.Name, row.Status.Value, row.ID);
    }
}
