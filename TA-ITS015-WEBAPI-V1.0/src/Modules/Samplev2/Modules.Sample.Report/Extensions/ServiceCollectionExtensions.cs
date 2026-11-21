using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Shared.Utility.Endpoints;

namespace Modules.Sample.Report.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSampleReportCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        
        public static IServiceCollection AddSampleReportInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
           
            return services;
        }
      
    }
}