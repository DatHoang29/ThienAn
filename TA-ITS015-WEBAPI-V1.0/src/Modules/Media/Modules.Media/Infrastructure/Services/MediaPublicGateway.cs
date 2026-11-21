using Microsoft.Extensions.Logging;
using Modules.Media.Infrastructure.Config;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;

namespace Modules.Media.Infrastructure.Services;

/// <summary>
/// Description: [WP] Luồng chung của mọi phân hệ MediaPublic
/// Created date: 08/10/2026
/// </summary>
public sealed class MediaPublicGateway
{
    /// <summary>Tiền tố khoá cache theo phân hệ.</summary>
    private static readonly Dictionary<string, string> CachePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        [MediaConst.Subsystem.Incident] = CacheConst.Media.MediaIncident,
        [MediaConst.Subsystem.EventType] = CacheConst.Media.MediaEventType,
        [MediaConst.Subsystem.EquipmentType] = CacheConst.Media.MediaEquipmentType,
        [MediaConst.Subsystem.Vms] = CacheConst.Media.MediaVms,
    };

    private readonly MediaProviderClient _client;
    private readonly BaseCacheService _cache;
    private readonly ILogger<MediaPublicGateway> _logger;

    public MediaPublicGateway(MediaProviderClient client, BaseCacheService cache, ILogger<MediaPublicGateway> logger)
    {
        _client = client;
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(
        MediaConfig config,
        string subsystem,
        string key,
        string version,
        object query,
        Func<T, bool> canCache,
        Func<string, T> fallback,
        CancellationToken ct) where T : class
    {
        var cacheKey = $"{CachePrefixes.GetValueOrDefault(subsystem, $"media_{subsystem}")}:{key}:{version}";

        var cached = _cache.Get<T>(cacheKey);
        if (cached != null) return cached;

        try
        {
            var value = await _client.GetAsync<T>(config, subsystem, query, ct)
                        ?? fallback(MediaConst.Status.NotFound);

            var duration = canCache(value)
                ? TimeSpan.FromMinutes(config.CacheMinutes)
                : TimeSpan.FromSeconds(config.PendingCacheSeconds);
            if (duration > TimeSpan.Zero) _cache.Set(cacheKey, value, duration);

            return value;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MediaPublic] Không lấy được {Subsystem}/{Key} từ ITS: {Message}",
                subsystem, key, ex.Message);

            var unavailable = fallback(MediaConst.Status.Unavailable);
            if (config.NegativeCacheSeconds > 0)
                _cache.Set(cacheKey, unavailable, TimeSpan.FromSeconds(config.NegativeCacheSeconds));
            return unavailable;
        }
    }
}
