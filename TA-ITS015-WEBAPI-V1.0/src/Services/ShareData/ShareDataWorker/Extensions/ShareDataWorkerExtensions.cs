using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Extensions;
using ShareDataWorker.Infrastructure.Services.DataChangeTracking;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.DataNats;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using ShareDataWorker.Infrastructure.Workers;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using ShareDataWorker.Infrastructure.Services.DataCleanup;
using Services.Shared.Messaging;

namespace ShareDataWorker.Extensions
{
    /// <summary>
    /// Extension chứa cấu hình Dependency Injection cho Worker Service (Rule 2.1.3)
    /// Author: Đạt
    /// Created date: 31/07/2026
    /// Modified date: 10/11/2026
    /// </summary>
    public static class ShareDataWorkerExtensions
    {
        /// <summary>
        /// ConfigId định danh cho kết nối CSDL LUỒNG NHẬN (DEV_ITS10_Inbound).
        /// Lấy bằng: db.AsTenant().GetConnectionScope(ShareDataWorkerExtensions.InboundConnectionKey)
        /// </summary>
        public const string InboundConnectionKey = "Inbound";

        /// <summary>
        /// Author: DatHQ
        /// Description: Đặt bảng mã UTF-8 cho console để log tiếng Việt hiển thị đúng.
        /// Created date: 31/07/2026
        /// </summary>
        public static IHostBuilder UseCustomEnvironment(this IHostBuilder builder)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            return builder;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lấy tên dịch vụ Windows từ ShareData:ServiceName để mỗi bản clone chạy dưới một tên riêng, trùng tên đã đăng ký dịch vụ.
        ///              Không khai thì dùng ShareDataWorker.
        /// Created date: 10/11/2026
        /// </summary>
        public static IHostBuilder UseConfiguredServiceName(this IHostBuilder builder)
        {
            return builder.ConfigureServices((hostContext, services) =>
            {
                var configured = hostContext.Configuration["ShareData:ServiceName"];
                var serviceName = string.IsNullOrWhiteSpace(configured) ? "ShareDataWorker" : configured.Trim();
                services.Configure<WindowsServiceLifetimeOptions>(options => options.ServiceName = serviceName);
            });
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: FileStorage:BasePath tương đối (hoặc để trống) tính theo ContentRoot như appsettings.json, không theo thư mục hiện hành
        ///              vì dịch vụ Windows chạy ở thư mục hệ thống: chạy bằng net start thì nằm cạnh exe, chạy bằng dotnet run thì nằm trong thư mục project.
        ///              Đường dẫn tuyệt đối giữ nguyên.
        /// Created date: 10/11/2026
        /// </summary>
        public static IHostBuilder UseContentRootStoragePath(this IHostBuilder builder)
        {
            return builder.ConfigureServices((hostContext, _) =>
            {
                var configured = hostContext.Configuration[ExportPathHelper.BasePathConfigKey];
                var basePath = string.IsNullOrWhiteSpace(configured) ? ExportPathHelper.DefaultBasePath : configured.Trim();
                if (!Path.IsPathRooted(basePath))
                {
                    var contentRoot = hostContext.HostingEnvironment.ContentRootPath;
                    hostContext.Configuration[ExportPathHelper.BasePathConfigKey] = Path.Combine(contentRoot, basePath);
                }
            });
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đăng ký tầng kết nối (AddDelivery) và các Service lõi ShareDataWorker dùng chung
        ///              cho cả Worker Service thật (AddWorkerInfrastructure) lẫn môi trường Test tích hợp.
        /// Created date: 18/08/2026
        /// Modified date: 19/10/2026
        /// </summary>
        public static IServiceCollection AddShareDataWorkerCoreServices(this IServiceCollection services)
        {
            services.AddDelivery();
            services.AddSingleton<TransportManager>();
            services.AddSingleton<DataWorkerConfigService>();
            services.AddSingleton<DataCleanupService>();
            services.AddSingleton<DataOutboundExtractionProcess>();
            services.AddSingleton<DataOutboundTransport>();
            services.AddSingleton<DataOutboundService>();
            services.AddSingleton<DataInboundService>();
            services.AddSingleton<DataChangeTrackingService>();
            services.AddSingleton<DataNatsService>();

            return services;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đăng ký kết nối CSDL, các service lõi, tiến trình nền theo cờ ShareData:Role và HeartbeatPublisher cho Worker chạy thật.
        ///              Không khai hoặc Both: đủ 5 tiến trình. SendOnly: bỏ DataInboundWorker. ReceiveOnly: chỉ DataInboundWorker và DataCleanupWorker. Giá trị khác thì ném lỗi lúc khởi động.
        ///              Thiếu DefaultConnection thì tự dùng localhost, CSDL dev_its10 chứ không báo lỗi nên khi triển khai phải kiểm kỹ.
        ///              Kết nối CSDL nhận (Inbound) chỉ được tạo khi có khai InboundConnection.
        /// Created date: 31/07/2026
        /// </summary>
        public static IServiceCollection AddWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? "Server=localhost;Database=dev_its10;Trusted_Connection=True;TrustServerCertificate=True;";

            var configs = new List<ConnectionConfig>
            {
                new ConnectionConfig
                {
                    ConnectionString = connectionString,
                    DbType = SqlSugar.DbType.SqlServer,
                    IsAutoCloseConnection = true,
                    ConfigId = "Default",
                }
            };

            // Kết nối CSDL LUỒNG NHẬN — đưa vào cùng SqlSugarScope đa kết nối (Multi-Tenant)
            // Lấy ra bằng: db.AsTenant().GetConnectionScope(ShareDataWorkerExtensions.InboundConnectionKey)
            var inboundConnectionString = configuration.GetConnectionString("InboundConnection");
            if (!string.IsNullOrWhiteSpace(inboundConnectionString))
            {
                configs.Add(new ConnectionConfig
                {
                    ConnectionString = inboundConnectionString,
                    DbType = SqlSugar.DbType.SqlServer,
                    IsAutoCloseConnection = true,
                    ConfigId = InboundConnectionKey,
                });
            }

            services.AddScoped<ISqlSugarClient>(_ => new SqlSugarScope(configs));
            services.AddShareDataWorkerCoreServices();

            // Đăng ký Master Workers theo cờ vai trò ShareData:Role (SV-4)
            var role = configuration["ShareData:Role"]?.Trim();
            if (string.IsNullOrEmpty(role) || string.Equals(role, "Both", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHostedService<DataOutboundWorker>();
                services.AddHostedService<DataInboundWorker>();
                services.AddHostedService<DataChangeTrackingWorker>();
                services.AddHostedService<DataCleanupWorker>();
                services.AddHostedService<DataNatsWorker>();
            }
            else if (string.Equals(role, "SendOnly", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHostedService<DataOutboundWorker>();
                services.AddHostedService<DataChangeTrackingWorker>();
                services.AddHostedService<DataCleanupWorker>();
                services.AddHostedService<DataNatsWorker>();
            }
            else if (string.Equals(role, "ReceiveOnly", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHostedService<DataInboundWorker>();
                services.AddHostedService<DataCleanupWorker>();
            }
            else
            {
                throw new InvalidOperationException(
                    $"Giá trị cấu hình ShareData:Role không hợp lệ: '{role}'. Chỉ chấp nhận: Both, SendOnly, ReceiveOnly.");
            }

            services.AddHostedService<HeartbeatPublisher>();

            return services;
        }
    }
}
