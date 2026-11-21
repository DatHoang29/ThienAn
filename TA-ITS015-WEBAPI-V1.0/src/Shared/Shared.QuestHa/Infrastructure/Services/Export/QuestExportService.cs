using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.QuestHa.Infrastructure.Services.Export.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Export;

/// <summary>
/// Author: KienLT
/// Description: Backup xuất Parquet theo ngày giờ server. Một lượt: chọn node phụ Ready → chốt head journal → chứng nhận coverage node
/// (cơ chế chung với đường đọc) → mỗi bảng 24 lượt giờ: đếm PG, <c>/exp</c> stream ra file tạm, đọc số dòng trong metadata Parquet phải
/// bằng số đếm (lệch ⇒ giờ hỏng), SHA-256, đổi tên → <c>schema.sql</c> → kiểm lại toàn bộ bộ file → manifest SAU CÙNG → catalog Verified
/// (WatermarkSeq = head đã chốt, ExportId chung manifest ↔ catalog).
/// Mỗi lần công bố là một PHIÊN BẢN mới <c>{ngày}/vNNNN</c>; file của phiên bản đã công bố không bao giờ bị sửa. Ngày đã có bản ⇒ kiểm lại:
/// mọi giờ được kiểm SHA-256/kích thước/số dòng thật; giờ lệch số đếm, có dòng journal sau watermark (đến muộn) hoặc file hỏng được xuất lại
/// vào phiên bản mới (giờ còn nguyên được chép sang), công bố xong mới chuyển catalog (bản cũ retire) và dọn phiên bản cũ. Lỗi giữa chừng ⇒
/// phiên bản cũ và catalog còn nguyên; phiên bản nháp chỉ được lượt sau dùng tiếp hoặc dọn. Manifest mới hơn catalog (lỗi giữa ghi manifest
/// và catalog) ⇒ lượt sau kiểm SHA rồi ghi catalog theo manifest.
/// Nạp ngược: kiểm SHA-256 theo manifest trước, file phải nằm trong copy root của node (chép vào thư mục phiên riêng nếu cần),
/// <c>INSERT … read_parquet</c> từng giờ, WAL phải áp xong.
/// Created date: 02/11/2026
/// </summary>
internal sealed class QuestExportService
{
    /// <summary>Byte/dòng ước lượng cho xem trước (lab: Data ~38 B/dòng = 332 MB/8,64 triệu; Statistic ~49 B/dòng), làm tròn lên.</summary>
    internal const long EstimatedBytesPerRow = 50;

    /// <summary>Thư mục con của copy root chứa các phiên chép tạm khi nạp ngược.</summary>
    internal const string ImportFolder = "questha-import";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly IJournalStore _store;
    private readonly QuestHaOptions _options;
    private readonly IReadOnlyList<QuestTableSpec> _tables;
    private readonly IReadOnlyList<QuestExportNodeService> _nodes;
    private readonly QuestNodeCoverageService _coverage;
    private readonly Func<QuestHaOptions> _runtime;
    private readonly Func<string?> _preferredNode;
    private readonly Func<Func<CancellationToken, Task>, CancellationToken, Task<bool>>? _exclusive;
    private readonly TimeProvider _clock;
    private readonly TimeZoneInfo _zone;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;

    /// <summary>
    /// <paramref name="runtime"/> = snapshot vận hành (lịch/giữ/ngưỡng); <paramref name="preferredNode"/> = node router đang đọc (null ⇒ theo
    /// Priority); <paramref name="exclusive"/> chạy lượt ghi (xuất VÀ retention) dưới khoá liên tiến trình, trả false khi khoá đang bị giữ
    /// (Worker và CLI không ghi trùng); <paramref name="zone"/> = giờ server (mặc định <see cref="TimeZoneInfo.Local"/>).
    /// </summary>
    public QuestExportService(IJournalStore store, QuestHaOptions options, IReadOnlyList<QuestTableSpec> tables,
        IReadOnlyList<QuestExportNodeService> nodes, QuestNodeCoverageService coverage, Func<QuestHaOptions>? runtime = null,
        Func<string?>? preferredNode = null,
        Func<Func<CancellationToken, Task>, CancellationToken, Task<bool>>? exclusive = null,
        TimeProvider? clock = null, TimeZoneInfo? zone = null, QuestHaTimelineService? timeline = null, ILogger? logger = null)
    {
        _store = store;
        _options = options;
        _tables = tables;
        _nodes = nodes;
        _coverage = coverage;
        _runtime = runtime ?? (() => options);
        _preferredNode = preferredNode ?? (() => null);
        _exclusive = exclusive;
        _clock = clock ?? TimeProvider.System;
        _zone = zone ?? TimeZoneInfo.Local;
        _timeline = timeline;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Múi giờ server dùng quy đổi ngày.</summary>
    public TimeZoneInfo Zone => _zone;

    /// <summary>
    /// Xuất (hoặc kiểm lại) ngày <paramref name="dayLocal"/> cho mọi bảng đã đăng ký. <paramref name="apply"/> = false chỉ xem trước.
    /// Không ném khi hoãn (không node Ready, chưa phủ, thiếu đĩa, ngày chưa kết thúc, khoá đang bị giữ): trả
    /// <see cref="QuestExportOutput.DeferredReason"/>. Lỗi một bảng ghi vào <see cref="QuestExportTableOutput.Error"/>, bảng khác vẫn chạy.
    /// Lỗi journal SQL ném ra.
    /// </summary>
    public async Task<QuestExportOutput> ExportAsync(DateOnly dayLocal, bool apply, CancellationToken ct)
    {
        if (!apply || _exclusive is null) return await RunAsync(dayLocal, apply, ct).ConfigureAwait(false);
        QuestExportOutput? output = null;
        // Khoá đang bị giữ (lượt khác của Worker/CLI đang xuất hoặc dọn) ⇒ hoãn, không chạy song song.
        var ran = await _exclusive(async token => output = await RunAsync(dayLocal, true, token).ConfigureAwait(false), ct).ConfigureAwait(false);
        return ran && output is not null ? output : Busy(dayLocal);
    }

    private QuestExportOutput Busy(DateOnly day)
    {
        var (from, to) = QuestExportDayRules.DayRange(day, _zone);
        return new(day, true, _zone.Id, from, to, null, null, 0, null, Array.Empty<QuestExportTableOutput>(), "ExportAlreadyRunning");
    }

    /// <summary>Catalog bản xuất (lọc bảng/khoảng ngày); <c>Path</c> là thư mục phiên bản của từng dòng.</summary>
    public Task<IReadOnlyList<QuestExportRecordOutput>> ListAsync(string? table, DateOnly? from, DateOnly? to, CancellationToken ct) =>
        _store.ListExportsAsync(table, from, to, ct);

    private async Task<QuestExportOutput> RunAsync(DateOnly day, bool apply, CancellationToken ct)
    {
        var runtime = _runtime();
        var root = _options.Export.ExportRoot;
        if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root))
            throw new InvalidOperationException("QuestHa:Export:ExportRoot chưa khai (đường dẫn tuyệt đối).");
        var (from, to) = QuestExportDayRules.DayRange(day, _zone);
        var hours = QuestExportDayRules.Hours(from, to);
        var now = _clock.GetUtcNow().UtcDateTime;
        QuestExportOutput Result(string? node, long? head, long estimate, long? free, IReadOnlyList<QuestExportTableOutput> tables, string? reason) =>
            new(day, apply, _zone.Id + " (UTC" + QuestExportDayRules.Offset(from, _zone) + ")", from, to, node, head, estimate, free, tables, reason);

        // Chỉ xuất ngày đã kết thúc theo giờ server: ngày đang chạy còn nhận dữ liệu.
        if (to > now) return Result(null, null, 0, null, Array.Empty<QuestExportTableOutput>(), "DayNotFinished");
        var statuses = await _store.ReadStatusesAsync(ct).ConfigureAwait(false);
        var exportable = _nodes.Select(n => n.Options).ToList();
        var nodeName = QuestExportNodeRules.Choose(exportable, statuses, _preferredNode(), now, runtime.Health.HeartbeatStaleAfter);
        if (nodeName is null) return Result(null, null, 0, null, Array.Empty<QuestExportTableOutput>(), "NoReadyNode");
        var node = _nodes.Single(n => n.Node.Equals(nodeName, StringComparison.OrdinalIgnoreCase));

        // Chốt head MỘT lần; node phải phủ head (cursor, fence, phục hồi, quarantine, WAL) như đường đọc có coverage.
        var head = (await _store.GetLagAsync(0, ct).ConfigureAwait(false)).MaxSeq;
        try
        {
            await _coverage.WaitCoveredAsync(node.Node, head, _options.Export.CoverageWait,
                (n, _, token) => _store.ReadCoverageAsync(n, token), ct).ConfigureAwait(false);
        }
        catch (QuestNodeNotCoveredException)
        {
            return Result(node.Node, head, 0, null, Array.Empty<QuestExportTableOutput>(), "NotCovered");
        }

        var plans = new List<(QuestTableSpec Spec, string Folder, long[] Counts)>();
        foreach (var spec in _tables)
        {
            var counts = new long[hours.Count];
            for (var i = 0; i < hours.Count; i++)
                counts[i] = await node.CountAsync(spec, hours[i].FromUtc, hours[i].ToUtc, ct).ConfigureAwait(false);
            plans.Add((spec, QuestExportDayRules.Folder(root, spec.Name, day), counts));
        }
        var estimate = plans.Sum(p => p.Counts.Sum()) * EstimatedBytesPerRow;
        var free = QuestExportStorageService.FreeBytes(root);
        var preview = plans.Select(p => new QuestExportTableOutput(p.Spec.Name, p.Folder, p.Counts.Sum(), p.Counts.Sum() * EstimatedBytesPerRow,
            false, null, hours.Select(h => new QuestExportHourOutput(h.Hour, h.FromUtc, h.ToUtc, p.Counts[h.Hour],
                p.Counts[h.Hour] * EstimatedBytesPerRow, null, "Preview")).ToList())).ToList();
        // Trống phải ≥ ước lượng × 2 (+ mức trống tối thiểu) — ước lượng theo số đo lab, dữ liệu thật có thể nén kém hơn.
        if (free is { } f && f < estimate * 2 + _options.Export.MinimumFreeBytes)
            return Result(node.Node, head, estimate, free, preview, "InsufficientDisk");
        if (!apply) return Result(node.Node, head, estimate, free, preview, null);

        var catalog = await _store.ListExportsAsync(null, day, day, ct).ConfigureAwait(false);
        var results = new List<QuestExportTableOutput>();
        foreach (var (spec, folder, counts) in plans)
        {
            try
            {
                results.Add(await ExportTableAsync(spec, folder, root, day, from, to, hours, counts, node, head,
                    catalog.Where(c => c.TableName.Equals(spec.Name, StringComparison.OrdinalIgnoreCase)).ToList(), ct).ConfigureAwait(false));
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogSafeWarning(ex, "[QuestHa.Export] Xuất {Table} ngày {Day:yyyy-MM-dd} trên node {Node} lỗi.", spec.Name, day, node.Node);
                _timeline?.Publish("Export", node.Node, QuestHaEventKindEnum.Error,
                    $"Xuất {spec.Name} {day:yyyy-MM-dd} lỗi: {ex.GetType().Name}");
                results.Add(new QuestExportTableOutput(spec.Name, folder, 0, 0, false, null, Array.Empty<QuestExportHourOutput>(),
                    ex.GetType().Name + ": " + QuestSafeLoggingHelper.Redact(ex.Message)));
            }
        }
        return Result(node.Node, head, estimate, free, results, results.Any(r => !r.Verified) ? "TableFailed" : null);
    }

    private async Task<QuestExportTableOutput> ExportTableAsync(QuestTableSpec spec, string dayFolder, string root, DateOnly day, DateTime from,
        DateTime to, IReadOnlyList<(int Hour, DateTime FromUtc, DateTime ToUtc)> hours, long[] counts, QuestExportNodeService node, long head,
        IReadOnlyList<QuestExportRecordOutput> catalog, CancellationToken ct)
    {
        QuestExportStorageService.EnsureDirectoryUnder(root, dayFolder);
        var versions = QuestExportStorageService.Versions(dayFolder);
        var published = versions.LastOrDefault(v => v.Published);
        var publishedFolder = published.Folder;
        var manifest = publishedFolder is null ? null : QuestExportStorageService.ReadManifest(publishedFolder);
        if (manifest is not null && (!manifest.Table.Equals(spec.Name, StringComparison.OrdinalIgnoreCase) || manifest.FromUtc != from || manifest.ToUtc != to))
            throw new InvalidDataException($"manifest.json của {publishedFolder} không khớp bảng/khoảng ngày (đổi múi giờ server?).");
        var live = catalog.FirstOrDefault(c => c.Verified && c.RetiredUtc is null);
        // Bản nháp = phiên bản chưa công bố mới hơn bản đã công bố (lần thử dở trước); dùng lại các giờ đã xong của nó.
        var draftFolder = versions.LastOrDefault(v => !v.Published && v.Version > published.Version).Folder;
        if (draftFolder is not null) QuestExportStorageService.CleanTemp(draftFolder);
        var fromDraft = new Dictionary<int, QuestExportManifestHourOutput>();
        foreach (var h in draftFolder is null ? new List<QuestExportManifestHourOutput>() : ReadProgress(draftFolder)) fromDraft[h.Hour] = h;
        var fromManifest = new Dictionary<int, QuestExportManifestHourOutput>();
        foreach (var h in manifest?.Hours ?? Array.Empty<QuestExportManifestHourOutput>())
            fromManifest[h.Hour] = h with { WatermarkSeq = h.WatermarkSeq ?? manifest!.WatermarkSeq };
        // Dòng đến muộn xét theo watermark của CHÍNH bản ứng viên (đã công bố hoặc nháp): giờ nháp đã xuất ở head mới không bị coi là
        // "muộn" chỉ vì bản đã công bố có watermark cũ hơn. Mỗi watermark khác nhau truy vấn journal một lần.
        var lateBySince = new Dictionary<long, IReadOnlyList<int>>();
        async Task<bool> IsLateAsync(int hour, long? watermark)
        {
            var since = watermark ?? 0;
            if (since >= head) return false;
            if (!lateBySince.TryGetValue(since, out var set))
                lateBySince[since] = set = await _store.ReadExportLateHoursAsync(spec.Name, from, to, since, head, ct).ConfigureAwait(false);
            return set.Contains(hour);
        }

        // Giờ của bản đã công bố còn dùng được: số đếm khớp, không có dòng đến muộn, file đúng tên/khoảng giờ/kích thước/số dòng/SHA-256 THẬT.
        var manifestOk = new bool[hours.Count];
        foreach (var (hour, hourFrom, hourTo) in hours)
        {
            ct.ThrowIfCancellationRequested();
            manifestOk[hour] = fromManifest.TryGetValue(hour, out var m) && m.Rows == counts[hour]
                && !await IsLateAsync(hour, m.WatermarkSeq).ConfigureAwait(false)
                && await IntactAsync(publishedFolder!, m, hour, hourFrom, hourTo, ct).ConfigureAwait(false);
        }
        if (manifest is not null && manifestOk.All(ok => ok))
        {
            var kept = await KeepPublishedAsync(spec, dayFolder, publishedFolder!, manifest, live, catalog, hours, fromManifest, ct).ConfigureAwait(false);
            if (kept is not null) return kept;
        }

        // Cần phiên bản mới: không đụng file của bản đã công bố (chỉ đọc để chép giờ còn nguyên).
        var draft = draftFolder ?? QuestExportStorageService.EnsureDirectoryUnder(root,
            QuestExportDayRules.VersionFolder(dayFolder, (versions.Count == 0 ? 0 : versions.Max(v => v.Version)) + 1), mustBeNew: true);
        var plan = new (QuestExportManifestHourOutput? Entry, bool FromDraft)[hours.Count];
        foreach (var (hour, hourFrom, hourTo) in hours)
        {
            ct.ThrowIfCancellationRequested();
            if (fromDraft.TryGetValue(hour, out var d) && d.Rows == counts[hour]
                && !await IsLateAsync(hour, d.WatermarkSeq).ConfigureAwait(false)
                && await IntactAsync(draft, d, hour, hourFrom, hourTo, ct).ConfigureAwait(false))
                plan[hour] = (d, true);
            else if (manifestOk[hour])
                plan[hour] = (fromManifest[hour], false);
        }

        var progress = new SortedDictionary<int, QuestExportManifestHourOutput>();
        foreach (var (entry, isDraft) in plan)
            if (entry is not null && isDraft) progress[entry.Hour] = entry;
        var entries = new List<QuestExportManifestHourOutput>();
        var outputs = new List<QuestExportHourOutput>();
        var redone = 0;
        foreach (var (hour, hourFrom, hourTo) in hours)
        {
            ct.ThrowIfCancellationRequested();
            var count = counts[hour];
            var path = Path.Combine(draft, QuestExportDayRules.HourFile(hour));
            var (reuse, isDraft) = plan[hour];
            QuestExportManifestHourOutput entry;
            if (reuse is not null)
            {
                if (!isDraft)
                {
                    // Chép giờ còn nguyên từ bản đã công bố sang bản nháp (bộ file của mỗi phiên bản tự đủ).
                    if (reuse.File is not null) File.Copy(Path.Combine(publishedFolder!, reuse.File), path, overwrite: true);
                    else if (File.Exists(path)) File.Delete(path);
                }
                entry = reuse with { WatermarkSeq = head };
                entries.Add(entry);
                outputs.Add(new(hour, hourFrom, hourTo, entry.Rows, entry.Bytes, entry.Sha256, "Kept"));
                if (isDraft) continue;
            }
            else
            {
                redone++;
                if (count == 0)
                {
                    // Giờ rỗng không gọi /exp; xoá file cũ trong bản nháp (nếu có) để thư mục khớp manifest.
                    if (File.Exists(path)) File.Delete(path);
                    entry = new() { Hour = hour, FromUtc = hourFrom, ToUtc = hourTo, File = null, Rows = 0, Bytes = 0, ExportedUtc = Now, WatermarkSeq = head };
                }
                else
                {
                    var temp = path + QuestExportDayRules.TempSuffix;
                    var bytes = await node.ExportAsync(spec, hourFrom, hourTo, temp, ct).ConfigureAwait(false);
                    long rows;
                    try { rows = QuestHaBaseHelper.ParquetHp.ReadRowCount(temp); }
                    catch { File.Delete(temp); throw; }
                    if (rows != count)
                    {
                        File.Delete(temp);
                        throw new InvalidDataException($"Giờ {hour:00}: file có {rows} dòng ≠ đếm PG {count} (dữ liệu đổi trong lúc xuất?).");
                    }
                    var sha = await QuestHaBaseHelper.ParquetHp.Sha256Async(temp, ct).ConfigureAwait(false);
                    File.Move(temp, path, overwrite: true);
                    entry = new() { Hour = hour, FromUtc = hourFrom, ToUtc = hourTo, File = Path.GetFileName(path), Rows = rows, Bytes = bytes,
                        Sha256 = sha, ExportedUtc = Now, WatermarkSeq = head };
                }
                entries.Add(entry);
                outputs.Add(new(hour, hourFrom, hourTo, entry.Rows, entry.Bytes, entry.Sha256, "Exported"));
            }
            // Ghi tiến độ của bản nháp sau mỗi giờ: lần thử sau dùng lại giờ đã xong.
            progress[hour] = entry;
            await QuestExportStorageService.WriteAtomicAsync(Path.Combine(draft, QuestExportDayRules.ProgressFile),
                JsonSerializer.SerializeToUtf8Bytes(progress.Values.ToList(), Json), ct).ConfigureAwait(false);
        }

        var ddl = await node.ShowCreateTableAsync(spec, ct).ConfigureAwait(false);
        await QuestExportStorageService.WriteAtomicAsync(Path.Combine(draft, QuestExportDayRules.SchemaFile),
            Encoding.UTF8.GetBytes(ddl.TrimEnd() + Environment.NewLine), ct).ConfigureAwait(false);
        var next = new QuestExportManifestOutput
        {
            ExportId = Guid.NewGuid(),
            Table = spec.Name,
            DesignatedColumn = spec.Designated.Name,
            DayLocal = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeZone = _zone.Id,
            UtcOffset = QuestExportDayRules.Offset(from, _zone),
            FromUtc = from,
            ToUtc = to,
            Node = node.Node,
            WatermarkSeq = head,
            Rows = entries.Sum(e => e.Rows),
            Bytes = entries.Sum(e => e.Bytes),
            CreatedUtc = Now,
            Hours = entries,
        };
        // Kiểm lại TOÀN BỘ bộ file của phiên bản trước khi công bố (cả giờ chép sang và giờ của lần thử trước).
        await VerifyFilesAsync(draft, next, ct).ConfigureAwait(false);
        await QuestExportStorageService.WriteManifestAsync(draft, next, ct).ConfigureAwait(false);
        File.Delete(Path.Combine(draft, QuestExportDayRules.ProgressFile));
        var record = Record(next, next.ExportId!.Value, draft);
        // Thêm dòng mới và retire dòng cũ của cùng bảng/ngày trong một transaction. Lỗi ở đây ⇒ lượt sau thấy manifest mới hơn catalog và ghi bù.
        await _store.AddExportAsync(record, ct).ConfigureAwait(false);
        RemoveOtherVersions(dayFolder, draft);
        if (manifest is not null)
            _timeline?.Publish("Export", node.Node, QuestHaEventKindEnum.Info,
                $"Xuất lại {redone} giờ của {spec.Name} {day:yyyy-MM-dd} vào phiên bản mới (dữ liệu đến muộn/lệch số đếm/file hỏng).");
        return new(spec.Name, draft, next.Rows, next.Bytes, true, record.ExportId, outputs);
    }

    // Bản đã công bố còn nguyên và khớp: không ghi file. Catalog lệch manifest (thiếu hoặc còn trỏ bản cũ) ⇒ ghi catalog theo manifest.
    // Trả null khi bản này không công nhận lại được (manifest thiếu ExportId hoặc ExportId đã bị retire) ⇒ người gọi công bố phiên bản mới.
    private async Task<QuestExportTableOutput?> KeepPublishedAsync(QuestTableSpec spec, string dayFolder, string publishedFolder,
        QuestExportManifestOutput manifest, QuestExportRecordOutput? live, IReadOnlyList<QuestExportRecordOutput> catalog,
        IReadOnlyList<(int Hour, DateTime FromUtc, DateTime ToUtc)> hours, IReadOnlyDictionary<int, QuestExportManifestHourOutput> fromManifest,
        CancellationToken ct)
    {
        if (manifest.ExportId is not { } id || catalog.Any(c => c.ExportId == id && c.RetiredUtc is not null)) return null;
        if (live?.ExportId != id)
        {
            await _store.AddExportAsync(Record(manifest, id, publishedFolder), ct).ConfigureAwait(false);
            _timeline?.Publish("Export", manifest.Node, QuestHaEventKindEnum.Info,
                $"Ghi bù catalog {spec.Name} {manifest.DayLocal} theo manifest đã công bố (watermark {manifest.WatermarkSeq}).");
        }
        RemoveOtherVersions(dayFolder, publishedFolder);
        var outputs = hours.Select(h => fromManifest[h.Hour])
            .Select(e => new QuestExportHourOutput(e.Hour, e.FromUtc, e.ToUtc, e.Rows, e.Bytes, e.Sha256, "Kept")).ToList();
        return new(spec.Name, publishedFolder, manifest.Rows, manifest.Bytes, true, id, outputs);
    }

    private QuestExportRecordOutput Record(QuestExportManifestOutput m, Guid id, string folder) =>
        new(id, m.Table, DateOnly.ParseExact(m.DayLocal, "yyyy-MM-dd", CultureInfo.InvariantCulture), m.FromUtc, m.ToUtc, m.Node,
            m.WatermarkSeq, m.Rows, m.Bytes, folder, true, Now, null);

    // Sau khi catalog trỏ phiên bản giữ lại: xoá phiên bản cũ (đã retire) và bản nháp bỏ dở; chỉ thư mục đúng mẫu, không link.
    private void RemoveOtherVersions(string dayFolder, string keep)
    {
        try
        {
            foreach (var (_, folder, _) in QuestExportStorageService.Versions(dayFolder))
            {
                if (string.Equals(folder, keep, StringComparison.OrdinalIgnoreCase)) continue;
                if (!QuestExportStorageService.DeleteVersion(folder, dayFolder))
                    _logger.LogWarning("[QuestHa.Export] Giữ lại phiên bản cũ {Folder}: có mục không do bản xuất tạo hoặc link.", folder);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogSafeWarning(ex, "[QuestHa.Export] Không dọn được phiên bản cũ trong {Folder}.", dayFolder);
        }
    }

    /// <summary>
    /// Xoá thư mục ngày cũ hơn RetentionDays (trừ ngày Verified mới nhất mỗi bảng), chạy dưới CÙNG khoá với lượt xuất (Worker/CLI): đọc lại
    /// catalog trong khoá, retire mọi dòng còn hiệu lực của ngày đó TRƯỚC, rồi mới xoá thư mục (chỉ trong ExportRoot, không qua link, chỉ mục
    /// do bản xuất tạo). Lỗi giữa hai bước chỉ để lại thư mục thừa (lượt sau dọn), không để dòng Verified trỏ thư mục đã xoá.
    /// <paramref name="apply"/> = false chỉ liệt kê. Khoá đang bị giữ ⇒ 0 (để lượt sau). Trả số thư mục (đã/sẽ) dọn.
    /// </summary>
    public async Task<int> RetainAsync(DateOnly todayLocal, bool apply, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Export.ExportRoot)) return 0;
        if (!apply || _exclusive is null) return await RetainCoreAsync(todayLocal, apply, ct).ConfigureAwait(false);
        var removed = 0;
        if (await _exclusive(async token => removed = await RetainCoreAsync(todayLocal, true, token).ConfigureAwait(false), ct).ConfigureAwait(false))
            return removed;
        _logger.LogInformation("[QuestHa.Export] Bỏ qua dọn bản xuất hết hạn: lượt xuất khác đang giữ khoá.");
        return 0;
    }

    private async Task<int> RetainCoreAsync(DateOnly todayLocal, bool apply, CancellationToken ct)
    {
        var root = _options.Export.ExportRoot;
        var expired = QuestExportRetentionRules.Expired(await _store.ListExportsAsync(null, null, null, ct).ConfigureAwait(false), todayLocal,
            _runtime().Export.RetentionDays);
        var count = 0;
        foreach (var (table, day, live) in expired)
        {
            ct.ThrowIfCancellationRequested();
            var folder = QuestExportDayRules.Folder(root, table, day);
            var exists = Directory.Exists(folder) || QuestExportStorageService.IsLink(folder);
            if (live.Count == 0 && !exists) continue;
            if (!apply) { count++; continue; }
            if (live.Count > 0) await _store.RetireExportsAsync(live.Select(r => r.ExportId).ToList(), ct).ConfigureAwait(false);
            if (exists && !QuestExportStorageService.DeleteDay(folder, root))
            {
                _logger.LogWarning("[QuestHa.Export] Đã retire catalog nhưng không xoá thư mục hết hạn {Table} {Day:yyyy-MM-dd}: ngoài ExportRoot, có link hoặc mục lạ.", table, day);
                continue;
            }
            count++;
        }
        return count;
    }

    /// <summary>
    /// Nạp ngược <paramref name="dayFolder"/> (thư mục phiên bản có manifest, hoặc thư mục ngày ⇒ phiên bản đã công bố mới nhất) vào node
    /// <paramref name="nodeName"/> (bảng theo manifest, hoặc <paramref name="intoTable"/> đã tạo sẵn cùng DDL). SHA-256 mọi file kiểm TRƯỚC;
    /// thư mục ngoài <c>ParquetCopyRoot</c> của node ⇒ chép vào thư mục phiên MỚI <c>{ParquetCopyRoot}/questha-import/{bảng}/{ngày}-{phiên}</c>
    /// (mọi cấp kiểm reparse point, kể cả thư mục có sẵn; file không ghi đè; kiểm lại SHA bản chép) rồi xoá phiên đó.
    /// WAL chưa áp xong ⇒ <see cref="QuestImportOutput.Error"/> = <c>WalNotApplied</c>.
    /// <paramref name="apply"/> = false chỉ kiểm và đếm. Lỗi kiểm ⇒ <see cref="InvalidDataException"/>; cây đích qua link ⇒
    /// <see cref="IOException"/>; tham số sai ⇒ <see cref="ArgumentException"/>.
    /// </summary>
    public async Task<QuestImportOutput> ImportAsync(string dayFolder, string nodeName, bool apply, string? intoTable, CancellationToken ct)
    {
        var folder = QuestExportStorageService.ResolvePublished(Path.GetFullPath(dayFolder))
            ?? throw new InvalidDataException("Thư mục không có manifest.json (bản dở hoặc không phải thư mục ngày/phiên bản xuất).");
        var manifest = QuestExportStorageService.ReadManifest(folder)
            ?? throw new InvalidDataException("Thư mục không có manifest.json.");
        var spec = _tables.FirstOrDefault(t => t.Name.Equals(manifest.Table, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Bảng {manifest.Table} của manifest không thuộc các bảng QuestHa đã đăng ký.", nameof(dayFolder));
        var target = intoTable ?? spec.Name;
        if (!QuestExportDayRules.IsSafeTableName(target)) throw new ArgumentException("Tên bảng nạp không hợp lệ.", nameof(intoTable));
        var node = _nodes.FirstOrDefault(n => n.Node.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Node không thuộc QuestHa:Nodes.", nameof(nodeName));
        var copyRoot = node.Options.ParquetCopyRoot;
        if (string.IsNullOrWhiteSpace(copyRoot) || !Path.IsPathFullyQualified(copyRoot))
            throw new ArgumentException($"Node {node.Node} chưa khai ParquetCopyRoot (= cairo.sql.copy.root của node).", nameof(nodeName));
        copyRoot = Path.GetFullPath(copyRoot);
        var day = DateOnly.ParseExact(manifest.DayLocal, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        await VerifyFilesAsync(folder, manifest, ct).ConfigureAwait(false);
        var files = manifest.Hours.Where(h => h.File is not null).ToList();
        var before = await node.CountAsync(target, manifest.DesignatedColumn, manifest.FromUtc, manifest.ToUtc, ct).ConfigureAwait(false);
        var inside = QuestExportRetentionRules.InsideRoot(folder, copyRoot);
        // Tên phiên dựng từ tên bảng đã đăng ký + ngày đã parse (không lấy chuỗi tuỳ ý của manifest vào đường dẫn).
        var source = inside ? folder : Path.Combine(copyRoot, ImportFolder, spec.Name,
            day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8]);
        if (!apply)
            return new(folder, target, node.Node, false, day, manifest.FromUtc, manifest.ToUtc, manifest.Rows, before, null, files.Count,
                inside ? null : source, null);

        var created = false;
        try
        {
            if (!inside)
            {
                // Kiểm cả cây đích TRƯỚC khi chép: copy root có sẵn, mọi cấp không là link, thư mục phiên phải mới.
                QuestExportStorageService.EnsureDirectoryUnder(copyRoot, source, mustBeNew: true, createRoot: false);
                created = true;
                foreach (var h in files)
                {
                    var copy = Path.Combine(source, h.File!);
                    File.Copy(Path.Combine(folder, h.File!), copy, overwrite: false);
                    if (QuestExportStorageService.IsLink(copy) ||
                        await QuestHaBaseHelper.ParquetHp.Sha256Async(copy, ct).ConfigureAwait(false) != h.Sha256)
                        throw new InvalidDataException($"Bản chép {h.File} vào copy root lệch SHA-256.");
                }
            }
            var relativeRoot = Path.GetRelativePath(copyRoot, source);
            var walSpec = target.Equals(spec.Name, StringComparison.OrdinalIgnoreCase) ? spec
                : new QuestTableSpec(target, spec.Columns, spec.VersionIndex >= 0 ? spec.Columns[spec.VersionIndex].Name : null, spec.SchemaVer);
            foreach (var h in files)
            {
                ct.ThrowIfCancellationRequested();
                // Từng giờ một: nạp nguyên ngày đỉnh làm QuestDB tốn tới 7,6 GB RAM khi DEDUP (số đo lab).
                await node.ImportAsync(target, Path.Combine(relativeRoot, h.File!), ct).ConfigureAwait(false);
            }
            // WAL chưa áp xong thì số dòng đủ không chứng minh được dữ liệu đã vào (upsert cùng khoá giữ nguyên số dòng) ⇒ báo lỗi.
            var walApplied = await node.Client.WaitWalAppliedAsync(walSpec, _options.Export.HttpTimeout, ct).ConfigureAwait(false);
            if (!walApplied)
                _logger.LogWarning("[QuestHa.Export] WAL bảng {Table} chưa apply xong sau khi nạp.", target);
            var after = await node.CountAsync(target, manifest.DesignatedColumn, manifest.FromUtc, manifest.ToUtc, ct).ConfigureAwait(false);
            return new(folder, target, node.Node, true, day, manifest.FromUtc, manifest.ToUtc, manifest.Rows, before, after, files.Count,
                inside ? null : source, !walApplied ? "WalNotApplied" : after < manifest.Rows ? "RowsBelowManifest" : null);
        }
        finally
        {
            if (created && !QuestExportStorageService.DeleteImportCopy(source, copyRoot))
                _logger.LogWarning("[QuestHa.Export] Không xoá được bản chép tạm {Folder} trong copy root.", source);
        }
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    // File của một giờ còn dùng lại được: đúng giờ/khoảng UTC, đúng tên, không link, đúng kích thước, số dòng metadata và SHA-256 thật.
    private static async Task<bool> IntactAsync(string folder, QuestExportManifestHourOutput entry, int hour, DateTime fromUtc, DateTime toUtc,
        CancellationToken ct)
    {
        if (entry.Hour != hour || entry.FromUtc != fromUtc || entry.ToUtc != toUtc) return false;
        if (entry.File is null) return entry.Rows == 0;
        if (!QuestExportDayRules.HourFile(hour).Equals(entry.File, StringComparison.Ordinal) || entry.Sha256 is null) return false;
        var path = Path.Combine(folder, entry.File);
        if (QuestExportStorageService.IsLink(path)) return false;
        var info = new FileInfo(path);
        if (!info.Exists || info.Length != entry.Bytes) return false;
        try
        {
            if (QuestHaBaseHelper.ParquetHp.ReadRowCount(path) != entry.Rows) return false;
        }
        catch (InvalidDataException) { return false; }
        return string.Equals(await QuestHaBaseHelper.ParquetHp.Sha256Async(path, ct).ConfigureAwait(false), entry.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    // Toàn bộ file theo manifest: đúng tên giờ, tồn tại, không link (cả thư mục cha), SHA-256 và số dòng metadata khớp.
    private static async Task VerifyFilesAsync(string folder, QuestExportManifestOutput manifest, CancellationToken ct)
    {
        QuestExportStorageService.RejectLinks(folder);
        foreach (var h in manifest.Hours.Where(h => h.File is not null))
        {
            var path = Path.Combine(folder, h.File!);
            if (!QuestExportDayRules.HourFile(h.Hour).Equals(h.File, StringComparison.Ordinal) || !File.Exists(path))
                throw new InvalidDataException($"Thiếu file {h.File} theo manifest.");
            if (QuestExportStorageService.IsLink(path)) throw new IOException($"File {h.File} là reparse point/symlink.");
            if (!string.Equals(await QuestHaBaseHelper.ParquetHp.Sha256Async(path, ct).ConfigureAwait(false), h.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"File {h.File} lệch SHA-256 so với manifest.");
            if (QuestHaBaseHelper.ParquetHp.ReadRowCount(path) != h.Rows)
                throw new InvalidDataException($"File {h.File} có số dòng khác manifest.");
        }
    }

    private static List<QuestExportManifestHourOutput> ReadProgress(string folder)
    {
        var path = Path.Combine(folder, QuestExportDayRules.ProgressFile);
        if (!File.Exists(path) || QuestExportStorageService.IsLink(path)) return new();
        try { return JsonSerializer.Deserialize<List<QuestExportManifestHourOutput>>(File.ReadAllText(path)) ?? new(); }
        catch (JsonException) { return new(); } // tiến độ hỏng ⇒ xuất lại các giờ, không mất an toàn
    }
}
