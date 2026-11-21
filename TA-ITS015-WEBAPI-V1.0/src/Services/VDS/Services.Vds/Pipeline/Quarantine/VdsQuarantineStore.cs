using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Services.Vds.Configuration;
using Services.Vds.Pipeline.Mapping;
using Services.Vds.Shared.State;

namespace Services.Vds.Pipeline.Quarantine;

/// <summary>Trạng thái của một mục trong vùng chờ.</summary>
public static class QuarantineStatus
{
    /// <summary>Đang nằm chờ; mục <c>Unmapped</c> còn được replay.</summary>
    public const string Quarantined = "quarantined";

    /// <summary>Đã replay và ghi được — giữ lại một lượt dọn để đối soát rồi mới biến mất.</summary>
    public const string Replayed = "replayed";

    /// <summary>Thôi thử lại: hết số lượt replay, hoặc là dữ liệu sai vĩnh viễn.</summary>
    public const string Dropped = "dropped";
}

/// <summary>
/// Author: KienLT
/// Description: Một bản tin đang nằm trong vùng chờ.
/// Created date: 23/09/2026
/// </summary>
public sealed class QuarantineEntry
{
    /// <summary>Khoá idempotent: <c>vendorEventId</c>, hoặc <c>sha256:{...}</c> khi không đọc được id.</summary>
    [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;

    /// <summary><see cref="VdsRejectKind"/> dạng chuỗi — quyết định có replay hay không.</summary>
    [JsonPropertyName("kind")] public string Kind { get; set; } = nameof(VdsRejectKind.Permanent);

    [JsonPropertyName("status")] public string Status { get; set; } = QuarantineStatus.Quarantined;

    /// <summary>Lý do lấy nguyên văn từ <c>VdsFrameProcessResult.Reason</c>.</summary>
    [JsonPropertyName("reason")] public string? Reason { get; set; }

    /// <summary>Id bản tin phía thiết bị; <c>null</c> khi frame không parse được.</summary>
    [JsonPropertyName("vendorEventId")] public long? VendorEventId { get; set; }

    /// <summary>Có hay không tuỳ frame còn đọc được; dùng để chạy backfill thủ công đúng khoảng.</summary>
    [JsonPropertyName("detectTimeUtc")] public DateTime? DetectTimeUtc { get; set; }

    [JsonPropertyName("cameraIp")] public string? CameraIp { get; set; }

    [JsonPropertyName("firstSeenUtc")] public DateTime FirstSeenUtc { get; set; }

    [JsonPropertyName("lastSeenUtc")] public DateTime LastSeenUtc { get; set; }

    /// <summary>Số lượt replay đã thử; chỉ tăng với mục <c>Unmapped</c>.</summary>
    [JsonPropertyName("attempts")] public int Attempts { get; set; }

    /// <summary>true = frame thô đã lưu được, replay được. false = chỉ còn metadata để đối soát.</summary>
    [JsonPropertyName("hasPayload")] public bool HasPayload { get; set; }

    /// <summary>
    /// Bản sao để sửa. Mọi thay đổi phải làm trên bản sao rồi mới ghi file: sửa thẳng mục đang nằm trong
    /// snapshot RAM thì lượt ghi file thất bại sẽ để lại RAM mới với file cũ — đúng thứ mà nguyên tắc
    /// "bền trước, hiện sau" của <see cref="VdsQuarantineStore"/> muốn tránh.
    /// </summary>
    public QuarantineEntry Clone() => new()
    {
        Key = Key,
        Kind = Kind,
        Status = Status,
        Reason = Reason,
        VendorEventId = VendorEventId,
        DetectTimeUtc = DetectTimeUtc,
        CameraIp = CameraIp,
        FirstSeenUtc = FirstSeenUtc,
        LastSeenUtc = LastSeenUtc,
        Attempts = Attempts,
        HasPayload = HasPayload,
    };
}

/// <summary>
/// Author: KienLT
/// Description: Vùng chờ bền cho những bản tin VDS không xử lý được, để lượt bù lịch sử ĐI TIẾP thay vì đổ
/// cả phiên.
///
/// <para><b>Vì sao không dùng dead-letter queue của Wolverine.</b> <c>Rejected</c> KHÔNG phải exception —
/// <c>ProcessVdsFrameCommandHandler</c> nhận nó rồi kết thúc bình thường, nên với message bus frame đó đã
/// xử lý THÀNH CÔNG và DLQ không bao giờ được kích hoạt. Vùng chờ cũng cần ba thứ mà DLQ không cấp: khoá
/// idempotent để không lưu trùng, số lượt replay, và payload thô đọc lại được.</para>
///
/// <para><b>Ghi bền TRƯỚC khi hiện trong RAM</b> (cùng khuôn <c>IncidentDispatchStore</c>): mọi thay đổi đi
/// qua <see cref="Save"/>, dùng <c>StatePath.WriteAtomic</c> nên file đích luôn ở một trong hai trạng thái
/// nguyên vẹn. Nếu ghi thất bại thì lỗi được ném ra để bên gọi KHÔNG coi frame là đã tiếp nhận.</para>
///
/// <para><b>Hai tầng file.</b> <c>index.json</c> giữ metadata (đọc cả lên RAM lúc khởi động);
/// <c>payload/{key}.json</c> giữ frame thô, chỉ ghi lần đầu. Nhét payload vào index sẽ làm file đó phình
/// theo dung lượng bản tin — đo được ~1,4 KB mỗi frame.</para>
/// Created date: 23/09/2026
/// </summary>
public sealed class VdsQuarantineStore
{
    /// <summary>Tên file index trong thư mục vùng chờ.</summary>
    public const string IndexFileName = "index.json";

    /// <summary>Tên thư mục con chứa frame thô.</summary>
    public const string PayloadDirectoryName = "payload";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly object _sync = new();
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VdsQuarantineStore> _logger;
    private readonly QuarantineHistoryLog _history;
    private readonly string _root;
    private readonly string _indexPath;
    private readonly string _payloadDirectory;

    private Dictionary<string, QuarantineEntry> _entries;
    private long _droppedByTrim;

    public VdsQuarantineStore(IVdsConfigManager config, ILogger<VdsQuarantineStore> logger)
    {
        _config = config;
        _logger = logger;

        var runtime = config.Current;
        _root = StatePath.Resolve(runtime.StateDirectory, runtime.Quarantine.EffectiveDirectory);
        _indexPath = Path.Combine(_root, IndexFileName);
        _payloadDirectory = Path.Combine(_root, PayloadDirectoryName);
        _history = new QuarantineHistoryLog(config, logger, _root);
        _entries = Load();

        if (_entries.Count > 0)
        {
            _logger.LogWarning(
                "[Quarantine] Nạp {Count} bản tin còn trong vùng chờ ({Unmapped} chờ danh mục) tại {Path}.",
                _entries.Count,
                _entries.Values.Count(x => IsReplayable(x)),
                _root);
        }
    }

    /// <summary>Đường dẫn file lịch sử của một ngày UTC — để công cụ đối soát tìm đúng file.</summary>
    public string HistoryPathFor(DateTime utc) => _history.PathFor(utc);

    /// <summary>Số mục đang nằm chờ (chưa replay được, chưa bị thôi thử).</summary>
    public int QuarantinedCount
    {
        get { lock (_sync) return _entries.Values.Count(x => x.Status == QuarantineStatus.Quarantined); }
    }

    /// <summary>Số mục chờ danh mục và còn lượt replay — con số để cảnh báo vận hành.</summary>
    public int ReplayableCount
    {
        get { lock (_sync) return _entries.Values.Count(IsReplayable); }
    }

    /// <summary>Số mục đã thôi thử lại.</summary>
    public int DroppedCount
    {
        get { lock (_sync) return _entries.Values.Count(x => x.Status == QuarantineStatus.Dropped); }
    }

    /// <summary>Tổng số mục bị trần <c>MaxEntries</c> đẩy ra khỏi vùng chờ từ lúc khởi động.</summary>
    public long DroppedByTrimCount
    {
        get { lock (_sync) return _droppedByTrim; }
    }

    /// <summary>
    /// Nhận một bản tin không xử lý được vào vùng chờ.
    ///
    /// <para>Trả về <c>true</c> khi bản tin đã có nơi lưu bền — CHỈ khi đó bên gọi mới được coi frame là đã
    /// tiếp nhận và đi tiếp. Vùng chờ bị tắt cũng trả <c>true</c> vì đó là lựa chọn tường minh của người vận
    /// hành, nhưng có log để không ai tưởng dữ liệu vẫn được giữ.</para>
    /// </summary>
    /// <param name="rawJson">Frame thô, dùng để replay.</param>
    /// <param name="result">Kết quả xử lý mang lý do và <see cref="VdsRejectKind"/>.</param>
    public bool Add(string rawJson, VdsFrameProcessResult result)
    {
        var options = _config.Current.Quarantine;
        if (!options.Enabled)
        {
            _logger.LogWarning(
                "[Quarantine] Đang TẮT nên bỏ bản tin event={EventId} mà không lưu: {Reason}",
                result.VendorEventId, result.Reason);

            // Vẫn ghi một dòng lịch sử: bỏ bản tin mà không để lại dấu vết nào là điều KHÔNG được xảy ra,
            // ngay cả khi người vận hành cố ý tắt vùng chờ.
            _history.Write(QuarantineEvent.Discarded, new QuarantineEntry
            {
                Key = BuildKey(result.VendorEventId, rawJson),
                Kind = (result.RejectKind ?? VdsRejectKind.Permanent).ToString(),
                VendorEventId = result.VendorEventId,
                Reason = result.Reason,
            });
            return true;
        }

        // Đọc lại mốc phát hiện và camera từ chính frame: hai thông tin này là thứ người vận hành cần để
        // chạy bù thủ công đúng khoảng, nhưng kết quả xử lý không mang chúng. Frame JSON đứt thì cả hai để
        // trống — vẫn giữ mục để đối soát được với log thô.
        DateTime? detectTimeUtc = null;
        string? cameraIp = null;
        var envelope = VdsEnvelopeCodec.TryParse(rawJson, out _);
        if (envelope is not null)
        {
            detectTimeUtc = VdsTrafficDataMapper.ResolveDetectTime(envelope);
            cameraIp = envelope.CameraIp?.Trim();
        }

        var kind = result.RejectKind ?? VdsRejectKind.Permanent;
        var key = BuildKey(result.VendorEventId, rawJson);
        var nowUtc = DateTime.UtcNow;

        lock (_sync)
        {
            var next = new Dictionary<string, QuarantineEntry>(_entries, StringComparer.Ordinal);

            if (next.TryGetValue(key, out var existing))
            {
                // Bản tin cũ gặp lại: chỉ dịch mốc lần cuối thấy. KHÔNG tăng attempts ở đây — attempts đếm
                // lượt REPLAY, mà lượt bù gặp lại cùng bản tin không phải một lượt replay.
                var touched = existing.Clone();
                touched.LastSeenUtc = nowUtc;
                touched.Reason = result.Reason ?? touched.Reason;
                next[key] = touched;
                Save(next);
                _history.Write(QuarantineEvent.SeenAgain, touched);
                return true;
            }

            var entry = new QuarantineEntry
            {
                Key = key,
                Kind = kind.ToString(),
                Status = kind == VdsRejectKind.Unmapped
                    ? QuarantineStatus.Quarantined
                    // Dữ liệu sai vĩnh viễn không có đường sửa nên vào thẳng trạng thái thôi thử lại; vẫn
                    // giữ mục để đối soát "thiết bị đã gửi gì".
                    : QuarantineStatus.Dropped,
                Reason = result.Reason,
                VendorEventId = result.VendorEventId,
                DetectTimeUtc = detectTimeUtc,
                CameraIp = cameraIp,
                FirstSeenUtc = nowUtc,
                LastSeenUtc = nowUtc,
            };

            // Payload chỉ cần cho mục còn replay được. Ghi payload TRƯỚC index: index có mục mà payload thiếu
            // là trạng thái tệ hơn, vì lượt replay sẽ đọc ra rỗng.
            if (kind == VdsRejectKind.Unmapped)
                entry.HasPayload = TryWritePayload(key, rawJson);

            next[key] = entry;
            PruneLocked(next, options, nowUtc);
            Save(next);
            _history.Write(QuarantineEvent.Quarantined, entry);

            _logger.LogWarning(
                "[Quarantine] Giữ bản tin event={EventId} camera={CameraIp} kind={Kind}: {Reason} " +
                "(đang chờ {Waiting} bản tin).",
                result.VendorEventId, cameraIp ?? "(không rõ)", entry.Kind, result.Reason,
                next.Values.Count(x => x.Status == QuarantineStatus.Quarantined));

            return true;
        }
    }

    /// <summary>
    /// Danh sách mục còn replay được, kèm frame thô đọc từ đĩa. Mục mất payload bị chuyển sang
    /// <c>dropped</c> ngay để lượt sau không thử lại vô ích.
    /// </summary>
    public List<(QuarantineEntry Entry, string RawJson)> TakeReplayable()
    {
        var result = new List<(QuarantineEntry, string)>();

        lock (_sync)
        {
            var candidates = _entries.Values
                .Where(IsReplayable)
                .OrderBy(x => x.FirstSeenUtc)
                .ToList();
            if (candidates.Count == 0) return result;

            var next = new Dictionary<string, QuarantineEntry>(_entries, StringComparer.Ordinal);
            var changed = false;

            foreach (var entry in candidates)
            {
                var raw = TryReadPayload(entry.Key);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    result.Add((entry.Clone(), raw));
                    continue;
                }

                var lost = entry.Clone();
                lost.Status = QuarantineStatus.Dropped;
                lost.HasPayload = false;
                lost.Reason = $"{lost.Reason} | mất payload nên không replay được.";
                next[lost.Key] = lost;
                changed = true;
                _history.Write(QuarantineEvent.Dropped, lost);
            }

            if (changed) Save(next);
        }

        return result;
    }

    /// <summary>Replay thành công: đánh dấu và bỏ payload.</summary>
    public void MarkReplayed(string key)
    {
        lock (_sync)
        {
            if (!_entries.TryGetValue(key, out var entry)) return;

            var next = new Dictionary<string, QuarantineEntry>(_entries, StringComparer.Ordinal);
            var done = entry.Clone();
            done.Status = QuarantineStatus.Replayed;
            done.LastSeenUtc = DateTime.UtcNow;
            done.HasPayload = false;
            next[key] = done;
            Save(next);
            DeletePayload(key);
            _history.Write(QuarantineEvent.Replayed, done);
        }
    }

    /// <summary>
    /// Replay vẫn chưa được: tăng số lượt. Hết <see cref="QuarantineOptions.MaxReplayAttempts"/> thì chuyển
    /// sang <c>dropped</c> và ghi log mức Error kèm đúng việc người vận hành phải làm.
    /// </summary>
    public void MarkReplayFailed(string key, string? reason)
    {
        var options = _config.Current.Quarantine;

        lock (_sync)
        {
            if (!_entries.TryGetValue(key, out var entry)) return;

            var next = new Dictionary<string, QuarantineEntry>(_entries, StringComparer.Ordinal);
            var failed = entry.Clone();
            failed.Attempts++;
            failed.LastSeenUtc = DateTime.UtcNow;
            failed.Reason = reason ?? failed.Reason;

            if (failed.Attempts >= options.EffectiveMaxReplayAttempts)
            {
                failed.Status = QuarantineStatus.Dropped;
                _logger.LogError(
                    "[Quarantine] Bản tin event={EventId} camera={CameraIp} đã thử map {Attempts} lượt mà " +
                    "danh mục vẫn chưa có thiết bị ⇒ thôi thử lại. Khai thiết bị vào danh mục " +
                    "vds_list_equipment rồi chạy backfill thủ công cho khoảng {DetectTime:O}.",
                    failed.VendorEventId, failed.CameraIp ?? "(không rõ)", failed.Attempts, failed.DetectTimeUtc);
            }

            next[key] = failed;
            Save(next);
            if (failed.Status == QuarantineStatus.Dropped) DeletePayload(key);
            _history.Write(
                failed.Status == QuarantineStatus.Dropped
                    ? QuarantineEvent.Dropped
                    : QuarantineEvent.ReplayFailed,
                failed);
        }
    }

    /// <summary>
    /// Khoá idempotent của một bản tin. <c>vendorEventId</c> là khoá tự nhiên, nhưng frame JSON đứt thì
    /// không đọc được id — khi đó băm nội dung để hai lần gặp cùng một frame vẫn ra một mục.
    ///
    /// <para><b>Không suy thứ tự xử lý từ khoá này.</b> Đo trên dữ liệu thật 23/09/2026: bản tin
    /// <c>event=25359</c> nằm trong lượt bù của checkpoint <c>event=47348</c>, tức id KHÔNG đơn điệu theo
    /// thời gian.</para>
    /// </summary>
    internal static string BuildKey(long? vendorEventId, string rawJson)
    {
        if (vendorEventId is > 0) return vendorEventId.Value.ToString();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawJson ?? string.Empty));
        return $"sha256:{Convert.ToHexString(hash, 0, 8)}";
    }

    /// <summary>Mục chờ danh mục và còn lượt replay.</summary>
    private static bool IsReplayable(QuarantineEntry entry) =>
        entry.Status == QuarantineStatus.Quarantined
        && string.Equals(entry.Kind, nameof(VdsRejectKind.Unmapped), StringComparison.OrdinalIgnoreCase);

    /// <summary>Dọn mục quá hạn và mục vượt trần. Gọi TRONG lock.</summary>
    private void PruneLocked(
        Dictionary<string, QuarantineEntry> next, QuarantineOptions options, DateTime nowUtc)
    {
        var cutoff = nowUtc.AddDays(-options.EffectiveRetentionDays);
        foreach (var expired in next.Values.Where(x => x.FirstSeenUtc < cutoff).ToList())
        {
            next.Remove(expired.Key);
            DeletePayload(expired.Key);
            // Ghi lịch sử TRƯỚC khi mục biến mất khỏi index: sau lượt dọn này thì đây là dấu vết duy nhất.
            _history.Write(QuarantineEvent.Expired, expired,
                $"quá hạn giữ {options.EffectiveRetentionDays} ngày");
        }

        var overflow = next.Count - options.EffectiveMaxEntries;
        if (overflow <= 0) return;

        // Vượt trần là tín hiệu có gì sai ở tầng trên (danh mục thiếu hàng loạt, hoặc nguồn gửi rác). Bỏ mục
        // cũ nhất để vùng chờ không phình vô hạn, nhưng phải để lại dấu vết ở mức Error kèm bộ đếm tổng.
        foreach (var victim in next.Values
                     .OrderBy(x => x.FirstSeenUtc)
                     .Take(overflow)
                     .ToList())
        {
            next.Remove(victim.Key);
            DeletePayload(victim.Key);
            _droppedByTrim++;
            _history.Write(QuarantineEvent.Trimmed, victim,
                $"vượt trần {options.EffectiveMaxEntries} mục");
        }

        _logger.LogError(
            "[Quarantine] Vượt trần {Max} mục nên đã bỏ {Overflow} mục cũ nhất " +
            "(tổng đã bỏ vì trần: {Total}). Kiểm tra danh mục thiết bị và chất lượng dữ liệu nguồn.",
            options.EffectiveMaxEntries, overflow, _droppedByTrim);
    }

    /// <summary>
    /// Ghi index rồi mới thay snapshot RAM. Ném khi ghi thất bại: bên gọi phải biết bản tin CHƯA có nơi lưu
    /// để không coi là đã tiếp nhận.
    /// </summary>
    private void Save(Dictionary<string, QuarantineEntry> next)
    {
        var ordered = next.Values.OrderBy(x => x.FirstSeenUtc).ToList();
        StatePath.WriteAtomic(_indexPath, JsonSerializer.Serialize(ordered, JsonOptions));
        _entries = next;
    }

    private Dictionary<string, QuarantineEntry> Load()
    {
        if (!File.Exists(_indexPath)) return new Dictionary<string, QuarantineEntry>(StringComparer.Ordinal);

        try
        {
            var entries =
                JsonSerializer.Deserialize<List<QuarantineEntry>>(File.ReadAllText(_indexPath), JsonOptions)
                ?? [];
            return entries
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.Last(), StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Index hỏng KHÔNG được âm thầm thành vùng chờ rỗng: các payload trên đĩa vẫn còn và phải đối
            // soát được. Bắt đầu bằng danh sách rỗng nhưng để lại log mức Error kèm đường dẫn.
            _logger.LogError(ex,
                "[Quarantine] Không đọc được {Path}; tiếp tục với vùng chờ rỗng, payload cũ vẫn nằm tại {Dir}.",
                _indexPath, _payloadDirectory);
            return new Dictionary<string, QuarantineEntry>(StringComparer.Ordinal);
        }
    }

    private bool TryWritePayload(string key, string rawJson)
    {
        try
        {
            StatePath.WriteAtomic(Path.Combine(_payloadDirectory, PayloadFileName(key)), rawJson);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Mất payload chỉ mất khả năng REPLAY, không mất dấu vết: metadata vẫn vào index và log thô vẫn
            // giữ nguyên văn frame. Vì vậy chỉ cảnh báo, không ném.
            _logger.LogWarning(ex,
                "[Quarantine] Không ghi được payload của {Key}; mục vẫn được giữ nhưng không replay được.",
                key);
            return false;
        }
    }

    private string? TryReadPayload(string key)
    {
        var path = Path.Combine(_payloadDirectory, PayloadFileName(key));

        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "[Quarantine] Không đọc được payload của {Key}.", key);
            return null;
        }
    }

    private void DeletePayload(string key)
    {
        try
        {
            var path = Path.Combine(_payloadDirectory, PayloadFileName(key));
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "[Quarantine] Không xoá được payload của {Key}.", key);
        }
    }

    /// <summary>Khoá băm có dấu <c>:</c> nên phải đổi trước khi dùng làm tên file trên Windows.</summary>
    private static string PayloadFileName(string key) => $"{key.Replace(':', '_')}.json";
}
