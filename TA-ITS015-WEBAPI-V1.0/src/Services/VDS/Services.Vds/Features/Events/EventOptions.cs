using Services.Vds.Shared.Domain;

namespace Services.Vds.Features.Events;

/// <summary>
/// Author: KienLT
/// Description: Sinh sự kiện và nâng sự cố (section <c>"Service:Events"</c>).
///
/// Các bộ sưu tập bind từ file nên có comparer MẶC ĐỊNH (phân biệt hoa thường). Mọi tra cứu theo loại bản
/// tin đều đi qua các method dưới đây; chúng dựng bản sao <c>OrdinalIgnoreCase</c> một lần rồi dùng lại, và
/// quy tên loại về bộ mã hiện hành — nhờ vậy cấu hình còn ghi tên cũ vẫn khớp bản tin mới.
/// Snapshot mới luôn là instance mới (<see cref="Clone"/>) nên cache tự reset.
/// Created date: 26/08/2026
/// </summary>
public sealed class EventOptions
{
    private HashSet<string>? _checkPlateSet;
    private Dictionary<string, int>? _dedupWindow;
    private Dictionary<string, int>? _incidentSkip;
    private Dictionary<string, int>? _skipWindow;
    private Dictionary<string, string>? _eventTypeCodes;

    /// <summary>
    /// Nâng sự kiện thành sự cố trên SQL Server.
    ///
    /// Tắt để chạy song song với pipeline cũ: hai bên cùng bật thì cùng một sự việc sẽ sinh HAI sự cố, vì
    /// cửa chống trùng của mỗi bên nằm trong tiến trình của nó.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Cửa sổ gộp mặc định khi loại bản tin không khai riêng (giây).</summary>
    public int DedupWindowSecondsDefault { get; set; } = 30;

    /// <summary>Cửa sổ gộp theo từng loại bản tin (giây).</summary>
    public Dictionary<string, int> DedupWindowSeconds { get; set; } = new();

    /// <summary>
    /// Cửa SKIP (ghi thô) theo từng loại bản tin, GIÂY. Nguồn: <c>SysConfig</c> danh mục
    /// <c>vds_event_skip</c> (con: Code=loại, Value=thời lượng có hậu tố).
    ///
    /// Trong cửa sổ này + trùng khoá ⇒ KHÔNG ghi bản thô. Loại không khai ⇒ dùng
    /// <see cref="SkipWindowSecondsDefault"/>. Giá trị <c>0</c> ⇒ TẮT cửa cho loại đó.
    ///
    /// <b>Phải đặt NGẮN cho loại có biển</b> (Normal/Speeding, 5–10s): cửa này chặn việc ghi bảng thô, mà số
    /// đếm xe tính lại từ bảng thô — đặt dài sẽ làm HỤT số đếm.
    /// </summary>
    public Dictionary<string, int> SkipWindowSeconds { get; set; } = new();

    /// <summary>Cửa SKIP mặc định khi loại không khai riêng (giây). Nguồn: <c>vds_skip_default</c>.</summary>
    public int SkipWindowSecondsDefault { get; set; } = 10;

    /// <summary>
    /// Danh sách loại bản tin CÓ so BIỂN SỐ khi lọc trùng. Nguồn: <c>SysConfig</c> danh mục
    /// <c>vds_event_check_plate</c> (con chỉ có Code, KHÔNG có thời lượng).
    ///
    /// Loại trong danh sách ⇒ khoá lọc = (camera + loại + biển). Loại ngoài danh sách ⇒ khoá lọc =
    /// (camera + loại), không xét biển.
    /// </summary>
    public List<string> CheckPlateTypes { get; set; } = new();

    /// <summary>
    /// Chống trùng sự cố theo VỊ TRÍ (km + met + loại), giây. Thiếu thì dùng cửa sổ gộp tương ứng.
    ///
    /// Đây là cửa thứ hai, độc lập với cửa sổ theo <c>ConditionHash</c>: hai camera khác nhau tại cùng một
    /// điểm vẫn là một sự việc ngoài đường, không phải hai.
    /// </summary>
    public Dictionary<string, int> IncidentSkipSeconds { get; set; } = new();

    /// <summary>
    /// Map loại bản tin của thiết bị sang <c>TmsEventType.Code</c> có sẵn trong danh mục. Không khai ⇒ bản ghi thô
    /// lưu <c>eventType = unknown</c> và không tạo hồ sơ.
    ///
    /// Chỉ loại TMS nhóm incident mới ghi <c>TmsIncident</c>; nhóm event chỉ hiển thị qua SSE sự kiện.
    /// </summary>
    public Dictionary<string, string> EventTypeCodes { get; set; } = new();

    /// <summary>
    /// Thời gian nhớ kết quả tra danh mục THẤT BẠI (giây). Không nhớ vĩnh viễn để danh mục seed sau lúc
    /// service đã chạy vẫn có hiệu lực mà không phải restart.
    /// </summary>
    public int EventTypeLookupMissTtlSeconds { get; set; } = 60;

    /// <summary>Cửa sổ gộp theo loại bản tin; thiếu thì dùng mặc định.</summary>
    public int DedupWindowFor(string? eventType)
    {
        _dedupWindow ??= TmsCodeMap(DedupWindowSeconds);
        return _dedupWindow.TryGetValue(WindowCode(eventType), out var value)
            ? value
            : DedupWindowSecondsDefault;
    }

    /// <summary>Cửa sổ chống trùng theo vị trí (giây); thiếu thì dùng cửa sổ gộp tương ứng.</summary>
    public int IncidentSkipFor(string? eventType)
    {
        _incidentSkip ??= CanonicalMap(IncidentSkipSeconds);
        return _incidentSkip.TryGetValue(VdsEventTypes.Canonical(eventType), out var value)
            ? value
            : DedupWindowFor(eventType);
    }

    /// <summary>
    /// Cửa SKIP (ghi thô) theo loại, giây; loại không khai ⇒ <see cref="SkipWindowSecondsDefault"/>.
    /// </summary>
    public int SkipWindowFor(string? eventType)
    {
        _skipWindow ??= TmsCodeMap(SkipWindowSeconds);
        return _skipWindow.TryGetValue(WindowCode(eventType), out var value)
            ? value
            : SkipWindowSecondsDefault;
    }

    /// <summary>Loại này có so BIỂN SỐ khi lọc trùng không.</summary>
    public bool IsCheckPlateType(string? eventType)
    {
        _checkPlateSet ??= CanonicalSet(CheckPlateTypes);
        return _checkPlateSet.Contains(VdsEventTypes.Canonical(eventType));
    }

    /// <summary><c>TmsEventType.Code</c> khai trong cấu hình cho một loại bản tin; null nếu chưa map.</summary>
    public string? MappedEventTypeCode(string? eventType)
    {
        _eventTypeCodes ??= CanonicalMap(EventTypeCodes);
        return _eventTypeCodes.TryGetValue(VdsEventTypes.Canonical(eventType), out var value)
               && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    // Window configuration keys belong to TMS; never canonicalize them as AID aliases.
    private string WindowCode(string? aidType) => MappedEventTypeCode(aidType) ?? (aidType ?? "").Trim();
    private static Dictionary<string, int> TmsCodeMap(Dictionary<string, int> source)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source) result[item.Key.Trim()] = item.Value;
        return result;
    }
    private static HashSet<string> CanonicalSet(IEnumerable<string> names)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var canonical = VdsEventTypes.Canonical(name);
            if (canonical.Length > 0) set.Add(canonical);
        }

        return set;
    }

    /// <summary>
    /// Như <see cref="CanonicalSet"/> nhưng cho map. Dùng phép GÁN chứ không <c>Add</c>: cấu hình hoàn toàn
    /// có thể khai cả tên cũ lẫn tên mới của cùng một loại, khi đó <c>Add</c> sẽ ném lỗi trùng khoá và làm
    /// service không khởi động được.
    /// </summary>
    private static Dictionary<string, T> CanonicalMap<T>(IEnumerable<KeyValuePair<string, T>> source)
    {
        var map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            var canonical = VdsEventTypes.Canonical(pair.Key);
            if (canonical.Length > 0) map[canonical] = pair.Value;
        }

        return map;
    }

    public EventOptions Clone() => new()
    {
        Enabled = Enabled,
        DedupWindowSecondsDefault = DedupWindowSecondsDefault,
        DedupWindowSeconds = new Dictionary<string, int>(DedupWindowSeconds),
        SkipWindowSeconds = new Dictionary<string, int>(SkipWindowSeconds),
        SkipWindowSecondsDefault = SkipWindowSecondsDefault,
        CheckPlateTypes = new List<string>(CheckPlateTypes),
        IncidentSkipSeconds = new Dictionary<string, int>(IncidentSkipSeconds),
        EventTypeCodes = new Dictionary<string, string>(EventTypeCodes),
        EventTypeLookupMissTtlSeconds = EventTypeLookupMissTtlSeconds,
    };
}
