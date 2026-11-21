
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.DataTransporter.Base.Infrastructure;
using Modules.DataTransporter.Base.Infrastructure.Handlers;
using Modules.DataTransporter.Natsio.Infrastructure.Configuration;
using Modules.DataTransporter.Natsio.Infrastructure.Handlers;

namespace Modules.DataTransporter.Base.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataTransporterBaseCore(this IServiceCollection services)
        {

            return services;
        }
        public static IServiceCollection AddDataTransporterBaseInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Đăng ký SocketCluster handler nếu enabled
            var scEnabled = App.GetConfig<bool>("SocketCluster:Enabled", false);
            if (scEnabled)
            {
                var scUrl = App.Configuration["SocketCluster:Url"];
                BaseDataTransporter.RegisterHandler(new SocketClusterEventHandler(true, scUrl));
            }

            // Đăng ký NATS handler nếu enabled
            var natsEnabled = App.GetConfig<bool>("Nats:Enabled", false);
            if (natsEnabled)
            {
                var natsConfig = App.GetConfig<NatsRuntimeConfig>("Nats", false);
                if (natsConfig != null && !string.IsNullOrWhiteSpace(natsConfig.Url))
                {
                    var streams = App.GetConfig<List<NatsStreamConfig>>("Nats:Streams", false);
                    var handler = new NatsEventHandler(natsConfig, streams);

                    var subjects = App.GetConfig<List<NatsSubjectConfig>>("Nats:Subjects", false);
                    if (subjects != null)
                    {
                        foreach (var subject in subjects)
                            handler.RegisterSubject(subject);
                    }

                    handler.WaitForInitializationAsync().GetAwaiter().GetResult();
                    BaseDataTransporter.RegisterHandler(handler);
                }
            }

            return services;
        }

    }

}
