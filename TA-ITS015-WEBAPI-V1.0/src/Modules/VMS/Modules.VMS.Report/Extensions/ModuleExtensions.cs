using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.VMS.Report.Extensions
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Extension đăng ký toàn bộ phân hệ Modules.VMS.Report vào ứng dụng
    /// Created date: 07/10/2026
    /// </summary>
    public static class ModuleExtensions
    {
        public static IServiceCollection AddVMSReportModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddVMSReportCore()
                .AddVMSReportInfrastructure(configuration);
            return services;
        }
    }
}
