using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Một cửa sổ trong kịch bản (VwWindowScene). Cùng entity này phục vụ cả hai chế độ
/// dựng cảnh: "từng cổng ra" (mỗi màn hình một cửa sổ phủ kín, ZIndex = 1) và "cửa sổ xếp lớp"
/// (nhiều cửa sổ chồng nhau, khác ZIndex) — chỉ khác nhau ở hình học và ZIndex.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwWindowSceneDto : VwEntityDto
{
    /// <summary>Tên cửa sổ trong kịch bản.</summary>
    public string? Name { get; set; }

    /// <summary>Mã định danh kịch bản (SceneId) chứa cửa sổ.</summary>
    public string? SceneId { get; set; }

    /// <summary>Mã định danh nguồn tín hiệu (SourceId) gán cho cửa sổ.</summary>
    public string? SourceId { get; set; }

    /// <summary>VWMWID do thiết bị cấp sau khi đẩy cửa sổ xuống — chỉ đọc.</summary>
    public string? DeviceWindowId { get; set; }

    /// <summary>Toạ độ X (pixel) của cửa sổ trên không gian tường ảo.</summary>
    public int? X { get; set; }

    /// <summary>Toạ độ Y (pixel) của cửa sổ trên không gian tường ảo.</summary>
    public int? Y { get; set; }

    /// <summary>Chiều rộng — backend đặt tên trường là "w", không phải "width".</summary>
    public int? W { get; set; }

    /// <summary>Chiều cao — backend đặt tên trường là "h", không phải "height".</summary>
    public int? H { get; set; }

    /// <summary>Thứ tự lớp hiển thị Z-Index của cửa sổ (lớp trên cùng hiển thị đè lên).</summary>
    [JsonPropertyName("zIndex")]
    public int? ZIndex { get; set; }

    /// <summary>Nhãn hiển thị đè lên tiêu đề cửa sổ.</summary>
    public string? Label { get; set; }

    /// <summary>Độ mờ — backend kiểu số (VwWindowScene.Opacity int?, vd 100).
    /// Cho đọc cả chuỗi để kịch bản lưu cục bộ cũ vẫn nạp được.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? Opacity { get; set; }

    /// <summary>Cờ hiển thị — KHÔNG nullable phía backend, luôn phải gửi.</summary>
    public VwWindowVisible Visible { get; set; } = VwWindowVisible.Visible;

    /// <summary>Thứ tự sắp xếp hiển thị của cửa sổ.</summary>
    public int? OrderNo { get; set; }

    /// <summary>Ghi chú hoặc mô tả bổ sung cho cửa sổ.</summary>
    public string? Remark { get; set; }

    /// <summary>Chế độ hiển thị cửa sổ (1: đơn lẻ, 4: chia 4 sub-window...).</summary>
    public int WindowMode { get; set; } = 1;

    /// <summary>Danh sách cửa sổ con (sub-window) khi ở chế độ chia khung hình.</summary>
    public List<VwSubWindowDto>? SubWindows { get; set; }

    /// <summary>Mô tả hình học gọn cho bảng hiển thị — chỉ dùng ở UI, không gửi lên API.</summary>
    [JsonIgnore]
    public string Geometry => $"{X ?? 0},{Y ?? 0} {W ?? 0}×{H ?? 0}";
}

/// <summary>
/// Description: Thông tin cửa sổ con (sub-window) bên trong cửa sổ chia đa màn hình
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSubWindowDto
{
    /// <summary>Chỉ số thứ tự của cửa sổ con (1, 2, 3, 4...).</summary>
    public int SubIndex { get; set; }

    /// <summary>Mã định danh nguồn tín hiệu (SourceId) gán cho cửa sổ con.</summary>
    public string? SourceId { get; set; }

    /// <summary>Nhãn hiển thị của cửa sổ con.</summary>
    public string? Label { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Bộ lọc cho GET /api/videowall/vwwindowscene/list.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwWindowSceneFilter
{
    /// <summary>Mã định danh duy nhất của cửa sổ.</summary>
    [JsonPropertyName("id")]
    public string? ID { get; set; }

    /// <summary>Tên cửa sổ cần lọc.</summary>
    public string? Name { get; set; }

    /// <summary>Mã kịch bản (SceneId) cần lọc danh sách cửa sổ.</summary>
    public string? SceneId { get; set; }

    /// <summary>Mã nguồn tín hiệu (SourceId) cần lọc.</summary>
    public string? SourceId { get; set; }

    /// <summary>Trạng thái hiển thị của cửa sổ cần lọc.</summary>
    public VwWindowVisible? Visible { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Body của POST /api/videowall/vwwindowscene/switchsource — đổi nguồn tín hiệu
/// của một cửa sổ đang chiếu.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSwitchWindowSourceInput
{
    /// <summary>Mã định danh duy nhất của cửa sổ cần chuyển nguồn.</summary>
    [JsonPropertyName("id")]
    public string ID { get; set; } = string.Empty;

    /// <summary>Mã định danh nguồn tín hiệu mới (SourceId) cần gán.</summary>
    public string SourceId { get; set; } = string.Empty;
}

/// <summary>
/// Author: Đạt
/// Description: Body của POST /api/videowall/vwwindowscene/setlayer — đưa cửa sổ lên trên
/// hoặc xuống dưới cùng.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSetWindowLayerInput
{
    /// <summary>Mã định danh duy nhất của cửa sổ cần điều chỉnh lớp hiển thị.</summary>
    [JsonPropertyName("id")]
    public string ID { get; set; } = string.Empty;

    /// <summary>Hành động điều chỉnh thứ tự lớp (Top: lên trên cùng, Bottom: xuống dưới cùng).</summary>
    public VwWindowLayerAction Action { get; set; } = VwWindowLayerAction.Top;
}
