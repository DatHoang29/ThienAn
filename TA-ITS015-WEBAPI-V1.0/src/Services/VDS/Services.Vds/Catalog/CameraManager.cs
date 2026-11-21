using System.Collections.Concurrent;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Persistence;
using Services.Vds.Configuration;

namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Giữ danh mục camera trong RAM, keyed theo IP.
///
/// <b>Vì sao phải cache</b>: mọi bản tin VDS chỉ mang <c>camera_ip</c>, cần tra ra
/// <c>EquipmentId</c>/Km/hướng để ghi được. Query DB mỗi bản tin thì một xe qua trạm là một round-trip —
/// không chấp nhận được. Danh mục đổi rất ít nên nạp một lần, nạp lại khi có thông báo.
///
/// Là singleton giữ trạng thái dùng chung. <see cref="ConcurrentDictionary{TKey,TValue}"/> vì luồng
/// nhận bản tin đọc song song với luồng nạp lại.
/// Created date: 24/08/2026
/// </summary>
public sealed class CameraManager
{
    private readonly VdsCatalogRepository _catalog;
    private readonly IVdsConfigManager _config;
    private readonly CatalogSnapshotStore _snapshots;
    private readonly ILogger<CameraManager> _logger;

    private readonly ConcurrentDictionary<string, CameraInfo> _byIp = new(StringComparer.OrdinalIgnoreCase);

    public CameraManager(
        VdsCatalogRepository catalog,
        IVdsConfigManager config,
        CatalogSnapshotStore snapshots,
        ILogger<CameraManager> logger)
    {
        _catalog = catalog;
        _config = config;
        _snapshots = snapshots;
        _logger = logger;
    }

    public int Count => _byIp.Count;

    /// <summary>Toàn bộ camera trong cache — để liệt kê/chọn (ví dụ ở công cụ mô phỏng).</summary>
    public IReadOnlyCollection<CameraInfo> All => _byIp.Values.ToList();

    /// <summary>
    /// Tra camera theo IP. Đây là hàm chạy trên MỌI bản tin nên chỉ đọc dictionary, không I/O.
    /// </summary>
    public bool TryGet(string? ip, out CameraInfo? camera)
    {
        camera = null;
        if (string.IsNullOrWhiteSpace(ip)) return false;
        return _byIp.TryGetValue(ip.Trim(), out camera);
    }

    /// <summary>
    /// Nạp lại toàn bộ danh mục từ SQL Server.
    ///
    /// Lỗi được NÉM RA cho bên gọi quyết định: khởi động mà không có danh mục thì service không làm được
    /// gì (mọi bản tin sẽ bị bỏ vì không nhận diện được camera) — khác với nạp lại giữa lúc chạy, lúc đó
    /// giữ cache cũ là đúng. Hai tình huống khác nhau nên để bên gọi xử lý.
    /// </summary>
    /// <summary><c>SysConfigType.Code</c> của danh mục camera VDS nạp vào service (con = EquipmentId).</summary>
    public const string ListEquipmentConfigType = "vds_list_equipment";

    public async Task LoadAsync(CancellationToken ct = default)
    {
        // Danh sách camera nạp lấy từ SysConfigData nhóm 'vds_list_equipment' (mỗi con Code = EquipmentId).
        // Chuyển từ Catalog:CameraCodes (appsettings, theo Code) sang DB (theo ID) — người vận hành sửa
        // trên web, và lọc theo ID (khoá chính) đáng tin hơn Code (sửa được, có thể trùng).
        //
        // Cả hai truy vấn dưới đây đi kèm ĐƯỜNG LÙI: DB không tới được thì dùng snapshot last-known-good,
        // để một nhịp chớp của SQL Server lúc khởi động không chặn cả service (xem CatalogSnapshotStore).
        var fromSnapshot = false;
        List<string> ids;
        List<VdsCatalogRepository.CameraRow> rows;

        try
        {
            var configRows = await _catalog.QueryConfigDataAsync(ListEquipmentConfigType, ct);
            ids = configRows
                .Select(r => r.Code)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Empty is an explicit stop; only DB failure may use the previous snapshot.
            rows = ids.Count == 0 ? new() : await _catalog.QueryVdsCamerasByIdsAsync(ids, ct);
            _snapshots.TrySave(CatalogSnapshotStore.CamerasName, rows);

        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("CameraManager");
            var cached = _snapshots.TryLoad<List<VdsCatalogRepository.CameraRow>>(
                CatalogSnapshotStore.CamerasName);

            // Không có snapshot ⇒ ném tiếp như trước: thiếu danh mục camera thì service không làm được gì,
            // bên gọi phải thấy lỗi để chặn khởi động.
            if (cached is null) throw;

            _logger.LogError(ex,
                "[Camera] Không đọc được danh mục từ DB ⇒ DÙNG SNAPSHOT {Count} camera của lần nạp gần nhất. " +
                "Danh mục có thể đã CŨ; phải kiểm lại kết nối DB.", cached.Count);

            rows = cached;
            ids = cached.Select(r => r.Id).ToList();
            fromSnapshot = true;
        }

        if (!fromSnapshot) WarnMissingIds(ids, rows);

        // Camera thiếu IP: loại, nhưng phải BÁO SỐ. Không nhận diện được thì mọi bản tin của nó bị bỏ ở
        // cửa vào — vận hành cần biết để bổ sung danh mục, không để nó im lặng xảy ra.
        var missingIp = rows.Count(r => string.IsNullOrWhiteSpace(r.Ip));
        if (missingIp > 0)
            _logger.LogWarning(
                "[Camera] {Count} camera thiếu IP trong danh mục ⇒ KHÔNG nhận diện được, " +
                "bản tin của chúng sẽ bị bỏ.", missingIp);

        if (missingIp > 0) CatalogRefreshAudit.Failed("CameraMissingIp");
        if (ids.Except(rows.Select(r => r.Id), StringComparer.OrdinalIgnoreCase).Any())
            CatalogRefreshAudit.Failed("CameraNotFound");

        var cameras = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Ip))
            .Select(r => new CameraInfo(
                CameraIp: r.Ip!.Trim(),
                EquipmentId: r.Id,
                Name: r.Name,
                Km: r.KmNumber,
                Met: r.MetNumber,
                Direction: r.Direction,
                Lane: r.LaneId,
                Location: r.Location,
                Code: r.Code))
            .ToList();

        _byIp.Clear();
        // Trùng IP: bản sau ghi đè bản trước. Sắp theo ID (ordinal) để "bản sau" TẤT ĐỊNH = ID lớn nhất — cùng luật
        // với WebAPI "Sinh đoạn" (VdsSegmentTopologyRules.SelectCameras), để đoạn tự sinh hai bên trùng nhau.
        foreach (var cam in cameras.OrderBy(c => c.EquipmentId, StringComparer.Ordinal))
            _byIp[cam.CameraIp] = cam;

        WarnDuplicateIps(cameras);
        WarnMissingDirection(cameras);

        _logger.LogInformation(
            "[Camera] Nạp {Count} camera ({Filter}){Source}: {List}.",
            _byIp.Count,
            ids.Count > 0 ? $"lọc theo {ids.Count} ID" : "danh sách cho phép rỗng",
            fromSnapshot ? " TỪ SNAPSHOT (DB lỗi)" : "",
            string.Join(", ", _byIp.Values.OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
                .Select(c => c.Label)));
    }

    /// <summary>ID khai trong danh mục mà không tra ra camera — thường thiết bị đã xoá hoặc sai ID.</summary>
    private void WarnMissingIds(List<string> ids, List<VdsCatalogRepository.CameraRow> rows)
    {
        if (ids.Count == 0) return;

        var missing = ids
            .Except(rows.Select(r => r.Id), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missing.Count > 0)
            _logger.LogWarning(
                "[Camera] Danh mục 'vds_list_equipment' có {Count} ID không tra ra camera VDS: {Ids}.",
                missing.Count, string.Join(", ", missing));
    }

    private void WarnDuplicateIps(List<CameraInfo> cameras)
    {
        var duplicates = cameras
            .GroupBy(c => c.CameraIp, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Count())
            .ToList();

        if (duplicates.Count == 0) return;
        _logger.LogError(
            "[Camera] {DupIp} IP bị dùng cho nhiều camera ⇒ {Skipped}/{Total} camera KHÔNG nhận diện " +
            "được và bản tin sẽ bị gán SAI vị trí. Cần làm sạch danh mục TmsEquipment. Chi tiết: {Detail}",
            duplicates.Count, cameras.Count - _byIp.Count, cameras.Count,
            string.Join(" | ", duplicates.Take(5).Select(g => $"{g.Key} x{g.Count()}")));
    }

    private void WarnMissingDirection(List<CameraInfo> cameras)
    {
        var missing = cameras.Where(c => string.IsNullOrWhiteSpace(c.Direction)).ToList();
        if (missing.Count == 0) return;

        // Ngưỡng tốc độ khai theo HƯỚNG. Camera không có hướng thì không tra được ngưỡng ⇒ fail-closed,
        // không sinh sự kiện quá tốc độ cho camera đó.
        _logger.LogWarning(
            "[Camera] {Count} camera thiếu hướng ({List}) ⇒ không tra được ngưỡng tốc độ, " +
            "sẽ KHÔNG sinh sự kiện quá tốc độ cho các camera này.",
            missing.Count, string.Join(", ", missing.Take(5).Select(c => c.Label)));
    }
}
