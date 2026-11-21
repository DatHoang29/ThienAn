
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using Modules.Chainzone.Base.Infrastructure;
//using Modules.Chainzone.Base.Infrastructure.Handlers;

namespace Modules.Chainzone.Base.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChainzoneBaseCore(this IServiceCollection services)
        {

            return services;
        }
        public static IServiceCollection AddChainzoneBaseInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //Đăng ký các handler xử lý dữ liệu
            return services;
        }

    }
   
}