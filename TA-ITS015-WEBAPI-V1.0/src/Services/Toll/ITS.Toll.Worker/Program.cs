using System.Net.Http;
using ITS.Toll.Core.Configuration;
using ITS.Toll.Core.Endpoints;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Messaging;
using ITS.Toll.Core.Services;
using ITS.Toll.Worker.Messaging;
using Microsoft.Extensions.Options;
using Services.Shared.Configuration;
using Services.Shared.Persistence;

// QUAN TRỌNG: khi chạy như Windows Service, cwd mặc định là C:\Windows\System32.
// Đặt ContentRootPath về thư mục chứa exe để đọc đúng appsettings.json.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Chạy được cả console (dotnet run) lẫn Windows Service (sc create).
builder.Host.UseWindowsService(options => options.ServiceName = "ITS Toll Ingest");

var configuration = builder.Configuration;
var services = builder.Services;

// ===== Nối dây lõi ITS.Toll.Core (giống composition root của WPF) =====

// Logger dùng chung (ghi file theo loại/ngày) — nằm ở Core.
var logDir = configuration.GetValue<string>("Logging:FileDirectory") ?? "Log";
var retainDays = configuration.GetValue<int?>("Logging:RetainDays") ?? 14;
services.AddSingleton(new TollLogger(logDir, retainDays));

// Kết nối DB qua nền dùng chung: DatabaseManager đọc section "DbConnection" (đa kết nối, ConfigId).
var dbConnOptions = new DbConnectionOptions();
configuration.GetSection("DbConnection").Bind(dbConnOptions);
var dbConnOptionsWrap = Options.Create(dbConnOptions);
services.AddSingleton(dbConnOptionsWrap);
services.AddSingleton(new DatabaseManager(dbConnOptionsWrap));

// Cấu hình service Toll (TenantId...) từ section "Toll".
var tollOptions = new TollServiceOptions();
configuration.GetSection(TollServiceOptions.SectionName).Bind(tollOptions);
services.AddSingleton(tollOptions);

// Đọc cấu hình endpoint từ SysOpConfig (nhóm toll_config) — dùng cho luồng import (pull lại).
services.AddSingleton<TollOpConfigReader>();

// HttpClient + client gọi API + kho lưu dữ liệu + điều phối nhận dữ liệu.
services.AddSingleton(_ => new HttpClient());
services.AddSingleton<TollApiClient>();
services.AddSingleton<TollDataStore>();
services.AddSingleton<TollCallBackService>();

// ===== Lắng nghe lệnh lấy dữ liệu thu phí thủ công qua NATS (kênh ta.its.control.toll) =====
var natsOptions = new TollControlNatsOptions();
configuration.GetSection(TollControlNatsOptions.SectionName).Bind(natsOptions);
services.AddSingleton(natsOptions);
services.AddSingleton<TollControlNatsListener>();       // listener dùng chung (Core)
services.AddHostedService<TollControlListener>();       // hosted service bọc listener

// ===== Heartbeat: phát { data = "ok" } lên ta.its.heartbeat.toll cho FE giám sát dịch vụ =====
services.AddSingleton<TollHeartbeatPublisher>();        // publisher dùng chung (Core), đọc cùng section "Nats"
services.AddHostedService<TollHeartbeatService>();      // hosted service bọc publisher

var app = builder.Build();

// Endpoint kiểm tra sống.
app.MapGet("/health", () => Results.Ok(new { ok = true, service = "ITS.Toll.Worker" }));

// 2 endpoint TCS gọi vào.
app.MapTollCallBackEndpoints();

app.Run();
