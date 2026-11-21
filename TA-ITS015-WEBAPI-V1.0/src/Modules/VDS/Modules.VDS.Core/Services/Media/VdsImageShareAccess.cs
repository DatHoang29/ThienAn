using Modules.VDS.Core.Interfaces;
using Modules.VDS.Core.Options;
using Modules.VDS.Core.Entities;

namespace Modules.VDS.Core.Services.Media;

/// <summary>
/// Author: KienLT
/// Description: Applies existing share credentials to configured VDS storage roots; local files keep process identity.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsImageShareAccess(
    Func<string, CancellationToken, Task<VdsImageShareOptions>> loadSettings, IVdsNetworkFileIdentity? identity = null) : IVdsImageFileAccess
{
    public VdsImageShareAccess(Func<CancellationToken, Task<VdsImageShareOptions>> loadSettings,
        IVdsNetworkFileIdentity? identity = null) : this((_, ct) => loadSettings(ct), identity) { }
    private readonly IVdsNetworkFileIdentity _identity = identity ?? new VdsWindowsNetworkFileIdentity();
    private readonly SemaphoreSlim _settingsLock = new(1, 1);
    private readonly Dictionary<string, (VdsImageShareOptions Settings, DateTime Expires)> _settings = new(StringComparer.OrdinalIgnoreCase);

    public static string? UncShare(string? path)
    {
        var normalized = path?.Trim().Replace('/', '\\');
        if (normalized is null || !normalized.StartsWith(@"\\", StringComparison.Ordinal)
            || normalized.StartsWith(@"\\?\", StringComparison.Ordinal) || normalized.StartsWith(@"\\.\", StringComparison.Ordinal)) return null;
        var parts = normalized[2..].Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && parts[0] is not ("." or "..") && parts[1] is not ("." or "..")
            ? @"\\" + parts[0] + @"\" + parts[1] : null;
    }

    public async Task<T> RunAsync<T>(string configuredRoot, Func<Task<T>> operation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (UncShare(configuredRoot) is null) return await operation().ConfigureAwait(false);
        var settings = await GetSettingsAsync(configuredRoot, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(settings.UserName)) return await operation().ConfigureAwait(false);
        if (!string.Equals(UncShare(configuredRoot), UncShare(settings.BasePath), StringComparison.OrdinalIgnoreCase))
        {
            // Preserve VDS's existing host access on independently configured storage. The legacy
            // account belongs to its own share; applying it elsewhere can replace valid host credentials.
            try { return await operation().ConfigureAwait(false); }
            catch (Exception ex) when (ex is UnauthorizedAccessException
                || ex is IOException && (ex.HResult & 0xffff) is 5 or 86 or 1326)
            {
                throw new IOException("Configured VDS storage uses a different UNC share from VdsConfig/vds_media_storage_directory. "
                    + "Access with the host account failed; VDS credentials were not applied to another share.", ex);
            }
        }
        return await _identity.RunAsync(configuredRoot, settings, operation, ct).ConfigureAwait(false);
    }

    private async Task<VdsImageShareOptions> GetSettingsAsync(string root, CancellationToken ct)
    {
        await _settingsLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var key = UncShare(root)!;
            if (_settings.TryGetValue(key, out var cached) && DateTime.UtcNow < cached.Expires) return cached.Settings;
            var settings = await loadSettings(root, ct).ConfigureAwait(false);
            _settings[key] = (settings, DateTime.UtcNow.AddSeconds(30));
            return settings;
        }
        finally { _settingsLock.Release(); }
    }
}
