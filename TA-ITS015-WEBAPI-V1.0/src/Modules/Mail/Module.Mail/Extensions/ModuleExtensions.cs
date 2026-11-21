using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Mail.Extensions
{
    /// <summary>
    /// Description: Đăng ký phân hệ gửi mail. Gọi trong Startup.RegisterModules.
    /// Created date: 2026-08-13
    /// </summary>
    public static class ModuleExtensions
    {
        public static IServiceCollection AddMailModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddMailCore()
                .AddMailInfrastructure(configuration);
            return services;
        }
    }
}
