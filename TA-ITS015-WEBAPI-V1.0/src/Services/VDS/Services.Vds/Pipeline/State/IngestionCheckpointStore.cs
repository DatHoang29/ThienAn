using System.Text.Json;
using System.Text.Json.Serialization;
using Services.Vds.Shared.State;
using Services.Vds.Configuration;

namespace Services.Vds.Pipeline.State;

public sealed record BackfillPendingLease(DateTime FromUtc, long Revision);

/// <summary>Mốc ingestion và trạng thái coverage của history backfill.</summary>
public sealed class IngestionCheckpoint
{
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("lastEventId")]
    public long LastEventId { get; set; }

    [JsonPropertyName("lastOccurredUtc")]
    public DateTime? LastOccurredUtc { get; set; }

    [JsonPropertyName("lastCameraIp")]
    public string? LastCameraIp { get; set; }

    [JsonPropertyName("pendingBackfillFromUtc")]
    public DateTime? PendingBackfillFromUtc { get; set; }

    [JsonPropertyName("pendingBackfillRevision")]
    public long PendingBackfillRevision { get; set; }

    [JsonPropertyName("lastBackfillThroughUtc")]
    public DateTime? LastBackfillThroughUtc { get; set; }

    [JsonPropertyName("updatedUtc")]
    public DateTime? UpdatedUtc { get; set; }

    public IngestionCheckpoint Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        LastEventId = LastEventId,
        LastOccurredUtc = LastOccurredUtc,
        LastCameraIp = LastCameraIp,
        PendingBackfillFromUtc = PendingBackfillFromUtc,
        PendingBackfillRevision = PendingBackfillRevision,
        LastBackfillThroughUtc = LastBackfillThroughUtc,
        UpdatedUtc = UpdatedUtc,
    };
}

/// <summary>
/// Author: KienLT
/// Description: Lưu checkpoint ingestion bằng ghi atomic. Checkpoint chỉ tiến SAU khi repository insert
/// thành công; pending backfill chỉ được xóa bằng đúng revision mà coordinator đã sở hữu.
/// Created date: 25/08/2026
/// </summary>
public sealed class IngestionCheckpointStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly object _sync = new();
    private readonly string _path;
    private readonly ILogger<IngestionCheckpointStore> _logger;
    private IngestionCheckpoint _current;

    public IngestionCheckpointStore(
        IVdsConfigManager config, ILogger<IngestionCheckpointStore> logger)
    {
        _logger = logger;
        _path = StatePath.Resolve(config.Current.StateDirectory, "ingestion-checkpoint.json");
        _current = Load();

        if (_current.LastEventId > 0
            || _current.LastOccurredUtc.HasValue
            || _current.PendingBackfillFromUtc.HasValue
            || _current.LastBackfillThroughUtc.HasValue)
        {
            _logger.LogInformation(
                "[Ingestion] Nạp checkpoint: event={EventId}, occurredUtc={OccurredUtc:O}, " +
                "pendingBackfillFromUtc={Pending:O}, pendingRevision={Revision}, " +
                "lastBackfillThroughUtc={Through:O}.",
                _current.LastEventId,
                _current.LastOccurredUtc,
                _current.PendingBackfillFromUtc,
                _current.PendingBackfillRevision,
                _current.LastBackfillThroughUtc);
        }
    }

    public IngestionCheckpoint Current
    {
        get
        {
            lock (_sync) return _current.Clone();
        }
    }

    public Task AdvanceAsync(
        long vendorEventId,
        DateTime occurredUtc,
        string? cameraIp,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        occurredUtc = EnsureUtc(occurredUtc);

        lock (_sync)
        {
            ct.ThrowIfCancellationRequested();

            var next = _current.Clone();
            if (vendorEventId > next.LastEventId)
                next.LastEventId = vendorEventId;

            if (!next.LastOccurredUtc.HasValue || occurredUtc > next.LastOccurredUtc.Value)
            {
                next.LastOccurredUtc = occurredUtc;
                next.LastCameraIp = cameraIp;
            }

            next.UpdatedUtc = DateTime.UtcNow;
            PersistLocked(next);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Ghi/thu hẹp đầu cửa sổ pending và cấp revision mới. Mọi live gap ghi xen kẽ cũng tăng revision,
    /// nên coordinator cũ không thể xóa nhầm marker mới.
    /// </summary>
    public Task<BackfillPendingLease> MarkBackfillStartedAsync(
        DateTime fromUtc,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        fromUtc = EnsureUtc(fromUtc);
        BackfillPendingLease lease;

        lock (_sync)
        {
            ct.ThrowIfCancellationRequested();

            var next = _current.Clone();
            if (!next.PendingBackfillFromUtc.HasValue
                || fromUtc < next.PendingBackfillFromUtc.Value)
            {
                next.PendingBackfillFromUtc = fromUtc;
            }

            next.PendingBackfillRevision = checked(next.PendingBackfillRevision + 1);
            next.UpdatedUtc = DateTime.UtcNow;
            PersistLocked(next);
            lease = new BackfillPendingLease(
                next.PendingBackfillFromUtc!.Value,
                next.PendingBackfillRevision);
        }

        return Task.FromResult(lease);
    }

    /// <summary>
    /// Compare-and-swap completion: chỉ advance coverage và clear pending khi marker không bị một live
    /// reject/exception đổi revision kể từ lúc coordinator bắt đầu.
    /// </summary>
    public Task<BackfillPendingLease?> TryAdvanceBackfillAsync(BackfillPendingLease lease, DateTime throughUtc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        throughUtc = EnsureUtc(throughUtc);
        if (throughUtc < lease.FromUtc) throw new ArgumentOutOfRangeException(nameof(throughUtc));
        lock (_sync)
        {
            if (_current.PendingBackfillFromUtc != lease.FromUtc || _current.PendingBackfillRevision != lease.Revision)
                return Task.FromResult<BackfillPendingLease?>(null);
            var next = _current.Clone();
            next.PendingBackfillFromUtc = throughUtc;
            next.PendingBackfillRevision = checked(next.PendingBackfillRevision + 1);
            if (next.LastBackfillThroughUtc == null || throughUtc > next.LastBackfillThroughUtc) next.LastBackfillThroughUtc = throughUtc;
            next.UpdatedUtc = DateTime.UtcNow;
            PersistLocked(next);
            return Task.FromResult<BackfillPendingLease?>(new(throughUtc, next.PendingBackfillRevision));
        }
    }

    public Task<bool> TryCompleteBackfillAsync(
        BackfillPendingLease lease,
        DateTime throughUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ct.ThrowIfCancellationRequested();
        var expectedFromUtc = EnsureUtc(lease.FromUtc);
        throughUtc = EnsureUtc(throughUtc);

        lock (_sync)
        {
            ct.ThrowIfCancellationRequested();

            if (!_current.PendingBackfillFromUtc.HasValue
                || _current.PendingBackfillFromUtc.Value != expectedFromUtc
                || _current.PendingBackfillRevision != lease.Revision)
            {
                return Task.FromResult(false);
            }

            var next = _current.Clone();
            if (!next.LastBackfillThroughUtc.HasValue
                || throughUtc > next.LastBackfillThroughUtc.Value)
            {
                next.LastBackfillThroughUtc = throughUtc;
            }

            next.PendingBackfillFromUtc = null;
            next.UpdatedUtc = DateTime.UtcNow;
            PersistLocked(next);
        }

        return Task.FromResult(true);
    }

    private void PersistLocked(IngestionCheckpoint next)
    {
        // Chỉ thay snapshot RAM sau khi file đã ghi thành công. File hỏng/đĩa đầy ⇒ checkpoint cũ,
        // replay lại vẫn an toàn nhờ DEDUP (detectTime, vendorEventId).
        next.SchemaVersion = IngestionCheckpoint.CurrentSchemaVersion;
        var json = JsonSerializer.Serialize(next, JsonOptions);
        StatePath.WriteAtomic(_path, json);
        _current = next;
    }

    private IngestionCheckpoint Load()
    {
        if (!File.Exists(_path)) return new IngestionCheckpoint();

        try
        {
            var json = File.ReadAllText(_path);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException(
                    "Checkpoint ingestion phải là một JSON object.");
            }

            // File từ version trước chưa có schemaVersion nhưng luôn có ba field cấu trúc này.
            // Chấp nhận để migrate; reject {}, null và object lạ thay vì âm thầm reset state.
            if (!HasProperty(root, "lastEventId")
                || !HasProperty(root, "lastOccurredUtc")
                || !HasProperty(root, "updatedUtc"))
            {
                throw new InvalidDataException(
                    "Checkpoint ingestion thiếu cấu trúc bắt buộc.");
            }

            if (TryGetProperty(root, "schemaVersion", out var schemaElement)
                && (schemaElement.ValueKind != JsonValueKind.Number
                    || !schemaElement.TryGetInt32(out var schemaVersion)
                    || schemaVersion != IngestionCheckpoint.CurrentSchemaVersion))
            {
                throw new InvalidDataException(
                    "Checkpoint ingestion có schemaVersion không được hỗ trợ.");
            }

            var checkpoint = JsonSerializer.Deserialize<IngestionCheckpoint>(json, JsonOptions)
                ?? throw new InvalidDataException(
                    "Checkpoint ingestion không deserialize được thành state.");
            checkpoint.SchemaVersion = IngestionCheckpoint.CurrentSchemaVersion;

            if (checkpoint.PendingBackfillRevision < 0)
                throw new InvalidDataException("pendingBackfillRevision không được âm.");

            // Migrate file pending được ghi trước khi có revision.
            if (checkpoint.PendingBackfillFromUtc.HasValue
                && checkpoint.PendingBackfillRevision == 0)
            {
                checkpoint.PendingBackfillRevision = 1;
            }

            if (checkpoint.LastOccurredUtc.HasValue)
                checkpoint.LastOccurredUtc = EnsureUtc(checkpoint.LastOccurredUtc.Value);
            if (checkpoint.PendingBackfillFromUtc.HasValue)
                checkpoint.PendingBackfillFromUtc = EnsureUtc(checkpoint.PendingBackfillFromUtc.Value);
            if (checkpoint.LastBackfillThroughUtc.HasValue)
                checkpoint.LastBackfillThroughUtc = EnsureUtc(checkpoint.LastBackfillThroughUtc.Value);
            if (checkpoint.UpdatedUtc.HasValue)
                checkpoint.UpdatedUtc = EnsureUtc(checkpoint.UpdatedUtc.Value);

            return checkpoint;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex,
                "[Ingestion] Checkpoint không hợp lệ; dừng để không làm mất pending backfill.");
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(ex,
                "[Ingestion] Không đọc được ingestion-checkpoint.json; dừng để không làm mất pending backfill.");
            throw new InvalidDataException(
                $"Không đọc được checkpoint ingestion tại '{_path}'.", ex);
        }
    }

    private static bool HasProperty(JsonElement root, string name) =>
        TryGetProperty(root, name, out _);

    private static bool TryGetProperty(
        JsonElement root,
        string name,
        out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            value = property.Value;
            return true;
        }

        value = default;
        return false;
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
