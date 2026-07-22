using Mapster;
using Mapster.Utils;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.System.Extensions;
using System.Reflection;

namespace Modules.FSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSystemCore(this IServiceCollection services)
        {
            TypeAdapterConfig.GlobalSettings.Default
                .PreserveReference(true);
            var a = Assembly.GetExecutingAssembly();
            TypeAdapterConfig.GlobalSettings.ScanInheritedTypes(Assembly.GetExecutingAssembly());

            var typeConfig = TypeAdapterConfig.GlobalSettings;
            typeConfig.Scan(Assembly.GetExecutingAssembly());
            var mapConfig = new Mapper(typeConfig);
            services.AddSingleton<IMapper>(mapConfig);


            return services;
        }


        public static IServiceCollection AddSystemInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddSystemModule(configuration);

            return services;
        }

    }
}