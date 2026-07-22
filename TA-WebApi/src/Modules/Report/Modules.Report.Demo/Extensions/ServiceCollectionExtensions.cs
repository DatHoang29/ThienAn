
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Report.Demo.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddReportDemoCore(this IServiceCollection services)
        {

            return services;
        }
        public static IServiceCollection AddReportDemoInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

    }

}