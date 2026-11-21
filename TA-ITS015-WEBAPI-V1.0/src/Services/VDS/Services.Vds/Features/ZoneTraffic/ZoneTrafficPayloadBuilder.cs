using Shared.Utility.Counting;
using Services.Vds.Catalog;
using Services.Vds.Configuration;
using Services.Vds.Features.Counting;
using Services.Vds.Shared.Domain;
using Shared.Utility.Helpers;

namespace Services.Vds.Features.ZoneTraffic;

/// <summary>
/// Author: KienLT
/// Description: Dựng payload <c>vds.traffic/v2</c> — lưu lượng theo TỪNG VÙNG, kèm chart ECharts.
///
/// <b>Đây là hình dạng MỚI của subject <c>ta.its.data.vds.traffic</c>.</b> Bản v1 là bản tin từng lượt xe
/// (<c>kind: "vehicle"</c>, một bản tin mỗi xe); từ nay subject này chở số liệu đã gộp theo vùng. Bên nhận
/// phân biệt bằng trường <c>schema</c>: v1 KHÔNG có trường này, v2 luôn có.
///
/// <b>Vì sao không phát ngay trên đường ghi</b>: mỗi lượt phát là một truy vấn gộp trên QuestDB. Phát theo
/// từng lượt xe sẽ biến giờ cao điểm thành hàng chục truy vấn nặng mỗi giây và nghẽn queue ghi tuần tự —
/// đúng lý do <see cref="CountPublishWorker"/> tồn tại. Đường ghi chỉ đánh dấu vùng qua
/// <see cref="ZoneTrafficPublishTrigger"/>.
///
/// <b>Một nguồn số duy nhất</b>: mọi con số tính từ bảng thô <c>VdsTrafficData</c> qua
/// <see cref="QuestCountReader.ReadZoneSlicesAsync"/>, cùng câu truy vấn với bản tin đếm theo hướng nên hai
/// subject không thể ra số lệch nhau. Phép gộp gọi <see cref="TrafficCountMath"/> của
/// <c>Modules.VDS.Core.Counting</c> — WebAPI dùng cùng hàm.
///
/// <b>KHÔNG cộng các vùng lại để ra tổng tuyến</b>: một vùng là một đoạn Km, xe đi hết tuyến được đếm ở
/// từng vùng nó đi qua. Tổng theo hướng phải lấy từ subject <c>count</c> (điểm vào của hướng).
/// Created date: 27/08/2026
/// </summary>
public sealed class ZoneTrafficPayloadBuilder
{
    /// <summary>Phiên bản payload — bên nhận kiểm tra trường này trước khi đọc phần còn lại.</summary>
    public const string Schema = "vds.traffic/v2";

    /// <summary>Nguồn số liệu: bảng dữ liệu thô trên QuestDB.</summary>
    private const string SourceTable = "VdsTrafficData";

    private readonly QuestCountReader _reader;
    private readonly ZoneManager _zones;
    private readonly CameraManager _cameras;
    private readonly IVdsConfigManager _config;
    private readonly ConfigDataManager _configData;

    public ZoneTrafficPayloadBuilder(
        QuestCountReader reader,
        ZoneManager zones,
        CameraManager cameras,
        IVdsConfigManager config,
        ConfigDataManager configData)
    {
        _reader = reader;
        _zones = zones;
        _cameras = cameras;
        _config = config;
        _configData = configData;
    }

    /// <summary>Payload ĐẦY ĐỦ: mọi vùng có thiết bị VDS trong danh mục.</summary>
    public Task<object> BuildAsync(DateTime nowUtc, CancellationToken ct = default) =>
        BuildAsync(nowUtc, changedZoneIds: null, ct);

    /// <summary>
    /// Payload TĂNG DẦN: chỉ gồm các vùng vừa có xe đi qua.
    /// Bên nhận phân biệt qua <c>mode</c>: <c>snapshot</c> = thay cả danh sách, <c>delta</c> = ghép theo
    /// <c>zoneId</c>.
    /// </summary>
    public Task<object> BuildDeltaAsync(
        DateTime nowUtc, IReadOnlyCollection<string> changedZoneIds, CancellationToken ct = default) =>
        BuildAsync(nowUtc, changedZoneIds, ct);

    private async Task<object> BuildAsync(
        DateTime nowUtc, IReadOnlyCollection<string>? changedZoneIds, CancellationToken ct)
    {
        var runtime = _config.Current;
        var counting = runtime.Counting;
        var publish = runtime.Publish;
        var period = counting.EffectivePeriodMinutes;
        var isDelta = changedZoneIds is not null;

        var (last1hFrom, _) = TrafficCountMath.SlidingWindow(nowUtc, TimeSpan.FromHours(1), period);
        var (last24hFrom, _) = TrafficCountMath.SlidingWindow(nowUtc, TimeSpan.FromHours(24), period);
        var todayFrom = ServerDay.StartOfTodayUtc(nowUtc);
        var currentHour = TrafficCountMath.FloorHour(nowUtc);
        var currentPeriodEnd = TrafficCountMath.FloorPeriod(nowUtc, period).AddMinutes(period);

        // ĐỌC MỘT LẦN cho khoảng rộng nhất rồi lọc trong bộ nhớ: đọc nhiều lần thì mỗi lần là một thời
        // điểm khác nhau và các con số trong cùng payload không khớp nhau.
        var rangeFrom = new[] { last1hFrom, last24hFrom, todayFrom }.Min();
        var slices = await _reader
            .ReadZoneSlicesAsync(rangeFrom, currentPeriodEnd, ct)
            .ConfigureAwait(false);

        var byZone = slices
            .GroupBy(s => s.Scope.ScopeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Vùng nào có CAMERA VDS nào — tính một lần cho cả lượt dựng.
        var vdsByZone = VdsEquipmentByZone();

        var zones = SelectZones(vdsByZone, changedZoneIds)
            .Select(zoneId => BuildZone(
                zoneId, vdsByZone[zoneId], Slices(byZone, zoneId),
                last1hFrom, last24hFrom, todayFrom, currentHour, currentPeriodEnd,
                publish.TrafficIncludeChart))
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
            zones,
        };
    }

    /// <summary>
    /// Vùng → CAMERA VDS của vùng đó.
    ///
    /// <b>Phải lọc</b>: <c>TmsZoneEquipment</c> liên kết MỌI loại thiết bị với vùng (camera CCTV, biển VMS,
    /// đèn LCS…), nên map thô của <see cref="ZoneManager.EquipmentByZone"/> chứa cả thiết bị KHÔNG sinh số
    /// liệu giao thông. Không lọc thì <c>equipmentIds</c> trả về id mà bên nhận tra không ra camera nào, và
    /// những vùng chỉ có thiết bị khác vẫn xuất hiện với số 0 vĩnh viễn.
    ///
    /// Vùng không có camera VDS nào bị loại khỏi kết quả.
    /// </summary>
    private Dictionary<string, IReadOnlyList<string>> VdsEquipmentByZone()
    {
        var vds = new HashSet<string>(
            _cameras.All.Select(c => c.EquipmentId), StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (zoneId, equipment) in _zones.EquipmentByZone)
        {
            var mine = equipment.Where(vds.Contains).ToList();
            if (mine.Count > 0) result[zoneId] = mine;
        }

        return result;
    }

    /// <summary>
    /// Vùng nào xuất hiện trong payload: <b>delta</b> = đúng các vùng vừa có xe; <b>snapshot</b> = mọi vùng
    /// CÓ camera VDS (kể cả vùng đang vắng xe — bên nhận cần đường 0 chứ không phải vùng biến mất khỏi
    /// danh sách).
    ///
    /// Vùng không có camera VDS bị bỏ ở cả hai nhánh: không có gì sinh số cho nó.
    /// </summary>
    private static IEnumerable<string> SelectZones(
        Dictionary<string, IReadOnlyList<string>> vdsByZone,
        IReadOnlyCollection<string>? changedZoneIds)
    {
        if (changedZoneIds is null)
        {
            return vdsByZone.Keys.OrderBy(z => z, StringComparer.OrdinalIgnoreCase);
        }

        return changedZoneIds
            .Where(z => !string.IsNullOrWhiteSpace(z) && vdsByZone.ContainsKey(z))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(z => z, StringComparer.OrdinalIgnoreCase);
    }

    private object BuildZone(
        string zoneId,
        IReadOnlyList<string> equipmentIds,
        List<TrafficCountSlice> slices,
        DateTime last1hFrom,
        DateTime last24hFrom,
        DateTime todayFrom,
        DateTime currentHour,
        DateTime to,
        bool includeChart)
    {
        var latest = LatestSpeed(slices);

        // Gộp theo giờ MỘT LẦN rồi dùng cho cả flowByHour (số thô) và chart: tính hai lần là mở đường cho
        // hai trường lệch nhau trong cùng bản tin.
        var hours = TrafficCountMath.ByHour(slices, todayFrom, currentHour);

        return new
        {
            zoneId,
            // CHỈ camera VDS của vùng, lấy từ DANH MỤC nên vùng vắng xe vẫn có danh sách đầy đủ.
            equipmentIds,
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

    /// <summary>
    /// Khối ECharts ăn liền, cùng định dạng với chart của bản tin đếm và của
    /// <c>api/tms/TmsTrafficData/TrafficDataToDayChart</c> ⇒ FE vẽ bằng đúng đoạn code đã có.
    /// </summary>
    private EChartHelper.EChartResponse? Chart(List<TrafficCountHour> hours, bool includeChart) =>
        includeChart
            ? CountChartBuilder.Build(
                hours, code => _configData.NameOf(ConfigDataManager.VehicleSizeType, code))
            : null;

    /// <summary>
    /// Chuỗi theo GIỜ từ 00:00 hôm nay (giờ server) tới giờ hiện tại, BÙ 0 cho giờ không có xe.
    /// Cùng bộ khoá với <c>flowByHour</c> của bản tin đếm để bên nhận dùng một hàm đọc cho cả hai.
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
    /// tên PascalCase, lẫn với phần còn lại của payload thì bên nhận phải xử lý hai kiểu tên trong cùng một
    /// bản tin.
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
    /// Tốc độ của ô MỚI NHẤT có mẫu tốc độ. Hạt nhỏ nhất là một chu kỳ nên "mới nhất" là trung bình của
    /// chu kỳ gần nhất có xe, không phải tốc độ của lượt xe cuối.
    /// </summary>
    private static (double? Speed, DateTime? At) LatestSpeed(List<TrafficCountSlice> slices)
    {
        var latest = slices
            .Where(s => s.AvgSpeed is not null && s.SpeedSampleCount > 0)
            .OrderByDescending(s => s.From)
            .FirstOrDefault();

        return latest.AvgSpeed is null ? (null, null) : (latest.AvgSpeed, latest.From);
    }

    private static List<TrafficCountSlice> Slices(
        Dictionary<string, List<TrafficCountSlice>> byZone, string zoneId) =>
        byZone.TryGetValue(zoneId, out var list) ? list : new List<TrafficCountSlice>();
}
