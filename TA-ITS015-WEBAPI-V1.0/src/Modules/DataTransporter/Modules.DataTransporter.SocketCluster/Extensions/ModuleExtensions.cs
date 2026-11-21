using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.DataTransporter.SocketCluster.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddDataTransporterSocketClusterModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddDataTransporterSocketClusterCore()
                .AddDataTransporterSocketClusterInfrastructure(configuration);
            return services;
        }

    }
}
