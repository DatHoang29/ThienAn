using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.CfgSystem.Extensions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public static class ModuleExtensions
    {
        public static IServiceCollection AddCfgSystemModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddSysConfigCore()
                .AddSysConfigInfrastructure(configuration)
                .AddSysConfigValidation();

            return services;
        }
    }
}
