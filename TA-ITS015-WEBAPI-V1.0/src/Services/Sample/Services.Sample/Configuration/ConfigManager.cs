using Services.Sample.Configuration.Options;

namespace Services.Sample.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Static accessor cho cấu hình runtime. Composition root (Worker Program.cs / WPF App.xaml.cs)
/// gọi <see cref="SetInstance"/> sau khi resolve IConfigManager từ DI. Sau đó truy cập từ bất cứ đâu:
/// <code>ConfigManager.Current.Api</code>, <code>ConfigManager.Current.Devices</code>, <code>ConfigManager.Current.Name</code>.
/// Created date: 17/07/2026
/// </summary>
public static class ConfigManager
{
    private static IConfigManager? _instance;

    /// <summary>Singleton IConfigManager đã được set từ composition root.</summary>
    public static IConfigManager Instance =>
        _instance ?? throw new InvalidOperationException(
            $"{nameof(ConfigManager)} chưa được khởi tạo. Gọi {nameof(SetInstance)} ở composition root trước khi dùng.");

    /// <summary>Snapshot cấu hình hiện tại — shortcut cho Instance.Current.</summary>
    public static ServiceConfig Current => Instance.Current;

    /// <summary>Nạp lại cấu hình — shortcut cho Instance.ReloadAsync.</summary>
    public static Task ReloadAsync(CancellationToken cancellationToken = default) =>
        Instance.ReloadAsync(cancellationToken);

    /// <summary>Composition root gọi sau khi resolve IConfigManager từ DI.</summary>
    public static void SetInstance(IConfigManager instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        _instance = instance;
    }
}
