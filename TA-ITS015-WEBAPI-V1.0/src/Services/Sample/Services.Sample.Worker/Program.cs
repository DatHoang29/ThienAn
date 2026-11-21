using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Sample.Configuration;
using Services.Sample.Extensions;
using Services.Shared.Hosting;
using Services.Shared.Logging;

// Worker Service chạy như Windows Service (xem https://learn.microsoft.com/dotnet/core/extensions/windows-service).
// Host lo lifecycle của NatsSource/ApiSource/DataWorker/ConfigReloadWorker. Manager được gọi tường minh:
// Init+Start SAU khi host start (source/worker đã sẵn sàng), Stop TRƯỚC khi host stop (reverse order).
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Load Database.json + DataTransporter.json + Logging.json từ thư mục Configuration/ (gộp vào IConfiguration root).
builder.Configuration.AddJsonFile("Configuration/Database.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile("Configuration/DataTransporter.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile("Configuration/Logging.json", optional: true, reloadOnChange: false);

// AddWindowsService + logging (ZLogger, config từ Logging.json) + DI registrations.
HostProfiles.ConfigureWorkerService(builder, serviceName: "TA-ITS015-Sample");
builder.Services.AddSampleServices(builder.Configuration);

var host = builder.Build();

// Set static singleton cho ConfigManager để truy cập từ bất cứ đâu: ConfigManager.Current.Api
ConfigManager.SetInstance(host.Services.GetRequiredService<IConfigManager>());

// Khởi tạo backing logger cho các hàm tiện lợi LogManager.Info/Warn/Error (gọi ở đâu cũng được).
LogManager.SetFactory(host.Services.GetRequiredService<ILoggerFactory>());

// Start source/worker hosted services trước, rồi mới Init+Start manager (nạp config, bật runner nếu Enabled).
await host.StartAsync();

var manager = host.Services.GetRequiredService<IServiceManager>();
var config = host.Services.GetRequiredService<IConfigManager>();
await manager.InitAsync();
// Worker (headless) auto-start acquisition theo Devices.Enabled; WPF thì operator tự bấm Start.
if (config.Current.Devices.Enabled)
    await manager.StartAsync();

// Chờ tín hiệu shutdown, rồi dừng manager TRƯỚC khi host stop → runner dừng trước khi DataWorker drain queue.
await host.WaitForShutdownAsync();
await manager.StopAsync();
await host.StopAsync();
