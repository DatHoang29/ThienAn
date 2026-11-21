using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Options;
using System.Reflection;

namespace Module.VideoWall.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Các hàm mở rộng cho Service Collection
    /// Created date: 14/08/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
            /// <summary>
            /// Author: HieuNV
            /// Description: Thêm core
            /// Created date: 14/08/2026
            /// </summary>
        public static IServiceCollection AddVWCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
            /// <summary>
            /// Author: HieuNV
            /// Description: Thêm infrastructure
            /// Created date: 14/08/2026
            /// </summary>


        public static IServiceCollection AddVWInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var deviceSection = configuration.GetSection(VwDeviceOptions.SectionName);
            services.Configure<VwDeviceOptions>(deviceSection);

            // NATS Publisher (fire-and-forget, không còn request-reply)
            services.AddSingleton<Module.VideoWall.Core.Interfaces.IVwNatsPublisher, Module.VideoWall.Infrastructure.Services.Messaging.VwItsNatsPublisher>();

            // Lịch phát kịch bản: mỗi VwSchedule là 1 recurring job Hangfire.
            // Dựng lại toàn bộ job lúc khởi động — chỉ khi Hangfire bật (giống CctvSyncDeviceJobRegistrar).
            if (configuration["Hangfire:Enable"]?.ToLower() == "true")
            {
                services.AddHostedService<Module.VideoWall.Infrastructure.Services.Schedule.VwScheduleJobRegistrar>();
            }
                return services;
            }
            /// <summary>
            /// Author: HieuNV
            /// Description: Thêm validation
            /// Created date: 14/08/2026
            /// </summary>
            public static IServiceCollection AddVWValidation(this IServiceCollection services)
            {
                //services.AddControllers().AddWPValidation();
            return services;
        }
    }
}