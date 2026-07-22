using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Modules.Samplev2.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSamplev2Core(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }


        public static IServiceCollection AddSamplev2Infrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

    }
}