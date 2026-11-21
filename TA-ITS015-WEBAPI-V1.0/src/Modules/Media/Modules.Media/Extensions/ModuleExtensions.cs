using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Media.Extensions
{
    /// <summary>
    /// Description: Đăng ký phân hệ Media (ITS-Media + WP-MediaPublic) vào IServiceCollection.
    /// Created date: 08/10/2026
    /// </summary>
    public static class ModuleExtensions
    {
        public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddMediaCore()
                .AddMediaInfrastructure(configuration);
            return services;
        }
    }
}
