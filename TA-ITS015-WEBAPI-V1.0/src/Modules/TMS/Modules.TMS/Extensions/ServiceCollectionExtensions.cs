using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.TMS.Infrastructure.Services.Incident;
using Modules.TMS.Infrastructure.Services.IncidentAutomation;
using Modules.TMS.Infrastructure.Services.Weather;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;
using System.Diagnostics;

namespace Modules.TMS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTMSCore(this IServiceCollection services)
        {
            services.AddHostedService<WeatherService>();
            Shared.Utility.Sse.SnapshotSseAuthorization.AddSnapshotSse(services);

            // SonTH - 24/07/2026: Worker kích hoạt luồng tự động xử lý sự cố (thay cho Hangfire).
            // Quét TmsIncidentAutomationRun theo chu kỳ ngắn để chạy/kết thúc run khi tới hạn.
            services.AddHostedService<TmsIncidentAutomationWorker>();

            // Chụp ảnh camera chạy độc lập với request cập nhật trạng thái sự cố.
            services.AddSingleton<IncidentSnapshotQueue>();
            // Bộ đệm snapshot SSE phải dùng chung giữa mọi request; theo scope thì mỗi kết nối một bộ đệm
            // riêng và việc gộp truy vấn mất tác dụng. Cùng lý do với IncidentSnapshotQueue ngay trên.
            services.AddSingleton<Modules.TMS.Infrastructure.Services.Incident.Snapshot.IncidentSnapshotCache>();
            services.AddHostedService<IncidentSnapshotWorker>();
            services.AddScoped<VdsImageShareSettingsReader>();
            services.AddSingleton(sp => new Modules.VDS.Core.Services.Media.VdsImageShareAccess(async (root, ct) =>
            {
                using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
                return await scope.ServiceProvider.GetRequiredService<VdsImageShareSettingsReader>().ReadAsync(root, ct);
            }));
            services.AddSingleton(sp => new Modules.VDS.Core.Services.Media.VdsRegistrationImageStore(
                sp.GetRequiredService<IConfiguration>().GetSection(Modules.VDS.Core.Options.VdsRegistrationImageOptions.Section)
                    .Get<Modules.VDS.Core.Options.VdsRegistrationImageOptions>() ?? new(),
                sp.GetRequiredService<Modules.VDS.Core.Services.Media.VdsImageShareAccess>()));
            // KienLT - 14/10/2026: ảnh sự cố chuyển từ wwwroot/tms/incident lên thư mục cấu hình (share dùng chung với ảnh VDS).
            services.AddSingleton(sp => new IncidentImageStorage(
                sp.GetRequiredService<IConfiguration>().GetSection(IncidentImageOptions.Section).Get<IncidentImageOptions>() ?? new(),
                sp.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(),
                sp.GetRequiredService<Modules.VDS.Core.Services.Media.VdsImageShareAccess>()));
            services.AddScoped<VdsAutomationImagePolicy>();
            services.AddHostedService<VdsRegistrationImageWorker>();

            return services;
        }

        public static IServiceCollection AddTMSInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            ItsDataTransporter.ConsumeData(ItsDataTransporterChannel.GetStatus, _ =>
            {
                try
                {
                    var scopeFactory = App.GetService<IServiceScopeFactory>();
                    using var scope = scopeFactory.CreateScope();
                    var cache = scope.ServiceProvider.GetService<BaseCacheService>();
                    cache?.RemoveByPrefixKey(CacheConst.Tms.TmsEquipment);
                }
                catch
                {
                    // Nuốt lỗi: không để exception làm rớt vòng subscribe NATS.
                }
            });

            return services;
        }

        public static IServiceCollection AddTMSValidation(this IServiceCollection services)
        {
            services.AddControllers().AddTMSValidation();
            return services;
        }
    }
}
