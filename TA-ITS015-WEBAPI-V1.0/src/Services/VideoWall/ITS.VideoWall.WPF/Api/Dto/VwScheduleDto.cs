using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Lịch bật/tắt kịch bản (VwSchedule). Time/Weekdays/OnceDate/CronExpr đều là chuỗi
/// tự do phía backend — client chỉ nhập và hiển thị, không tự suy diễn lịch.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwScheduleDto : VwEntityDto
{
    /// <summary>Tên lịch phát kịch bản.</summary>
    public string? Name { get; set; }

    /// <summary>ID kịch bản đích (TargetSceneId) được lên lịch kích hoạt.</summary>
    public string? TargetSceneId { get; set; }

    /// <summary>Loại lịch trình (theo ngày, theo tuần, một lần, cron...).</summary>
    public int? ScheduleType { get; set; }

    /// <summary>Giờ chạy dạng chuỗi, ví dụ "07:30".</summary>
    public string? Time { get; set; }

    /// <summary>Các ngày trong tuần dạng chuỗi, ví dụ "1,2,3,4,5".</summary>
    public string? Weekdays { get; set; }

    /// <summary>Ngày chạy duy nhất (OnceDate) dạng chuỗi (yyyy-MM-dd).</summary>
    public string? OnceDate { get; set; }

    /// <summary>Biểu thức Cron (CronExpr) định kỳ kích hoạt.</summary>
    public string? CronExpr { get; set; }

    /// <summary>Hành động thực thi khi kích hoạt lịch (ví dụ: activate, switch...).</summary>
    public string? Action { get; set; }

    /// <summary>Trạng thái hoạt động của lịch trình.</summary>
    public VwStatus? Status { get; set; }

    /// <summary>Thời điểm dự kiến chạy lần tiếp theo.</summary>
    public DateTime? NextRun { get; set; }

    /// <summary>Thời điểm thực thi lần gần nhất.</summary>
    public DateTime? LastRun { get; set; }

    /// <summary>Ghi chú bổ sung.</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Bộ lọc cho GET /api/videowall/vwschedule/list.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwScheduleFilter
{
    /// <summary>ID bản ghi lịch trình cần lọc.</summary>
    [JsonPropertyName("id")]
    public string? ID { get; set; }

    /// <summary>Tên lịch trình cần lọc.</summary>
    public string? Name { get; set; }

    /// <summary>ID kịch bản đích cần lọc.</summary>
    public string? TargetSceneId { get; set; }

    /// <summary>Hành động cần lọc.</summary>
    public string? Action { get; set; }

    /// <summary>Trạng thái hoạt động cần lọc.</summary>
    public VwStatus? Status { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Tham số phân trang lịch cho POST /api/videowall/vwschedule/page.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSchedulePageInput : PageInput
{
    /// <summary>Tên lịch trình phân trang cần lọc.</summary>
    public string? Name { get; set; }

    /// <summary>ID kịch bản đích phân trang cần lọc.</summary>
    public string? TargetSceneId { get; set; }

    /// <summary>Hành động phân trang cần lọc.</summary>
    public string? Action { get; set; }

    /// <summary>Trạng thái hoạt động phân trang cần lọc.</summary>
    public VwStatus? Status { get; set; }
}
