using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.DataTransporter.Natsio.Extensions;
using Modules.DataTransporter.SocketCluster.Extensions;

namespace Modules.DataTransporter.Base.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddDataTransporterBaseModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddDataTransporterSocketClusterModule(configuration)
                .AddDataTransporterNatsioModule(configuration)
                .AddDataTransporterBaseCore()
                .AddDataTransporterBaseInfrastructure(configuration);
            return services;
        }

    }
}
