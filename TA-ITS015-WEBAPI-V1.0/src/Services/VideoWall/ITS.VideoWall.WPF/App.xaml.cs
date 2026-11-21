using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Shared.Logging;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.Views;
using Module.VideoWall.WPF.Extensions;

namespace Module.VideoWall.WPF;

/// <summary>
/// Description: Lớp ứng dụng chính (App) của WPF VideoWall, cấu hình Generic Host, Dependency Injection và vòng đời
/// Created date: 13/09/2026
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    static App()
    {
        // Tự động chuyển con trỏ chuột sang icon Cấm (No 🚫) khi hover qua bất kỳ nút bấm nào đang bị Disable (IsEnabled = false)
        EventManager.RegisterClassHandler(
            typeof(Window),
            UIElement.QueryCursorEvent,
            new QueryCursorEventHandler((sender, args) =>
            {
                if (sender is Window win)
                {
                    var mousePos = Mouse.GetPosition(win);
                    var hitResult = VisualTreeHelper.HitTest(win, mousePos);
                    if (hitResult?.VisualHit != null)
                    {
                        var button = FindParentOrSelf<ButtonBase>(hitResult.VisualHit);
                        if (button != null && !button.IsEnabled)
                        {
                            args.Cursor = Cursors.No;
                            args.Handled = true;
                        }
                    }
                }
            }),
            true);
    }

    private static T? FindParentOrSelf<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T typed)
                return typed;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            try
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "startup_error.log"), args.Exception.ToString());
            }
            catch
            {
                // ignored
            }

            MessageBox.Show(
                $"Lỗi không xử lý được:\n\n{args.Exception}",
                "Video Wall Commissioning Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            try
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "fatal_error.log"), args.ExceptionObject?.ToString() ?? "Unknown error");
            }
            catch
            {
                // ignored
            }

            MessageBox.Show(
                $"Lỗi nghiêm trọng:\n\n{args.ExceptionObject}",
                "Video Wall Commissioning Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        try
        {
            _host = BuildHost(e.Args);
            LogManager.SetFactory(_host.Services.GetRequiredService<ILoggerFactory>());

            var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
            var window = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = window;

            window.Closed += (s, ev) =>
            {
                _ = DisposeHost();
                Shutdown(0);
            };

            mainViewModel.ReportReady();
            window.Show();
        }
        catch (Exception exception)
        {
            try
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "startup_catch.log"), exception.ToString());
            }
            catch
            {
                // ignored
            }

            MessageBox.Show(
                $"Không thể khởi động công cụ đấu nối tường ghép.\n\n{exception}",
                "Video Wall Commissioning",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            _ = DisposeHost();
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await DisposeHost().ConfigureAwait(true);
        base.OnExit(e);
    }

    private IHost BuildHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Configuration.AddJsonFile("Configuration/Logging.json", optional: true, reloadOnChange: false);
        LogManager.ConfigureLogging(builder.Logging, builder.Configuration);
        builder.Services.AddVideoWallWpf(builder.Configuration);
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }

    private async Task DisposeHost()
    {
        if (_host is null)
            return;

        var host = _host;
        _host = null;

        await host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        if (host is IAsyncDisposable asyncHost)
            await asyncHost.DisposeAsync().ConfigureAwait(false);
        else
            host.Dispose();
    }
}
