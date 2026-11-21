using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.VDS.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddVDSModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddVDSCore()
                .AddVDSInfrastructure(configuration)
                .AddVDSValidation();
            return services;
        }

    }
}
