using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services.Shared.Logging;

namespace Services.CctvProxy.Extensions;

/// <summary>
/// Author: kien.le
/// Description: Cấu hình host cho Cctv Proxy (giống profile WorkerService của Sample): logging qua LogManager
/// (cấu hình Configuration/Logging.json) + chạy như Windows Service.
/// Created date: 06/10/2026
/// </summary>
public static class HostProfiles
{
    /// <summary>
    /// Caller phải <c>AddJsonFile("Configuration/Logging.json", optional: true)</c> trước khi gọi hàm này.
    /// </summary>
    public static void ConfigureWorkerService(WebApplicationBuilder builder, string serviceName)
    {
        LogManager.ConfigureLogging(builder.Logging, builder.Configuration);
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = serviceName;
        });
    }
}
