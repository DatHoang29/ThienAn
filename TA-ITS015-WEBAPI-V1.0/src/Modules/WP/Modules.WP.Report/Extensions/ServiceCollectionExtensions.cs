using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.WP.Report.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đăng ký Core/Infrastructure cho module WP.Report
    /// Created date: 17/07/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm report core
        /// Created date: 14/08/2026
        /// </summary>
        public static IServiceCollection AddWPReportCore(this IServiceCollection services)
        {
            return services;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm report infrastructure
        /// Created date: 14/08/2026
        /// </summary>

        public static IServiceCollection AddWPReportInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
