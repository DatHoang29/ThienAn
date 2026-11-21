using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Module.VideoWall.Core.Extensions
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
        /// Description: Thêm core module
        /// Created date: 14/08/2026
        /// </summary>
        public static IServiceCollection AddVWCoreModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddVWCoreInfrastructure(configuration)
                .AddVWCoreValidation();
            return services;
        }

    }
}
