using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// For: IConfiguration
using Microsoft.Extensions.Configuration;

// For: IServiceCollection
using Microsoft.Extensions.DependencyInjection;

namespace Modules.CfgSystem.Extensions
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: Lớp đăng ký các service mở rộng vào đối tượng container global IoC/DI/Factory IServiceCollection
    /// Created date: 09/08/2024
    /// Note: Sử dụng phương thức mở rộng (extension method) để tiêm các service cần dùng vào IServiceCollection
    /// Note: Sử dụng lớp tĩnh static để mở rộng (extension method) cho container IoC/DI/Factory IServiceCollection 
    /// </summary>

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSysConfigCore(this IServiceCollection services)
        {
            return services;
        }
        
        public static IServiceCollection AddSysConfigValidation(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddSysConfigInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
