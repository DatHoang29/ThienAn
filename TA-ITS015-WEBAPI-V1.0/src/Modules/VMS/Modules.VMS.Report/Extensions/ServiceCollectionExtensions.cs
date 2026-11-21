using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.VMS.Report.Infrastructure.Services.EquipmentHistory;

namespace Modules.VMS.Report.Extensions
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Extension đăng ký các service của module VMS.Report
    /// Created date: 07/10/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVMSReportCore(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddVMSReportInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<VmsEquipmentHistoryListService>();
            return services;
        }
    }
}
