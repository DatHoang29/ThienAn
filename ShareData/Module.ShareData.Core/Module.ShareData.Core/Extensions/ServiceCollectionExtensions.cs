using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Module.ShareData.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddShareDataCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddShareDataCoreValidation(this IServiceCollection services)
        {
            return services;
        }
    }
}
