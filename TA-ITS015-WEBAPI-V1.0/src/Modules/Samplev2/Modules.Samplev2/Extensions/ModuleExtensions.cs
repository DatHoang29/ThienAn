using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Samplev2.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddSamplev2Module(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddSampleCore()
                .AddSampleInfrastructure(configuration)
                .AddSampleValidation();
            return services;
        }

    }
}
