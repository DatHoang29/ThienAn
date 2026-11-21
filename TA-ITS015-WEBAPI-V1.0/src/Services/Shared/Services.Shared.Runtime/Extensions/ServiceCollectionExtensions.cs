using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Shared.Configuration;
using Services.Shared.Messaging;
using Services.Shared.Persistence;

namespace Services.Shared.Extensions;

/// <summary>
/// Author: TAC Team
/// Description: Đăng ký technical foundation dùng chung cho các service .NET:
/// DatabaseManager (SqlSugar, nhiều kết nối), TransportManager (NATS).
/// Đọc cấu hình từ Database.json (section "DbConnection") + DataTransporter.json (section "Nats").
/// Created date: 16/07/2026
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database.json — section "DbConnection" (DbConnectionOptions + ConnectionConfigs[]).
        services.Configure<DbConnectionOptions>(
            configuration.GetSection("DbConnection"));

        // DatabaseManager (SqlSugarScope nhiều kết nối).
        services.AddSingleton<DatabaseManager>();

        // Config loader registry: điều phối các IConfigLoader<TConfig> do từng service đăng ký.
        services.AddSingleton<IConfigLoaderRegistry, ConfigLoaderRegistry>();

        // DataTransporter.json — TransportManager tự đọc section "Nats" + "Nats:Streams" từ IConfiguration.
        services.AddSingleton<TransportManager>();

        // Heartbeat (giám sát dịch vụ): KHÔNG auto-register ở đây để không ảnh hưởng các service khác.
        // Service nào muốn phát heartbeat tự đăng ký: services.AddHostedService<HeartbeatPublisher>()
        // và khai section "Heartbeat" (Enabled/Subject/IntervalSeconds) trong DataTransporter.json.

        return services;
    }
}
