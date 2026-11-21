using Microsoft.Extensions.DependencyInjection;
using Module.Integration.CCTV.Core.Abstracts;
using Module.Integration.CCTV.Infrastructure.Services;

namespace Module.Integration.NxVms.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Đăng ký toàn bộ service NxVMS vào DI container.
        ///
        /// INxVmsConfigProvider được đăng ký từ CCTV module (CctvConfigService).
        /// NxVms.Infrastructure chỉ biết interface — không reference CCTV.
        ///
        /// Author: ChuongLP
        /// Date: 2026-06-09
        /// </summary>
        public static IServiceCollection AddNxVmsIntegration(this IServiceCollection services)
        {
            static HttpClientHandler InsecureHandler() => new()
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            // Timeout 15s (21/10/2026): request NxVMS chỉ là lệnh/URL ngắn (video không đi qua WebAPI);
            // mặc định 100s khiến FE chờ rất lâu khi NxVMS treo.
            static void ConfigureClient(HttpClient client) => client.Timeout = TimeSpan.FromSeconds(15);

            services
                .AddHttpClient<NxVmsAuthService>()
                .ConfigureHttpClient(ConfigureClient)
                .ConfigurePrimaryHttpMessageHandler(InsecureHandler);

            services
                .AddHttpClient<NxVmsApiService>()
                .ConfigureHttpClient(ConfigureClient)
                .ConfigurePrimaryHttpMessageHandler(InsecureHandler);

            // INxVmsConfigProvider được đăng ký bên CCTV module (CctvConfigService)
            // NxVmsConfigService nhận interface qua DI — không cần biết implement là gì
            services.AddScoped<NxVmsConfigService>();

            services.AddScoped<NxVmsAuthService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient(nameof(NxVmsAuthService));
                var configService = sp.GetRequiredService<NxVmsConfigService>();
                return new NxVmsAuthService(httpClient, configService);
            });

            services.AddScoped<NxVmsApiService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient(nameof(NxVmsApiService));
                var authService = sp.GetRequiredService<NxVmsAuthService>();
                var configService = sp.GetRequiredService<NxVmsConfigService>();
                return new NxVmsApiService(httpClient, authService, configService);
            });

            // Đăng ký từng vendor — thêm dòng mới khi có vendor mới
            services.AddScoped<NxVmsService>();

            // services.AddScoped<HikvisionService>(); // mở ra khi cần
            // Factory nằm ở CCTV module — đăng ký ở AddCctvInfrastructure
            // services.AddScoped<VmsServiceFactory>() ← xem AddCctvInfrastructure_snippet

            return services;
        }
    }
}
