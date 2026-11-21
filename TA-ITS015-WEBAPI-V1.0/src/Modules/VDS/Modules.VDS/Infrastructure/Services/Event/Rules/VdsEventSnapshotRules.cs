using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.Event.Dto;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Modules.VDS.Infrastructure.Services.Config.Rules;
using Shared.Utility.Helpers;
using System.Globalization;

namespace Modules.VDS.Infrastructure.Services.Event.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật THUẦN của snapshot sự kiện VDS — không I/O để kiểm chứng trực tiếp (tests/VdsEventSse).
/// Handler chỉ đọc dữ liệu rồi giao cho các hàm ở đây quyết định loại nào lên, gom thế nào, hết hạn khi nào.
/// Created date: 19/10/2026
/// </summary>
public static class VdsEventSnapshotRules
{
    /// <summary>
    /// Author: KienLT
    /// Description: Loại TMS nhóm event lên snapshot, kèm thời gian hiển thị đã phân giải.
    /// Created date: 19/10/2026
    /// </summary>
    public sealed record SnapshotType(string Code, string? Name, string? Icon, string? WarningType, int? Priority, int DisplaySeconds);

    /// <summary>
    /// Author: KienLT
    /// Description: Một dòng danh mục <c>TmsEventType</c> (đầu vào của <see cref="BuildTypes"/>).
    /// Created date: 19/10/2026
    /// </summary>
    public sealed record CatalogRow(string? Code, string? Name, string? Type, string? Icon, string? WarningType, int? Priority);

    /// <summary>
    /// Author: KienLT
    /// Description: Một dòng <c>VdsConfig</c> thời gian hiển thị (nhóm <c>vds_event_display</c> hoặc mã mặc định).
    /// Created date: 19/10/2026
    /// </summary>
    public sealed record DisplayRow(string? GroupCode, string? Code, string? Value);

    /// <summary>Thời gian hiển thị khi chưa khai <see cref="VdsConfigConst.Code.EventDisplayDefault"/>.</summary>
    public const int DefaultDisplaySeconds = 60;

    /// <summary>Trần số item trả về để snapshot không phình.</summary>
    public const int MaxItems = 500;

    public const string SourceAid = "aid";
    public const string SourceJourney = "journey";

    /// <summary>
    /// Author: KienLT
    /// Description: Giá trị cấu hình không hợp lệ hoặc ngoài 5s..1h thì bỏ, để rơi về mặc định thay vì làm hỏng snapshot.
    /// Created date: 19/10/2026
    /// </summary>
    public static int? DisplaySeconds(string? raw)
        => BaseHelper.DurationHp.TryParseSeconds(raw) is int seconds
           && seconds is >= VdsConfigRules.MinEventDisplaySeconds and <= VdsConfigRules.MaxEventDisplaySeconds
            ? seconds
            : null;

    /// <summary>
    /// Author: KienLT
    /// Description: Loại nhóm event (trừ unknown) lên snapshot. Thời gian hiển thị: theo loại → mặc định → <see cref="DefaultDisplaySeconds"/>.
    /// Danh mục truyền vào phải là các loại ĐANG BẬT. Khoá so không phân biệt hoa thường.
    /// Created date: 19/10/2026
    /// </summary>
    public static Dictionary<string, SnapshotType> BuildTypes(
        IEnumerable<CatalogRow> catalog, IEnumerable<DisplayRow> display)
    {
        var displayRows = display.ToList();
        var fallback = DisplaySeconds(displayRows.FirstOrDefault(x => x.GroupCode == VdsConfigConst.Group.ConfigDefault
                                                                      && x.Code == VdsConfigConst.Code.EventDisplayDefault)?.Value)
                       ?? DefaultDisplaySeconds;
        var perType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in displayRows.Where(x => x.GroupCode == VdsConfigConst.Group.EventDisplay && !string.IsNullOrWhiteSpace(x.Code)))
            if (DisplaySeconds(row.Value) is int seconds) perType[row.Code!.Trim()] = seconds;

        var result = new Dictionary<string, SnapshotType>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in catalog)
        {
            var code = row.Code?.Trim();
            if (string.IsNullOrEmpty(code) || IncidentEventGroupConst.Normalize(row.Type) != IncidentEventGroupConst.Event
                || string.Equals(code, IncidentEventTypeConst.Unknown, StringComparison.OrdinalIgnoreCase)) continue;
            result[code] = new SnapshotType(code, row.Name, row.Icon, row.WarningType, row.Priority,
                perType.TryGetValue(code, out var seconds) ? seconds : fallback);
        }
        return result;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Mã cần đưa vào <c>IN (...)</c> của QuestDB, gom theo thời gian hiển thị để mỗi nhóm có cận <c>detectTime</c>
    /// riêng — một loại ồn với cửa sổ ngắn không chiếm trần dòng của loại có cửa sổ dài.
    ///
    /// <para>QuestDB so SYMBOL PHÂN BIỆT hoa thường, còn worker lưu nguyên chuỗi ánh xạ: thêm mọi cách viết của mã
    /// trong <paramref name="storedSpellings"/> (giá trị <c>vds_mapping_event_type</c>) khớp không phân biệt hoa thường.</para>
    /// Created date: 19/10/2026
    /// </summary>
    public static List<(int DisplaySeconds, string[] Codes)> QueryWindows(
        IReadOnlyDictionary<string, SnapshotType> types, IEnumerable<string?> storedSpellings)
    {
        var spellings = storedSpellings.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).ToList();
        return types.Values
            .GroupBy(x => x.DisplaySeconds)
            .OrderBy(x => x.Key)
            .Select(g => (g.Key, g
                .SelectMany(t => spellings.Where(s => string.Equals(s, t.Code, StringComparison.OrdinalIgnoreCase)).Prepend(t.Code))
                .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray()))
            .ToList();
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Bản tin AID: gom theo (camera, loại, biển số chuẩn hoá), giữ bản mới nhất. Dòng ngoài cửa sổ hiển thị của
    /// loại mình, hoặc loại không lên snapshot, bị bỏ. Khoá ổn định suốt cửa sổ: <c>aid|camera|mã|BIỂN</c>.
    /// Created date: 19/10/2026
    /// </summary>
    public static List<EventSnapshotItemOutput> GroupAid(
        IEnumerable<VdsTrafficData> rows, IReadOnlyDictionary<string, SnapshotType> types, DateTime nowUtc)
    {
        var items = new List<EventSnapshotItemOutput>();
        foreach (var group in rows
                     .Where(x => x.EventType != null && types.TryGetValue(x.EventType.Trim(), out var t)
                                 && x.DetectTime >= nowUtc.AddSeconds(-t.DisplaySeconds))
                     .GroupBy(x => (x.EquipmentId ?? "", types[x.EventType!.Trim()].Code, NormalizePlate(x.LicensePlate))))
        {
            var latest = group.MaxBy(x => x.DetectTime)!;
            var type = types[group.Key.Code];
            items.Add(new EventSnapshotItemOutput
            {
                Key = $"{SourceAid}|{group.Key.Item1}|{type.Code}|{group.Key.Item3}",
                Source = SourceAid,
                VendorEventId = latest.VendorEventId.ToString(CultureInfo.InvariantCulture),
                DetectTime = latest.DetectTime,
                EventTypeCode = type.Code,
                EventTypeName = type.Name,
                Icon = type.Icon,
                WarningType = type.WarningType,
                Priority = type.Priority,
                AidEventType = latest.AidEventType,
                EquipmentId = latest.EquipmentId,
                InfluenceScope = latest.Direction,
                KmNumber = latest.KmNumber,
                MetNumber = latest.MetNumber,
                Lane = latest.Lane,
                LicensePlate = latest.LicensePlate,
                PlateColor = latest.PlateColor,
                Speed = latest.Speed,
                SpeedMax = latest.SpeedMax,
                SpeedMin = latest.SpeedMin,
                Count = group.Count(),
                FirstDetectTime = group.Min(x => x.DetectTime),
                LastDetectTime = latest.DetectTime,
                DisplayExpiresAt = latest.DetectTime.AddSeconds(type.DisplaySeconds),
            });
        }
        return items;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Hành trình vượt tốc độ trung bình → item. <paramref name="imageSource"/> là bản tin điểm B (nếu tìm thấy) để FE
    /// tải ảnh qua <c>vdsMedia/File</c> như bản tin AID. Khoá: <c>journey|đoạn|biển|FromTime</c>.
    /// Created date: 19/10/2026
    /// </summary>
    public static EventSnapshotItemOutput JourneyItem(TmsTrafficJourney x, SnapshotType type, VdsTrafficData? imageSource = null)
    {
        var toTime = x.ToTime!.Value;
        return new EventSnapshotItemOutput
        {
            Key = $"{SourceJourney}|{x.SegmentId}|{x.LicensePlate}|{x.FromTime:O}",
            Source = SourceJourney,
            VendorEventId = imageSource?.VendorEventId.ToString(CultureInfo.InvariantCulture),
            DetectTime = imageSource?.DetectTime ?? toTime,
            JourneyId = x.ID,
            AvgSpeedKmh = x.AvgSpeedKmh,
            FromEquipmentId = x.FromEquipmentId,
            DistanceMeter = x.DistanceMeter,
            TravelSeconds = x.TravelSeconds,
            EventTypeCode = type.Code,
            EventTypeName = type.Name,
            Icon = type.Icon,
            WarningType = type.WarningType,
            Priority = type.Priority,
            AidEventType = VdsJourneyEventConst.EventTypeName,
            EquipmentId = x.ToEquipmentId,
            InfluenceScope = x.Direction,
            LicensePlate = x.LicensePlate,
            PlateColor = imageSource?.PlateColor,
            SpeedMax = x.SpeedLimitKmh,
            Count = 1,
            FirstDetectTime = x.FromTime ?? toTime,
            LastDetectTime = toTime,
            DisplayExpiresAt = toTime.AddSeconds(type.DisplaySeconds),
        };
    }

    /// <summary>
    /// Sai số khi so mốc hành trình với bản tin thô: cột <c>TmsTrafficJourney.ToTime</c> kiểu <c>datetime</c> của SQL
    /// Server làm tròn theo 1/300 giây (lệch tới ~1,7 ms so với QuestDB, ví dụ .816 → .817).
    /// </summary>
    public static readonly TimeSpan JourneyTimeTolerance = TimeSpan.FromMilliseconds(4);

    /// <summary>
    /// Author: KienLT
    /// Description: Bản tin điểm B của hành trình: cùng camera, cùng mốc (trong <see cref="JourneyTimeTolerance"/>), cùng biển chuẩn hoá.
    /// Created date: 19/10/2026
    /// </summary>
    public static VdsTrafficData? MatchImageSource(TmsTrafficJourney journey, IEnumerable<VdsTrafficData> candidates)
    {
        if (journey.ToTime is not DateTime toTime) return null;
        var plate = NormalizePlate(journey.LicensePlate);
        var matches = candidates.Where(r => string.Equals(r.EquipmentId, journey.ToEquipmentId, StringComparison.OrdinalIgnoreCase)
                                            && (r.DetectTime - toTime).Duration() <= JourneyTimeTolerance
                                            && NormalizePlate(r.LicensePlate) == plate).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Sắp mới nhất trước (hoà thì theo khoá) và cắt trần <see cref="MaxItems"/>.
    /// Created date: 19/10/2026
    /// </summary>
    public static List<EventSnapshotItemOutput> Finalize(IEnumerable<EventSnapshotItemOutput> items)
        => items.OrderByDescending(x => x.LastDetectTime).ThenBy(x => x.Key, StringComparer.Ordinal).Take(MaxItems).ToList();

    /// <summary>
    /// Author: KienLT
    /// Description: Biển số so khớp: bỏ khoảng trắng/ký tự phân cách, viết hoa.
    /// Created date: 19/10/2026
    /// </summary>
    public static string NormalizePlate(string? plate)
        => plate is null ? "" : new string(plate.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
