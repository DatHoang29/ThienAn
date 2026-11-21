using System.Text.Json;
using System.Text.Json.Serialization;
using Services.Vds.Configuration;
using Services.Vds.Shared.State;

namespace Services.Vds.Features.Events.Automation;

public sealed class PendingDispatchEntry
{
    [JsonPropertyName("incidentId")] public string IncidentId { get; set; } = string.Empty;
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("createdUtc")] public DateTime CreatedUtc { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "pending";
    [JsonPropertyName("expiredUtc")] public DateTime? ExpiredUtc { get; set; }
}
/// <summary>Legacy dispatch journal. Mutations are persisted before becoming visible in memory.
/// New incidents also carry a pending marker in SQL, atomically with the incident insert.
///
/// <para><b>ĐANG LÀ ĐƯỜNG DI TRÚ (08/09/2026)</b>: nguồn sự thật của "còn phải bàn giao lệnh lập lịch" đã
/// chuyển sang MARKER SQL ghi cùng lệnh INSERT sự cố (<c>IncidentRepository.WithPendingDispatch</c> /
/// <c>ReadPendingDispatchAsync</c> / <c>CompleteDispatchAsync</c>) — bền hơn file vì nằm trong cùng giao
/// dịch với sự cố. Vì vậy <see cref="MarkPending"/> KHÔNG còn được gọi ở đường chạy chính; lớp này chỉ còn
/// để <see cref="TakeReplayable"/> VÉT các mục tồn từ file của bản chạy trước rồi
/// <see cref="MarkDone"/> xoá dần. Khi chắc chắn không còn máy nào giữ file cũ thì xoá được cả lớp này.</para>
/// </summary>
public sealed class IncidentDispatchStore : IDisposable
{
    public const string FileName = "incident-dispatch-pending.json";
    private readonly object _sync = new();
    private readonly string _path;
    private readonly ILogger<IncidentDispatchStore> _logger;
    private readonly IVdsConfigManager _config;
    private Dictionary<string, PendingDispatchEntry> _pending;
    public IncidentDispatchStore(IVdsConfigManager config, ILogger<IncidentDispatchStore> logger)
    {
        _config = config;
        _logger = logger;
        _path = StatePath.Resolve(config.Current.StateDirectory, FileName);
        // A corrupt journal must remain visible as an error, never silently become an empty journal.
        var entries = File.Exists(_path)
            ? JsonSerializer.Deserialize<List<PendingDispatchEntry>>(File.ReadAllText(_path)) ?? [] : [];
        _pending = entries.Where(x => !string.IsNullOrWhiteSpace(x.IncidentId))
            .GroupBy(x => x.IncidentId).ToDictionary(x => x.Key, x => x.Last());
    }
    public int PendingCount { get { lock (_sync) return _pending.Values.Count(x => x.Status == "pending"); } }
    public int ExpiredCount { get { lock (_sync) return _pending.Values.Count(x => x.Status == "expired"); } }
    public void MarkPending(string? incidentId, string? code, DateTime createdUtc)
    {
        if (string.IsNullOrWhiteSpace(incidentId)) return;
        lock (_sync)
        {
            if (_pending.ContainsKey(incidentId)) return;
            var next = new Dictionary<string, PendingDispatchEntry>(_pending) {
                [incidentId] = new() { IncidentId = incidentId, Code = code, CreatedUtc = createdUtc }
            };
            Save(next);
        }
    }
    public void MarkDone(string? incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId)) return;
        lock (_sync)
        {
            if (!_pending.ContainsKey(incidentId)) return;
            var next = new Dictionary<string, PendingDispatchEntry>(_pending);
            next.Remove(incidentId);
            Save(next);
        }
    }
    public void MarkFailed(string incidentId, string? reason)
    {
        lock (_sync)
        {
            if (!_pending.TryGetValue(incidentId, out var entry) || entry.Status != "pending") return;
            var next = new Dictionary<string, PendingDispatchEntry>(_pending);
            next[incidentId] = new PendingDispatchEntry { IncidentId = entry.IncidentId, Code = entry.Code,
                CreatedUtc = entry.CreatedUtc, Status = "failed", Reason = reason };
            Save(next);
        }
    }

    public List<PendingDispatchEntry> TakeReplayable()
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            var cutoff = now.AddMinutes(-_config.Current.TmsApi.EffectiveDispatchRetentionMinutes);
            var expired = _pending.Values.Where(x => x.Status == "pending" && x.CreatedUtc < cutoff).ToList();
            if (expired.Count > 0)
            {
                var next = new Dictionary<string, PendingDispatchEntry>(_pending);
                foreach (var entry in expired)
                    next[entry.IncidentId] = new PendingDispatchEntry { IncidentId = entry.IncidentId, Code = entry.Code,
                        CreatedUtc = entry.CreatedUtc, Status = "expired", ExpiredUtc = now };
                Save(next); // Retain durable evidence; never resurrect on retention config changes.
                _logger.LogWarning("[TmsApi] {Count} legacy dispatch quá hạn; giữ status=expired trong {Path} để đối soát.", expired.Count, _path);
            }
            return _pending.Values.Where(x => x.Status == "pending").OrderBy(x => x.CreatedUtc).ToList();
        }
    }
    private void Save(Dictionary<string, PendingDispatchEntry> next)
    {
        StatePath.WriteAtomic(_path, JsonSerializer.Serialize(next.Values.OrderBy(x => x.CreatedUtc)));
        _pending = next;
    }
    public void Flush() { } // Already durable; kept for callers upgrading from the timer journal.
    public void Dispose() { }
}
