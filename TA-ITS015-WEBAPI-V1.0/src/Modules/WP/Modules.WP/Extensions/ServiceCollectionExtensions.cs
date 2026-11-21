using System.Diagnostics;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence;
using SqlSugar;

namespace Modules.Samplev2.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đăng ký Core/Infrastructure/Validation cho module WP (WebPortal)
    /// Created date: 03/07/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm core
        /// Created date: 14/08/2026
        /// </summary>
        public static IServiceCollection AddWPCore(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm infrastructure
        /// Created date: 14/08/2026
        /// </summary>

        
        public static IServiceCollection AddWPInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm validation
        /// Created date: 14/08/2026
        /// </summary>
        public static IServiceCollection AddWPValidation(this IServiceCollection services)
        {
            //services.AddControllers().AddWPValidation();
            return services;
        }
    }
}