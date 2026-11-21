using Microsoft.Extensions.Configuration;
using Modules.CfgSystem.Core.Entities;
using Services.CctvProxy.Configuration.Options;
using Services.Shared.Configuration;
using Services.Shared.Persistence;

namespace Services.CctvProxy.Configuration.Loaders;

/// <summary>
/// Author: kien.le
/// Description: Đọc cấu hình NxVMS + ticket từ bảng SysOpConfig (nhóm <see cref="GroupCode"/>) — cùng nguồn với
/// WebAPI (Module.CCTV DeviceService), nên tài khoản NxVMS chỉ cấu hình một chỗ trên màn hình "Cấu hình hoạt động".
/// Key nào chưa có trong DB thì giữ giá trị fallback trong file (section "Service:Cctv").
/// Created date: 06/10/2026
/// </summary>
public sealed class CctvOptionsLoader(IConfiguration configuration, DatabaseManager databaseManager)
    : SqlSugarAccess(databaseManager, () => configuration["Service:DataConnectionId"] ?? "Default"),
      IConfigLoader<CctvOptions>
{
    public const string GroupCode = "cctv_config";

    public const string CodeBaseUrl = "cctv_base_url";
    public const string CodeUsername = "cctv_username";
    public const string CodePassword = "cctv_password";
    public const string CodeTicketSecret = "cctv_proxy_ticket_secret";

    public CctvOptions DefaultValue =>
        configuration.GetSection("Service:Cctv").Get<CctvOptions>() ?? new CctvOptions();

    /// <inheritdoc/>
    public Task<CctvOptions> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = Client.Queryable<SysOpConfig>()
            .Where(x => x.GroupCode == GroupCode && x.IsDelete == null)
            .ToList();

        var byCode = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Value?.Trim(), StringComparer.OrdinalIgnoreCase);

        string Pick(string code, string fallback) =>
            byCode.TryGetValue(code, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

        var fallback = DefaultValue;
        return Task.FromResult(new CctvOptions
        {
            BaseUrl = Pick(CodeBaseUrl, fallback.BaseUrl),
            Username = Pick(CodeUsername, fallback.Username),
            Password = Pick(CodePassword, fallback.Password),
            TicketSecret = Pick(CodeTicketSecret, fallback.TicketSecret),
            AcceptAnyCertificate = fallback.AcceptAnyCertificate,
        });
    }
}
