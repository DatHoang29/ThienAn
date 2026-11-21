using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Notification.Extensions
{
    /// <summary>
    /// Author: SonTH
    /// Description: Đăng ký phân hệ Notification. Gọi trong Startup.RegisterModules.
    /// Created date: 17/09/2026
    /// </summary>
    public static class ModuleExtensions
    {
        public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddNotificationCore()
                .AddNotificationInfrastructure(configuration);
            return services;
        }
    }
}
