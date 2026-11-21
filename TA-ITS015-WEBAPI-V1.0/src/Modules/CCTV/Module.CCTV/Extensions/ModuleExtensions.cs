using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Module.CCTV.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddCctvModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddCctvCore()
                .AddCctvInfrastructure(configuration)
                .AddCctvValidation();
            return services;
        }
    }
}