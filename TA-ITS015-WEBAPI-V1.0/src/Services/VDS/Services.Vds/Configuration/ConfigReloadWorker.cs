using Microsoft.Extensions.Hosting;

namespace Services.Vds.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Gọi <see cref="IVdsConfigManager.ReloadAsync"/> theo chu kỳ để sửa
/// <c>appsettings.json</c> có hiệu lực mà không phải restart service.
///
/// <b>Mặc định TẮT</b> (<c>ConfigReloadSeconds = 0</c>). Đường thay đổi cấu hình chính của VDS là đẩy
/// tham số xuống (<see cref="IVdsConfigManager.Apply"/>) — có hiệu lực ngay lập tức, không phải chờ chu kỳ.
/// Bật worker này khi muốn sửa file trên máy chủ mà không dừng service.
///
/// Bật lên vẫn an toàn: mỗi lần reload đều phát lại toàn bộ override lên trên cấu hình vừa đọc từ file
/// (xem <see cref="VdsConfigManager.ReloadAsync"/>), nên không ghi đè ngược tham số đã đẩy xuống.
/// Created date: 24/08/2026
/// </summary>
public sealed class ConfigReloadWorker : BackgroundService
{
    private readonly IVdsConfigManager _config;
    private readonly ILogger<ConfigReloadWorker> _logger;

    public ConfigReloadWorker(IVdsConfigManager config, ILogger<ConfigReloadWorker> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Đọc chu kỳ MỘT LẦN lúc bắt đầu: đổi chu kỳ lúc đang chạy thì phải restart mới có tác dụng.
        // Cố ý như vậy — để chu kỳ đọc lại mỗi vòng thì cấu hình sai có thể khiến worker quay tít.
        var seconds = _config.Current.ConfigReloadSeconds;

        if (seconds <= 0)
        {
            _logger.LogInformation(
                "[Config] Reload theo chu kỳ đang TẮT (Service:ConfigReloadSeconds = {Seconds}). " +
                "Cấu hình đổi qua đường Apply (đẩy tham số xuống) vẫn có hiệu lực ngay.", seconds);
            return;
        }

        var period = TimeSpan.FromSeconds(seconds);
        _logger.LogInformation("[Config] Reload cấu hình từ file mỗi {Seconds} giây.", seconds);

        using var timer = new PeriodicTimer(period);

        // PeriodicTimer thay vì Task.Delay trong vòng lặp: nó không dồn nhịp khi một lượt reload chạy lâu
        // hơn chu kỳ, nên không có chuyện nhiều lượt reload xếp hàng chờ nhau.
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // ReloadAsync tự bắt mọi ngoại lệ và giữ snapshot last-known-good, nên ở đây không cần try.
            await _config.ReloadAsync(stoppingToken);
        }
    }
}
