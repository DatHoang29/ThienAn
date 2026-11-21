using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.FMS.Report.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFMSReportCore(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddFMSReportInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}