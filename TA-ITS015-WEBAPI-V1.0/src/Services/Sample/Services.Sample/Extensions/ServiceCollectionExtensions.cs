using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Sample.Configuration;
using Services.Sample.Configuration.Loaders;
using Services.Sample.Configuration.Options;
using Services.Sample.DeviceA;
using Services.Sample.DeviceB;
using Services.Sample.Features.DataFlow.Mapping;
using Services.Sample.Features.DataFlow.Persistence;
using Services.Sample.Features.DataFlow.Processing;
using Services.Sample.Features.DataFlow.Sources.Api;
using Services.Sample.Features.DataFlow.Sources.Nats;
using Services.Sample.Features.Devices;
using Services.Sample.Manager;
using Services.Shared.Configuration;
using Services.Shared.Devices;
using Services.Shared.Events;
using Services.Shared.Extensions;
using Services.Shared.Hosting;
using Wolverine;
using Wolverine.Transports;

namespace Services.Sample.Extensions;

/// <summary>
/// Author: TAC Team
/// Description: Đăng ký runtime cho Sample pipeline + SampleServiceManager.
/// Composition root (Worker Program.cs / WPF App.xaml.cs) gọi AddSampleServices khi build host;
/// SampleServiceManager được resolve từ DI, chỉ nhận service qua constructor — không tự build host.
/// Created date: 16/07/2026
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký pipeline + device acquisition + SampleServiceManager (như IServiceManager/IServiceSimulator).
    /// NatsSource/ApiSource/DataWorker/ConfigReloadWorker tự không chạy nếu config Enabled = false.
    /// </summary>
    public static IServiceCollection AddSampleServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddServiceFoundation(configuration);
        services.AddMediatR(options =>
            options.RegisterServicesFromAssembly(typeof(SampleServiceManager).Assembly));
        services.AddHttpClient("InputApi");
        AddCommonServices(services);

        // Wolverine: local queue "sample-data" cho pipeline data (thay MessageQueue<RawMessage> cũ).
        // Backpressure qua BufferingLimits: khi queue đầy (Maximum=1000), PublishAsync block source
        // cho đến khi consumer drain xuống dưới Restart=500 (hysteresis). Sequential = single consumer,
        // giữ thứ tự như MessageQueue SingleReader cũ.
        // KHÔNG dùng DurabilityMode.MediatorOnly vì mode đó cấm PublishAsync (chỉ cho InvokeAsync sync).
        services.AddWolverine(opts =>
        {
            opts.LocalQueue("sample-data")
                .BufferedInMemory(new BufferingLimits(1000, 500))
                .Sequential();
        });

        services.AddSingleton<DataProcessor>();
        services.AddSingleton<ServiceDataRepository>();

        // Generic Host start theo registration order, stop reverse order:
        // - Start: NatsSource/ApiSource (producer) → ConfigReloadWorker → manager (sau cùng)
        // - Stop: manager dừng runner TRƯỚC, rồi producer.
        // (DataWorker cũ đã thay bằng Wolverine handler ProcessDataCommandHandler — Wolverine tự start/stop.)
        services.AddHostedService<NatsSource>();
        services.AddHostedService<ApiSource>();
        services.AddHostedService<ConfigReloadWorker>();

        // SampleServiceManager: implement IServiceManager (lifecycle) + IServiceSimulator (giả lập).
        // Host (Worker/WPF) tự gọi manager.InitAsync + StartAsync sau khi host đã start,
        // và manager.StopAsync trước khi host stop — đảm bảo runner start sau / stop trước DataWorker.
        // UI inject IServiceSimulator để expose nút Simulate mà không phụ thuộc concrete manager.
        services.AddSingleton<SampleServiceManager>();
        services.AddSingleton<IServiceManager>(sp => sp.GetRequiredService<SampleServiceManager>());
        services.AddSingleton<IServiceSimulator>(sp => sp.GetRequiredService<SampleServiceManager>());

        return services;
    }

    private static void AddCommonServices(IServiceCollection services)
    {
        services.AddSingleton<SampleConfigManager>();
        services.AddSingleton<IConfigManager>(sp => sp.GetRequiredService<SampleConfigManager>());

        // Typed config loaders — mỗi loại config 1 loader, registry (Shared) tự điều phối.
        services.AddSingleton<IConfigLoader<QueueOptions>, QueueOptionsLoader>();
        services.AddSingleton<IConfigLoader<ApiOptions>, ApiOptionsLoader>();
        services.AddSingleton<IConfigLoader<DevicesOptions>, DevicesOptionsLoader>();

        services.AddSingleton<InputMapper>();
        services.AddSingleton<ActivityPublisher>();

        services.AddSingleton<IDeviceCodec, DeviceACodec>();
        services.AddSingleton<IDeviceCodec, DeviceBCodec>();
        services.AddSingleton<DevicePluginLoader>();
        services.AddSingleton<DeviceCodecRegistry>();
        services.AddSingleton<DeviceMessageConverter>();
        services.AddSingleton<DeviceAggregationService>();
        services.AddSingleton<DeviceAcquisitionRunner>();
    }
}
