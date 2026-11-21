using Services.CctvProxy.Configuration;
using Services.CctvProxy.Endpoints;
using Services.CctvProxy.Extensions;
using Services.Shared.Hosting;
using Services.Shared.Logging;

// QUAN TRỌNG: khi chạy như Windows Service, cwd mặc định là C:\Windows\System32.
// Đặt ContentRootPath về thư mục chứa exe để đọc đúng appsettings.json + Configuration/*.json.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Database.json (kết nối chứa SysOpConfig) + Logging.json từ thư mục Configuration/.
builder.Configuration.AddJsonFile("Configuration/Database.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile("Configuration/Logging.json", optional: true, reloadOnChange: false);
// Biến môi trường vẫn ưu tiên hơn các file trên (vd DbConnection__ConnectionConfigs__0__ConnectionString khi triển khai).
builder.Configuration.AddEnvironmentVariables();

HostProfiles.ConfigureWorkerService(builder, serviceName: "TA-ITS015-CctvProxy");
builder.Services.AddCctvProxyServices(builder.Configuration);

// CORS cho request HTTP (tải video). WebSocket không chịu CORS — endpoint tự kiểm tra Origin.
var allowedOrigins = builder.Configuration.GetSection("Service:Proxy:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length == 0 || allowedOrigins.Contains("*"))
            policy.AllowAnyOrigin();
        else
            policy.WithOrigins(allowedOrigins);
        policy.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition", "Content-Range", "Accept-Ranges");
    });

    // Ảnh công khai cho web người dân: mọi origin, chỉ GET; cho JS đọc thời điểm chụp ảnh.
    options.AddPolicy(CctvProxyEndpoints.PublicCorsPolicy, policy =>
        policy.AllowAnyOrigin().WithMethods("GET").WithExposedHeaders("X-Snapshot-Time", "X-Snapshot-Source"));
});

var app = builder.Build();

// Set static singleton cho ConfigManager để truy cập từ bất cứ đâu: ConfigManager.Current.Cctv
ConfigManager.SetInstance(app.Services.GetRequiredService<IConfigManager>());

// Khởi tạo backing logger cho LogManager.Info/Error (gọi ở đâu cũng được).
LogManager.SetFactory(app.Services.GetRequiredService<ILoggerFactory>());

var manager = app.Services.GetRequiredService<IServiceManager>();

// Chỉ phục vụ proxy khi manager đang Running (lúc khởi động/dừng trả 503); /health luôn trả lời.
app.Use(async (context, next) =>
{
    if (manager.State != ServiceManagerState.Running && !context.Request.Path.StartsWithSegments("/health"))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return;
    }

    await next(context);
});
app.UseCors();
app.UseRateLimiter();
app.UseWebSockets();
app.MapCctvProxyEndpoints();

// Khác Sample (worker thuần): Init (nạp cấu hình SysOpConfig) TRƯỚC khi Kestrel nhận request,
// để request đầu tiên đã có địa chỉ/tài khoản NxVMS + ticket secret. Sau đó start host rồi Start manager.
await manager.InitAsync();
await app.StartAsync();
await manager.StartAsync();
LogManager.Info($"Cctv Proxy khởi động, lắng nghe {builder.Configuration["Urls"]}");

// Chờ tín hiệu shutdown, dừng manager (ngừng nhận request proxy) TRƯỚC khi host stop.
await app.WaitForShutdownAsync();
await manager.StopAsync();
await app.StopAsync();
