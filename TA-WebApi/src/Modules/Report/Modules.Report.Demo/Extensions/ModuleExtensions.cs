using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Report.Demo.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddReportDemoModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddReportDemoCore()
                .AddReportDemoInfrastructure(configuration);
            return services;
        }

    }
}
