using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.CfgSystem.Extensions
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: Lớp đăng ký các service mở rộng vào đối tượng container global IoC/DI/Factory IServiceCollection
    /// Created date: 09/08/2024
    /// Note: Sử dụng phương thức mở rộng (extension method) để tiêm các service cần dùng vào IServiceCollection
    /// Note: Sử dụng lớp tĩnh static để mở rộng (extension method) cho container IoC/DI/Factory IServiceCollection 
    /// </summary>
    public static class ModuleExtensions
    {
        public static IServiceCollection AddCfgSystemModule(this IServiceCollection services, IConfiguration configuration) {
            services
                .AddSysConfigCore()
                .AddSysConfigValidation(configuration)
                .AddSysConfigInfrastructure(configuration);

            return services;
        }
    }
}
