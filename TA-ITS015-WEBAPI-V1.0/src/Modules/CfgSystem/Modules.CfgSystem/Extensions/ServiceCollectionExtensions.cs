using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.CfgSystem.Extensions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSysConfigCore(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddSysConfigValidation(this IServiceCollection services)
        {
            services.AddControllers().AddSysConfigValidation();

            return services;
        }

        public static IServiceCollection AddSysConfigInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
