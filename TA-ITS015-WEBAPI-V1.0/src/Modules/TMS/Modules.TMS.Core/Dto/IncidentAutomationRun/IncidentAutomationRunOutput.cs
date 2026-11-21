using Modules.TMS.Core.Entities;

namespace Modules.TMS.Core.Dto.IncidentAutomationRun;

/// <summary>
/// Output trạng thái run để frontend hiển thị countdown/trạng thái.
/// </summary>
public class IncidentAutomationRunOutput : TmsIncidentAutomationRun
{
}

/// <summary>
/// Output run đang hoạt động kèm thông tin sự cố để hiển thị banner cảnh báo.
/// </summary>
public class IncidentAutomationRunViewOutput : TmsIncidentAutomationRun
{    /// <summary>Mã sự cố.</summary>
    public string? IncidentCode { get; set; }

    /// <summary>Tên sự cố.</summary>
    public string? IncidentName { get; set; }

    /// <summary>State hiện tại của sự cố.</summary>
    public string? IncidentState { get; set; }

    /// <summary>Thời điểm xảy ra sự cố.</summary>
    public DateTime? IncidentStartDate { get; set; }

    /// <summary>Vị trí Km của sự cố.</summary>
    public int? KmNumber { get; set; }

    /// <summary>Vị trí mét của sự cố.</summary>
    public int? MetNumber { get; set; }

    /// <summary>Tên loại sự kiện.</summary>
    public string? EventTypeName { get; set; }

    /// <summary>Đường dẫn icon loại sự kiện.</summary>
    public string? EventTypeUrl { get; set; }

    /// <summary>Tên kịch bản tự động sẽ được áp dụng.</summary>
    public string? TrafficPlanName { get; set; }

    /// <summary>
    /// Thời gian server hiện tại để frontend hiệu chỉnh countdown theo lệch giờ client.
    /// </summary>
    public DateTime ServerTime { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Kết quả một lần nhờ TMS lập lịch tự động cho sự cố ĐÃ TỒN TẠI (đường tích hợp).
///
///              Luôn trả về 200 kèm <see cref="ReasonCode"/> thay vì ném lỗi khi không lập lịch được:
///              phần lớn trường hợp "không lập lịch" là hợp lệ (loại sự kiện chưa bật tự động, nguồn
///              không được phép, đã có run đang chạy) và hệ thống ngoài KHÔNG nên thử lại vô ích.
/// Created date: 12/08/2026
/// </summary>
public class ScheduleIncidentAutomationOutput
{
    /// <summary>Sự cố được yêu cầu lập lịch.</summary>
    public string? IncidentId { get; set; }

    /// <summary>Đã tạo được lần chạy tự động hay chưa.</summary>
    public bool Scheduled { get; set; }

    /// <summary>Mã lý do — xem <c>IncidentAutomationConst.ScheduleReason</c>.</summary>
    public string? ReasonCode { get; set; }

    /// <summary><c>TmsIncidentAutomationRun.ID</c> nếu đã lập lịch.</summary>
    public string? RunId { get; set; }

    /// <summary>Trạng thái run vừa tạo (thường là <c>WarningPending</c>).</summary>
    public string? RunStatus { get; set; }

    /// <summary>Thời điểm dự kiến tự động thực thi (hết thời gian cảnh báo).</summary>
    public DateTime? ExecuteDueAt { get; set; }
}
