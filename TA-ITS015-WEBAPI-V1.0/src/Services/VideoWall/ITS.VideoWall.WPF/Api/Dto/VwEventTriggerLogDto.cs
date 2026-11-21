using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Body của POST /api/videowall/vweventtriggerlog/page — lọc nhật ký thao tác thiết bị của
/// các phiên đấu nối TRƯỚC ĐÓ (log của phiên đang chạy đã có sẵn qua ActivityNotification /
/// DeviceStepNotification, không cần gọi API).
/// Created date: 26/08/2026
/// </summary>
public sealed class VwEventTriggerLogPageInput : PageInput
{
    /// <summary>ID quy tắc kích hoạt cần lọc.</summary>
    public string? RuleId { get; set; }

    /// <summary>ID loại sự kiện cần lọc.</summary>
    public string? EventTypeId { get; set; }

    /// <summary>ID bộ điều khiển cần lọc.</summary>
    public string? ControllerId { get; set; }

    /// <summary>ID kịch bản cần lọc.</summary>
    public string? SceneId { get; set; }

    /// <summary>Mã kịch bản cần lọc.</summary>
    public string? SceneCode { get; set; }

    /// <summary>Ping | Probe | SyncSources | SetupScene | Isapi | ActiveScene.</summary>
    public string? Action { get; set; }

    /// <summary>Lọc riêng các bước chạy thử (true) hoặc riêng các bước đã thật sự gửi lệnh (false).</summary>
    public bool? Skipped { get; set; }

    /// <summary>Thời điểm bắt đầu khoảng thời gian cần lọc.</summary>
    public DateTime? TriggeredAtFrom { get; set; }

    /// <summary>Thời điểm kết thúc khoảng thời gian cần lọc.</summary>
    public DateTime? TriggeredAtTo { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Một dòng nhật ký kích hoạt kịch bản / thao tác thiết bị đọc từ CSDL (VwEventTriggerLog).
///
/// MỘT DÒNG = MỘT BƯỚC ISAPI hoặc MỘT LẦN KÍCH HOẠT, nên hình dạng gần trùng <see cref="VwSetupSceneStep"/>. Không tái dùng
/// luôn DTO đó vì bản ghi DB còn mang thêm phần định danh phiên (thời điểm, người thao tác, bộ điều
/// khiển, kịch bản) mà một bước trả về trực tiếp từ API không có.
/// Created date: 26/08/2026
/// </summary>
public sealed class VwEventTriggerLogDto : VwEntityDto
{
    /// <summary>ID quy tắc kích hoạt sự kiện.</summary>
    public string? RuleId { get; set; }

    /// <summary>ID loại sự kiện kích hoạt.</summary>
    public string? EventTypeId { get; set; }

    /// <summary>Hành động thực thi (Ping, Probe, SyncSources, SetupScene, Isapi...).</summary>
    public string? Action { get; set; }

    /// <summary>ID bộ điều khiển tiếp nhận thao tác.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Tên bộ điều khiển tiếp nhận thao tác.</summary>
    public string? ControllerName { get; set; }

    /// <summary>ID kịch bản liên quan đến thao tác — backend trả là "targetSceneId" (VwEventTriggerLog.TargetSceneId).</summary>
    [JsonPropertyName("targetSceneId")]
    public string? SceneId { get; set; }

    /// <summary>Mã kịch bản liên quan đến thao tác.</summary>
    public string? SceneCode { get; set; }

    /// <summary>Thứ tự bước trong chuỗi thao tác kịch bản.</summary>
    public int? StepOrder { get; set; }

    /// <summary>Tên mô tả bước thao tác.</summary>
    public string? StepName { get; set; }

    /// <summary>Phương thức HTTP gửi đến thiết bị (GET, PUT, POST...).</summary>
    public string? Method { get; set; }

    /// <summary>Đường dẫn API endpoint trên thiết bị.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Bước bị bỏ qua (chạy thử) — không có lượt HTTP nào xuống thiết bị.</summary>
    public bool? Skipped { get; set; }

    /// <summary>
    /// Enum BaseEnums.SuccessEnums của backend, trả về dạng số; rỗng với bước bị bỏ qua.
    /// CẨN THẬN: Success = 1 và Fail = 0 — ngược với trực giác "0 là OK".
    /// </summary>
    public int? Success { get; set; }

    /// <summary>Mã trạng thái HTTP trả về từ thiết bị.</summary>
    public int? HttpStatus { get; set; }

    /// <summary>Thông điệp chi tiết hoặc lỗi từ thiết bị.</summary>
    public string? Message { get; set; }

    /// <summary>Nội dung gói tin gửi đi (XML/JSON).</summary>
    public string? RequestPayload { get; set; }

    /// <summary>Nội dung gói tin nhận về (XML/JSON).</summary>
    public string? ResponsePayload { get; set; }

    /// <summary>Thời gian thực thi thao tác tính bằng mili-giây (ms).</summary>
    public long? DurationMs { get; set; }

    /// <summary>Thời điểm xảy ra thao tác hoặc kích hoạt sự kiện.</summary>
    public DateTime? TriggeredAt { get; set; }

    /// <summary>Tên người dùng thực hiện thao tác.</summary>
    public string? OperatorName { get; set; }

    /// <summary>Địa chỉ IP của người dùng thực hiện thao tác.</summary>
    public string? OperatorIp { get; set; }

    /// <summary>
    /// Quy bản ghi DB về đúng hình dạng một bước ISAPI để hộp thoại "Xem chi tiết" của tab Log dùng
    /// lại được y nguyên — nhờ vậy dòng lịch sử và dòng của phiên đang chạy hiển thị giống nhau.
    /// </summary>
    public VwSetupSceneStep ToStep() => new()
    {
        Order = StepOrder ?? 0,
        Name = StepName ?? Action ?? string.Empty,
        Method = Method ?? string.Empty,
        Endpoint = Endpoint ?? string.Empty,

        // Bước bỏ qua được backend ghi Success rỗng; quy về false rồi để cờ Skipped quyết định nhãn.
        Success = Success == SuccessValue,
        Skipped = Skipped ?? false,
        HttpStatus = HttpStatus,
        Message = Message,
        DurationMs = DurationMs ?? 0,
        RequestXml = RequestPayload,
        ResponseXml = ResponsePayload,
    };

    /// <summary>Giá trị số của BaseEnums.SuccessEnums.Success phía backend.</summary>
    private const int SuccessValue = 1;
}
