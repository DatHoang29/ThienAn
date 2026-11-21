using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.FMS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFMSCore(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddFMSInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

    }
}