using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Module.ShareData.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddShareDataCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        public static IServiceCollection AddShareDataInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddShareDataValidation(this IServiceCollection services)
        {
            return services;
        }
    }
}
