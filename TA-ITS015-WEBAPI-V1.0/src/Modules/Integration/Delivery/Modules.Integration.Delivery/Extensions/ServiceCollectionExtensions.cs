using Microsoft.Extensions.DependencyInjection;
using Modules.Integration.Delivery.Encrypt;
using Modules.Integration.Delivery.Senders;

namespace Modules.Integration.Delivery.Extensions
{
    /// <summary>
    /// Description: Đăng ký các dịch vụ tầng chuyển giao dữ liệu (Delivery).
    /// Created date: 16/10/2026
    /// Modified date: 20/10/2026 (Đăng ký tay các lớp cụ thể của Delivery)
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Đăng ký HttpClient, các kênh gửi, bộ điều phối và bộ mã hóa dạng singleton.
        /// Created date: 16/10/2026
        /// </summary>
        public static IServiceCollection AddDelivery(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddSingleton<HttpDeliverySender>();
            services.AddSingleton<FileDeliverySender>();
            services.AddSingleton<FtpDeliverySender>();
            services.AddSingleton<KafkaDeliverySender>();
            services.AddSingleton<DeliveryDispatcher>();
            services.AddSingleton<EncryptKeyReader>();
            services.AddSingleton<EncryptManager>();

            return services;
        }
    }
}
