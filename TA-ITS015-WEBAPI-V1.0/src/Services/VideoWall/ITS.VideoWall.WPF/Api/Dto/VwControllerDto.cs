using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Bộ điều khiển tường ghép (VwController). Dùng chung cho List/ById/Add/Update
/// vì backend khai báo VwAddControllerInput : VwController (gửi/nhận trọn bản ghi).
/// Created date: 25/08/2026
/// </summary>
public sealed class VwControllerDto : VwEntityDto
{
    /// <summary>Tên bộ điều khiển.</summary>
    public string? Name { get; set; }

    /// <summary>Model thiết bị điều khiển (ví dụ: DS-C66S, DS-C30S...).</summary>
    public string? Model { get; set; }

    /// <summary>Địa chỉ IP kết nối của thiết bị.</summary>
    [JsonPropertyName("ip")]
    public string? IP { get; set; }

    /// <summary>Tài khoản đăng nhập thiết bị.</summary>
    public string? Account { get; set; }

    /// <summary>Mật khẩu thiết bị — backend serialize là "passWord" (hoa chữ W).</summary>
    [JsonPropertyName("passWord")]
    public string? PassWord { get; set; }

    /// <summary>Khung máy (VwController.Chassis).</summary>
    public string? Chassis { get; set; }

    /// <summary>ID kịch bản (SceneId) hiện đang kích hoạt hiển thị trên bộ điều khiển.</summary>
    public string? ActiveSceneId { get; set; }

    /// <summary>Thời điểm kích hoạt kịch bản hiện tại.</summary>
    public DateTime? ActiveSceneAt { get; set; }

    /// <summary>Trạng thái hoạt động của bộ điều khiển.</summary>
    public VwStatus? Status { get; set; }

    /// <summary>Mã màu đại diện định dạng Hex hiển thị trên sơ đồ.</summary>
    public string? ColorHex { get; set; }

    /// <summary>Tọa độ cột gốc (OriginCol) trên ma trận màn hình tổng thể.</summary>
    public int? OriginCol { get; set; }

    /// <summary>Tọa độ hàng gốc (OriginRow) trên ma trận màn hình tổng thể.</summary>
    public int? OriginRow { get; set; }

    /// <summary>Số cột ma trận màn hình do bộ điều khiển quản lý.</summary>
    public int? CoverCols { get; set; }

    /// <summary>Số hàng ma trận màn hình do bộ điều khiển quản lý.</summary>
    public int? CoverRows { get; set; }

    /// <summary>Số khe cắm đầu vào (Input Slots).</summary>
    public int? InputSlotsNumber { get; set; }

    /// <summary>Tổng số khe cắm bo mạch (Slots).</summary>
    public int? SlotsNumber { get; set; }

    /// <summary>Trạng thái kết nối cổng đồng bộ tín hiệu vào (Genlock In).</summary>
    public int? GenlockInConnected { get; set; }

    /// <summary>Trạng thái kết nối cổng đồng bộ tín hiệu ra (Genlock Out).</summary>
    public int? GenlockOutConnected { get; set; }

    /// <summary>Ghi chú bổ sung.</summary>
    public string? Remark { get; set; }

    /// <summary>Nhãn hiển thị trên combobox chọn bộ điều khiển — chỉ dùng ở UI, không gửi lên API.</summary>
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"{Code} ({IP})" : $"{Name} ({IP})";
}

/// <summary>
/// Author: Đạt
/// Description: Bộ lọc cho GET /api/videowall/vwcontroller/list.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwControllerFilter
{
    /// <summary>ID bản ghi bộ điều khiển cần lọc.</summary>
    [JsonPropertyName("id")]
    public string? ID { get; set; }

    /// <summary>Tên bộ điều khiển cần lọc.</summary>
    public string? Name { get; set; }

    /// <summary>Model thiết bị cần lọc.</summary>
    public string? Model { get; set; }

    /// <summary>Khung máy cần lọc.</summary>
    public string? Chassis { get; set; }

    /// <summary>Trạng thái hoạt động cần lọc.</summary>
    public VwStatus? Status { get; set; }
}
