using ITS.FA.Core.Configuration;
using ITS.FA.Core.Persistence;

namespace ITS.FA.Core.Processing;

/// <summary>
/// Tìm vùng và camera theo vị trí báo cháy. Port NGUYÊN logic ITS008:
///   LocationHelper.GetZoneFromLocation(db, km, met, tunnel, 0)         → <see cref="GetZonesAsync"/>
///   EquipmentHelper.GetEquipmentCameraRegion(db, km, met, tunnel, 0)   → <see cref="GetCamerasAsync"/>
///
/// Với phạm vi ảnh hưởng 0 (hai hướng) bản cũ gom vùng theo cả chiều Vĩnh Hảo (từ nhỏ đến lớn) lẫn chiều
/// Cam Lâm (từ lớn đến nhỏ), nên điều kiện thực chất là "vị trí nằm trong đoạn [min, max] của vùng",
/// bất kể vùng khai Từ/Đến theo chiều nào. Bộ lọc làn của bản cũ chỉ phân hai chiều nên không còn tác dụng
/// khi lấy cả hai.
///
/// Khác biệt do khác cấu trúc bảng: ZONES.TUNNEL → TmsZone.Location (mã tuyến/hầm), vw_CAMERA_EQUIPMENTS →
/// TmsZoneEquipment + TmsEquipment lọc theo loại camera khai ở <see cref="FaIncidentOptions.CameraEquipmentTypeId"/>;
/// TmsZone có cấp cha/con nên chỉ giữ vùng nhỏ nhất (ZONES cũ phẳng). Vẫn giữ biên đóng hai đầu và không lọc Status như cũ.
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
public sealed class FaZoneLocator
{
    private readonly FaRepository _repository;
    private readonly FaIncidentOptions _options;

    public FaZoneLocator(FaRepository repository, IOptions<FaOptions> options)
    {
        _repository = repository;
        _options = options.Value.Incident;
    }

    /// <summary>Vùng chứa lý trình km+mét; có mã vị trí thì chỉ lấy vùng cùng TmsZone.Location.</summary>
    public async Task<List<TmsZone>> GetZonesAsync(int km, int met, string? location, CancellationToken ct = default)
    {
        var position = km * 1000 + met;
        var zones = await _repository.GetZonesAsync(ct).ConfigureAwait(false);

        var matched = zones
            .Where(z => Contains(z, position))
            .Where(z => location is null || string.Equals(z.Location, location, StringComparison.Ordinal))
            .DistinctBy(z => z.ID)
            .ToList();

        // Bảng ZONES cũ phẳng (không có cha/con), mỗi dòng là một khu vực nhỏ nhất. TmsZone thêm cấp cha
        // ("Đoạn IC.01-IC.02") bao các khu vực con, nên bỏ vùng cha có vùng con cũng chứa vị trí để giữ đúng
        // nghĩa cũ. Cùng cách IncidentPositionHelper của VDS/web chọn vùng cấp nhỏ nhất.
        var parentIds = matched.Where(z => !string.IsNullOrEmpty(z.Pid)).Select(z => z.Pid!).ToHashSet(StringComparer.Ordinal);
        return matched.Where(z => !parentIds.Contains(z.ID)).ToList();
    }

    /// <summary>
    /// Camera thuộc từng vùng và nằm trong đoạn vùng, sắp theo khoảng cách tới điểm báo cháy rồi theo hướng hoạt động.
    /// Trả rỗng khi chưa khai loại camera.
    /// </summary>
    public async Task<List<TmsEquipment>> GetCamerasAsync(int km, int met, IReadOnlyCollection<TmsZone> zones, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.CameraEquipmentTypeId) || zones.Count == 0)
            return [];

        var position = km * 1000 + met;
        var links =await _repository.GetZoneEquipmentsAsync(zones.Select(z => z.ID).ToList(), ct).ConfigureAwait(false);
        var cameras = await _repository
            .GetCamerasAsync(links.Select(l => l.EquipmentId).Where(id => id != null).Select(id => id!).Distinct().ToList(),
                _options.CameraEquipmentTypeId, ct)
            .ConfigureAwait(false);
        var cameraById = cameras.ToDictionary(c => c.ID);

        var result = new List<TmsEquipment>();
        foreach (var zone in zones)
        {
            var (start, end) = Range(zone);
            foreach (var link in links.Where(l => l.ZoneId == zone.ID && l.EquipmentId != null))
            {
                if (!cameraById.TryGetValue(link.EquipmentId!, out var camera) || camera.KmNumber == null) continue;
                var cameraPosition = camera.KmNumber.Value * 1000 + (camera.MetNumber ?? 0);
                if (cameraPosition >= start && cameraPosition <= end) result.Add(camera);
            }
        }

        return result
            .OrderBy(c => Math.Abs(position - ((c.KmNumber ?? 0) * 1000 + (c.MetNumber ?? 0))))
            .ThenBy(c => c.ActiveDirection)
            .DistinctBy(c => c.ID)
            .ToList();
    }

    private static bool Contains(TmsZone zone, int position)
    {
        if (zone.FromKmNumber == null || zone.ToKmNumber == null) return false;
        var (start, end) = Range(zone);
        return start <= position && position <= end;
    }

    private static (int Start, int End) Range(TmsZone zone)
    {
        var from = (zone.FromKmNumber ?? 0) * 1000 + (zone.FromMetNumber ?? 0);
        var to = (zone.ToKmNumber ?? 0) * 1000 + (zone.ToMetNumber ?? 0);
        return (Math.Min(from, to), Math.Max(from, to));
    }
}
