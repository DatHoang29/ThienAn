using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.WP.Report.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đăng ký module WP.Report vào IServiceCollection
    /// Created date: 17/07/2026
    /// </summary>
    public static class ModuleExtensions
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm report module
        /// Created date: 14/08/2026
        /// </summary>
        public static IServiceCollection AddWPReportModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddWPReportCore()
                .AddWPReportInfrastructure(configuration);
            return services;
        }
    }
}
