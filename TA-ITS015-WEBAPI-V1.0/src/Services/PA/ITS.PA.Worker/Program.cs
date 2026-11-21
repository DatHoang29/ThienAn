using ITS.PA.Core;
using ITS.PA.Worker;
using Services.Shared.Logging;

// Chạy được cả console (dotnet run) lẫn Windows Service. Khi chạy như Service, cwd là C:\Windows\System32
// nên trỏ ContentRoot về thư mục chứa exe để đọc đúng appsettings.json.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// DataTransporter.json — section "Nats" cho TransportManager. Không có file thì dịch vụ vẫn chạy, chỉ không nhận lệnh.
builder.Configuration.AddJsonFile("Configuration/DataTransporter.json", optional: true, reloadOnChange: false);

// Nạp lại biến môi trường để đặt mật khẩu ngoài file (Service__Pa__Password).
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddWindowsService(options => options.ServiceName = "ITS PA Control");
builder.Services.AddPaServices(builder.Configuration);
builder.Services.AddHostedService<PaWorker>();

var host = builder.Build();

// HeartbeatPublisher ghi log qua LogManager tĩnh; thiếu dòng này thì log NATS bị mất.
LogManager.SetFactory(host.Services.GetRequiredService<ILoggerFactory>());

await host.RunAsync();
