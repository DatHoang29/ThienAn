using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Samplev2.Extensions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đăng ký module WP (WebPortal) vào IServiceCollection
    /// Created date: 03/07/2026
    /// </summary>
    public static class ModuleExtensions
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm module
        /// Created date: 14/08/2026
        /// </summary>
        public static IServiceCollection AddWPModule(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddWPCore()
                .AddWPInfrastructure(configuration)
                .AddWPValidation();
            return services;
        }

    }
}
