using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Report.Base.Extensions;
//using Modules.Report.Carbone.Extensions;

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
