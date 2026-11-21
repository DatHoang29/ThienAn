using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services.Vds.Manager;
using Services.Vds;

// ===================================================================================
// Services.Vds.Worker — host chạy nền của service VDS.
//
// Host chỉ làm ba việc: nạp cấu hình từ các file tách theo hạ tầng, chọn profile host (logging +
// Windows Service), và gọi MỘT hàm đăng ký dịch vụ. Mọi thứ khác nằm trong Services.Vds —
// nhờ vậy đổi vỏ host (worker nền, công cụ có giao diện, test) không phải sửa phần nghiệp vụ.
//
// Bật/tắt từng tính năng bằng cấu hình trong appsettings.json (mỗi tính năng có cờ Enabled riêng);
// mặc định các tính năng GHI vào bảng dùng chung với worker cũ đều TẮT để chạy song song an toàn.
// ===================================================================================

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    // Chạy như Windows Service thì working directory là C:\Windows\System32, không phải thư mục
    // chứa exe. Trỏ ContentRoot về BaseDirectory để tìm được Configuration/*.json trong cả hai chế độ.
    ContentRootPath = AppContext.BaseDirectory,
});

// Database.json gộp vào IConfiguration root ⇒ section "DbConnection" đọc được như thể nằm trong
// appsettings.json. Cùng lược đồ với Database.json của WebAPI nên hai bên dùng lại được của nhau.
// optional: false — thiếu file này thì fail ngay lúc start, tốt hơn là chạy rồi mới lỗi kết nối.
builder.Configuration.AddJsonFile("Configuration/Database.json", optional: false, reloadOnChange: false);

// DataTransporter.json — section "Nats" cho TransportManager.
// optional: true — không có NATS thì service vẫn chạy, chỉ mất khả năng nhận cấu hình từ xa.
builder.Configuration.AddJsonFile("Configuration/DataTransporter.json", optional: true, reloadOnChange: false);

// Logging.json — section "Logging" cho LogManager (đường dẫn file log theo từng loại).
// optional: true — thiếu file thì LogManager dùng mặc định, service vẫn chạy và vẫn có log console.
// PHẢI nạp TRƯỚC HostProfiles.ConfigureWorkerService vì hàm đó đọc cấu hình ngay khi gọi.
builder.Configuration.AddJsonFile("Configuration/Logging.json", optional: true, reloadOnChange: false);

// Nạp LẠI biến môi trường để chúng nằm TRÊN các file vừa thêm.
//
// Host.CreateApplicationBuilder đã nạp theo thứ tự: appsettings.json → appsettings.{Env}.json →
// biến môi trường → command line. Các lời gọi AddJsonFile ở trên đẩy file lên TRÊN biến môi trường,
// nên nếu không có dòng này thì DbConnection__ConnectionConfigs__0__ConnectionString hoàn toàn vô hiệu —
// đúng cái cách mà Database.json hướng dẫn đặt mật khẩu để không commit vào repo.
builder.Configuration.AddEnvironmentVariables();

// Logging theo loại (console + một file cho mỗi loại) và đăng ký chạy được như Windows Service.
// Chạy bằng dotnet run vẫn bình thường: AddWindowsService tự nhận biết không nằm trong Service Manager.
HostProfiles.ConfigureWorkerService(builder);

builder.Services.AddVdsServices(builder.Configuration);

var host = builder.Build();

// Vòng đời TƯỜNG MINH qua IServiceManager — cùng một hợp đồng mà Services.Vds.Wpf sẽ dùng.
//
// Thứ tự bắt buộc, mỗi bước đều có lý do:
//   host.StartAsync()   mở các hosted service độc lập (job định kỳ, worker hàng đợi, kiểm kết nối) và
//                       Wolverine — hàng đợi phải sẵn sàng TRƯỚC khi bản tin đầu tiên vào.
//   manager.InitAsync() nạp danh mục camera/vùng/đoạn + ngưỡng tốc độ + danh sách biển, rồi bù dữ liệu.
//                       Camera rỗng thì hàm này NÉM, host thoát để supervisor retry — tốt hơn là chạy
//                       tiếp rồi bỏ toàn bộ frame vì không tra được camera.
//   manager.StartAsync() mở nguồn bản tin.
//   manager.StopAsync()  đóng nguồn TRƯỚC khi host dừng, để bản tin đã nằm trong hàng đợi được drain hết.
var manager = host.Services.GetRequiredService<IVdsServiceManager>();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var stopping = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
using var stopRegistration = lifetime.ApplicationStopping.Register(() => stopping.TrySetResult());
try
{
    await host.StartAsync();
    await manager.InitAsync(lifetime.ApplicationStopping);
    await manager.StartAsync();
    await stopping.Task;
}
finally
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    try
    {
        await Services.Vds.Manager.VdsShutdown.StopInOrderAsync(
            () => manager.StopAsync(timeout.Token), () => host.StopAsync());
    }
    finally { host.Dispose(); }
}
