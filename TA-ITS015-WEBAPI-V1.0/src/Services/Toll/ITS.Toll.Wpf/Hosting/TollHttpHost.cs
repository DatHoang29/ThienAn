using ITS.Toll.Core.Endpoints;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITS.Toll.Wpf.Hosting;

/// <summary>
/// Host Kestrel NHÚNG trong form WPF: mở đúng 2 endpoint để TCS gọi tới ngay khi bật form
///   POST /api/toll/record | POST /api/toll/import (+ GET /health).
/// Tái sử dụng <see cref="TollCallBackService"/> + <see cref="TollLogger"/> đã dựng ở DI của form
/// (đăng ký lại đúng CÙNG instance vào container của web host) nên mọi diễn tiến ghi qua TollLogger
/// sẽ tự hiển thị lên Nhật ký của form — tiện theo dõi luồng trước khi chuyển sang chạy Worker.
/// Chạy nền (StartAsync không chặn UI); Stop khi đóng form.
/// Author: ChuongLP
/// Created date: 16/09/2026
/// </summary>
public sealed class TollHttpHost
{
    private readonly TollCallBackService _callback;
    private readonly TollLogger _logger;
    private readonly string _urls;

    private WebApplication? _app;
    private volatile bool _starting;

    /// <param name="urls">Địa chỉ Kestrel lắng nghe, ngăn cách bằng ';' (vd "http://localhost:8710").</param>
    public TollHttpHost(TollCallBackService callback, TollLogger logger, string urls)
    {
        _callback = callback;
        _logger = logger;
        _urls = string.IsNullOrWhiteSpace(urls) ? "http://localhost:8710" : urls.Trim();
    }

    /// <summary>Đang mở API hay không.</summary>
    public bool IsRunning => _app is not null;

    /// <summary>Mở API (không chặn UI). Gọi lại nhiều lần an toàn.</summary>
    public void Start()
    {
        if (_app is not null || _starting) return;
        _starting = true;

        // Dựng + start trên thread nền để không chặn UI và tránh deadlock sync-over-async.
        Task.Run(async () =>
        {
            try
            {
                var builder = WebApplication.CreateBuilder();

                // Không cần provider log mặc định (console/eventlog) trong app WPF.
                builder.Logging.ClearProviders();
                builder.WebHost.UseUrls(_urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

                // Dùng lại CÙNG instance service của form (để log chảy về Nhật ký form).
                builder.Services.AddSingleton(_callback);
                builder.Services.AddSingleton(_logger);

                var app = builder.Build();

                app.MapGet("/health", () => Results.Ok(new { ok = true, service = "ITS.Toll.Wpf" }));
                app.MapTollCallBackEndpoints();

                await app.StartAsync().ConfigureAwait(false);
                _app = app;

                _logger.Success(LogCategory.CallBack,
                    $"[HTTP] Đã mở API nhận dữ liệu TCS tại {_urls}. Endpoint: POST /api/toll/record | POST /api/toll/import | GET /health.");
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Sync, $"[HTTP] Không mở được API nhận dữ liệu TCS ({_urls}): {ex.Message}");
            }
            finally
            {
                _starting = false;
            }
        });
    }

    /// <summary>Đóng API (chờ dừng hẳn).</summary>
    public async Task StopAsync()
    {
        var app = _app;
        _app = null;
        if (app is null) return;
        try
        {
            await app.StopAsync().ConfigureAwait(false);
            await app.DisposeAsync().ConfigureAwait(false);
            _logger.Info(LogCategory.CallBack, "[HTTP] Đã đóng API nhận dữ liệu TCS.");
        }
        catch { /* đang thoát, bỏ qua lỗi dọn dẹp */ }
    }
}
