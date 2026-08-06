using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Module.ShareData.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddShareDataModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddShareDataCore()
                .AddShareDataInfrastructure(configuration);
            return services;
        }
    }
}
