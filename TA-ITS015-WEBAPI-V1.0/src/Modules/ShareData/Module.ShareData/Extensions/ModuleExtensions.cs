using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Module.ShareData.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Các hàm mở rộng cho Module
    /// Created date: 14/08/2026
    /// </summary>
    public static class ModuleExtensions
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm module
        /// Created date: 14/08/2026
        /// </summary>
        public static IServiceCollection AddShareDataModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddShareDataCore()
                .AddShareDataInfrastructure(configuration);
            return services;
        }
    }
}
