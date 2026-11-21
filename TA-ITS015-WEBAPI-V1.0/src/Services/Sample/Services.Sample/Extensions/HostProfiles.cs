using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services.Shared.Logging;

namespace Services.Sample.Extensions;

/// <summary>
/// Author: TAC Team
/// Description: Cấu hình host (logging, Windows Service) khai báo tập trung cho 2 profile:
/// WorkerService (chạy như Windows Service) và GuiForm (WPF operator). Composition root gọi trước Build.
/// Logging qua LogManager (Shared.Runtime) — cấu hình thư mục/tên file/rotation qua Configuration/Logging.json.
/// Created date: 17/07/2026
/// </summary>
public static class HostProfiles
{
    /// <summary>
    /// Profile cho WorkerService: logging + đăng ký Windows Service (SCM).
    /// Logging cấu hình đọc từ <c>Configuration/Logging.json</c> (section "Logging") — caller phải
    /// <c>AddJsonFile("Configuration/Logging.json", optional: true)</c> trước khi gọi hàm này.
    /// </summary>
    /// <param name="builder">IHostApplicationBuilder từ Host.CreateApplicationBuilder.</param>
    /// <param name="serviceName">Tên hiển thị trong Windows Service Manager (SCM).</param>
    public static void ConfigureWorkerService(IHostApplicationBuilder builder, string serviceName)
    {
        LogManager.ConfigureLogging(builder.Logging, builder.Configuration);
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = serviceName;
        });
    }

    /// <summary>
    /// Profile cho GuiForm (WPF operator): chỉ logging, không đăng ký Windows Service.
    /// Logging cấu hình đọc từ <c>Configuration/Logging.json</c> (section "Logging") — caller phải
    /// <c>AddJsonFile("Configuration/Logging.json", optional: true)</c> trước khi gọi hàm này.
    /// </summary>
    /// <param name="builder">IHostApplicationBuilder từ Host.CreateApplicationBuilder.</param>
    public static void ConfigureGuiForm(IHostApplicationBuilder builder) =>
        LogManager.ConfigureLogging(builder.Logging, builder.Configuration);
}
