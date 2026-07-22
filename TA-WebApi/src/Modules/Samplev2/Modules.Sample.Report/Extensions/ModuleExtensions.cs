using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Sample.Report.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddSampleReportModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSampleReportCore()
                .AddSampleReportInfrastructure(configuration);
            return services;
        }

    }
}
