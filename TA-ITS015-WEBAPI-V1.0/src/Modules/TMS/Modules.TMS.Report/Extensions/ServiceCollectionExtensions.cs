
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.TMS.Report.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTMSReportCore(this IServiceCollection services)
        {

            return services;
        }
        public static IServiceCollection AddTMSReportInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

    }
   
}