namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Job định kỳ nạp lại danh mục để thay đổi trên web có hiệu lực mà không cần khởi động lại
/// service.
///
/// <para>Toàn bộ việc nạp nằm ở <see cref="CatalogRefresher"/> — <b>nơi duy nhất</b> biết mảng nào nạp bằng
/// bộ nạp nào. Lớp này chỉ là điểm treo cho <c>VdsJobScheduler</c>: nó quyết định "chu kỳ thì nạp lại TẤT
/// CẢ", không tự liệt kê lại từng bộ nạp (trước đây liệt kê lại nên thêm mảng mới phải sửa nhiều chỗ).</para>
///
/// Khác lần nạp lúc khởi động: ở đó thiếu camera là lý do CHẶN khởi động (service không làm được gì), còn ở
/// đây giữ cache cũ mới là đúng — nên <see cref="CatalogRefresher"/> không ném.
/// Created date: 26/08/2026
/// </summary>
public sealed class CatalogReloadJob
{
    private readonly CatalogRefresher _refresher;
    private readonly CameraManager _cameras;
    private readonly ZoneManager _zones;
    private readonly ILogger<CatalogReloadJob> _logger;

    public CatalogReloadJob(
        CatalogRefresher refresher,
        CameraManager cameras,
        ZoneManager zones,
        ILogger<CatalogReloadJob> logger)
    {
        _refresher = refresher;
        _cameras = cameras;
        _zones = zones;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await _refresher.RefreshAsync(CatalogParts.All, ct).ConfigureAwait(false);

        _logger.LogDebug("[Catalog] Nạp lại xong: {Cameras} camera, {ZoneCameras} camera có vùng.",
            _cameras.Count, _zones.Count);
    }
}
