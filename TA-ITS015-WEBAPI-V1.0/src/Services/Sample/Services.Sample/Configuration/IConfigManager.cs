using Services.Sample.Configuration.Options;

namespace Services.Sample.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Facade đọc cấu hình runtime của Sample service. Gộp file appsettings +
/// typed loaders, validate, và expose snapshot hiện tại qua <see cref="Current"/>.
/// Worker/WPF/ConfigReloadWorker đều tương tác qua interface này.
/// Truy cập section: configManager.Current.Api, configManager.Current.Devices, v.v.
/// Created date: 17/07/2026
/// </summary>
public interface IConfigManager
{
    /// <summary>Snapshot cấu hình hiện tại (sau lần ReloadAsync gần nhất). Truy cập section qua .Current.XXX.</summary>
    ServiceConfig Current { get; }

    /// <summary>Nạp / nạp lại cấu hình: bind file, apply typed loaders, validate.</summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
