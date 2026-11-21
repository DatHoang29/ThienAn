using Services.CctvProxy.Configuration.Options;

namespace Services.CctvProxy.Configuration;

/// <summary>
/// Author: kien.le
/// Description: Facade đọc cấu hình runtime của Cctv Proxy. Gộp appsettings + typed loaders,
/// expose snapshot hiện tại qua <see cref="Current"/>.
/// Created date: 06/10/2026
/// </summary>
public interface IConfigManager
{
    /// <summary>Snapshot cấu hình hiện tại (sau lần ReloadAsync gần nhất).</summary>
    ServiceConfig Current { get; }

    /// <summary>Nạp / nạp lại cấu hình: bind file, apply typed loaders, validate.</summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
