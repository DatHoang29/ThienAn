using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Notification.Infrastructure.Services.Config;
using System.Reflection;

namespace Modules.Notification.Extensions
{
    /// <summary>
    /// Author: SonTH
    /// Description: Các hàm mở rộng đăng ký dịch vụ của phân hệ Notification.
    /// Created date: 17/09/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Author: SonTH
        /// Description: Đăng ký tầng Core: validator FluentValidation của phân hệ.
        /// Created date: 17/09/2026
        /// </summary>
        public static IServiceCollection AddNotificationCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Đăng ký hạ tầng: cấu hình Notification.json.
        ///              Các service khác tự đăng ký DI qua marker IScoped của Furion
        ///              (NotiBroadcaster, NotificationPublisher, NotiUserResolver,
        ///              NotiConfigService, NotiRetentionService) nên không khai báo ở đây.
        /// Created date: 17/09/2026
        /// </summary>
        public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //Nạp cấu hình từ Configuration/Notification.json, khóa gốc "Notification".
            services.Configure<NotiSettings>(configuration.GetSection(NotiSettings.SectionName));

            //Warm-up mapping SqlSugar của entity System chạy 1 luồng trước khi nhận request
            //(InitMappingInfo của SqlSugar không thread-safe — xem NotiSystemWarmUpService).
            services.AddHostedService<NotiSystemWarmUpService>();

            return services;
        }
    }
}
