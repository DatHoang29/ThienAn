using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Report.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddReportCore(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddReportInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddReportValidation(this IServiceCollection services)
        {
            services.AddControllers().AddReportValidation();
            return services;
        }
    }
}
