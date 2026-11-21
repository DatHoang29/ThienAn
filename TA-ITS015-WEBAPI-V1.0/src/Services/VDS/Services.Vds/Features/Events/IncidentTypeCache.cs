using System.Collections.Concurrent;

namespace Services.Vds.Features.Events;

/// <summary>Lookup by resolved ITS code. Swapping generations on refresh prevents an
/// in-flight lookup from repopulating the new cache with an old result.</summary>
public sealed class IncidentTypeCache
{
    private sealed record Entry(IncidentRepository.EventTypeRow? Value, DateTime ExpiresUtc);
    private ConcurrentDictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public void Invalidate() => Interlocked.Exchange(ref _entries,
        new ConcurrentDictionary<string, Entry>(StringComparer.OrdinalIgnoreCase));

    internal async Task<IncidentRepository.EventTypeRow?> GetAsync(string code, int missTtlSeconds,
        Func<Task<IncidentRepository.EventTypeRow?>> load)
    {
        var generation = Volatile.Read(ref _entries);
        if (generation.TryGetValue(code, out var cached) && cached.ExpiresUtc > DateTime.UtcNow)
            return cached.Value;

        var value = await load().ConfigureAwait(false);
        generation[code] = new Entry(value, value is null
            ? DateTime.UtcNow.AddSeconds(Math.Max(5, missTtlSeconds)) : DateTime.MaxValue);
        return value;
    }
}
