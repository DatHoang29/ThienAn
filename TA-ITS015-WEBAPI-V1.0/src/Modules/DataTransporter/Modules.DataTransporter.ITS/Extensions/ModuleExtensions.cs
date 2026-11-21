using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.DataTransporter.Base.Extensions;

namespace Modules.DataTransporter.ITS.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddDataTransporterItsModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddDataTransporterBaseModule(configuration)
                .AddDataTransporterItsCore()
                .AddDataTransporterItsInfrastructure(configuration)
                ;
            return services;
        }

    }
}
