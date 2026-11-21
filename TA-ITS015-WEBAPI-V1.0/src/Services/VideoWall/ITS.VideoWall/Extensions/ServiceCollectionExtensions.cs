using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using ITS.VideoWall.Services.ISAPIDevice;
using ITS.VideoWall.Messaging.Consumers;
using ITS.VideoWall.Messaging.Publishers;
using ITS.VideoWall.Services.Heartbeat;
using Services.Shared.Extensions;
using Shared.Infrastructure.Services;

namespace ITS.VideoWall.Extensions
{
    /// <summary>
    /// Description: Extension methods đăng ký các dịch vụ của ITS.VideoWall Worker vào ServiceCollection
    /// Created date: 10/09/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Description: Đăng ký đầy đủ hạ tầng và dịch vụ của ITS.VideoWall Worker khi chạy như host thật
        /// Created date: 13/09/2026
        /// </summary>
        public static IServiceCollection AddVideoWallWorkerHost(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<NewLife.Caching.ICacheProvider, NewLife.Caching.CacheProvider>();
            services.AddSingleton<BaseCacheService>();
            services.AddVideoWallWorker(configuration);

            return services;
        }

        /// <summary>
        /// Description: Đăng ký toàn bộ dịch vụ nghiệp vụ cốt lõi (Core Services) của VideoWall Worker.
        ///              Dùng chung cho cả tiến trình Worker và Test Host. Không chứa HostedService.
        /// Created date: 14/09/2026
        /// </summary>
        public static IServiceCollection AddVideoWallWorkerCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ─── Options ───
            var deviceSection = configuration.GetSection(VwDeviceOptions.SectionName);
            services.Configure<VwDeviceOptions>(deviceSection);

            // ─── Singletons: Hạ tầng dùng chung suốt vòng đời ứng dụng ───
            services.AddServiceFoundation(configuration);
            services.AddSingleton<IVwNatsPublisher, VwTransportPublisher>();
            services.AddSingleton<VwISAPICredentialResolver>();

            // ─── Scoped: Database, Repositories và Business Services (phụ thuộc kết nối DB) ───
            services.AddScoped(typeof(BaseRepository<>));
            services.AddScoped<IVwISAPIDeviceService, VwISAPIDeviceService>();
            services.AddScoped<IVwSceneRegionService, Services.Scene.VwSceneRegionService>();
            services.AddScoped<IVwEventTriggerLogService, Services.EventTriggerLog.VwEventTriggerLogService>();

            // ─── Transient & HttpClient ───
            services.AddTransient<VwISAPIDigestHandler>();
            // Hạn chờ đặt theo từng request trong VwISAPIDeviceClient.SendCoreAsync (mặc định Device.TimeoutSeconds,
            // lệnh bật/tắt màn dài hơn) ⇒ HttpClient không tự giới hạn.
            services.AddHttpClient<IVwISAPIDeviceClient, VwISAPIDeviceClient>(client => client.Timeout = Timeout.InfiniteTimeSpan)
                .AddHttpMessageHandler<VwISAPIDigestHandler>();

            return services;
        }

        /// <summary>
        /// Description: Đăng ký toàn bộ dịch vụ của Worker gồm Core Services + Hosted Services (Background Workers)
        /// Created date: 10/09/2026
        /// </summary>
        public static IServiceCollection AddVideoWallWorker(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddVideoWallWorkerCoreServices(configuration);

            // ─── Hosted Services: Background Workers ───
            // 1. NATS Consumer: Lắng nghe command điều khiển (Event-Driven)
            services.AddHostedService<VwCommandConsumer>();

            // 2. Heartbeat Service: Định kỳ thăm dò trạng thái phần cứng ISAPI (Timer-Based Polling)
            services.AddHostedService<VwDeviceHeartbeatService>();

            return services;
        }
    }
}
