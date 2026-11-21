using Modules.VDS.Core.Constants;
using Services.Vds.Shared.Persistence;

namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Danh mục cấu hình hệ thống (<c>SysConfigData</c>) trong RAM, keyed theo
/// <c>SysConfigType.Code</c> → (<c>Code</c> → <c>Name</c>).
///
/// Chỉ nạp những loại danh mục pipeline THỰC SỰ cần để hiển thị giống hệ thống web (tên sự cố, chú giải
/// chart):
/// <list type="bullet">
/// <item><c>tms_zone_location</c> — vị trí tuyến/hầm: <c>main_route</c> → "Tuyến CT.01".</item>
/// <item><c>direction_config_type</c> — hướng tuyến: <c>1</c> → "Phải tuyến".</item>
/// <item><c>vehicle_type</c> — cỡ xe: <c>short</c> → "Xe dài &lt;6m"; đặt tên series chart đếm xe.</item>
/// </list>
///
/// <para><b>Đường lùi khi DB lỗi</b>: dùng snapshot của lần nạp gần nhất
/// (<c>catalog/catalog-config-data.json</c>), đắp cho ĐÚNG những loại vừa lỗi. Loại đọc được mà trả rỗng thì
/// KHÔNG đắp — rỗng là trạng thái hợp lệ (chưa ai khai), lùi về snapshot ở đó sẽ làm giá trị vừa xoá trên web
/// sống lại. Snapshot chỉ được ghi khi cả ba loại đều nạp được, để một nhịp DB chập không đóng băng bản
/// thiếu vào file.</para>
///
/// Không có cả DB lẫn snapshot thì <see cref="NameOf"/> lùi về chính mã: tên sự cố xấu hơn nhưng pipeline vẫn
/// chạy. Đây chỉ là phần hiển thị nên không được phép làm dừng luồng dữ liệu.
/// Created date: 26/08/2026
/// </summary>
public sealed class ConfigDataManager
{
    /// <summary>Loại danh mục cho vị trí vùng (tuyến/hầm).</summary>
    public const string ZoneLocationType = VdsConfigConst.ZoneLocationTypeCode;

    /// <summary>Loại danh mục cho hướng tuyến.</summary>
    public const string DirectionType = "direction_config_type";

    /// <summary>
    /// Loại danh mục cho CỠ xe. Mã danh mục là <c>vehicle_type</c> — trùng đúng <c>SysConfigType.Code</c>
    /// trên DB và cùng danh mục mà TMS dùng để đặt tên series chart, nên chú giải hai bên khớp chữ.
    ///
    /// Nội dung là CỠ xe (<c>short</c>/<c>medium</c>/<c>long</c>) — cùng bộ mã của
    /// <c>TmsTrafficData.Type</c> và cột <c>vehicleSize</c> trên QuestDB, KHÔNG phải cột
    /// <c>vehicleType</c> (loại xe: Car/Truck/Bus…).
    ///
    /// Lệch tên này là bẫy có thật: dùng nó để tra tên cho giá trị cột <c>vehicleType</c> sẽ không tra
    /// được gì. Chỉ dùng cho cỡ xe.
    /// </summary>
    public const string VehicleSizeType = "vehicle_type";

    private static readonly string[] LoadedTypes = { ZoneLocationType, DirectionType, VehicleSizeType };

    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly VdsCatalogRepository _catalog;
    private readonly CatalogSnapshotStore _snapshots;
    private readonly ILogger<ConfigDataManager> _logger;

    private Dictionary<string, IReadOnlyDictionary<string, string>> _byType =
        new(StringComparer.OrdinalIgnoreCase);

    public ConfigDataManager(
        VdsCatalogRepository catalog,
        CatalogSnapshotStore snapshots,
        ILogger<ConfigDataManager> logger)
    {
        _catalog = catalog;
        _snapshots = snapshots;
        _logger = logger;
    }

    /// <summary>Tổng số giá trị đã nạp (mọi loại danh mục) — để ghi log lúc khởi động.</summary>
    public int Count => Volatile.Read(ref _byType).Values.Sum(v => v.Count);

    /// <summary>
    /// Tên hiển thị của một mã. Không tra được (mã rỗng, danh mục chưa nạp, mã lạ) ⇒ trả về chính mã đã
    /// trim; null/rỗng ⇒ null. Không bao giờ ném lỗi.
    /// </summary>
    public string? NameOf(string configTypeCode, string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var key = code.Trim();

        var map = Volatile.Read(ref _byType);
        if (map.TryGetValue(configTypeCode, out var values)
            && values.TryGetValue(key, out var name)
            && !string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        return key;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var map = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var failed = new List<string>();

        foreach (var type in LoadedTypes)
        {
            // try/catch THEO TỪNG LOẠI: thiếu một danh mục không được làm mất các loại còn lại.
            try
            {
                var rows = await _catalog.QueryConfigDataAsync(type, ct);
                map[type] = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Code))
                    .GroupBy(r => r.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Name ?? g.Key,
                        StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("ConfigDataManager");
                _logger.LogWarning(ex,
                    "[ConfigData] Không nạp được danh mục '{Type}' từ DB; sẽ thử snapshot của lần nạp " +
                    "gần nhất.", type);
                failed.Add(type);
            }
        }

        // Đường lùi: snapshot của lần nạp gần nhất, ĐẮP cho đúng những loại vừa lỗi. Không đắp cho loại
        // đọc được mà trả rỗng — rỗng là trạng thái hợp lệ (chưa ai khai), lùi về snapshot ở đó sẽ làm
        // giá trị vừa xoá trên web sống lại.
        if (failed.Count > 0) ApplySnapshotFor(failed, map);

        // Chỉ ghi snapshot khi KHÔNG có loại nào lỗi: ghi khi đang lỗi sẽ đóng băng bản thiếu vào file và
        // xoá mất bản tốt của lần trước.
        if (failed.Count == 0 && map.Values.Any(v => v.Count > 0))
            _snapshots.TrySave(CatalogSnapshotStore.ConfigDataName, ToSnapshot(map));

        Interlocked.Exchange(ref _byType, map);
        _logger.LogInformation("[ConfigData] Nạp {Total} giá trị{Source}: {Detail}.",
            Count,
            failed.Count == 0 ? string.Empty : $" ({failed.Count} danh mục lấy từ SNAPSHOT do DB lỗi)",
            string.Join(", ", map.Select(kv => $"{kv.Key}={kv.Value.Count}")));
    }

    /// <summary>Đắp các loại nạp lỗi bằng snapshot; loại nào snapshot cũng không có thì để rỗng.</summary>
    private void ApplySnapshotFor(
        List<string> failed,
        Dictionary<string, IReadOnlyDictionary<string, string>> map)
    {
        var cached = _snapshots.TryLoad<Dictionary<string, Dictionary<string, string>>>(
            CatalogSnapshotStore.ConfigDataName);

        foreach (var type in failed)
        {
            if (cached is not null
                && cached.TryGetValue(type, out var values)
                && values.Count > 0)
            {
                map[type] = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
                _logger.LogInformation(
                    "[ConfigData] Danh mục '{Type}': DÙNG SNAPSHOT {Count} giá trị của lần nạp gần nhất. " +
                    "Dữ liệu có thể đã CŨ.", type, values.Count);
            }
            else
            {
                // Không có snapshot: NameOf lùi về chính mã — tên sự cố xấu hơn nhưng pipeline vẫn chạy.
                map[type] = Empty;
                _logger.LogWarning(
                    "[ConfigData] Danh mục '{Type}': DB lỗi và chưa có snapshot ⇒ tên sự cố sẽ dùng mã " +
                    "thay cho tên hiển thị.", type);
            }
        }
    }

    /// <summary>Đổi sang kiểu ghi được ra JSON (bỏ <c>IReadOnlyDictionary</c> để đọc lại không cần map lạ).</summary>
    private static Dictionary<string, Dictionary<string, string>> ToSnapshot(
        Dictionary<string, IReadOnlyDictionary<string, string>> map)
        => map.ToDictionary(
            kv => kv.Key,
            kv => new Dictionary<string, string>(
                kv.Value.ToDictionary(x => x.Key, x => x.Value), StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
}
