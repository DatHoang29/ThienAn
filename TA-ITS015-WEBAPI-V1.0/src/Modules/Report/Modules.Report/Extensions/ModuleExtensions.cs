using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Report.Extensions
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Đăng ký phân hệ quản lý báo cáo
    /// Created date: 27/07/2026
    /// </summary>
    public static class ModuleExtensions
    {
        public static IServiceCollection AddReportModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddReportCore()
                .AddReportInfrastructure(configuration)
                .AddReportValidation();
            return services;
        }
    }
}
