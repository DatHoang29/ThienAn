namespace Modules.TMS.Controllers.IncidentAutomationRun.Dto;

/// <summary>
/// Author: SonTH
/// Description: DTO lấy run hiện tại (hoặc gần nhất) theo Incident, phục vụ hiển thị countdown/trạng thái.
/// Created date: 24/07/2026
/// </summary>
public class GetIncidentAutomationRunByIncidentInput
{
    public string IncidentId { get; set; }
}

/// <summary>
/// Author: SonTH
/// Description: DTO chạy ngay run đang WarningPending (bỏ qua countdown còn lại).
/// Created date: 24/07/2026
/// </summary>
public class ExecuteIncidentAutomationRunNowInput
{
    public string RunId { get; set; }
}

/// <summary>
/// Author: SonTH
/// Description: DTO hủy run tự động (WarningPending hoặc đang chờ auto-finish).
/// Created date: 24/07/2026
/// </summary>
public class CancelIncidentAutomationRunInput
{
    public string RunId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: DTO lập lịch tự động cho một sự cố ĐÃ TỒN TẠI — dành cho hệ thống ngoài tự ghi
///              <c>TmsIncident</c> rồi mới nhờ TMS xử lý tiếp (hiện là service VDS).
///
///              Khác <c>AddIncidentExternalInput</c>: DTO đó TẠO sự cố rồi lập lịch, còn ở đây sự cố đã
///              có sẵn. VDS cần đường này vì nó tự ghi sự cố với những thứ DTO tạo-mới không mang được:
///              <c>EventId</c> (khoá chống trùng của sự cố tự động), <c>Metadata</c> (nguồn thay token
///              <c>{VehiclePlate}</c>/<c>{VehicleSpeed}</c> vào nội dung VMS) và danh sách camera xem được.
/// Created date: 12/08/2026
/// </summary>
public class ScheduleIncidentAutomationInput
{
    /// <summary>Durable receipt per incident/trigger; a replay must not undo an operator cancellation.</summary>
    public bool OncePerIncident { get; set; }
    /// <summary><c>TmsIncident.ID</c> của sự cố cần lập lịch. Bắt buộc.</summary>
    public string? IncidentId { get; set; }

    /// <summary>
    /// State kích hoạt (<c>IncidentAutomationConst.TriggerState</c>): <c>register</c> hoặc <c>verify</c>.
    /// Để trống thì mặc định <c>register</c> — sự cố do thiết bị sinh ra đang ở trạng thái Đăng ký mới.
    ///
    /// Cấu hình tự động phải khai ĐÚNG state này (<c>TmsIncidentAutomationConfig.TriggerState</c>), lệch
    /// nhau thì trả về <c>NoConfig</c>.
    /// </summary>
    public string? TriggerState { get; set; }

    /// <summary>
    /// Định danh hệ thống gọi, dùng cho bản tin realtime. Để trống thì lấy <c>SYSTEM_AUTOMATION</c> —
    /// endpoint này chạy được cả khi không có người dùng đăng nhập nên không đọc <c>App.User</c>.
    /// </summary>
    public string? Actor { get; set; }
}



/// <summary>
/// Author: KienLT
/// Description: Message chuyển sự cố sang xử lý thủ công (hủy run tự động, giữ nguyên trạng thái sự cố).
///
///              Tách khỏi <see cref="CancelIncidentAutomationRunInput"/> vì Wolverine chỉ cho MỘT handler
///              cho mỗi kiểu message, trong khi <c>Cancel</c> và <c>ConvertToManual</c> khác nhau ở lý do mặc định.
///              Controller vẫn nhận <see cref="CancelIncidentAutomationRunInput"/> để request giữ nguyên shape,
///              rồi dựng message này bên trong. CỐ Ý không kế thừa <see cref="CancelIncidentAutomationRunInput"/>
///              để handler của Cancel không bắt nhầm message này theo kiểu cha.
/// Created date: 13/10/2026
/// </summary>
public class ConvertIncidentAutomationRunToManualInput
{
    public string RunId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Message lấy danh sách run đang chờ thực thi / chờ tự kết thúc cho banner cảnh báo.
///              Endpoint <c>GetActiveList</c> không có body; controller tự dựng message rỗng này.
/// Created date: 13/10/2026
/// </summary>
public class GetActiveIncidentAutomationRunListInput
{
}
