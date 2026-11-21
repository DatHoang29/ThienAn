namespace Modules.TMS.Controllers.Equipment.Dto;

/// <summary>
/// Author: ThangNN
/// Description: DTO chứa thông tin liên quan vị trí
/// </summary>
public class TmsEquipmentPositionInput
{
    public string? ZoneId { get; set; }
    public string? DirectionId { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
}

/// <summary>
/// Author: ThangNN
/// Description: DTO chứa thông tin danh sách zone và thiết bị
/// </summary>
public class TmsEquipmentZoneListInput
{
    public string[] ZoneIdList { get; set; }
    public string[] EquipmentIdList { get; set; }
    public string? ZoneType { get; set; }
    public string? EquipmentType { get; set; }
}

/// <summary>
/// Author: ThangNN
/// Description: DTO chứa thông tin vị trí và hướng và khoảng cách.
/// Note: Đây là lớp CƠ SỞ cho TmsEquipmentSuggestionInput, không còn endpoint riêng dùng trực tiếp
///       (endpoint ListByPositionAndDirection đã bỏ vì trùng nghiệp vụ với ListEquipmentSuggestion).
/// </summary>
public class TmsEquipmentPositionAndDirectionInput : TmsEquipmentPositionInput
{
    public int? SuggestionSmallRange { get; set; }
    public int? SuggestionLongRange { get; set; }
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Loại sự kiện sự cố (TmsEventType.ID) - dùng để tra ProcessType (Before/After) trong TmsEquipmentEventRule.
    /// </summary>
    public string? EventTypeId { get; set; }

    /// <summary>
    /// Tuyến của sự cố (TmsIncident.Location theo config tms_zone_location: main_route/second_route).
    /// Dùng để lọc zone theo đúng tuyến -> chỉ tích chọn sẵn thiết bị thuộc tuyến của sự cố.
    /// </summary>
    public string? Location { get; set; }
}

/// <summary>
/// Description: DTO chứa danh sách zoneId để lấy thiết bị qua bảng TmsZoneEquipment
/// </summary>
public class TmsEquipmentByZoneListInput
{
    public List<string> ZoneIds { get; set; } = new();
    public string? EquipmentTypeId { get; set; }
}

/// <summary>
/// Description: DTO gợi ý thiết bị nâng cao cho VMS/LCS.
///              Hỗ trợ "Mở rộng" (dùng SuggestionLongRange để hiện thêm ứng viên) và
///              "Toàn tuyến" (lấy thêm thiết bị trong khoảng [RouteStartPosition, RouteEndPosition]).
///              Luồng tích chọn sẵn (IsRecommended) vẫn giữ nguyên theo SuggestionSmallRange.
/// </summary>
public class TmsEquipmentSuggestionInput : TmsEquipmentPositionAndDirectionInput
{
    /// <summary>Bấm "Mở rộng": dùng SuggestionLongRange để hiện thêm ứng viên (không đổi tick tự động).</summary>
    public bool IsExpand { get; set; }

    /// <summary>Tick "Toàn tuyến": lấy thêm thiết bị trong khoảng [RouteStartPosition, RouteEndPosition].</summary>
    public bool IsWholeRoute { get; set; }

    /// <summary>Vị trí bắt đầu tuyến (đơn vị mét = km*1000+met). Lấy từ OpConfig, FE truyền xuống.</summary>
    public int? RouteStartPosition { get; set; }

    /// <summary>Vị trí kết thúc tuyến (đơn vị mét = km*1000+met). Lấy từ OpConfig, FE truyền xuống.</summary>
    public int? RouteEndPosition { get; set; }

    /// <summary>
    /// GHI ĐÈ loại xử lý Before/After cho riêng lần gọi này (giá trị: EquipmentEventRuleConst.ProcessType).
    ///
    /// - Luồng THỦ CÔNG: để trống -> handler tra toàn bộ rule Enable của cặp (EventTypeId + EquipmentTypeId),
    ///   nên nếu cấu hình cả Before và After thì thiết bị hai phía đều được tích.
    /// - Luồng TỰ ĐỘNG: truyền ProcessType của đúng rule gắn trên bước kịch bản
    ///   (TmsTrafficPlanWorkDetail.EquipmentEventRuleId), để một rule Before không bị rule After
    ///   của cùng cặp nới lỏng thành "cả hai phía".
    /// </summary>
    public string? ProcessType { get; set; }
}
