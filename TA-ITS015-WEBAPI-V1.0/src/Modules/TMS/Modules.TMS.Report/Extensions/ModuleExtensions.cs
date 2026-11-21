using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Report.Base.Extensions;

namespace Modules.TMS.Report.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddTMSReportModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddTMSReportCore()
                .AddTMSReportInfrastructure(configuration);
            return services;
        }

    }
}
