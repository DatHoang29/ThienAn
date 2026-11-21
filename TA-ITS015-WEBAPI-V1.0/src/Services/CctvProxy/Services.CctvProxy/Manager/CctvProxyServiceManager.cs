using Services.CctvProxy.Configuration;
using Services.Shared.Hosting;
using Services.Shared.Logging;

namespace Services.CctvProxy.Manager;

/// <summary>
/// Author: kien.le
/// Description: Facade lifecycle của Cctv Proxy (theo mẫu SampleServiceManager). Host Worker gọi tường minh:
/// Init (nạp cấu hình SysOpConfig) TRƯỚC khi Kestrel nhận request, Start sau đó, Stop trước khi host dừng.
/// Endpoint proxy chỉ phục vụ khi <see cref="State"/> = Running (lúc khởi động/dừng trả 503).
/// Created date: 06/10/2026
/// </summary>
public sealed class CctvProxyServiceManager(IConfigManager configManager) : IServiceManager
{
    public ServiceManagerState State { get; private set; } = ServiceManagerState.Created;

    /// <summary>
    /// Author: kien.le
    /// Description: Nạp cấu hình runtime (appsettings + SysOpConfig nhóm cctv_config) để request đầu tiên đã có
    /// BaseUrl/tài khoản NxVMS/ticket secret.
    /// Created date: 06/10/2026
    /// </summary>
    /// <param name="cancellationToken">Huỷ khi host dừng.</param>
    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await configManager.ReloadAsync(cancellationToken).ConfigureAwait(false);
        State = ServiceManagerState.Initialized;
    }

    /// <summary>
    /// Author: kien.le
    /// Description: Bắt đầu phục vụ request proxy (idempotent).
    /// Created date: 06/10/2026
    /// </summary>
    /// <param name="cancellationToken">Huỷ khi host dừng.</param>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        State = ServiceManagerState.Running;
        LogManager.Info($"{configManager.Current.Name} đang chạy (NxVMS: {configManager.Current.Cctv.BaseUrl})");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Author: kien.le
    /// Description: Ngừng nhận request proxy mới trước khi host dừng (idempotent).
    /// Created date: 06/10/2026
    /// </summary>
    /// <param name="cancellationToken">Huỷ khi host dừng.</param>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        State = ServiceManagerState.Stopped;
        LogManager.Info($"{configManager.Current.Name} đã dừng");
        return Task.CompletedTask;
    }
}
