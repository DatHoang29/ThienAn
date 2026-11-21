using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.DataTransporter.Natsio.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataTransporterNatsioCore(
        this IServiceCollection services) => services;

    public static IServiceCollection AddDataTransporterNatsioInfrastructure(
        this IServiceCollection services, IConfiguration configuration) => services;
}
