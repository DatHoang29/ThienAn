using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.FSystem.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddFSystemModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddSystemCore()
                .AddSystemInfrastructure(configuration);
            return services;
        }

    }
}
