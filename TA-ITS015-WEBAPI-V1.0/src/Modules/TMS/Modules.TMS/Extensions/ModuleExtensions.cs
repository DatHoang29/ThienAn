using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.TMS.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddTMSModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddTMSCore()
                .AddTMSInfrastructure(configuration)
                .AddTMSValidation();
            return services;
        }

    }
}
