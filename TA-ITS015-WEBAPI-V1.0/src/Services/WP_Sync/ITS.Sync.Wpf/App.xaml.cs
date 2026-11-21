using System;
using System.IO;
using System.Windows;
using ITS.Sync.Application;
using ITS.Sync.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ITS.Sync.Wpf;

/// <summary>
/// Composition root của ứng dụng WPF: đọc cấu hình (appsettings.json + Configuration/Database.json + Configuration/Nats.json)
/// và dựng container DI. Cùng cấu hình này Worker/Service sẽ dùng lại.
/// </summary>
public partial class App : System.Windows.Application
{
    /// <summary>Container DI dùng chung cho toàn app (truy cập qua App.Services).</summary>
    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1) Đọc cấu hình. Database.json chứa DbConnection (Cách A) khai 2 DB Source/Target, Nats.json chứa section "Nats".
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile(Path.Combine("Configuration", "Database.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine("Configuration", "Nats.json"), optional: false, reloadOnChange: false)
            .Build();

        // 2) Đăng ký dịch vụ: Infrastructure (SqlSugar) + Application (đồng bộ) + UI.
        var services = new ServiceCollection();
        // Bắt buộc: NatsSyncListener phụ thuộc ILogger<T>.
        services.AddLogging(builder => builder.AddDebug());
        services.AddSync(configuration);
        services.AddSingleton<ViewModels.MainViewModel>();
        services.AddSingleton<Views.MainWindow>();

        Services = services.BuildServiceProvider();

        // 3) Mở cửa sổ chính (tạo qua DI để ViewModel được tiêm phụ thuộc).
        var window = Services.GetRequiredService<Views.MainWindow>();
        window.Show();

        // 4) Phát heartbeat định kỳ ({ data = "ok" } lên ta.its.heartbeat.wpsync) cho FE giám sát dịch vụ — bật form là chạy.
        Services.GetRequiredService<SyncHeartbeatPublisher>().Start();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dừng phát heartbeat khi đóng form (nếu đã khởi tạo).
        try { Services?.GetService<SyncHeartbeatPublisher>()?.Stop(); }
        catch { /* đang thoát, bỏ qua lỗi dọn dẹp */ }

        base.OnExit(e);
    }
}
