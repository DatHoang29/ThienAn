using System.Net.Security;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.CctvProxy.Configuration;
using Services.CctvProxy.Configuration.Loaders;
using Services.CctvProxy.Configuration.Options;
using Services.CctvProxy.Endpoints;
using Services.CctvProxy.Features.Proxy;
using Services.CctvProxy.Features.PublicSnapshot;
using Services.CctvProxy.Features.Session;
using Services.CctvProxy.Manager;
using Services.Shared.Configuration;
using Services.Shared.Extensions;
using Services.Shared.Hosting;

namespace Services.CctvProxy.Extensions;

/// <summary>
/// Author: kien.le
/// Description: Đăng ký runtime cho Cctv Proxy. Composition root (Worker Program.cs) gọi AddCctvProxyServices
/// rồi MapCctvProxyEndpoints.
/// Created date: 06/10/2026
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký nền dùng chung (DB, config loader), cấu hình, manager, phiên NxVMS, các proxy và ảnh công khai (kèm rate limit).
    /// </summary>
    /// <param name="services">DI container của host.</param>
    /// <param name="configuration">IConfiguration đã nạp appsettings + Configuration/*.json.</param>
    /// <returns>Chính <paramref name="services"/> để gọi nối tiếp.</returns>
    public static IServiceCollection AddCctvProxyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddServiceFoundation(configuration);

        services.AddSingleton<CctvProxyConfigManager>();
        services.AddSingleton<IConfigManager>(sp => sp.GetRequiredService<CctvProxyConfigManager>());
        services.AddSingleton<IConfigLoader<CctvOptions>, CctvOptionsLoader>();
        services.AddHostedService<ConfigReloadWorker>();

        // Host (Worker) tự gọi manager.InitAsync trước khi nhận request, StartAsync sau đó, StopAsync trước khi dừng.
        services.AddSingleton<CctvProxyServiceManager>();
        services.AddSingleton<IServiceManager>(sp => sp.GetRequiredService<CctvProxyServiceManager>());

        // HttpClient gọi NxVMS: chấp nhận cert tự ký theo cấu hình (đọc live để đổi không cần restart).
        // Timeout vô hạn — từng request tự đặt timeout chờ header; body media stream theo RequestAborted.
        services.AddHttpClient(NxVmsSessionManager.HttpClientName, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var config = sp.GetRequiredService<IConfigManager>();
                return new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = (_, _, _, errors) =>
                            errors == SslPolicyErrors.None || config.Current.Cctv.AcceptAnyCertificate,
                    },
                };
            });

        services.AddSingleton<NxVmsSessionManager>();
        services.AddSingleton<StreamTicketValidator>();
        services.AddSingleton<WebRtcSignalingProxy>();
        services.AddSingleton<MediaHttpProxy>();
        services.AddSingleton<SnapshotHttpProxy>();

        // Ảnh camera công khai cho web người dân (không ticket).
        services.AddSingleton<PublicCameraDirectory>();
        services.AddSingleton<WebApiThumbnailStore>();
        services.AddSingleton<PublicSnapshotHandler>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(CctvProxyEndpoints.PublicRateLimitPolicy, context =>
            {
                // Đọc live để đổi Service:Public:RateLimitPerMinute không cần restart (giới hạn nằm trong khoá phân vùng).
                var limit = context.RequestServices.GetRequiredService<IConfigManager>().Current.Public.RateLimitPerMinute;
                if (limit <= 0)
                    return RateLimitPartition.GetNoLimiter("unlimited");

                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"{ip}|{limit}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }
}
