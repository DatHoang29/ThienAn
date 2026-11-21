
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;



namespace Modules.DataTransporter.SocketCluster.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataTransporterSocketClusterCore(this IServiceCollection services)
        {

            return services;
        }
        public static IServiceCollection AddDataTransporterSocketClusterInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

    }
   
}