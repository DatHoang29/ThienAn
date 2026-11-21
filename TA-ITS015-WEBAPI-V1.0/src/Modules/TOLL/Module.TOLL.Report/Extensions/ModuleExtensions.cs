using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Report.Base.Extensions;

namespace Modules.TOLL.Report.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddTollReportModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddTollReportCore()
                .AddTollReportInfrastructure(configuration);
            return services;
        }
    }
}
