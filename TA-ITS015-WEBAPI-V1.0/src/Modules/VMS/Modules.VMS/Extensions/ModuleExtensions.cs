using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.VMS.Extensions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public static class ModuleExtensions
    {
        public static IServiceCollection AddVMSModule(this IServiceCollection services, IConfiguration configuration) {
            services
                .AddVMSCore()
                .AddVMSInfrastructure(configuration)
                .AddVMSValidation()
                ;

            return services;
        }
    }
}
