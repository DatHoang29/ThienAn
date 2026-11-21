using ITS.PA.Core.Configuration;
using ITS.PA.Core.Integration;
using Microsoft.Extensions.Options;

namespace ITS.PA.Worker;

/// <summary>
/// Kiểm tra đăng nhập máy chủ PA lúc khởi động để phát hiện sớm sai IP/tài khoản.
/// Lệnh điều khiển do <see cref="ITS.PA.Core.Messaging.PaControlListener"/> nhận.
/// Author: ChuongLP
/// Created date: 31/10/2026
/// </summary>
public sealed class PaWorker : BackgroundService
{
    private readonly PaApiClient _api;
    private readonly PaOptions _options;
    private readonly ILogger<PaWorker> _logger;

    public PaWorker(PaApiClient api, IOptions<PaOptions> options, ILogger<PaWorker> logger)
    {
        _api = api;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Ip))
        {
            _logger.LogWarning("Service:Pa:Ip chưa khai báo — dịch vụ PA chạy nhưng chưa kết nối được máy chủ PA.");
        }
        else
        {
            var token = await _api.LoginAsync(stoppingToken);
            if (token is null)
                _logger.LogError("Không đăng nhập được máy chủ PA {Ip}:{Port}.", _options.Ip, _options.Port);
            else
                _logger.LogInformation("Đăng nhập máy chủ PA {Ip}:{Port} thành công.", _options.Ip, _options.Port);
        }

        _logger.LogInformation("ITS PA Control started.");
    }
}
