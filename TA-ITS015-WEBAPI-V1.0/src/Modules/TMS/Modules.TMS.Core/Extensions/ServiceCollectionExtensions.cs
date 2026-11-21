using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.TMS.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddTMSCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            return services;
        }

        public static IServiceCollection AddTMSCoreValidation(this IServiceCollection services)
        {
            return services;
        }

    }
}