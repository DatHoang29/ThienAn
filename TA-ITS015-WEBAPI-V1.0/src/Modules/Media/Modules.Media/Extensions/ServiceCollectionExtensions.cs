using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Media.Infrastructure.Config;
using Modules.Media.Infrastructure.Services;

namespace Modules.Media.Extensions
{
    /// <summary>
    /// Description: Đăng ký Core/Infrastructure cho phân hệ Media.
    /// Created date: 08/10/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMediaCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }

        public static IServiceCollection AddMediaInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Cấu hình (đọc SysOpConfig) — theo request.
            services.AddScoped<MediaConfigService>();

            // ITS: đọc file wwwroot.
            services.AddSingleton<MediaFileReader>();

            // WP: gọi ITS + gateway (cache qua BaseCacheService như các module khác).
            services.AddSingleton<MediaProviderClient>();
            services.AddScoped<MediaPublicGateway>();

            return services;
        }
    }
}
