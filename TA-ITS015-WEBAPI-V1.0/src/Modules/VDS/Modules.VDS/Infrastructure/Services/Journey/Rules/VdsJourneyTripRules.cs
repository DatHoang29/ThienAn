using Modules.VDS.Controllers.Journey.Dto;
using Shared.Utility.Helpers;

namespace Modules.VDS.Infrastructure.Services.Journey.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật THUẦN gom các hành trình A→B (TmsTrafficJourney) thành CHUYẾN của xe, cho lưới gom nhóm và sơ đồ lộ
/// trình trang Tốc độ trung bình.
///
/// <para><b>Mở chuyến mới</b> (xét theo từng biển số, sắp theo <c>FromTime</c>) khi hành trình kế tiếp:</para>
/// <list type="number">
/// <item>bắt đầu sau khi chuyến kết thúc quá ngưỡng nghỉ (<c>vds_journey_trip_gap</c>, mặc định 2h);</item>
/// <item>QUAY LẠI: xuất hiện ở một camera đã đi qua ở thời điểm trước (không phải vị trí hiện tại), hoặc đi ngược dấu
/// Km so với chuyến trên cùng tuyến;</item>
/// <item>khác hướng — trừ đoạn RẼ nối đuôi (camera đầu đã có trong chuyến): chuyến được phép đổi tuyến qua nút giao.</item>
/// </list>
/// <para><b>Chuỗi chính</b>: worker sinh cả đoạn kề nhau lẫn đoạn "nhảy cóc" (A→C song song A→B, B→C). Chuỗi chính đi
/// từ đầu chuyến, mỗi bước chọn đoạn bắt đầu tại camera vừa tới, ưu tiên hợp lệ rồi NGẮN nhất; mất dấu giữa chừng thì
/// nối tiếp bằng đoạn sớm nhất bắt đầu sau đó. Tổng km/thời gian/tốc độ TB của chuyến tính trên chuỗi này.</para>
/// Created date: 21/10/2026
/// </summary>
public static class VdsJourneyTripRules
{
    public const int DefaultGapSeconds = 2 * 3600;
    public const int MinGapSeconds = 60;
    public const int MaxGapSeconds = 24 * 3600;

    /// <summary>Lệch thời gian coi như CÙNG một lần bắt gặp ở camera (đầu đoạn sau = cuối đoạn trước).</summary>
    private static readonly TimeSpan SameSighting = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Author: KienLT
    /// Description: Vị trí camera trên tuyến (mét) theo Topology.
    /// Created date: 21/10/2026
    /// </summary>
    public sealed record CameraPoint(string Route, int Pos);

    /// <summary>
    /// Author: KienLT
    /// Description: Ngưỡng nghỉ (giây) từ giá trị cấu hình; thiếu/sai/ngoài 1m..24h ⇒ <see cref="DefaultGapSeconds"/>.
    /// Created date: 21/10/2026
    /// </summary>
    public static int GapSeconds(string? raw)
        => BaseHelper.DurationHp.TryParseSeconds(raw) is int seconds && seconds is >= MinGapSeconds and <= MaxGapSeconds
            ? seconds : DefaultGapSeconds;

    /// <summary>
    /// Author: KienLT
    /// Description: Gom hành trình thành chuyến. <paramref name="cameras"/> khoá theo ID camera (không phân biệt hoa thường);
    /// <paramref name="turnSegmentIds"/> = ID các đoạn rẽ qua nút (Topology <c>Kind = T</c>). Kết quả sắp chuyến mới
    /// nhất trước.
    /// Created date: 21/10/2026
    /// </summary>
    public static List<JourneyTripOutput> Build(IEnumerable<PageJourneyOutput> journeys,
        IReadOnlyDictionary<string, CameraPoint> cameras, IReadOnlySet<string> turnSegmentIds, int gapSeconds)
    {
        var gap = TimeSpan.FromSeconds(gapSeconds);
        var trips = new List<JourneyTripOutput>();
        foreach (var plate in journeys.GroupBy(j => j.LicensePlate ?? string.Empty))
        {
            Trip? current = null;
            foreach (var journey in plate.OrderBy(j => j.FromTime ?? DateTime.MaxValue).ThenBy(j => j.ToTime ?? DateTime.MaxValue)
                         .ThenBy(j => j.ID, StringComparer.Ordinal))
            {
                if (current is null || StartsNewTrip(current, journey, cameras, turnSegmentIds, gap))
                {
                    if (current is not null) trips.Add(Summarize(current.Journeys));
                    current = new Trip();
                }
                current.Accept(journey, cameras, turnSegmentIds);
            }
            if (current is not null) trips.Add(Summarize(current.Journeys));
        }
        return trips.OrderByDescending(t => t.ToTime ?? DateTime.MinValue).ThenBy(t => t.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Chuỗi chính của một chuyến (xem mô tả lớp).
    /// Created date: 21/10/2026
    /// </summary>
    public static List<PageJourneyOutput> Chain(IReadOnlyList<PageJourneyOutput> journeys)
    {
        var pool = journeys.Where(j => j.FromTime.HasValue && j.ToTime.HasValue).ToList();
        var used = new HashSet<string>(StringComparer.Ordinal);
        var chain = new List<PageJourneyOutput>();
        PageJourneyOutput? current = null;
        while (true)
        {
            var candidates = pool.Where(j => !used.Contains(j.ID));
            PageJourneyOutput? next;
            if (current is null)
                next = Preferred(candidates.OrderBy(j => j.FromTime)).FirstOrDefault();
            else
            {
                var after = candidates.Where(j => j.FromTime >= current.ToTime!.Value - SameSighting).ToList();
                next = Preferred(after.Where(j => SameCamera(j.FromEquipmentId, current.ToEquipmentId)).OrderBy(j => 0)).FirstOrDefault()
                       ?? Preferred(after.OrderBy(j => j.FromTime)).FirstOrDefault();
            }
            if (next is null) break;
            chain.Add(next);
            used.Add(next.ID);
            current = next;
        }
        return chain;

        // Cùng thứ tự đã sắp, ưu tiên hợp lệ rồi đoạn ngắn nhất (đoạn kề nhau thay cho đoạn nhảy cóc).
        static IEnumerable<PageJourneyOutput> Preferred(IOrderedEnumerable<PageJourneyOutput> source)
            => source.ThenBy(j => j.IsValid == false).ThenBy(j => j.DistanceMeter ?? int.MaxValue).ThenBy(j => j.ID, StringComparer.Ordinal);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Dựng dòng tiêu đề chuyến từ các hành trình của nó.
    /// Created date: 21/10/2026
    /// </summary>
    public static JourneyTripOutput Summarize(IReadOnlyList<PageJourneyOutput> journeys)
    {
        var ordered = journeys.OrderBy(j => j.FromTime ?? DateTime.MaxValue).ThenBy(j => j.ID, StringComparer.Ordinal).ToList();
        var chain = Chain(ordered);
        var first = chain.FirstOrDefault() ?? ordered[0];
        var last = chain.LastOrDefault() ?? ordered[^1];
        // Số liệu tóm tắt chỉ lấy đoạn HỢP LỆ của chuỗi chính: đoạn không hợp lệ (ghép nhầm biển, nghỉ giữa đoạn) là số đo
        // sai, cộng vào sẽ kéo lệch TB của cả chuyến (ví dụ 260 km/h hoặc đi 1h40 cho 11 km). Không còn đoạn hợp lệ ⇒ dùng cả chuỗi.
        var measured = chain.Where(j => j.IsValid != false).ToList();
        if (measured.Count == 0) measured = chain;
        var distance = measured.Count == 0 || measured.All(j => j.DistanceMeter is null) ? (int?)null : measured.Sum(j => j.DistanceMeter ?? 0);
        var seconds = measured.Count == 0 || measured.All(j => j.TravelSeconds is null) ? (int?)null : measured.Sum(j => j.TravelSeconds ?? 0);
        return new JourneyTripOutput
        {
            Key = $"{ordered[0].LicensePlate}|{ordered[0].ID}",
            LicensePlate = ordered[0].LicensePlate,
            StartEquipmentId = first.FromEquipmentId,
            EndEquipmentId = last.ToEquipmentId,
            StartDirection = first.Direction,
            EndDirection = last.Direction,
            FromTime = ordered.Min(j => j.FromTime),
            ToTime = ordered.Max(j => j.ToTime),
            JourneyCount = ordered.Count,
            DistanceMeter = distance,
            TravelSeconds = seconds,
            AvgSpeedKmh = distance is > 0 && seconds is > 0 ? Math.Round(distance.Value / (double)seconds.Value * 3.6, 1) : null,
            MaxSpeedKmh = measured.Max(j => j.AvgSpeedKmh),
            HasOverSpeed = chain.Any(j => j.IsOverSpeed == true),
            HasInvalid = ordered.Any(j => j.IsValid == false),
            ChainJourneyIds = chain.Select(j => j.ID).ToList(),
            Journeys = ordered,
        };
    }

    private static bool StartsNewTrip(Trip trip, PageJourneyOutput next, IReadOnlyDictionary<string, CameraPoint> cameras,
        IReadOnlySet<string> turnSegmentIds, TimeSpan gap)
    {
        // Hành trình thiếu mốc thời gian không ghép được với ai: đứng riêng một chuyến.
        if (next.FromTime is null || next.ToTime is null || trip.MaxToTime is null) return true;
        // 1. Nghỉ quá ngưỡng.
        if (next.FromTime.Value - trip.MaxToTime.Value > gap) return true;

        var from = Normalize(next.FromEquipmentId);
        // 2a. Quay lại một camera đã qua ở thời điểm trước, không phải vị trí hiện tại của xe.
        if (from != trip.Position && trip.Seen.TryGetValue(from, out var seenAt) && next.FromTime.Value - seenAt > SameSighting) return true;
        // 2a'. Đi TỚI một camera đã qua ở thời điểm trước (ví dụ VDS-01→03→05 rồi VDS-07→03): xe đã quay lại.
        // Đoạn nhảy cóc song song tới cùng camera ở CÙNG lần bắt gặp không tính (lệch ≤ SameSighting).
        var to = Normalize(next.ToEquipmentId);
        if (trip.Seen.TryGetValue(to, out var seenTo) && next.ToTime.Value - seenTo > SameSighting) return true;
        // 2b. Ngược dấu Km so với chuyến trên cùng tuyến.
        if (StraightSign(next, cameras, turnSegmentIds) is { } leg && trip.RouteSign.TryGetValue(leg.Route, out var sign) && sign != leg.Sign)
            return true;
        // 3. Khác hướng — đoạn rẽ nối đuôi vẫn cùng chuyến.
        if (!string.IsNullOrWhiteSpace(next.Direction) && !string.IsNullOrWhiteSpace(trip.Direction)
            && !string.Equals(next.Direction.Trim(), trip.Direction.Trim(), StringComparison.OrdinalIgnoreCase))
            return !(IsTurn(next, cameras, turnSegmentIds) && trip.Seen.ContainsKey(from));
        return false;
    }

    private static bool IsTurn(PageJourneyOutput journey, IReadOnlyDictionary<string, CameraPoint> cameras, IReadOnlySet<string> turnSegmentIds)
    {
        if (journey.SegmentId is { } id && turnSegmentIds.Contains(id)) return true;
        return cameras.TryGetValue(Normalize(journey.FromEquipmentId), out var a) && cameras.TryGetValue(Normalize(journey.ToEquipmentId), out var b)
               && !string.Equals(a.Route, b.Route, StringComparison.Ordinal);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Dấu Km của một đoạn đi thẳng (cùng tuyến); null khi là đoạn rẽ hoặc thiếu vị trí camera.
    /// Created date: 21/10/2026
    /// </summary>
    private static (string Route, int Sign)? StraightSign(PageJourneyOutput journey, IReadOnlyDictionary<string, CameraPoint> cameras,
        IReadOnlySet<string> turnSegmentIds)
    {
        if (IsTurn(journey, cameras, turnSegmentIds)) return null;
        if (!cameras.TryGetValue(Normalize(journey.FromEquipmentId), out var a) || !cameras.TryGetValue(Normalize(journey.ToEquipmentId), out var b))
            return null;
        var sign = Math.Sign(b.Pos - a.Pos);
        return sign == 0 ? null : (a.Route, sign);
    }

    private static bool SameCamera(string? a, string? b) => Normalize(a) == Normalize(b) && !string.IsNullOrEmpty(Normalize(a));
    private static string Normalize(string? id) => id?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>
    /// Author: KienLT
    /// Description: Trạng thái chuyến đang gom.
    /// Created date: 21/10/2026
    /// </summary>
    private sealed class Trip
    {
        public List<PageJourneyOutput> Journeys { get; } = [];
        public DateTime? MaxToTime { get; private set; }
        /// <summary>Camera xe đang đứng = camera cuối của hành trình kết thúc muộn nhất.</summary>
        public string Position { get; private set; } = string.Empty;
        public string? Direction { get; private set; }
        /// <summary>Camera → lần bắt gặp muộn nhất trong chuyến.</summary>
        public Dictionary<string, DateTime> Seen { get; } = new(StringComparer.Ordinal);
        /// <summary>Tuyến → dấu Km của chuyến trên tuyến đó (từ đoạn đi thẳng đầu tiên).</summary>
        public Dictionary<string, int> RouteSign { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Author: KienLT
        /// Description: Thêm hành trình và cập nhật vị trí, hướng cùng các mốc camera của chuyến.
        /// Created date: 21/10/2026
        /// </summary>
        public void Accept(PageJourneyOutput journey, IReadOnlyDictionary<string, CameraPoint> cameras, IReadOnlySet<string> turnSegmentIds)
        {
            Journeys.Add(journey);
            if (!string.IsNullOrWhiteSpace(journey.Direction)) Direction = journey.Direction;
            if (journey.FromTime is { } fromTime) Mark(Normalize(journey.FromEquipmentId), fromTime);
            if (journey.ToTime is { } toTime)
            {
                Mark(Normalize(journey.ToEquipmentId), toTime);
                if (MaxToTime is null || toTime >= MaxToTime)
                {
                    MaxToTime = toTime;
                    Position = Normalize(journey.ToEquipmentId);
                }
            }
            if (StraightSign(journey, cameras, turnSegmentIds) is { } leg) RouteSign.TryAdd(leg.Route, leg.Sign);
        }

        private void Mark(string camera, DateTime at)
        {
            if (camera.Length == 0) return;
            if (!Seen.TryGetValue(camera, out var seen) || at > seen) Seen[camera] = at;
        }
    }
}
