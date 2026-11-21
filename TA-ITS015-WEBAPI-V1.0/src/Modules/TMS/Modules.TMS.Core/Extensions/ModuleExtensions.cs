using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.TMS.Core.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddTMSCoreModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddTMSCoreInfrastructure(configuration)
                .AddTMSCoreValidation();
            return services;
        }

    }
}
