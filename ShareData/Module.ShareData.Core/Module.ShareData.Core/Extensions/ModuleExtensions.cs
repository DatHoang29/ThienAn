using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Module.ShareData.Core.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddShareDataCoreModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddShareDataCoreInfrastructure(configuration)
                .AddShareDataCoreValidation();
            return services;
        }
    }
}
