using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Integration.VMS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationVmsCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        
        public static IServiceCollection AddIntegrationVmsInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
        public static IServiceCollection AddIntegrationVmsValidation(this IServiceCollection services)
        {
            services.AddControllers();
            return services;
        }
    }
}