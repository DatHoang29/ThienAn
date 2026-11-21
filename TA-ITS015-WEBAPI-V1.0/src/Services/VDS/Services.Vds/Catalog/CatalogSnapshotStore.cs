using System.Text.Json;
using Services.Vds.Configuration;
using Services.Vds.Shared.State;

namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Lưu bản sao LAST-KNOWN-GOOD của các danh mục đọc từ SQL Server ra file JSON trong thư mục
/// state, để service KHỞI ĐỘNG ĐƯỢC khi DB không tới được.
///
/// <b>Vì sao cần</b>: danh mục camera là điều kiện sống của service — không tra được camera theo IP thì MỌI
/// bản tin bị bỏ ở cửa vào, nên <c>VdsServiceManager</c> cố tình NÉM lỗi và thoát khi danh mục rỗng. Hệ quả:
/// SQL Server chớp một nhịp đúng lúc khởi động là service không lên được, dù dữ liệu danh mục gần như không
/// bao giờ đổi. File này là đường lùi cho đúng tình huống đó.
///
/// <b>Chỉ dùng khi ĐỌC DB THẤT BẠI.</b> Đọc được (kể cả trả về ít dòng) thì luôn ưu tiên DB và ghi lại
/// snapshot mới. Không bao giờ dùng file để "trộn" với dữ liệu DB — một nguồn cho một lượt nạp.
///
/// <b>Không thay thế các tham số đi qua <c>IVdsConfigManager.Apply</c></b> (ngưỡng tốc độ, mapping sự kiện,
/// mapping cỡ xe, cửa skip/dedup/checkPlate): những thứ đó đã sống qua restart nhờ
/// <c>config-overrides.json</c>. Store này chỉ cho danh mục giữ thuần trong RAM: camera, vùng, đoạn A→B.
///
/// Mọi lỗi đọc/ghi đều chỉ log: mất snapshot chỉ làm mất đường lùi, không được phép làm hỏng lượt nạp đang
/// chạy bình thường.
/// Created date: 08/09/2026
/// </summary>
public sealed class CatalogSnapshotStore
{
    /// <summary>Tên snapshot danh mục camera VDS.</summary>
    public const string CamerasName = "catalog-cameras";

    /// <summary>Tên snapshot liên kết camera ↔ vùng.</summary>
    public const string ZoneLinksName = "catalog-zone-links";

    /// <summary>
    /// Tên snapshot ngưỡng tốc độ min/max và hướng của vùng (<c>TmsZone.MinSpeed/MaxSpeed/LaneId</c>).
    /// Giữ tên cũ: file cũ thiếu <c>LaneId</c> vẫn đọc được (trường có mặc định null).
    /// </summary>
    public const string ZoneSpeedsName = "catalog-zone-speeds";

    /// <summary>Tên snapshot camera xem được theo vùng (module CCTV).</summary>
    public const string ZoneCamerasName = "catalog-zone-cameras";

    /// <summary>Tên snapshot đoạn A→B.</summary>
    public const string SegmentsName = "catalog-segments";

    /// <summary>Tên snapshot đoạn A→B TỰ SINH từ km/tuyến/hướng (đã tính xong, kèm biên thời gian).</summary>
    public const string AutoSegmentsName = "catalog-auto-segments";

    /// <summary>Danh mục hiển thị (<c>SysConfigData</c>): tên vị trí, tên hướng, tên loại xe.</summary>
    public const string ConfigDataName = "catalog-config-data";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IVdsConfigManager _config;
    private readonly ILogger<CatalogSnapshotStore> _logger;

    public CatalogSnapshotStore(IVdsConfigManager config, ILogger<CatalogSnapshotStore> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>Đường dẫn file snapshot của một danh mục.</summary>
    public string PathOf(string name) =>
        StatePath.Resolve(_config.Current.StateDirectory, "catalog", name + ".json");

    /// <summary>
    /// Ghi snapshot. Trả false nếu không ghi được (đã log) — bên gọi KHÔNG cần xử lý gì thêm vì lượt nạp
    /// hiện tại vẫn có dữ liệu từ DB.
    /// </summary>
    public bool TrySave<T>(string name, T value)
    {
        try
        {
            StatePath.WriteAtomic(PathOf(name), JsonSerializer.Serialize(value, JsonOptions));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
                                      or NotSupportedException)
        {
            _logger.LogWarning(ex,
                "[Catalog] Không ghi được snapshot '{Name}' ⇒ lần khởi động sau sẽ không có đường lùi nếu DB lỗi.",
                name);
            return false;
        }
    }

    /// <summary>
    /// Đọc snapshot. Trả <c>null</c> khi chưa có file hoặc file hỏng — bên gọi phải tự quyết (thường là
    /// ném tiếp lỗi gốc của DB).
    /// </summary>
    public T? TryLoad<T>(string name) where T : class
    {
        var path = PathOf(name);

        try
        {
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
                                      or NotSupportedException)
        {
            _logger.LogWarning(ex, "[Catalog] Snapshot '{Name}' hỏng hoặc không đọc được; bỏ qua.", name);
            return null;
        }
    }
}
