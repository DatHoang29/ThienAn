using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.FMS.Report.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddFMSReportModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddFMSReportCore()
                .AddFMSReportInfrastructure(configuration);
            return services;
        }
    }
}
