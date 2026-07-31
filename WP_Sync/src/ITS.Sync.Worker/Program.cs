using ITS.Sync.Application;
using ITS.Sync.Worker;
using ITS.Sync.Worker.Logging;

// QUAN TRỌNG: khi chạy như Windows Service, thư mục làm việc mặc định là C:\Windows\System32.
// Phải đặt ContentRootPath về thư mục chứa exe, nếu không sẽ không tìm thấy
// appsettings.json và Configuration/Database.json.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Nạp thêm cấu hình DB (Cách A) - cùng định dạng với WPF.
builder.Configuration.AddJsonFile("Configuration/Database.json", optional: false, reloadOnChange: true);

// Chạy được cả 2 chế độ: console (dotnet run) và Windows Service (sc create).
// AddWindowsService cũng tự bật ghi log vào Windows Event Log.
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ITS WP Sync";
});

// Khi chạy như service KHÔNG có console -> ghi log ra file để theo dõi.
var configuredLogDir = builder.Configuration.GetValue<string>("Logging:FileDirectory");
// Đường dẫn tương đối phải tính từ thư mục exe (service có cwd = System32).
var logDirectory = string.IsNullOrWhiteSpace(configuredLogDir)
    ? Path.Combine(AppContext.BaseDirectory, "logs")
    : Path.IsPathRooted(configuredLogDir)
        ? configuredLogDir
        : Path.Combine(AppContext.BaseDirectory, configuredLogDir);
var retainDays = builder.Configuration.GetValue<int?>("Logging:RetainDays") ?? 14;
builder.Logging.AddProvider(new FileLoggerProvider(logDirectory, LogLevel.Information, retainDays));

// Đăng ký toàn bộ dịch vụ đồng bộ (dùng chung 1 hàm với WPF).
builder.Services.AddSync(builder.Configuration);

builder.Services.AddHostedService<SyncBackgroundService>();

var host = builder.Build();
host.Run();
