using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Services.Vds.Configuration;
using Services.Vds.Shared.State;

namespace Services.Vds.Pipeline;

/// <summary>Một mục cửa SKIP như nằm trên đĩa.</summary>
public sealed class VehicleSkipFileEntry
{
    public double? WindowSeconds { get; set; }
    /// <summary>Khoá chặn đã dựng sẵn: <c>camera|loại</c> hoặc <c>camera|loại|biển</c>.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Thời điểm của lượt đã cho qua (<c>detectTime</c>, UTC).</summary>
    [JsonPropertyName("atUtc")]
    public DateTime AtUtc { get; set; }

    /// <summary>Id bản tin phía thiết bị của lượt đã cho qua — cần để lượt retry không bị chặn oan.</summary>
    [JsonPropertyName("vendorEventId")]
    public long VendorEventId { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Lưu trạng thái cửa SKIP (<see cref="VehicleDedupGate"/>) ra đĩa để sống qua restart.
/// Tách <b>MỘT FILE MỖI LOẠI BẢN TIN</b> trực tiếp tại <c>{StateDirectory}/skip-{loại}.json</c>, thay cho
/// cửa DEDUP sự việc (<c>recent/{loại}.json</c>).
///
/// <para><b>Vì sao cần lưu.</b> Trước đây cửa SKIP thuần RAM, nên khởi động lại là mất sạch mốc "xe nào vừa
/// qua camera nào". Hậu quả không phải lỗi thấy ngay mà là <b>số đếm bị phồng âm thầm</b>: thiết bị bắn lặp
/// một chiếc xe với <c>vendorEventId</c> khác nhau, khoá chống trùng của QuestDB
/// (<c>vendorEventId, detectTime</c>) coi đó là hai dòng thật, và số đếm — vốn được TÍNH LẠI từ bảng thô
/// — cộng thêm một lượt không có thật.</para>
///
/// <para><b>Vì sao tách theo LOẠI, không phải theo CAMERA.</b> Chi phí đáng lo ở đây là <b>ghi</b>, không
/// phải đọc: file chỉ đọc một lần lúc khởi động, còn ghi thì lặp lại mỗi
/// <c>StateStore:FlushIntervalSeconds</c> suốt thời gian chạy.
/// <list type="bullet">
/// <item><b>Theo loại</b> khoanh được đúng chỗ phình. Cửa sổ mỗi loại một khác (đang seed 5–60 giây), và
/// nếu ai đó khai một loại với cửa sổ rất dài thì chỉ file của loại đó lớn, các loại còn lại vẫn nhỏ.</item>
/// <item><b>Theo camera</b> sẽ tạo ~100 file mà gần như file nào cũng có bản tin mới trong mỗi nhịp ⇒ mỗi
/// nhịp phải mở và đổi tên nguyên tử 100 lần, trong khi tổng số byte không giảm. Đắt hơn, không lợi hơn.</item>
/// </list>
/// Tách theo loại còn cho <b>cách ly lỗi</b>: một file hỏng chỉ làm mất mốc của loại đó, giống <c>recent/</c>.</para>
///
/// <para><b>Việc lưu file KHÔNG nhằm giảm RAM</b> — nói rõ để sau này không ai sửa sai mục đích. Bộ nhớ của
/// cửa SKIP đã bị chặn bởi lượt dọn theo nhịp bên trong cửa và bởi trần
/// <c>Ingestion:VehicleDedupMaxKeys</c>. Ghi ra đĩa còn tốn thêm một bản sao lúc ghi. Cái file mang lại là
/// <b>độ bền</b>.</para>
///
/// <para><b>Ghi theo nhịp, không ghi mỗi bản tin.</b> Mỗi lượt xe đều đụng vào trạng thái này; ghi đĩa từng
/// lượt sẽ thành điểm nghẽn ngay trên đường nhận (queue chạy tuần tự). Đánh đổi có chủ đích: mất tối đa một
/// nhịp nếu tiến trình bị kill.</para>
///
/// <para>Chỉ đúng cho MỘT tiến trình, giống <c>EventDedupStore</c>: chạy nhiều bản sao thì mỗi bản có cửa
/// riêng và ghi lên cùng thư mục, phải chuyển sang bộ lưu trữ dùng chung.</para>
/// Created date: 09/09/2026
/// </summary>
public sealed class VehicleDedupStateStore : IHostedService, IDisposable
{
    /// <summary>Tiền tố riêng để không đọc hoặc xóa các file data khác.</summary>
    private const string FilePrefix = "skip-";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IVdsConfigManager _config;
    private readonly VehicleDedupGate _gate;
    private readonly ILogger<VehicleDedupStateStore> _logger;
    private readonly string _root;
    private readonly Timer _flushTimer;
    private readonly object _flushLock = new();

    private bool _disposed;

    public VehicleDedupStateStore(
        IVdsConfigManager config,
        VehicleDedupGate gate,
        ILogger<VehicleDedupStateStore> logger)
    {
        _config = config;
        _gate = gate;
        _logger = logger;
        _root = StatePath.Resolve(config.Current.StateDirectory);
        MigrateLegacyFiles();

        // Nạp trong CONSTRUCTOR, không đợi StartAsync: cửa phải có mốc trước bản tin đầu tiên, và thứ tự
        // giữa các hosted service không phải là thứ đáng đem ra bảo đảm điều đó.
        Load();

        var interval = TimeSpan.FromSeconds(config.Current.StateStore.EffectiveFlushIntervalSeconds);
        _flushTimer = new Timer(_ => FlushSafe(), null, interval, interval);
    }

    /// <summary>Chỉ để container dựng sớm đối tượng này; việc nạp đã làm trong constructor.</summary>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Ghi nhịp cuối khi dừng để không mất mốc của quãng vừa chạy.</summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        FlushSafe();
        return Task.CompletedTask;
    }

    /// <summary>Ghi ngay ra đĩa, mỗi loại một file.</summary>
    public void Flush()
    {
        lock (_flushLock)
        {
            if (_disposed) return;
            FlushCore();
        }
    }

    private void FlushCore()
    {
        Directory.CreateDirectory(_root);

        var byType = _gate.Export()
            .GroupBy(x => SafeName(x.EventType), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var (type, entries) in byType)
        {
            try
            {
                var payload = entries
                    .OrderBy(x => x.At)
                    .Select(x => new VehicleSkipFileEntry
                    {
                        Key = x.Key,
                        AtUtc = EnsureUtc(x.At),
                        VendorEventId = x.VendorEventId,
                        WindowSeconds = x.WindowSeconds,
                    })
                    .ToList();

                StatePath.WriteAtomic(
                    Path.Combine(_root, FilePrefix + type + ".json"),
                    JsonSerializer.Serialize(payload, JsonOptions));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // try/catch THEO TỪNG FILE: một loại ghi lỗi không được kéo theo các loại còn lại.
                // Không ném: mất một lượt ghi chỉ làm cửa lùi về nhịp trước, còn ném ra từ timer sẽ hạ tiến trình.
                _logger.LogWarning(ex, "[State] Không ghi được {Folder}/{Type}.json.", _root, type);
            }
        }

        RemoveStaleFiles(byType.Keys);
    }

    /// <summary>
    /// Bỏ file của loại không còn mục nào. Cần thiết vì nếu để lại, lần khởi động sau sẽ nạp một mốc đã
    /// hết ý nghĩa từ file cũ.
    /// </summary>
    private void RemoveStaleFiles(ICollection<string> liveTypes)
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(_root, FilePrefix + "*.json"))
            {
                if (liveTypes.Contains(Path.GetFileNameWithoutExtension(path)[FilePrefix.Length..])) continue;
                try { File.Delete(path); } catch (IOException) { /* nhịp sau xoá lại */ }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "[State] Không dọn được file cũ trong {Folder}.", _root);
        }
    }

    private void MigrateLegacyFiles()
    {
        // Migrate the previous flat layout before the older subdirectory layout.
        if (Directory.Exists(_root))
            foreach (var path in Directory.EnumerateFiles(_root, "vehicle-skip-*.json"))
                MigrateFile(path, Path.Combine(_root,
                    FilePrefix + Path.GetFileName(path)["vehicle-skip-".Length..]));

        var legacy = Path.Combine(_root, "vehicle-skip");
        if (!Directory.Exists(legacy)) return;
        foreach (var path in Directory.EnumerateFiles(legacy, "*.json"))
        {
            var destination = Path.Combine(_root, FilePrefix + Path.GetFileName(path));
            // New layout is authoritative once present. Archive the old file so stale
            // entries cannot reappear after a later cleanup of the new cache.
            MigrateFile(path, destination);
        }
        if (!Directory.EnumerateFileSystemEntries(legacy).Any()) Directory.Delete(legacy);
    }

    private static void MigrateFile(string source, string destination)
    {
        if (File.Exists(destination))
            File.Move(source, source + ".migrated", overwrite: true);
        else
            File.Move(source, destination);
    }
    private void Load()
    {
        if (!Directory.Exists(_root)) return;

        // Chặn trên hào phóng, KHÔNG phải cửa sổ chặn thật: cửa sổ là của từng loại và đọc từ DB, ở đây chưa
        // biết. Mục quá hạn cũng vô hại — TryEnter so hiệu số với cửa sổ nên chúng không chặn ai — và lượt
        // dọn theo nhịp đầu tiên sẽ bỏ chúng. Cắt ở đây chỉ để không nạp một file cũ rất lớn.
        var cutoff = DateTime.UtcNow.AddHours(-_config.Current.StateStore.EffectiveRecentRetentionHours);
        var loaded = 0;

        foreach (var path in Directory.EnumerateFiles(_root, FilePrefix + "*.json"))
        {
            var type = Path.GetFileNameWithoutExtension(path)[FilePrefix.Length..];
            try
            {
                // try/catch THEO TỪNG FILE: một file hỏng không được kéo theo các loại còn lại.
                var entries = JsonSerializer.Deserialize<List<VehicleSkipFileEntry>>(
                    File.ReadAllText(path), JsonOptions);
                if (entries is null || entries.Count == 0) continue;

                loaded += _gate.Import(
                    entries
                        .Where(e => e is not null && !string.IsNullOrEmpty(e.Key)
                            && (e.AtUtc >= cutoff || (e.WindowSeconds is > 0 and <= 2592000 && e.AtUtc.AddSeconds(e.WindowSeconds.Value) > DateTime.UtcNow)))
                        .Select(e => new VehicleDedupSnapshot(
                            e.Key, type, EnsureUtc(e.AtUtc), e.VendorEventId, e.WindowSeconds)),
                    cutoffUtc: null);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogWarning(ex,
                    "[State] {Folder}/{Type}.json hỏng; cửa SKIP của loại này bắt đầu lại rỗng. " +
                    "Hệ quả: mỗi khoá của loại này có thể để lọt một lượt trùng trong cửa sổ đầu tiên.",
                    _root, type);
            }
        }

        if (loaded > 0)
            _logger.LogInformation("[State] Nạp {Count} mốc cửa SKIP từ {Folder}/.", loaded, _root);
    }

    private void FlushSafe()
    {
        try
        {
            Flush();
        }
        catch (Exception ex)
        {
            // Timer callback: ném ra ngoài sẽ hạ tiến trình.
            _logger.LogError(ex, "[State] Lỗi khi ghi trạng thái cửa SKIP theo nhịp.");
        }
    }

    /// <summary>Tên file an toàn từ loại bản tin (bỏ ký tự không hợp lệ, khoảng trắng thành gạch dưới).</summary>
    private static string SafeName(string? eventType)
    {
        var value = string.IsNullOrWhiteSpace(eventType) ? "unknown" : eventType.Trim();
        var cleaned = string.Concat(value.Split(Path.GetInvalidFileNameChars()));
        return cleaned.Length == 0 ? "unknown" : cleaned.Replace(' ', '_');
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    public void Dispose()
    {
        lock (_flushLock)
        {
            if (_disposed) return;
            _flushTimer.Dispose();
            FlushSafe();
            _disposed = true;
        }
    }
}
