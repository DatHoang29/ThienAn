using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Template.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTemplateCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        
        public static IServiceCollection AddTemplateInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
        public static IServiceCollection AddTemplateValidation(this IServiceCollection services)
        {
            services.AddControllers();
            return services;
        }
    }
}