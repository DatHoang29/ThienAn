using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Template.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddTemplateModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddTemplateCore()
                .AddTemplateInfrastructure(configuration)
                .AddTemplateValidation();
            return services;
        }

    }
}
