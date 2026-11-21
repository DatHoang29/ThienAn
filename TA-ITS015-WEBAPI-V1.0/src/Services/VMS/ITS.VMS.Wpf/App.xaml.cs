using System.Net.Http;
using System.Windows;
using ITS.VMS.Core.Integration;
using ITS.VMS.Core.Integration.Helpers;
using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;
using ITS.VMS.Core.Storage;
using ITS.VMS.Core.Telemetry;
using ITS.VMS.Wpf.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITS.VMS.Wpf;

/// <summary>
/// Composition root của công cụ theo dõi/kiểm thử điều khiển VMS: đọc appsettings.json, dựng DI và mở form.
/// KHÔNG chạm CSDL — dịch vụ điều khiển VMS là stateless, mọi thông tin thiết bị nhập trực tiếp trên form
/// (hoặc do BE gửi kèm trong gói tin).
/// Author: ChuongLP
/// Created date: 22/09/2026
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();

            // Logger dùng chung (ghi file theo loại/ngày) — nằm ở Core, form đăng ký sự kiện EntryLogged để hiển thị.
            var logDir = configuration.GetValue<string>("Logging:FileDirectory") ?? "Log";
            var retainDays = configuration.GetValue<int?>("Logging:RetainDays") ?? 14;
            services.AddSingleton(new VmsLogger(logDir, retainDays));

            // Cấu hình kênh NATS + thiết bị + lưu trữ ảnh (section "Nats").
            var natsOptions = new VmsControlNatsOption();
            configuration.GetSection(VmsControlNatsOption.SectionName).Bind(natsOptions);
            services.AddSingleton(natsOptions);

            // HttpClient dùng chung để tải ảnh khi Url là HTTP.
            services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(15) });
            services.AddSingleton<VmsFileHelper>();
            services.AddSingleton<VmsDeviceExecutor>();

            // ===== Luồng TELEMETRY: dịch vụ tự đồng bộ thiết bị và tự đọc thông số =====
            // Form đóng vai ITS.VMS.Worker nhưng cho bấm tay từng bước để test hiện trường.
            // Cấu hình ở section "Telemetry" của appsettings.json.
            var telemetryOptions = new VmsTelemetryOption();
            configuration.GetSection(VmsTelemetryOption.SectionName).Bind(telemetryOptions);
            services.AddSingleton(telemetryOptions);

            services.AddSingleton<VmsEquipmentStore>();       // danh sách thiết bị (RAM + file)
            services.AddSingleton<VmsTelemetryBuffer>();      // gói chưa đẩy được, gửi lại sau
            services.AddSingleton<VmsDataChannel>();          // kết nối NATS riêng cho kênh dữ liệu
            services.AddSingleton<VmsEquipmentSyncService>(); // logic đồng bộ (dùng chung với Worker)
            services.AddSingleton<VmsTelemetryCollector>();   // logic quét telemetry (dùng chung với Worker)

            // Vai DỊCH VỤ (thay ITS.VMS.Worker) và vai GỬI LỆNH (thay VmsControlBridge của BE).
            // Cả 2 do người dùng bật/tắt trên form, KHÔNG tự chạy lúc khởi động để tránh
            // vô tình gửi lệnh thật ra thiết bị khi vừa mở form.
            services.AddSingleton<VmsControlNatsListener>();
            services.AddSingleton<VmsControlSender>();

            // Phát heartbeat định kỳ ({ data = "ok" } lên ta.its.heartbeat.vms) cho FE giám sát dịch vụ — bật form là chạy.
            services.AddSingleton<VmsHeartbeatPublisher>();

            // UI.
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<TelemetryViewModel>();
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            // UI: resolve MainWindow trước để ViewModel đăng ký lắng nghe EntryLogged của logger
            var mainWindow = Services.GetRequiredService<MainWindow>();

            // Bắt đầu phát heartbeat (log kết nối sẽ hiển thị ngay lên form)
            Services.GetRequiredService<VmsHeartbeatPublisher>().Start();

            // Dọn ảnh tạm còn sót từ lần chạy trước.
            Services.GetRequiredService<VmsFileHelper>().CleanupTemp();

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không khởi động được công cụ test VMS.\n\n{ex}", "VMS Control Test",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dừng lắng nghe + ngắt kết nối khi đóng form (nếu đã khởi tạo).
        try { Services?.GetService<VmsControlNatsListener>()?.Stop(); } catch { }
        try { Services?.GetService<VmsHeartbeatPublisher>()?.Stop(); } catch { }

        try
        {
            var sender = Services?.GetService<VmsControlSender>();
            if (sender is not null)
                Task.Run(() => sender.StopAsync()).Wait(TimeSpan.FromSeconds(3));
        }
        catch { /* đang thoát, bỏ qua lỗi dọn dẹp */ }

        try
        {
            var dataChannel = Services?.GetService<VmsDataChannel>();
            if (dataChannel is not null)
                Task.Run(() => dataChannel.StopAsync()).Wait(TimeSpan.FromSeconds(3));
        }
        catch { /* đang thoát, bỏ qua lỗi dọn dẹp */ }

        base.OnExit(e);
    }
}
