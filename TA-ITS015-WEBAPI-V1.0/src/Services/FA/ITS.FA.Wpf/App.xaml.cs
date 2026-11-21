using System.Windows;
using ITS.FA.Core;
using ITS.FA.Wpf.Logging;
using ITS.FA.Wpf.Persistence;
using ITS.FA.Wpf.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Shared.Logging;

namespace ITS.FA.Wpf;

/// <summary>
/// Composition root của form test: cùng AddFaServices với Worker, chỉ thêm phần giao diện.
/// async void là cố ý: chặn luồng UI khi chờ DB sẽ gây deadlock (xem App của Services.Vds.Wpf).
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private MainViewModel? _viewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = AppContext.BaseDirectory,
            });

            builder.Configuration.AddJsonFile("Configuration/Database.json", optional: false, reloadOnChange: false);
            builder.Configuration.AddJsonFile("Configuration/DataTransporter.json", optional: true, reloadOnChange: false);
            builder.Configuration.AddEnvironmentVariables();

            var logSink = new UiLogSink();
            builder.Logging.AddProvider(logSink);

            builder.Services.AddFaServices(builder.Configuration);
            builder.Services.AddSingleton(logSink);
            builder.Services.AddSingleton<FaTestRepository>();
            builder.Services.AddSingleton<MainViewModel>();

            _host = builder.Build();
            LogManager.SetFactory(_host.Services.GetRequiredService<ILoggerFactory>());

            _viewModel = _host.Services.GetRequiredService<MainViewModel>();
            MainWindow = new MainWindow { DataContext = _viewModel };
            MainWindow.Show();

            await _host.StartAsync();
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không khởi động được form test FA.\n\n{ex}", "ITS FA Test",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_viewModel is not null) await _viewModel.DisposeAsync();
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
