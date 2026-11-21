using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.FMS.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddFMSModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddFMSCore()
                .AddFMSInfrastructure(configuration);
            return services;
        }

    }
}
