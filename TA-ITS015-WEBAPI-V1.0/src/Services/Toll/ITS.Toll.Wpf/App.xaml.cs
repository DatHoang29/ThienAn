using System.Net.Http;
using System.Windows;
using ITS.Toll.Core.Configuration;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Messaging;
using ITS.Toll.Core.Persistence;
using ITS.Toll.Core.Services;
using ITS.Toll.Wpf.Hosting;
using ITS.Toll.Wpf.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Services.Shared.Configuration;
using Services.Shared.Persistence;

namespace ITS.Toll.Wpf;

/// <summary>
/// Composition root của công cụ test API thu phí: đọc appsettings.json, dựng DI và mở form.
/// DB dùng REPOSITORY DÙNG CHUNG (DatabaseManager/SqlSugarAccess của Services.Shared.Runtime);
/// cấu hình Toll lấy từ bảng SysOpConfig (nhóm toll_config) qua TollOpConfigReader.
/// Author: ChuongLP
/// Created date: 24/08/2026
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

            // Logger dùng chung (ghi file theo loại/ngày) — nằm ở Core, form chỉ đăng ký sự kiện để hiển thị.
            var logDir = configuration.GetValue<string>("Logging:FileDirectory") ?? "logs";
            var retainDays = configuration.GetValue<int?>("Logging:RetainDays") ?? 14;
            services.AddSingleton(new TollLogger(logDir, retainDays));

            // Kết nối DB qua nền dùng chung: DatabaseManager đọc section "DbConnection" (đa kết nối, ConfigId).
            var dbConnOptions = new DbConnectionOptions();
            configuration.GetSection("DbConnection").Bind(dbConnOptions);
            var dbConnOptionsWrap = Options.Create(dbConnOptions);
            services.AddSingleton(dbConnOptionsWrap);                 // IOptions<DbConnectionOptions> cho repository
            var databaseManager = new DatabaseManager(dbConnOptionsWrap);
            services.AddSingleton(databaseManager);

            // Cấu hình service Toll (TenantId...) từ section "Toll".
            var tollOptions = new TollServiceOptions();
            configuration.GetSection(TollServiceOptions.SectionName).Bind(tollOptions);
            services.AddSingleton(tollOptions);

            // Nạp cấu hình Toll TỪ BẢNG SysOpConfig (nhóm toll_config). Chưa cấu hình/không kết nối được DB → báo rõ và thoát.
            var configReader = new TollOpConfigReader(databaseManager, dbConnOptionsWrap, tollOptions);
            services.AddSingleton(configReader);
            TollOptions options;
            try
            {
                options = configReader.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không đọc được cấu hình Toll từ bảng SysOpConfig (nhóm '{TollOpConfigReader.GroupCode}').\n\n" +
                    $"Lý do: {ex.Message}\n\n" +
                    "Hãy kiểm tra kết nối DB (section DbConnection) và cấu hình trên màn hình 'Cấu hình hoạt động' rồi chạy lại.",
                    "Toll API Test", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }
            services.AddSingleton(options);

            // HttpClient + client gọi API + kho lưu dữ liệu (repository dùng chung).
            services.AddSingleton(_ => new HttpClient());
            services.AddSingleton<TollApiClient>();
            services.AddSingleton<TollDataStore>();

            // Lưu vết bằng file: con trỏ resume (watermark) + history theo ngày.
            services.AddSingleton<TollSyncCursorStore>();

            // Điều phối pull lại theo loại dữ liệu + khoảng thời gian (dùng lại cho listener NATS).
            services.AddSingleton<TollCallBackService>();

            // Lắng nghe lệnh lấy dữ liệu thủ công qua NATS (kênh ta.its.control.toll) — bật form là chạy.
            var natsOptions = new TollControlNatsOptions();
            configuration.GetSection(TollControlNatsOptions.SectionName).Bind(natsOptions);
            services.AddSingleton(natsOptions);
            services.AddSingleton<TollControlNatsListener>();

            // Phát heartbeat định kỳ ({ data = "ok" } lên ta.its.heartbeat.toll) cho FE giám sát dịch vụ — bật form là chạy.
            services.AddSingleton<TollHeartbeatPublisher>();

            // API inbound NHÚNG trong form: mở Kestrel để TCS gọi tới ngay khi bật form.
            // URL lấy từ appsettings.json (Toll:CallBackUrls); mặc định localhost:8710.
            var callBackUrls = configuration.GetValue<string>("Toll:CallBackUrls") ?? "http://localhost:8710";
            services.AddSingleton(sp => new TollHttpHost(
                sp.GetRequiredService<TollCallBackService>(),
                sp.GetRequiredService<TollLogger>(),
                callBackUrls));

            // UI.
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            // Mở form + bắt đầu lắng nghe NATS. Log NATS sẽ tự hiện lên Nhật ký của form
            // (MainViewModel đang lắng nghe TollLogger.EntryLogged).
            Services.GetRequiredService<MainWindow>().Show();
            Services.GetRequiredService<TollControlNatsListener>().Start();
            Services.GetRequiredService<TollHeartbeatPublisher>().Start();

            // Mở API inbound cho TCS gọi tới (chạy nền, log hiện lên Nhật ký form).
            Services.GetRequiredService<TollHttpHost>().Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không khởi động được công cụ test Toll.\n\n{ex}", "Toll API Test",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dừng lắng nghe NATS + đóng API inbound khi đóng form (nếu đã khởi tạo).
        try
        {
            Services?.GetService<TollControlNatsListener>()?.Stop();
            Services?.GetService<TollHeartbeatPublisher>()?.Stop();
        }
        catch { /* đang thoát, bỏ qua lỗi dọn dẹp */ }

        try
        {
            var httpHost = Services?.GetService<TollHttpHost>();
            if (httpHost is not null)
                Task.Run(() => httpHost.StopAsync()).Wait(TimeSpan.FromSeconds(3));
        }
        catch { /* đang thoát, bỏ qua lỗi dọn dẹp */ }

        base.OnExit(e);
    }
}
