using System.Text.Json;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.State;
using Services.Vds.Configuration;

namespace Services.Vds.Features.VehicleFilter;

/// <summary>
/// Author: KienLT
/// Description: Danh sách đen/trắng biển số trong RAM, keyed theo BIỂN SỐ ĐÃ CHUẨN HOÁ — tra cache chứ
/// không truy vấn DB mỗi bản tin.
///
/// <para><b>Thứ tự lấy dữ liệu</b> — cùng mô hình với bộ nạp ngưỡng tốc độ:
/// <list type="number">
/// <item><b>DB</b>. Thành công ⇒ ghi file cache làm bản last-known-good.</item>
/// <item><b>File cache</b> khi DB lỗi Ở LẦN NẠP ĐẦU — service vẫn cảnh báo theo danh sách của lần trước.</item>
/// <item><b>Không có cả hai</b> ⇒ danh sách RỖNG kèm log Error: không khớp biển nào nhưng pipeline vẫn
/// chạy. Fail-open ở đây là ĐÚNG: mất một cảnh báo không được phép làm mất bản ghi lượt xe.</item>
/// </list></para>
///
/// <para>
/// <b>DB trả về 0 dòng là trạng thái HỢP LỆ</b> (chưa ai khai biển nào) nên KHÔNG lùi về file cache — khác
/// bộ nạp ngưỡng tốc độ, ở đó thiếu ngưỡng là hỏng tính năng. Lùi về cache ở đây sẽ làm biển vừa bị xoá trên
/// web SỐNG LẠI.
/// </para>
/// Created date: 26/08/2026
/// </summary>
public sealed class VehicleFilterManager
{
    /// <summary>Tên file cache trong thư mục state.</summary>
    public const string CacheFileName = "vehicle-filter.json";

    private static readonly VehicleFilterEntry[] NoEntries = Array.Empty<VehicleFilterEntry>();

    private readonly VehicleFilterRepository _repo;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VehicleFilterManager> _logger;
    private readonly string _cachePath;

    /// <summary>
    /// Snapshot bất biến, đổi bằng <see cref="Interlocked.Exchange{T}(ref T, T)"/> ⇒ luồng đọc không bị
    /// chặn khi nạp lại. So khớp biển dùng comparer PHÂN BIỆT hoa thường vì khoá đã in hoa hết ở bước chuẩn
    /// hoá — dùng comparer bỏ qua hoa thường chỉ che mất lỗi chuẩn hoá thiếu.
    /// </summary>
    private Dictionary<string, VehicleFilterEntry[]> _byPlate = new(StringComparer.Ordinal);

    public VehicleFilterManager(
        VehicleFilterRepository repo, IVdsConfigManager config, ILogger<VehicleFilterManager> logger)
    {
        _repo = repo;
        _config = config;
        _logger = logger;
        _cachePath = StatePath.Resolve(config.Current.StateDirectory, CacheFileName);
    }

    /// <summary>Số BIỂN đang giữ trong RAM (một biển có thể có nhiều dòng danh sách).</summary>
    public int Count => Volatile.Read(ref _byPlate).Count;

    /// <summary>
    /// Đối chiếu một biển số với danh sách. Trả dòng khớp CÒN HIỆU LỰC tại thời điểm xe đi qua.
    ///
    /// Nhiều dòng cùng một biển thì ƯU TIÊN dòng BẬT cảnh báo: người vận hành có thể khai trùng biển với
    /// một dòng không cảnh báo, không được vì dòng đó mà im lặng.
    /// </summary>
    public bool TryMatch(string? plate, DateTime at, out VehicleFilterEntry? match)
    {
        match = null;

        var key = PlateRules.Normalize(plate);
        if (key.Length == 0) return false;

        var map = Volatile.Read(ref _byPlate);
        if (!map.TryGetValue(key, out var entries)) return false;

        match = Match(entries, at, _config.Current.VehicleFilter);
        return match is not null;
    }

    /// <summary>
    /// Chọn dòng khớp trong các dòng CÙNG MỘT BIỂN — hàm THUẦN, tách riêng để kiểm được mà không cần DB.
    ///
    /// Ưu tiên dòng BẬT cảnh báo; không có dòng nào bật thì trả dòng còn hiệu lực đầu tiên (vẫn phải ghi lịch
    /// sử). Không dòng nào còn hiệu lực ⇒ null.
    /// </summary>
    public static VehicleFilterEntry? Match(
        IReadOnlyList<VehicleFilterEntry> entries, DateTime at, VehicleFilterOptions options)
    {
        VehicleFilterEntry? fallback = null;

        foreach (var entry in entries)
        {
            if (!entry.IsActiveAt(at)) continue;
            if (options.IsAlarmOn(entry.Alarm)) return entry;

            fallback ??= entry;
        }

        return fallback;
    }

    /// <summary>
    /// Nạp danh sách. KHÔNG BAO GIỜ ném lỗi ra ngoài: thiếu danh sách chỉ làm mất cảnh báo biển số, không
    /// được chặn khởi động hay làm chết worker nạp lại.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (!_config.Current.VehicleFilter.Enabled)
        {
            _logger.LogInformation(
                "[VehicleFilter] Service:VehicleFilter:Enabled = false ⇒ không đối chiếu biển số.");
            Interlocked.Exchange(ref _byPlate, new Dictionary<string, VehicleFilterEntry[]>(StringComparer.Ordinal));
            return;
        }

        var rows = await FromDatabaseAsync(ct).ConfigureAwait(false);

        if (rows is not null)
        {
            var map = Build(rows);
            Interlocked.Exchange(ref _byPlate, map);
            SaveCache(map);

            _logger.LogInformation("[VehicleFilter] Nạp {Rows} dòng danh sách ⇒ {Plates} biển trong RAM.",
                rows.Count, map.Count);
            return;
        }

        // DB lỗi ⇒ GIỮ snapshot đang có nếu đã từng nạp được. Lùi về cache lúc này sẽ thay danh sách mới
        // bằng danh sách cũ hơn — đi lùi.
        if (Count > 0)
        {
            _logger.LogWarning(
                "[VehicleFilter] Không đọc được danh sách; giữ {Plates} biển đang có trong RAM.", Count);
            return;
        }

        var cached = LoadCache();
        if (cached is { Count: > 0 })
        {
            Interlocked.Exchange(ref _byPlate, cached);
            _logger.LogWarning(
                "[VehicleFilter] Không đọc được danh sách; dùng file cache {Path} ({Plates} biển).",
                _cachePath, cached.Count);
            return;
        }

        _logger.LogError(
            "[VehicleFilter] KHÔNG có danh sách đen/trắng (DB lỗi và chưa có file cache {Path}) ⇒ sẽ không " +
            "cảnh báo biển nào.", _cachePath);
    }

    /// <summary>
    /// Đọc DB. Lỗi ⇒ null (bên gọi giữ snapshot cũ hoặc lùi về cache); RỖNG ⇒ danh sách rỗng, vẫn là kết
    /// quả hợp lệ.
    /// </summary>
    private async Task<List<TmsVehicleFilter>?> FromDatabaseAsync(CancellationToken ct)
    {
        try
        {
            return await _repo.QueryFiltersAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("VehicleFilterManager");
            _logger.LogWarning(ex, "[VehicleFilter] Lỗi đọc bảng danh sách đen/trắng.");
            return null;
        }
    }

    /// <summary>
    /// Dựng map biển → dòng danh sách. Hàm THUẦN, tách riêng để kiểm được mà không cần DB.
    /// Bỏ dòng không có biển hoặc biển chuẩn hoá ra rỗng — không có gì để đối chiếu.
    /// </summary>
    public static Dictionary<string, VehicleFilterEntry[]> Build(IEnumerable<TmsVehicleFilter> rows)
    {
        var map = new Dictionary<string, VehicleFilterEntry[]>(StringComparer.Ordinal);

        foreach (var group in rows
                     .Select(r => (Key: PlateRules.Normalize(r.LicensePlate), Row: r))
                     .Where(x => x.Key.Length > 0)
                     .GroupBy(x => x.Key, StringComparer.Ordinal))
        {
            map[group.Key] = group.Select(x => VehicleFilterEntry.From(group.Key, x.Row)).ToArray();
        }

        return map;
    }

    /// <summary>Nạp sẵn danh sách (không đụng DB, không ghi file) — dùng cho smoke/test.</summary>
    internal void Seed(IEnumerable<TmsVehicleFilter> rows) => Interlocked.Exchange(ref _byPlate, Build(rows));

    private void SaveCache(Dictionary<string, VehicleFilterEntry[]> map)
    {
        try
        {
            var flat = map.Values.SelectMany(v => v).ToList();
            StatePath.WriteAtomic(_cachePath, JsonSerializer.Serialize(flat, ConfigParamApplier.FileWriteOptions));
        }
        catch (Exception ex)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("VehicleFilterManager");
            // Mất file cache chỉ ảnh hưởng lần khởi động sau khi DB lỗi; lượt này vẫn có danh sách từ DB.
            _logger.LogWarning(ex, "[VehicleFilter] Không ghi được file cache {Path}.", _cachePath);
        }
    }

    private Dictionary<string, VehicleFilterEntry[]>? LoadCache()
    {
        try
        {
            if (!File.Exists(_cachePath)) return null;

            var flat = JsonSerializer.Deserialize<List<VehicleFilterEntry>>(
                File.ReadAllText(_cachePath), ConfigParamApplier.JsonOptions);
            if (flat is null) return null;

            return flat
                .Where(e => !string.IsNullOrWhiteSpace(e.Plate))
                .GroupBy(e => e.Plate, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("VehicleFilterManager");
            _logger.LogWarning(ex, "[VehicleFilter] File cache {Path} hỏng; bỏ qua.", _cachePath);
            return null;
        }
    }

    /// <summary>Dòng danh sách của một biển đã chuẩn hoá — chỉ dùng để chẩn đoán.</summary>
    internal VehicleFilterEntry[] EntriesOf(string normalizedPlate)
        => Volatile.Read(ref _byPlate).TryGetValue(normalizedPlate, out var entries) ? entries : NoEntries;
}
