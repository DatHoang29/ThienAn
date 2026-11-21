using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Modules.TOLL.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTollCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddTollCoreValidation(this IServiceCollection services)
        {
            return services;
        }
    }
}
