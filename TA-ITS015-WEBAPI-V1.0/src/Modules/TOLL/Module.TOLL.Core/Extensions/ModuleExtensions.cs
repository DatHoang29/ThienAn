using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.TOLL.Core.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddTollCoreModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddTollCoreInfrastructure(configuration)
                .AddTollCoreValidation();
            return services;
        }
    }
}