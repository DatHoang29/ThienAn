using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Module.ShareData.Extensions
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
        public static IServiceCollection AddShareDataCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm infrastructure
        /// Created date: 14/08/2026
        /// </summary>

        public static IServiceCollection AddShareDataInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm validation
        /// Created date: 14/08/2026
        /// </summary>

        public static IServiceCollection AddShareDataValidation(this IServiceCollection services)
        {
            return services;
        }
    }
}
