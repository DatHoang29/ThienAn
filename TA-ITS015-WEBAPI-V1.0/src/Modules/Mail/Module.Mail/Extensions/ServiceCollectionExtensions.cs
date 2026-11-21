using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Modules.Mail.Core.Dto.Smtp;
using Modules.Mail.Infrastructure.Services.Mail.Schedule;

namespace Modules.Mail.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Các hàm mở rộng đăng ký dịch vụ của phân hệ Mail. Các service khác tự đăng ký DI qua marker
    ///              IScoped của Furion nên không khai báo ở đây.
    /// Created date: 13/08/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>Đăng ký tầng Core: các validator FluentValidation của phân hệ.</summary>
        public static IServiceCollection AddMailCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }

        /// <summary>Đăng ký hạ tầng: phần cấu hình ở Mail.json và việc chạy lúc khởi động (RptEmailStartupService).</summary>
        public static IServiceCollection AddMailInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MailSettings>(configuration.GetSection("Mail"));

            // Kiểm tra danh mục báo cáo + đồng bộ Hangfire. Hangfire tắt thì RptEmailJobScheduler tự bỏ qua.
            services.AddHostedService<RptEmailStartupService>();

            return services;
        }
    }
}
