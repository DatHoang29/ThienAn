using ITS.FA.Core;
using ITS.FA.Worker;
using Services.Shared.Logging;

// Chạy được cả console (dotnet run) lẫn Windows Service. Khi chạy như Service, cwd là C:\Windows\System32
// nên trỏ ContentRoot về thư mục chứa exe để đọc đúng appsettings.json và Configuration/Database.json.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Database.json cùng lược đồ với WebAPI và các service khác. Thiếu file thì dừng ngay lúc start.
builder.Configuration.AddJsonFile("Configuration/Database.json", optional: false, reloadOnChange: false);

// DataTransporter.json — section "Nats" cho TransportManager. Không có file thì dịch vụ vẫn chạy, chỉ không phát NATS.
builder.Configuration.AddJsonFile("Configuration/DataTransporter.json", optional: true, reloadOnChange: false);

// Nạp lại biến môi trường để đặt mật khẩu DB ngoài file (DbConnection__ConnectionConfigs__0__ConnectionString).
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddWindowsService(options => options.ServiceName = "ITS FA Monitor");
builder.Services.AddFaServices(builder.Configuration);
builder.Services.AddHostedService<FaWorker>();

var host = builder.Build();

// HeartbeatPublisher ghi log qua LogManager tĩnh; thiếu dòng này thì log NATS bị mất.
LogManager.SetFactory(host.Services.GetRequiredService<ILoggerFactory>());

await host.RunAsync();
