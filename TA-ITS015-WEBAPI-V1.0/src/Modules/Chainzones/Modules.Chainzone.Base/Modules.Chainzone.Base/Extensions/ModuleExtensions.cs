using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Chainzone.Base.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddChainzoneBaseModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddChainzoneBaseCore()
                .AddChainzoneBaseInfrastructure(configuration);
            return services;
        }

    }
}
