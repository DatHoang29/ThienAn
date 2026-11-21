using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Module.VideoWall.Extensions
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
        public static IServiceCollection AddVWModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddVWCore()
                .AddVWInfrastructure(configuration);
            return services;
        }
    }
}