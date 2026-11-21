using Services.CctvProxy.Configuration.Options;

namespace Services.CctvProxy.Configuration;

/// <summary>
/// Author: kien.le
/// Description: Static accessor cho cấu hình runtime (theo mẫu Services.Sample.Configuration.ConfigManager).
/// Composition root (Worker Program.cs) gọi <see cref="SetInstance"/> sau khi resolve IConfigManager từ DI;
/// sau đó truy cập từ bất cứ đâu: <code>ConfigManager.Current.Cctv</code>, <code>ConfigManager.Current.Public</code>.
/// Created date: 06/10/2026
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
    /// <param name="cancellationToken">Huỷ thao tác nạp lại.</param>
    public static Task ReloadAsync(CancellationToken cancellationToken = default) =>
        Instance.ReloadAsync(cancellationToken);

    /// <summary>Composition root gọi sau khi resolve IConfigManager từ DI.</summary>
    /// <param name="instance">IConfigManager singleton.</param>
    public static void SetInstance(IConfigManager instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        _instance = instance;
    }
}
