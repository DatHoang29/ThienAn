using ITS.FA.Core.Configuration;
using ITS.FA.Core.Processing;
using Microsoft.Extensions.Options;

namespace ITS.FA.Worker;

/// <summary>
/// Vòng ngoài của dịch vụ. Cũ: do { Init() } while (true) trong Its.FireAlarm.Service/Program.cs —
/// phiên quét lỗi (mất Modbus, lỗi DB) thì reset, chờ vài giây rồi dựng lại từ đầu.
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
public sealed class FaWorker : BackgroundService
{
    private readonly FaAlarmMonitor _monitor;
    private readonly FaOptions _options;
    private readonly ILogger<FaWorker> _logger;

    public FaWorker(FaAlarmMonitor monitor, IOptions<FaOptions> options, ILogger<FaWorker> logger)
    {
        _monitor = monitor;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ITS FA Monitor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _monitor.RunSessionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phiên quét báo cháy lỗi.");
                _monitor.Reset();

                for (var s = _options.Modbus.RestartDelaySeconds; s > 0 && !stoppingToken.IsCancellationRequested; s--)
                {
                    _logger.LogInformation("Restart in {Seconds} seconds", s);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                }
            }
        }

        _monitor.Reset();
        _logger.LogInformation("ITS FA Monitor stopped.");
    }
}
