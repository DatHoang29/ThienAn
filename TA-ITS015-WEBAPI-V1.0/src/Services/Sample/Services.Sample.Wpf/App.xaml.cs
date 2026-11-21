using System.ComponentModel;
using System.Windows;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Sample.Configuration;
using Services.Sample.Extensions;
using Services.Sample.Wpf.ViewModels;
using Services.Shared.Events;
using Services.Shared.Hosting;
using Services.Shared.Logging;

namespace Services.Sample.Wpf;

/// <summary>
/// Author: TAC Team
/// Description: Composition root của WPF operator client. Build host + đăng ký DI.
/// Host start các source/worker trước, rồi App gọi manager.Init+Start; khi đóng cửa sổ gọi manager.Stop
/// trước khi host stop. Operator cũng có thể điều khiển lại runner qua các nút trên MainViewModel.
/// Created date: 16/07/2026
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private bool _allowWindowClose;
    private bool _shutdownStarted;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = e.Args,
                ContentRootPath = AppContext.BaseDirectory,
            });

            // Load Database.json + DataTransporter.json + Logging.json từ thư mục Configuration/.
            builder.Configuration.AddJsonFile("Configuration/Database.json", optional: false, reloadOnChange: false);
            builder.Configuration.AddJsonFile("Configuration/DataTransporter.json", optional: false, reloadOnChange: false);
            builder.Configuration.AddJsonFile("Configuration/Logging.json", optional: true, reloadOnChange: false);

            HostProfiles.ConfigureGuiForm(builder);
            builder.Services.AddSampleServices(builder.Configuration);

            // MainViewModel đồng thời làm notification handler (cùng instance singleton)
            // để MediatR deliver ActivityNotification thẳng vào ViewModel.
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<INotificationHandler<ActivityNotification>>(
                sp => sp.GetRequiredService<MainViewModel>());
            builder.Services.AddSingleton<MainWindow>();

            _host = builder.Build();

            // Set static singleton cho ConfigManager để truy cập từ bất cứ đâu: ConfigManager.Current.Api
            ConfigManager.SetInstance(_host.Services.GetRequiredService<IConfigManager>());

            // Khởi tạo backing logger cho các hàm tiện lợi LogManager.Info/Warn/Error (gọi ở đâu cũng được).
            LogManager.SetFactory(_host.Services.GetRequiredService<ILoggerFactory>());

            // Start host: hosted services (NatsSource/ApiSource/DataWorker/ConfigReloadWorker) start trước,
            // rồi Init manager (nạp config DB + warm codec). KHÔNG auto-Start — operator bấm Start/Stop trên UI
            // để điều khiển luồng thu thập thiết bị.
            await _host.StartAsync();
            var manager = _host.Services.GetRequiredService<IServiceManager>();
            await manager.InitAsync();

            var window = _host.Services.GetRequiredService<MainWindow>();
            window.Closing += OnMainWindowClosing;
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Không thể khởi động Sample operator.\n\n{exception.Message}",
                "Sample operator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            await DisposeHostAsync();
            Shutdown(-1);
        }
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_allowWindowClose)
            return;

        e.Cancel = true;
        if (_shutdownStarted || sender is not Window window)
            return;

        _shutdownStarted = true;
        _ = ShutdownAndCloseAsync(window);
    }

    private async Task ShutdownAndCloseAsync(Window window)
    {
        try
        {
            await DisposeHostAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Không thể dừng Sample operator đúng cách.\n\n{exception.Message}",
                "Sample operator",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _allowWindowClose = true;
            window.Close();
        }
    }

    private async Task DisposeHostAsync()
    {
        if (_host is null)
            return;

        var host = _host;
        _host = null;

        // Dừng manager TRƯỚC khi host stop (reverse order): runner dừng trước, rồi DataWorker drain queue, rồi producer.
        var manager = host.Services.GetService<IServiceManager>();
        if (manager is not null)
            await manager.StopAsync().ConfigureAwait(false);

        await host.StopAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

        if (host is IAsyncDisposable asyncHost)
            await asyncHost.DisposeAsync().ConfigureAwait(false);
        else
            host.Dispose();
    }
}
