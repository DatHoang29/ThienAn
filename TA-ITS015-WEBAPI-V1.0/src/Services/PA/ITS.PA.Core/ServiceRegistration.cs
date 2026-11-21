using ITS.PA.Core.Configuration;
using ITS.PA.Core.Integration;
using ITS.PA.Core.Messaging;
using ITS.PA.Core.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Shared.Configuration;
using Services.Shared.Extensions;
using Services.Shared.Messaging;

namespace ITS.PA.Core;

/// <summary>Đăng ký toàn bộ thành phần của dịch vụ PA vào DI.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddPaServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DataTransporter.json (Nats). PA không dùng DB nên DatabaseManager đăng ký nhưng không bao giờ được khởi tạo.
        services.AddServiceFoundation(configuration);

        services.Configure<PaOptions>(configuration.GetSection(PaOptions.SectionName));

        // Singleton để toàn dịch vụ dùng chung một token. HttpClient sống cùng dịch vụ, máy chủ PA có IP cố định.
        services.AddSingleton(sp => ActivatorUtilities.CreateInstance<PaApiClient>(sp, new HttpClient()));
        services.AddSingleton<PaDeviceExecutor>();
        services.AddSingleton<PaControlService>();
        services.AddHostedService<PaControlListener>();

        // Mặc định BẬT; mục "Heartbeat" trong DataTransporter.json chỉ để ghi đè.
        services.AddHostedService(sp => new HeartbeatPublisher(
            sp.GetRequiredService<TransportManager>(),
            sp.GetRequiredService<IConfiguration>(),
            new HeartbeatOptions { Enabled = true, Subject = PaSubjects.Heartbeat }));

        return services;
    }
}
