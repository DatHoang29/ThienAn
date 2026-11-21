
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Chainzones.ITS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChainzonesItsCore(this IServiceCollection services)
        {

            return services;
        }
        public static IServiceCollection AddChainzonesItsInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //Call Register channel name
            //ItsVmsControl.RegisterChannel();
            return services;
        }

    }
   
}