using ITS.VMS.Core.Integration;
using ITS.VMS.Core.Integration.Helpers;
using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;
using ITS.VMS.Core.Storage;
using ITS.VMS.Core.Telemetry;
using ITS.VMS.Worker.Messaging;

// QUAN TRỌNG: khi chạy như Windows Service, cwd mặc định là C:\Windows\System32.
// Đặt ContentRootPath về thư mục chứa exe để đọc đúng appsettings.json / nats.creds / DLL driver.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Chạy được cả console (dotnet run) lẫn Windows Service (sc create).
builder.Host.UseWindowsService(options => options.ServiceName = "ITS VMS Control");

var configuration = builder.Configuration;
var services = builder.Services;

// ===== Nối dây lõi ITS.VMS.Core =====

// Logger ghi file theo loại (control/device/config) + theo ngày.
var logDir = configuration.GetValue<string>("Logging:FileDirectory") ?? "Log";
var retainDays = configuration.GetValue<int?>("Logging:RetainDays") ?? 14;
services.AddSingleton(new VmsLogger(logDir, retainDays));

// Cấu hình kênh NATS + thiết bị + lưu trữ ảnh (section "Nats").
var natsOptions = new VmsControlNatsOption();
configuration.GetSection(VmsControlNatsOption.SectionName).Bind(natsOptions);
services.AddSingleton(natsOptions);

// HttpClient dùng chung để tải ảnh khi BE gửi HTTP URL (timeout ngắn, tránh treo cả thiết bị).
services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(15) });

services.AddSingleton<VmsFileHelper>();
services.AddSingleton<VmsDeviceExecutor>();

// ===== Luồng 1: BE ra lệnh điều khiển (kênh ta.its.control.vms) =====
services.AddSingleton<VmsControlNatsListener>();     // listener dùng chung (Core)
services.AddHostedService<VmsControlListener>();     // hosted service bọc listener

// ===== Heartbeat: phát { data = "ok" } lên ta.its.heartbeat.vms cho FE giám sát dịch vụ =====
services.AddSingleton<VmsHeartbeatPublisher>();        // publisher dùng chung (Core), đọc cùng section "Nats"
services.AddHostedService<VmsHeartbeatService>();      // hosted service bọc publisher

// ===== Luồng 2: dịch vụ TỰ thu thập telemetry (kênh ta.its.data.vms.equipment / .telemetry) =====
// Cấu hình riêng ở section "Telemetry" để tắt luồng này không ảnh hưởng luồng điều khiển.
var telemetryOptions = new VmsTelemetryOption();
configuration.GetSection(VmsTelemetryOption.SectionName).Bind(telemetryOptions);
services.AddSingleton(telemetryOptions);

services.AddSingleton<VmsEquipmentStore>();          // danh sách thiết bị (RAM + file)
services.AddSingleton<VmsTelemetryBuffer>();         // gói chưa đẩy được, gửi lại sau
services.AddSingleton<VmsDataChannel>();             // kết nối NATS riêng cho kênh dữ liệu

services.AddSingleton<VmsEquipmentSyncService>();    // logic đồng bộ (dùng chung với form)
services.AddSingleton<VmsTelemetryCollector>();      // logic quét telemetry (dùng chung với form)

services.AddHostedService<VmsEquipmentSyncWorker>(); // lập lịch đồng bộ danh sách thiết bị
services.AddHostedService<VmsTelemetryWorker>();     // lập lịch quét thông số

var app = builder.Build();

// Dọn ảnh tạm còn sót từ lần chạy trước để thư mục temp không phình đĩa.
app.Services.GetRequiredService<VmsFileHelper>().CleanupTemp();

// Endpoint kiểm tra sống (không có API nghiệp vụ nào — mọi lệnh đi qua NATS).
// Kèm trạng thái luồng telemetry để vận hành biết dịch vụ đã có danh sách thiết bị chưa,
// và còn gói nào chưa đẩy được về BE không.
app.MapGet("/health", (VmsControlNatsListener listener, VmsEquipmentStore store, VmsTelemetryBuffer buffer, VmsHeartbeatPublisher heartbeat) => Results.Ok(new
{
    ok = true,
    service = "ITS.VMS.Worker",
    heartbeat = new
    {
        running = heartbeat.IsRunning,
        subject = natsOptions.HeartbeatSubject,
    },
    control = new
    {
        listening = listener.IsListening,
        subject = natsOptions.ControlSubject,
    },
    telemetry = new
    {
        enabled = telemetryOptions.Enabled,
        intervalMinutes = telemetryOptions.CollectIntervalMinutes,
        equipmentCount = store.Count,
        equipmentSyncedAt = store.SyncedAt,
        pendingPackets = buffer.Count,
    },
}));

app.Run();
