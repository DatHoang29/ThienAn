using System.Text.Json;
using System.Text.Json.Serialization;
using Services.Vds.Configuration;
using Services.Vds.Shared.State;

namespace Services.Vds.Pipeline.Quarantine;

/// <summary>Tên biến cố được ghi vào lịch sử vùng chờ.</summary>
public static class QuarantineEvent
{
    /// <summary>Bản tin mới vào vùng chờ.</summary>
    public const string Quarantined = "quarantined";

    /// <summary>Lượt bù gặp lại một bản tin đã nằm trong vùng chờ.</summary>
    public const string SeenAgain = "seen-again";

    /// <summary>Replay lại và ghi được.</summary>
    public const string Replayed = "replayed";

    /// <summary>Replay chưa được; còn lượt thử.</summary>
    public const string ReplayFailed = "replay-failed";

    /// <summary>Thôi thử lại: hết lượt replay, hoặc dữ liệu sai vĩnh viễn.</summary>
    public const string Dropped = "dropped";

    /// <summary>Bị dọn vì quá hạn giữ.</summary>
    public const string Expired = "expired";

    /// <summary>Bị trần <c>MaxEntries</c> đẩy ra khỏi vùng chờ.</summary>
    public const string Trimmed = "trimmed";

    /// <summary>Vùng chờ đang TẮT nên bản tin bị bỏ mà không lưu.</summary>
    public const string Discarded = "discarded";
}

/// <summary>
/// Author: KienLT
/// Description: Một dòng lịch sử vùng chờ.
/// Created date: 23/09/2026
/// </summary>
public sealed class QuarantineHistoryRecord
{
    [JsonPropertyName("atUtc")] public DateTime AtUtc { get; set; }

    /// <summary>Xem <see cref="QuarantineEvent"/>.</summary>
    [JsonPropertyName("event")] public string Event { get; set; } = string.Empty;

    [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;

    [JsonPropertyName("kind")] public string? Kind { get; set; }

    [JsonPropertyName("cameraIp")] public string? CameraIp { get; set; }

    [JsonPropertyName("vendorEventId")] public long? VendorEventId { get; set; }

    [JsonPropertyName("detectTimeUtc")] public DateTime? DetectTimeUtc { get; set; }

    [JsonPropertyName("attempts")] public int Attempts { get; set; }

    [JsonPropertyName("reason")] public string? Reason { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Lịch sử CHỈ-THÊM của vùng chờ, tách file theo ngày.
///
/// <para><b>Vì sao cần dù đã có <c>index.json</c>.</b> Index là TRẠNG THÁI HIỆN HÀNH, không phải lịch sử: mục
/// replay xong sẽ mất ở lượt dọn, mục quá hạn giữ bị xoá, mục vượt trần <c>MaxEntries</c> bị đẩy ra. Nên nó
/// không trả lời được những câu hỏi vận hành thật sự cần — hôm qua có bao nhiêu bản tin bị bỏ, của camera
/// nào, vì lý do gì, và bản tin đó về sau có được bù không.</para>
///
/// <para><b>Chỉ thêm, không bao giờ sửa.</b> Ghi một dòng JSON cho mỗi biến cố (nhận vào, gặp lại, replay
/// được, thôi thử lại, quá hạn, bị trần đẩy ra). Nhờ vậy chi phí ghi là O(1) theo một dòng, khác với index
/// phải ghi lại toàn bộ file mỗi lần đổi — đo ngày 23/09/2026: index đã 522 KB với 1.243 mục, tức mỗi bản tin
/// bị bỏ lại kéo theo một lượt ghi 522 KB.</para>
///
/// <para><b>Không tự xoá file cũ</b> khi <see cref="QuarantineOptions.HistoryRetentionDays"/> = 0, cùng quy
/// ước với <c>VdsRawLogger</c>: đây là bản ghi để đối soát, việc dọn thuộc về vận hành.</para>
///
/// <para>Lỗi ghi lịch sử CHỈ cảnh báo, không ném: mất một dòng lịch sử không được phép làm mất một bản tin —
/// việc giữ bản tin là của <c>index.json</c> và <c>payload/</c>.</para>
/// Created date: 23/09/2026
/// </summary>
public sealed class QuarantineHistoryLog
{
    /// <summary>Tên thư mục con chứa file lịch sử theo ngày.</summary>
    public const string DirectoryName = "history";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IVdsConfigManager _config;
    private readonly ILogger _logger;
    private readonly string _directory;
    private readonly object _sync = new();

    private int _writeFailedLogged;
    private DateOnly _lastPrunedOn;

    public QuarantineHistoryLog(IVdsConfigManager config, ILogger logger, string quarantineRoot)
    {
        _config = config;
        _logger = logger;
        _directory = Path.Combine(quarantineRoot, DirectoryName);
    }

    /// <summary>Đường dẫn file lịch sử của một ngày UTC.</summary>
    public string PathFor(DateTime utc) =>
        Path.Combine(_directory, $"quarantine-{utc:yyyyMMdd}.jsonl");

    /// <summary>
    /// Ghi một biến cố. Gọi từ đường ghi dữ liệu nên phải rẻ và KHÔNG ném.
    /// </summary>
    public void Write(
        string @event,
        QuarantineEntry entry,
        string? reasonOverride = null)
    {
        var options = _config.Current.Quarantine;
        if (!options.HistoryEnabled) return;

        var record = new QuarantineHistoryRecord
        {
            AtUtc = DateTime.UtcNow,
            Event = @event,
            Key = entry.Key,
            Kind = entry.Kind,
            CameraIp = entry.CameraIp,
            VendorEventId = entry.VendorEventId,
            DetectTimeUtc = entry.DetectTimeUtc,
            Attempts = entry.Attempts,
            Reason = reasonOverride ?? entry.Reason,
        };

        try
        {
            // Một dòng một biến cố: gộp mọi ký tự xuống dòng trong lý do lại, nếu không thì không
            // grep/đếm theo dòng được.
            var line = JsonSerializer.Serialize(record, JsonOptions).ReplaceLineEndings(" ");

            lock (_sync)
            {
                Directory.CreateDirectory(_directory);
                File.AppendAllText(PathFor(record.AtUtc), line + Environment.NewLine);
                PruneLocked(options, record.AtUtc);
            }

            Interlocked.Exchange(ref _writeFailedLogged, 0);
        }
        catch (Exception ex)
        {
            // Chỉ cảnh báo MỘT lần cho mỗi đợt lỗi: đĩa đầy hay thiếu quyền sẽ lặp trên mọi bản tin.
            if (Interlocked.Exchange(ref _writeFailedLogged, 1) == 0)
            {
                _logger.LogWarning(ex,
                    "[Quarantine] Ghi lịch sử thất bại; vùng chờ vẫn giữ bản tin trong index.json.");
            }
        }
    }

    /// <summary>
    /// Dọn file lịch sử quá hạn. Chạy nhiều nhất MỘT lần mỗi ngày để không quét thư mục trên mỗi dòng ghi.
    /// <see cref="QuarantineOptions.HistoryRetentionDays"/> = 0 ⇒ không xoá gì.
    /// </summary>
    private void PruneLocked(QuarantineOptions options, DateTime nowUtc)
    {
        var retentionDays = options.HistoryRetentionDays;
        if (retentionDays <= 0) return;

        var today = DateOnly.FromDateTime(nowUtc);
        if (_lastPrunedOn == today) return;
        _lastPrunedOn = today;

        var cutoff = nowUtc.Date.AddDays(-retentionDays);
        foreach (var file in Directory.EnumerateFiles(_directory, "quarantine-*.jsonl"))
        {
            var stamp = Path.GetFileNameWithoutExtension(file).Replace("quarantine-", "");
            if (!DateTime.TryParseExact(stamp, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out var day)) continue;
            if (day >= cutoff) continue;

            try { File.Delete(file); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "[Quarantine] Không xoá được file lịch sử {File}.", file);
            }
        }
    }
}
