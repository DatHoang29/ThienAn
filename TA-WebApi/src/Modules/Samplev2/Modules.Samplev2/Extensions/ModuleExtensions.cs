using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Samplev2.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection AddSamplev2Module(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddSamplev2Core()
                .AddSamplev2Infrastructure(configuration);
            return services;
        }

    }
}
