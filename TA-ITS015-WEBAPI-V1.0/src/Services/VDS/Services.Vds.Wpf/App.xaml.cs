using Services.Vds;
using Services.Vds.Wpf.Logging;
using Services.Vds.Wpf.Simulation;
using Services.Vds.Wpf.ViewModels;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Shared.Events;
using Services.Vds.Manager;
using Services.Vds.Features.Media;
using Microsoft.Extensions.Options;
using Services.Shared.Logging;
using System.Windows;

namespace Services.Vds.Wpf;

/// <summary>
/// Author: KienLT
/// Description: Composition root của WPF harness — host khung VDS ở chế độ FakeSource để test
/// end-to-end. Chỉ nối dây, không chứa nghiệp vụ (nghiệp vụ ở <c>ITS.VDS.Core</c>, trạng thái UI ở
/// <see cref="MainViewModel"/>).
/// Created date: 04/08/2026
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private IVdsServiceManager? _manager;

    /// <summary>
    /// <c>async void</c> là CỐ Ý: lifecycle phải được <c>await</c>, không được
    /// <c>GetAwaiter().GetResult()</c>. Từ P5 host gọi <c>manager.InitAsync()</c> tường minh; chặn luồng
    /// UI ở đây làm treo cứng — <c>CameraManager.LoadAsync</c> await truy vấn DB, continuation của nó
    /// post về <c>Dispatcher</c> đang bị chặn ⇒ deadlock, cửa sổ không bao giờ hiện.
    /// (Trước P5 không lộ vì <c>VdsHostedService</c> chạy Init/Start trên luồng nền.)
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            await StartHostAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không khởi động được VDS harness.\n\n{ex}", "VDS Test Console",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private async Task StartHostAsync()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        // Hạ tầng tách file theo khuôn service (giống Worker). Override theo môi trường đặt ở
        // Configuration/{Ten}.{Environment}.json vì các file này nạp sau appsettings.{Environment}.json.
        var environment = builder.Environment.EnvironmentName;
        foreach (var name in new[] { "Database", "DataTransporter", "Logging" })
        {
            builder.Configuration.AddJsonFile($"Configuration/{name}.json",
                optional: name != "Database", reloadOnChange: false);
            builder.Configuration.AddJsonFile($"Configuration/{name}.{environment}.json",
                optional: true, reloadOnChange: false);
        }

        // ZLogger: console + file theo LogKind. Gọi TRƯỚC khi thêm provider của UI vì hàm này
        // ClearProviders() — thêm provider trước sẽ bị xoá mất.
        HostProfiles.ConfigureGuiHost(builder);

        // Log của pipeline chảy vào feed hoạt động của ViewModel qua một event thuần (KHÔNG qua MediatR —
        // xem ghi chú trong ActivityLoggerProvider). Provider tạo ở đây vì logging cấu hình lúc dựng
        // builder; log trước khi UI đăng ký được đệm lại.
        var activityLogs = new ActivityLoggerProvider();
        builder.Logging.AddProvider(activityLogs);
        builder.Services.AddSingleton(activityLogs);   // MainViewModel đăng ký nhận feed qua instance này
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        builder.Services.AddVdsServices(builder.Configuration);

        // Kênh "hoạt động" cho UI: ActivityPublisher phát ActivityNotification qua MediatR.
        //
        // Đăng ký Ở ĐÂY chứ không trong AddVdsServices: chỉ harness có giao diện mới cần feed này, nên
        // Services.Vds không phải kéo theo MediatR. Bản cũ đăng ký trong Core và đó là lý do khi tách
        // sang cấu trúc mới thì RegularSimulator không resolve được ActivityPublisher.
        // Quét assembly của chính WPF: handler duy nhất là ActivityNotificationForwarder (MediatR tự đăng ký
        // transient). KienLT - 29/10/2026: KHÔNG để MainViewModel làm handler — scan luôn thêm bản transient
        // của nó dù đã đăng ký singleton ⇒ mỗi activity dựng một ViewModel mới, rò RAM + cạn pool SQL.
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(App).Assembly));
        builder.Services.AddSingleton<ActivityPublisher>();

        builder.Services.Configure<SimulatorOptions>(builder.Configuration.GetSection(SimulatorOptions.SectionName));
        builder.Services.AddSingleton<IVdsFrameInjector>(sp => new WpfSampleFrameInjector(
            sp.GetRequiredService<VdsServiceManager>(), sp.GetRequiredService<IOptions<SimulatorOptions>>()));
        builder.Services.AddTransient<WpfSampleImageHandler>();
        builder.Services.AddHttpClient(VdsMediaStorage.HttpClientName)
            .AddHttpMessageHandler<WpfSampleImageHandler>();
        builder.Services.AddSingleton<RegularSimulator>();

        // MainViewModel chỉ là DataContext, đúng MỘT instance: nó đăng ký sự kiện nạp danh mục và feed log
        // nên mọi bản thừa đều sống mãi.
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();

        // Backing factory cho LogManager.Info/Error (dùng ở nơi không inject được ILogger).
        LogManager.SetFactory(_host.Services.GetRequiredService<ILoggerFactory>());

        // Cửa sổ hiện NGAY, trước khi nạp danh mục: người dùng thấy tiến trình khởi động trong nhật ký
        // thay vì màn hình trắng. Các lệnh bơm bản tin tự chặn khi manager chưa Running.
        _host.Services.GetRequiredService<MainWindow>().Show();

        // Lifecycle gọi tường minh (R12). Harness cần pipeline chạy sẵn để bơm bản tin nên Init + Start
        // ngay, không chờ người dùng bấm.
        await _host.StartAsync();
        _manager = _host.Services.GetRequiredService<IVdsServiceManager>();
        await _manager.InitAsync();
        await _manager.StartAsync();

        // Danh mục (camera / vùng) CHỈ có sau InitAsync, mà MainViewModel đã dựng xong từ trước (cửa sổ
        // hiện ngay để thấy nhật ký khởi động) nên lúc đó các danh sách còn rỗng. Nạp lại ở đây để người
        // dùng không phải bấm "Tải lại danh sách camera".
        //
        // Đang ở luồng UI: OnStartup là async void chạy trên Dispatcher nên mọi continuation của chuỗi
        // await này quay về đúng luồng đó — RefreshCatalog sửa ObservableCollection nên bắt buộc phải vậy.
        _host.Services.GetRequiredService<MainViewModel>().RefreshCatalog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Manager dừng TRƯỚC host: nguồn đóng và hàng đợi drain xong rồi mới hạ các hosted service.
        // Bọc trong Task.Run: OnExit chạy trên luồng UI và không await được, mà StopAsync có await bên
        // trong — nếu chờ ngay trên luồng UI thì continuation post về Dispatcher đã bị chặn ⇒ deadlock.
        var shutdown = Task.Run(async () =>
        {
            if (_manager is not null) await _manager.StopAsync();
            if (_host is not null) await _host.StopAsync(TimeSpan.FromSeconds(5));
        });

        try { shutdown.Wait(TimeSpan.FromSeconds(15)); } catch { /* dừng lỗi thì vẫn thoát */ }
        _host?.Dispose();
        base.OnExit(e);
    }
}
