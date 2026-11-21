using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.TOLL.Report.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTollReportCore(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddTollReportInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
