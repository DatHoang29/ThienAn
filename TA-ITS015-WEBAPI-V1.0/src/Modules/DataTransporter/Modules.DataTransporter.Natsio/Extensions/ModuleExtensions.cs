using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.DataTransporter.Natsio.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddDataTransporterNatsioModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDataTransporterNatsioCore()
            .AddDataTransporterNatsioInfrastructure(configuration);
        return services;
    }
}
