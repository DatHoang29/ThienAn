using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Module.ShareData.Core.Extensions
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
        /// Description: Thêm core infrastructure
        /// Created date: 14/08/2026
        /// </summary>
        public static IServiceCollection AddShareDataCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm core validation
        /// Created date: 14/08/2026
        /// </summary>

        public static IServiceCollection AddShareDataCoreValidation(this IServiceCollection services)
        {
            return services;
        }
    }
}
