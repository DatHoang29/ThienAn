using ITS.FA.Core.Configuration;
using ITS.FA.Core.Integration;
using ITS.FA.Core.Messaging;
using ITS.FA.Core.Persistence;
using ITS.FA.Core.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Shared.Configuration;
using Services.Shared.Extensions;
using Services.Shared.Messaging;

namespace ITS.FA.Core;

/// <summary>Đăng ký toàn bộ thành phần của dịch vụ FA vào DI.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddFaServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database.json (DbConnection) + DataTransporter.json (Nats). Không khai Nats:Url thì dịch vụ vẫn chạy, chỉ không phát.
        services.AddServiceFoundation(configuration);

        services.Configure<FaOptions>(configuration.GetSection(FaOptions.SectionName));

        services.AddSingleton<FaRepository>();
        services.AddSingleton<FaModbusClient>();
        services.AddSingleton<FaZoneLocator>();
        services.AddSingleton<FaIncidentService>();
        services.AddSingleton<IFaEventPublisher, NatsFaEventPublisher>();
        services.AddSingleton<FaAlarmMonitor>();

        // Mặc định BẬT; mục "Heartbeat" trong DataTransporter.json chỉ để ghi đè.
        services.AddHostedService(sp => new HeartbeatPublisher(
            sp.GetRequiredService<TransportManager>(),
            sp.GetRequiredService<IConfiguration>(),
            new HeartbeatOptions { Enabled = true, Subject = FaSubjects.Heartbeat }));

        return services;
    }
}
