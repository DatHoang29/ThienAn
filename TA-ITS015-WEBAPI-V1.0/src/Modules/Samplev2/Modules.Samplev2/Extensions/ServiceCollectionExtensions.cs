using System.Diagnostics;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence;
using SqlSugar;

namespace Modules.Samplev2.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSampleCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        
        public static IServiceCollection AddSampleInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
        public static IServiceCollection AddSampleValidation(this IServiceCollection services)
        {
            services.AddControllers().AddSampleValidation();
            return services;
        }
    }
}