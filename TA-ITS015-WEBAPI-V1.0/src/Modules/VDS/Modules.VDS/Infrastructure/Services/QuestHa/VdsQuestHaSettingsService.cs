using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Helper;
using Shared.DTO.Enums;
using Shared.QuestHa;

namespace Modules.VDS.Infrastructure.Services.QuestHa;

/// <summary>
/// Author: KienLT
/// Description: Áp tham số router QuestDB HA (<c>vds_questha_router_*</c> trong <c>VdsConfig</c>) cho đường đọc của WebAPI. WebAPI không chạy
/// health/bảo trì/backup nên chỉ đọc nhóm router; các tham số khác giữ appsettings. Gọi lúc khởi động (worker nền) và ngay sau khi
/// lưu/xoá một dòng router (handler VdsConfig) — WebAPI không nhận refresh NATS của chính nó. Sửa thẳng DB ngoài API thì cần restart WebAPI.
/// Router là singleton toàn host ⇒ chỉ đọc dòng của tenant sở hữu (<see cref="VdsConfigConst.QuestHa.SettingsTenantConfigKey"/>, mặc định
/// <see cref="VdsConfigConst.QuestHa.DefaultSettingsTenantId"/>); lưu dòng của tenant khác không đổi router. Mỗi lượt lấy vé đọc TRƯỚC khi truy vấn
/// nên kết quả đọc cũ về muộn (request song song, khởi động) không đè bộ mới hơn đã áp. HA tắt (không có <see cref="QuestHaManager"/>) ⇒ không làm gì.
/// Created date: 31/10/2026
/// </summary>
public sealed class VdsQuestHaSettingsService(BaseRepository<VdsConfig> repo, IServiceProvider services, IConfiguration configuration,
    ILogger<VdsQuestHaSettingsService> logger) : IScoped
{
    /// <summary>Tenant sở hữu tham số QuestDB HA của WebAPI này.</summary>
    public string OwnerTenantId => VdsBaseHelper.QuestHaConfigHp.SettingsTenant(configuration[VdsConfigConst.QuestHa.SettingsTenantConfigKey]);

    /// <summary>
    /// Đọc dòng router đang bật của tenant sở hữu và áp. Mã thiếu ⇒ appsettings. Giá trị sai ⇒ giữ bộ đang chạy + Warning, trả false;
    /// kết quả đọc đã cũ (lượt khác bắt đầu sau đã áp) ⇒ trả false, không log Warning. Lỗi DB ném ra cho bên gọi quyết định.
    /// </summary>
    public async Task<bool> ApplyRouterAsync(CancellationToken ct = default)
    {
        var manager = services.GetService<QuestHaManager>();
        if (manager is null) return false;

        var tenant = OwnerTenantId;
        return await RefreshRouterAsync(manager, async token =>
        {
            // Chạy cả khi không có người dùng (khởi động) ⇒ bỏ bộ lọc ngầm, lọc tường minh như truy vấn VdsConfig của Worker.
            var rows = await repo.Context.Queryable<VdsConfig>().ClearFilter()
                .Where(x => x.GroupCode == VdsConfigConst.Group.Config && x.TenantId == tenant && x.IsDelete == null
                    && x.Status == BaseEnums.StatusEnum.Enable && x.Code!.StartsWith(VdsConfigConst.QuestHa.RouterPrefix))
                .Select(x => new { x.Code, x.Value })
                .ToListAsync(token);
            return rows.ToDictionary(r => r.Code ?? "", r => r.Value, StringComparer.Ordinal);
        }, logger, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Một lượt "đọc → áp" router: lấy vé đọc của <paramref name="manager"/> TRƯỚC khi gọi <paramref name="load"/>, chỉ giữ mã router,
    /// dựng gói rồi áp kèm vé. Dùng chung cho khởi động và hook sau lưu (tách ra để kiểm thứ tự đa luồng không cần DB).
    /// </summary>
    public static async Task<bool> RefreshRouterAsync(QuestHaManager manager,
        Func<CancellationToken, Task<Dictionary<string, string?>>> load, ILogger logger, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(load);
        var ticket = manager.BeginSettingsRead();
        var rows = await load(ct).ConfigureAwait(false);

        var router = rows.Where(r => r.Key.StartsWith(VdsConfigConst.QuestHa.RouterPrefix, StringComparison.Ordinal))
            .ToDictionary(r => r.Key, r => r.Value, StringComparer.Ordinal);
        var errors = new List<string>();
        var runtime = VdsBaseHelper.QuestHaConfigHp.Build(router, errors);
        if (errors.Count > 0)
        {
            logger.LogWarning("[QuestHa] Tham số router QuestDB HA không hợp lệ ⇒ giữ bộ đang chạy: {Errors}.", string.Join("; ", errors));
            return false;
        }
        // Manager log Information các khoá đổi hoặc Warning lý do từ chối.
        var applied = manager.ApplySettings(runtime, ticket, out var superseded);
        if (superseded) logger.LogDebug("[QuestHa] Bỏ kết quả đọc router đã cũ; một lượt đọc sau đã áp bộ mới hơn.");
        return applied;
    }

    /// <summary>
    /// Sau khi lưu/xoá một dòng: mã CŨ hoặc MỚI là router và dòng thuộc tenant sở hữu ⇒ áp lại ngay (đổi một dòng router sang mã
    /// khác cũng phải đưa router về mặc định). Lỗi chỉ log Warning (dòng đã lưu, không làm hỏng request).
    /// </summary>
    public async Task AfterWriteAsync(string? group, string? code, string? storedGroup, string? storedCode, string? tenantId)
    {
        if (!IsRouter(group, code) && !IsRouter(storedGroup, storedCode)) return;
        if (!string.Equals(tenantId?.Trim(), OwnerTenantId, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("[QuestHa] Dòng router của tenant khác tenant sở hữu cấu hình HA ⇒ không áp lên router của host.");
            return;
        }
        try { await ApplyRouterAsync().ConfigureAwait(false); }
        catch (Exception ex)
        {
            logger.LogWarning("[QuestHa] Không áp lại được tham số router QuestDB HA sau khi lưu ({ErrorType}); giữ bộ đang chạy.", ex.GetType().Name);
        }
    }

    private static bool IsRouter(string? group, string? code) =>
        group == VdsConfigConst.Group.Config && code?.StartsWith(VdsConfigConst.QuestHa.RouterPrefix, StringComparison.Ordinal) == true;
}
