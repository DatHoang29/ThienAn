using Shared.Utility.Counting;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Shared.Utility.Helpers;

namespace Services.Vds.Features.Counting;

/// <summary>
/// Author: KienLT
/// Description: Dựng payload <c>vds.count/v1</c> cho subject đếm xe.
///
/// <b>Một nguồn số duy nhất</b>: mọi con số trong payload đều tính từ bảng dữ liệu thô trên QuestDB
/// (<see cref="QuestCountReader"/>). Bản cũ phải trộn ba nguồn — cache theo giờ đã hoà giải cho giờ đã
/// đóng, bộ đếm RAM cho giờ đang chạy, và RAM cho cửa sổ trượt — nên mỗi lần lệch nhau là phải đi tìm
/// nguồn nào sai. Giờ không còn chuyện đó, và restart cũng không làm nhảy số.
///
/// Phép gộp (cửa sổ trượt, chuỗi theo giờ, trung bình có trọng số) gọi <see cref="TrafficCountMath"/> của
/// <c>Modules.VDS.Core.Counting</c> — phía WebAPI dùng cùng hàm nên hai bên không thể ra số khác nhau.
///
/// <b>Hình dạng payload giữ nguyên bản cũ</b> để bên nhận không phải sửa: cùng bộ khoá và cùng camelCase.
/// Riêng <c>origin</c> của từng giờ luôn là <c>db</c> vì không còn số nào lấy từ RAM.
///
/// <b>Thêm mới</b>: mỗi camera và mỗi nhóm hướng có thêm khối <c>chart</c> đúng định dạng ECharts mà FE đã
/// dùng cho <c>api/tms/TmsTrafficData/TrafficDataToDayChart</c> — xem <see cref="CountChartBuilder"/>.
/// Đây là trường THÊM, không đổi trường nào đang có, nên bên nhận cũ bỏ qua được.
/// Created date: 26/08/2026
/// </summary>
public sealed class CountPayloadBuilder
{
    /// <summary>Phiên bản payload — bên nhận kiểm tra trường này trước khi đọc phần còn lại.</summary>
    public const string Schema = "vds.count/v1";

    /// <summary>Nguồn số liệu, thay tên bảng cũ bằng bảng thô trên QuestDB.</summary>
    private const string SourceTable = "VdsTrafficData";

    private readonly QuestCountReader _reader;
    private readonly CameraManager _cameras;
    private readonly ZoneManager _zones;
    private readonly IVdsConfigManager _config;
    private readonly ConfigDataManager _configData;

    public CountPayloadBuilder(
        QuestCountReader reader,
        CameraManager cameras,
        ZoneManager zones,
        IVdsConfigManager config,
        ConfigDataManager configData)
    {
        _reader = reader;
        _cameras = cameras;
        _zones = zones;
        _config = config;
        _configData = configData;
    }

    /// <summary>
    /// Payload ĐẦY ĐỦ: mọi camera trong phạm vi cấu hình + mọi nhóm hướng.
    /// </summary>
    public Task<object> BuildAsync(DateTime nowUtc, CancellationToken ct = default) =>
        BuildAsync(nowUtc, changedEquipmentIds: null, ct);

    /// <summary>
    /// Payload TĂNG DẦN: chỉ gồm camera vừa có dữ liệu mới và các nhóm hướng CÓ CHỨA một trong số đó.
    ///
    /// Số của hướng vẫn tính trên TOÀN BỘ điểm vào khai trong nhóm (kể cả điểm vào không đổi trong lượt
    /// này) — chỉ cộng camera vừa đổi thì con số của hướng sẽ hụt.
    ///
    /// Bên nhận phân biệt qua trường <c>mode</c>: <c>snapshot</c> = thay cả danh sách, <c>delta</c> = ghép
    /// theo <c>equipmentId</c>/<c>directionKey</c>.
    /// </summary>
    public Task<object> BuildDeltaAsync(
        DateTime nowUtc, IReadOnlyCollection<string> changedEquipmentIds, CancellationToken ct = default) =>
        BuildAsync(nowUtc, changedEquipmentIds, ct);

    private async Task<object> BuildAsync(
        DateTime nowUtc, IReadOnlyCollection<string>? changedEquipmentIds, CancellationToken ct)
    {
        var runtime = _config.Current;
        var counting = runtime.Counting;
        var publish = runtime.Publish;
        var period = counting.EffectivePeriodMinutes;
        var isDelta = changedEquipmentIds is not null;

        var (last1hFrom, _) = TrafficCountMath.SlidingWindow(nowUtc, TimeSpan.FromHours(1), period);
        var (last24hFrom, _) = TrafficCountMath.SlidingWindow(nowUtc, TimeSpan.FromHours(24), period);
        var todayFrom = ServerDay.StartOfTodayUtc(nowUtc);
        var currentHour = TrafficCountMath.FloorHour(nowUtc);
        var currentPeriodEnd = TrafficCountMath.FloorPeriod(nowUtc, period).AddMinutes(period);

        // ĐỌC MỘT LẦN cho khoảng rộng nhất rồi lọc trong bộ nhớ: đọc nhiều lần thì mỗi lần là một thời
        // điểm khác nhau và các con số trong cùng payload không khớp nhau.
        var rangeFrom = new[] { last1hFrom, last24hFrom, todayFrom }.Min();
        var slices = await _reader
            .ReadEquipmentSlicesAsync(rangeFrom, currentPeriodEnd, ct)
            .ConfigureAwait(false);

        var byEquipment = slices
            .GroupBy(s => s.Scope.ScopeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var groups = publish.CountDirectionGroups;
        var affectedGroups = isDelta
            ? groups
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .Where(g => g.EquipmentIds.Any(
                    id => changedEquipmentIds!.Contains(id, StringComparer.OrdinalIgnoreCase)))
                .ToList()
            : groups.Where(g => !string.IsNullOrWhiteSpace(g.Key)).ToList();

        var cameras = SelectCameras(byEquipment, publish, changedEquipmentIds)
            .Select(camera => BuildCamera(
                camera, Slices(byEquipment, camera.EquipmentId), groups,
                last1hFrom, last24hFrom, todayFrom, currentHour, currentPeriodEnd,
                publish.CountIncludeChart))
            .ToList();

        var directions = affectedGroups
            .Select(group => BuildDirection(
                group, byEquipment, last1hFrom, last24hFrom, todayFrom, currentHour, currentPeriodEnd,
                publish.CountIncludeChart))
            .ToList();

        return new
        {
            schema = Schema,
            mode = isDelta ? "delta" : "snapshot",
            computedAt = nowUtc,
            source = SourceTable,
            countedEventTypes = counting.EffectiveCountedEventTypes,
            countsAllEventTypes = counting.CountsAllEventTypes,
            countsUnknownPlate = !counting.RequirePlate,
            periodMinutes = period,
            window = new
            {
                last1h = new { from = last1hFrom, to = nowUtc },
                last24h = new { from = last24hFrom, to = nowUtc },
                today = new { from = todayFrom, to = nowUtc },
            },
            cameras,
            directions,
        };
    }

    /// <summary>
    /// Camera nào xuất hiện trong payload:
    /// <list type="bullet">
    /// <item><b>Delta</b>: đúng các camera vừa có dữ liệu mới.</item>
    /// <item><b>Snapshot</b>: khai <c>Publish:CountCameras</c> thì đúng danh sách đó; rỗng thì lấy camera
    /// CÓ số trong cửa sổ đang xét cộng mọi camera thuộc nhóm hướng.</item>
    /// </list>
    /// Camera không có trong danh mục thì bỏ: không có tên/Km để trả về.
    /// </summary>
    private IEnumerable<CameraInfo> SelectCameras(
        Dictionary<string, List<TrafficCountSlice>> byEquipment,
        PublishOptions publish,
        IReadOnlyCollection<string>? changedEquipmentIds)
    {
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (changedEquipmentIds is not null)
        {
            foreach (var id in changedEquipmentIds) wanted.Add(id);
        }
        else if (publish.CountCameras.Count > 0)
        {
            foreach (var id in publish.CountCameras) wanted.Add(id);
        }
        else
        {
            foreach (var id in byEquipment.Keys) wanted.Add(id);
            foreach (var group in publish.CountDirectionGroups)
            {
                foreach (var id in group.EquipmentIds) wanted.Add(id);
            }
        }

        return _cameras.All
            .Where(c => wanted.Contains(c.EquipmentId))
            .OrderBy(c => c.Km ?? int.MaxValue)
            .ThenBy(c => c.Code ?? c.EquipmentId, StringComparer.OrdinalIgnoreCase);
    }

    private object BuildCamera(
        CameraInfo camera,
        List<TrafficCountSlice> slices,
        List<CountDirectionGroup> groups,
        DateTime last1hFrom,
        DateTime last24hFrom,
        DateTime todayFrom,
        DateTime currentHour,
        DateTime to,
        bool includeChart)
    {
        var latest = LatestSpeed(slices);

        // Gộp theo giờ MỘT LẦN rồi dùng cho cả hai trường: flowByHour (số thô) và chart (ECharts) phải là
        // cùng một chuỗi số, tính hai lần là mở đường cho hai trường lệch nhau trong cùng bản tin.
        var hours = TrafficCountMath.ByHour(slices, todayFrom, currentHour);

        return new
        {
            equipmentId = camera.EquipmentId,
            code = camera.Code,
            name = camera.Name,
            km = camera.Km,
            met = camera.Met,
            ipAddress = camera.CameraIp,
            zoneId = ZoneOf(camera.EquipmentId, slices),
            directionKey = groups.FirstOrDefault(g => Contains(g, camera.EquipmentId))?.Key,
            directionId = camera.Direction,
            last1h = Block(TrafficCountMath.Sum(slices, last1hFrom, to)),
            last24h = Block(TrafficCountMath.Sum(slices, last24hFrom, to)),
            speed = new
            {
                latest = latest.Speed,
                latestAt = latest.At,
            },
            flowByHour = FlowByHour(hours),
            chart = Chart(hours, includeChart),
        };
    }

    private object BuildDirection(
        CountDirectionGroup group,
        Dictionary<string, List<TrafficCountSlice>> byEquipment,
        DateTime last1hFrom,
        DateTime last24hFrom,
        DateTime todayFrom,
        DateTime currentHour,
        DateTime to,
        bool includeChart)
    {
        // Số của hướng = tổng các ĐIỂM VÀO khai trong nhóm. Phép cộng hợp lệ vì một xe vào tuyến qua đúng
        // MỘT điểm vào; khai thêm camera nằm giữa tuyến thì mỗi xe bị đếm lại ở từng camera nó đi qua.
        // Không lấy theo cột hướng của dữ liệu: làm vậy là gom cả hướng và phồng số theo số camera.
        var slices = group.EquipmentIds
            .SelectMany(id => Slices(byEquipment, id))
            .ToList();

        var latest = LatestSpeed(slices);
        var hours = TrafficCountMath.ByHour(slices, todayFrom, currentHour);

        return new
        {
            directionKey = group.Key,
            name = group.Name,
            equipmentIds = group.EquipmentIds,
            last1h = Block(TrafficCountMath.Sum(slices, last1hFrom, to)),
            last24h = Block(TrafficCountMath.Sum(slices, last24hFrom, to)),
            speed = new
            {
                latest = latest.Speed,
                latestAt = latest.At,
                latestEquipmentId = latest.EquipmentId,
            },
            flowByHour = FlowByHour(hours),
            chart = Chart(hours, includeChart),
        };
    }

    /// <summary>
    /// Khối ECharts ăn liền cho bên nhận. Tên cỡ xe lấy từ danh mục <c>vehicle_type</c> để chú giải trùng
    /// khớp chữ với chart TMS trên web. Tắt cờ ⇒ null (bên nhận vẫn có <c>flowByHour</c>).
    /// </summary>
    private EChartHelper.EChartResponse? Chart(List<TrafficCountHour> hours, bool includeChart) =>
        includeChart
            ? CountChartBuilder.Build(
                hours, code => _configData.NameOf(ConfigDataManager.VehicleSizeType, code))
            : null;

    /// <summary>
    /// Chuỗi theo GIỜ từ 00:00 hôm nay tới giờ hiện tại, BÙ 0 cho giờ không có xe để bên nhận vẽ chart
    /// không phải tự lấp lỗ.
    ///
    /// <c>origin</c> luôn là <c>db</c>: giữ trường để bên nhận cũ không phải sửa, nhưng không còn số nào
    /// lấy từ RAM nên không còn hai loại nguồn.
    /// </summary>
    private static List<object> FlowByHour(List<TrafficCountHour> hours)
    {
        var result = new List<object>();

        foreach (var hour in hours)
        {
            result.Add(new
            {
                hour = hour.Hour,
                @short = hour.Block.Short,
                medium = hour.Block.Medium,
                @long = hour.Block.Long,
                other = hour.Block.Other,
                total = hour.Block.Total,
                avgSpeed = hour.Block.AvgSpeed,
                origin = "db",
            });
        }

        return result;
    }

    /// <summary>
    /// Khối số liệu dạng camelCase. Không serialize thẳng <see cref="TrafficCountBlock"/>: record cho ra
    /// tên PascalCase, lẫn với phần còn lại của payload thì bên nhận phải xử lý hai kiểu tên trong cùng
    /// một bản tin.
    /// </summary>
    private static object Block(TrafficCountBlock block) => new
    {
        @short = block.Short,
        medium = block.Medium,
        @long = block.Long,
        other = block.Other,
        total = block.Total,
        avgSpeed = block.AvgSpeed,
        minSpeed = block.MinSpeed,
        maxSpeed = block.MaxSpeed,
    };

    /// <summary>
    /// Tốc độ của ô MỚI NHẤT có mẫu tốc độ. Bản cũ lấy tốc độ của lượt xe cuối trong RAM; ở đây hạt nhỏ
    /// nhất là một chu kỳ nên "mới nhất" là trung bình của chu kỳ gần nhất có xe.
    /// </summary>
    private static (double? Speed, DateTime? At, string? EquipmentId) LatestSpeed(
        List<TrafficCountSlice> slices)
    {
        var latest = slices
            .Where(s => s.AvgSpeed is not null && s.SpeedSampleCount > 0)
            .OrderByDescending(s => s.From)
            .FirstOrDefault();

        return latest.AvgSpeed is null
            ? (null, null, null)
            : (latest.AvgSpeed, latest.From, latest.Scope.ScopeId);
    }

    private static List<TrafficCountSlice> Slices(
        Dictionary<string, List<TrafficCountSlice>> byEquipment, string? equipmentId) =>
        equipmentId is not null && byEquipment.TryGetValue(equipmentId, out var list)
            ? list
            : new List<TrafficCountSlice>();

    private static bool Contains(CountDirectionGroup group, string equipmentId) =>
        group.EquipmentIds.Any(id => string.Equals(id, equipmentId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Vùng của camera. Lấy từ DANH MỤC trước (<see cref="ZoneManager.PrimaryZoneOf"/>), chỉ lùi về vùng
    /// ghi trong số liệu khi danh mục không có liên kết.
    ///
    /// <b>Vì sao không lấy từ số liệu như trước</b>: camera KHÔNG có xe trong cửa sổ thì không có ô số liệu
    /// nào, nên vùng suy từ số liệu trả về null và camera đó rơi ra khỏi mọi phép gom theo vùng của bên
    /// nhận — đúng những camera cần vẽ thành đường 0 (camera mới thêm, camera đang chết, nhánh phụ vắng xe
    /// qua đêm). Vùng là thuộc tính của camera trong danh mục, không phải thuộc tính của lưu lượng.
    /// </summary>
    private string? ZoneOf(string? equipmentId, List<TrafficCountSlice> slices) =>
        _zones.PrimaryZoneOf(equipmentId)
        ?? slices.Select(s => s.ZoneId).FirstOrDefault(z => !string.IsNullOrWhiteSpace(z));
}
