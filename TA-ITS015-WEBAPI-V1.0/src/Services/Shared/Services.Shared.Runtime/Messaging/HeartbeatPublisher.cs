using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Services.Shared.Configuration;
using Services.Shared.Logging;

namespace Services.Shared.Messaging;

/// <summary>
/// Author: TAC Team
/// Description: Background service publish heartbeat định kỳ lên NATS để FE giám sát dịch vụ.
/// Mỗi <see cref="HeartbeatOptions.IntervalSeconds"/> giây gửi 1 gói { data = "ok" } lên
/// <see cref="HeartbeatOptions.Subject"/> (vd "ta.its.heartbeat.vms"). Dùng chung cho mọi service
/// gọi AddServiceFoundation; opt-in bằng section "Heartbeat" trong DataTransporter.json.
/// Không cấu hình / Enabled=false / Subject rỗng => no-op (thoát ngay, không tốn tài nguyên).
/// Created date: 24/09/2026
/// </summary>
public sealed class HeartbeatPublisher : BackgroundService
{
    private readonly TransportManager _transport;
    private readonly HeartbeatOptions _options;

    public HeartbeatPublisher(TransportManager transport, IConfiguration configuration)
    {
        _transport = transport;
        _options = configuration.GetSection(HeartbeatOptions.SectionName).Get<HeartbeatOptions>()
                   ?? new HeartbeatOptions();
    }

    /// <summary>
    /// Cho service tự đặt giá trị MẶC ĐỊNH (bật sẵn + subject riêng) mà không phải khai mục
    /// <c>Heartbeat</c> trong DataTransporter.json; mục đó, nếu có, chỉ GHI ĐÈ từng khoá đã khai lên mặc định
    /// (ví dụ <c>"Enabled": false</c> để tắt). Đăng ký bằng factory vì <paramref name="defaults"/> không nằm trong DI.
    /// </summary>
    public HeartbeatPublisher(TransportManager transport, IConfiguration configuration, HeartbeatOptions defaults)
    {
        _transport = transport;
        configuration.GetSection(HeartbeatOptions.SectionName).Bind(defaults);
        _options = defaults;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Subject))
        {
            LogManager.Info("Heartbeat tắt hoặc thiếu Subject — HeartbeatPublisher không chạy.", kind: LogKind.Nats);
            return;
        }

        var subject = _options.Subject!.Trim();
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds));
        var payloadData = string.IsNullOrWhiteSpace(_options.Data) ? "ok" : _options.Data;

        // Kết nối lần đầu; nếu lỗi vẫn vào vòng lặp và thử lại (transport tự retry ở tầng dưới).
        try
        {
            await _transport.ConnectAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            LogManager.Error(ex, "Heartbeat: kết nối transport thất bại (sẽ thử lại trong vòng lặp).", kind: LogKind.Nats);
        }

        LogManager.Info($"HeartbeatPublisher đã khởi động. Subject={subject}, Interval={interval.TotalSeconds}s.", kind: LogKind.Nats);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_transport.IsConnected)
                    await _transport.ConnectAsync(stoppingToken);

                if (_transport.IsConnected)
                    await _transport.PublishAsync(subject, new { data = payloadData }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex, $"Heartbeat publish thất bại. Subject={subject}", kind: LogKind.Nats);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
