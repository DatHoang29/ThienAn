using System;
using System.IO;
using System.Windows;
using ITS.Sync.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ITS.Sync.Wpf;

/// <summary>
/// Composition root của ứng dụng WPF: đọc cấu hình (appsettings.json + Configuration/Database.json)
/// và dựng container DI. Cùng cấu hình này Worker/Service sẽ dùng lại.
/// </summary>
public partial class App : System.Windows.Application
{
    /// <summary>Container DI dùng chung cho toàn app (truy cập qua App.Services).</summary>
    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1) Đọc cấu hình. Database.json chứa DbConnection (Cách A) khai 2 DB Source/Target.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile(Path.Combine("Configuration", "Database.json"), optional: false, reloadOnChange: true)
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

        base.OnStartup(e);
    }
}
