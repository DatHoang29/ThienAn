
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.DataTransporter.ITS.Infrastructure.Services;

namespace Modules.DataTransporter.ITS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataTransporterItsCore(this IServiceCollection services)
        {

            return services;
        }
        public static IServiceCollection AddDataTransporterItsInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //Call Register channel name
            ItsDataTransporter.RegisterChannel();
            return services;
        }

    }
   
}