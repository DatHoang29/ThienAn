using Microsoft.Extensions.DependencyInjection;
using Services.Sample.Configuration;
using Services.Sample.Features.Devices;
using Services.Shared.Hosting;
using Services.Shared.Logging;

namespace Services.Sample.Manager;

/// <summary>
/// Author: TAC Team
/// Description: Facade điều phối logic Sample. Chia thành partial class để tách nhóm chức năng:
/// <list type="bullet">
/// <item><term>SampleServiceManager.cs</term><description> Lifecycle chính: constructor + Init/Start/Stop + State.</description></item>
/// <item><term>SampleServiceManager.Simulation.cs</term><description> Chức năng giả lập dữ liệu (SimulateBatchAsync) để test pipeline khi không có device/NATS thật.</description></item>
/// </list>
///
/// Thiết kế dependency: constructor chỉ nhận 3 thứ cốt lõi (IConfigManager, DeviceAcquisitionRunner,
/// IServiceProvider). Các dependency phụ (DeviceCodecRegistry warm-up, MessageQueue, ActivityPublisher)
/// resolve lazily qua <see cref="System.IServiceProvider"/> khi cần — tránh nhồi constructor với dependency
/// mà chỉ 1 nhóm chức năng (Simulation) dùng.
///
/// Lifecycle do host (Worker/WPF) gọi tường minh: Init+Start sau khi host start, Stop trước khi host stop.
/// Created date: 17/07/2026
/// </summary>
public sealed partial class SampleServiceManager : IServiceManager, IServiceSimulator
{
    private readonly IConfigManager _configManager;
    private readonly DeviceAcquisitionRunner _runner;
    private readonly IServiceProvider _services;

    public SampleServiceManager(
        IConfigManager configManager,
        DeviceAcquisitionRunner runner,
        IServiceProvider services)
    {
        _configManager = configManager;
        _runner = runner;
        _services = services;
    }

    public ServiceManagerState State { get; private set; } = ServiceManagerState.Created;

    // Nạp cấu hình runtime (file + DB override) và warm-up codec registry.
    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await _configManager.ReloadAsync(cancellationToken).ConfigureAwait(false);

        // Warm-up: force codec registry load plugin ngay tại Init thay vì lười khi first use.
        // Resolve lazily — codecRegistry không nằm trong constructor để giữ gọn.
        _ = _services.GetRequiredService<DeviceCodecRegistry>().Codecs;

        State = ServiceManagerState.Initialized;
    }

    // Start runner ngay khi được gọi (idempotent — runner tự bỏ qua nếu đang chạy).
    // Quyết định "có auto-start lúc boot hay không" do HOST lo qua Devices.Enabled:
    //   - Worker: if (Devices.Enabled) StartAsync()  (auto-start headless).
    //   - WPF: không auto-start, operator bấm nút Start/Stop.
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _runner.Start();
        State = ServiceManagerState.Running;
        return Task.CompletedTask;
    }

    // Dừng runner gọn nếu đang chạy (idempotent).
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _runner.StopAsync().ConfigureAwait(false);
        State = ServiceManagerState.Stopped;
    }
}
