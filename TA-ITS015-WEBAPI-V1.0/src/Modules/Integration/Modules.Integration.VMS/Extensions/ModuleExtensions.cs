using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Integration.VMS.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddIntegrationVmsModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddIntegrationVmsCore()
                .AddIntegrationVmsInfrastructure(configuration)
                .AddIntegrationVmsValidation();
            return services;
        }

    }
}
