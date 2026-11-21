using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.TOLL.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddTollModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddTollCore()
                .AddTollInfrastructure(configuration)
                .AddTollValidation();
            return services;
        }
    }
}
