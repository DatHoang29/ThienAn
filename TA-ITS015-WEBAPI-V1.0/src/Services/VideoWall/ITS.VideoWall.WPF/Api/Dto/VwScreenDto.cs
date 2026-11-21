using System.Globalization;
using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Màn hình / cổng ra của tường ghép (VwScreen).
/// Created date: 25/08/2026
/// </summary>
public sealed class VwScreenDto : VwEntityDto
{
    /// <summary>Tên màn hình hiển thị.</summary>
    public string? Name { get; set; }

    /// <summary>ID bộ điều khiển của cổng ra đang cắm (backend suy ra từ ControllerOutputId; kho cục bộ ghi trực tiếp).</summary>
    public string? ControllerId { get; set; }

    /// <summary>Cổng ra đang cắm (VwControllerOutput.ID) — null: màn trong kho / đã tháo. Chỉ backend trả.</summary>
    public string? ControllerOutputId { get; set; }

    /// <summary>Tên cổng ra đang cắm (backend trả, chỉ đọc).</summary>
    public string? OutputName { get; set; }

    /// <summary>Serial màn do thiết bị báo.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>Khối chứa ô của cổng đang cắm (backend trả, chỉ đọc).</summary>
    public int? BlockNo { get; set; }

    /// <summary>Cổng ra vật lý — CHỈ dùng cho kho cục bộ (chế độ trực tiếp). Backend đã bỏ trường này (06/11/2026),
    /// thay bằng ControllerOutputId / OutputName. Cho đọc cả chuỗi để file lưu cục bộ cũ (giá trị "3") vẫn nạp được.</summary>
    [JsonPropertyName("outPutPort")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? OutPutPort { get; set; }

    /// <summary>ID cổng ra trên thiết bị ISAPI — chỉ dùng cho kho cục bộ (backend đã bỏ).</summary>
    public string? OutputId { get; set; }

    /// <summary>Vị trí cột (GridCol) trên ma trận ghép tường.</summary>
    public int? GridCol { get; set; }

    /// <summary>Vị trí hàng (GridRow) trên ma trận ghép tường.</summary>
    public int? GridRow { get; set; }

    // PosX/PosY đã bỏ khỏi backend: chúng là giá trị dẫn xuất của GridCol/GridRow × pitch ô tường.
    // Toạ độ tuyệt đối giờ tính tại chỗ dùng, pitch lấy từ vwwalltopology/current.

    /// <summary>Độ phân giải dạng chuỗi, ví dụ "1920x1080".</summary>
    public string? Resolution { get; set; }

    /// <summary>Chiều rộng pixel — backend lưu dạng CHUỖI, không phải số.</summary>
    public string? WidthPx { get; set; }

    /// <summary>Chiều cao pixel — backend lưu dạng CHUỖI, không phải số.</summary>
    public string? HeightPx { get; set; }

    /// <summary>Loại tấm nền hiển thị (LCD, LED, DLP...).</summary>
    public string? PanelType { get; set; }

    /// <summary>Model tấm nền màn hình.</summary>
    public string? PanelModel { get; set; }

    /// <summary>Kích thước vật lý tấm nền (ví dụ: 55 inch) — backend kiểu số (VwScreen.PanelSize int?).</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? PanelSize { get; set; }

    /// <summary>Trạng thái hoạt động của màn hình.</summary>
    public VwStatus? Status { get; set; }

    /// <summary>Trạng thái màn hình — backend kiểu chuỗi: "Online" / "Offline" / "Warning" (BaseEnums.ScreenState).</summary>
    public string? ScreenState { get; set; }

    /// <summary>Nhiệt độ hoạt động đo được từ cảm biến màn hình.</summary>
    public string? Temperature { get; set; }

    /// <summary>Tổng số giờ hoạt động của màn hình.</summary>
    public string? RunTimeHours { get; set; }

    /// <summary>Độ sáng hiện tại của màn hình.</summary>
    public string? Brightness { get; set; }

    /// <summary>Tuổi thọ đèn nền / bóng chiếu còn lại (giờ).</summary>
    public string? LampLifeHours { get; set; }

    /// <summary>Thứ tự sắp xếp hiển thị.</summary>
    public int? OrderNo { get; set; }

    /// <summary>Ghi chú bổ sung.</summary>
    public string? Remark { get; set; }

    /// <summary>Nhãn hiển thị trên bảng gán nguồn — chỉ dùng ở UI, không gửi lên API.</summary>
    [JsonIgnore]
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name) ? $"{Code} / {PortLabel}" : $"{Name} ({PortLabel})";

    /// <summary>Cổng ra để hiển thị: tên cổng từ backend, không có thì số cổng của kho cục bộ.</summary>
    [JsonIgnore]
    public string PortLabel => OutputName ?? OutPutPort?.ToString() ?? string.Empty;

    /// <summary>
    /// Kích thước pixel của màn hình: ưu tiên WidthPx/HeightPx, thiếu thì suy ra từ Resolution
    /// ("1920x1080"). Trả về null khi không nguồn nào đọc được — người vận hành phải tự nhập.
    /// </summary>
    public (int Width, int Height)? TryResolvePixelSize()
    {
        if (TryParsePositive(WidthPx, out var width) && TryParsePositive(HeightPx, out var height))
            return (width, height);

        return TryParseResolution(Resolution);
    }

    private static (int Width, int Height)? TryParseResolution(string? resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
            return null;

        var parts = resolution.Split(['x', 'X', '*', '×'], StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return null;

        if (!TryParsePositive(parts[0], out var width) || !TryParsePositive(parts[1], out var height))
            return null;

        return (width, height);
    }

    private static bool TryParsePositive(string? raw, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            && value > 0;
    }
}

/// <summary>
/// Author: Đạt
/// Description: Bộ lọc cho GET /api/videowall/vwscreen/list.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwScreenFilter
{
    /// <summary>ID bản ghi màn hình cần lọc.</summary>
    [JsonPropertyName("id")]
    public string? ID { get; set; }

    /// <summary>Tên màn hình cần lọc.</summary>
    public string? Name { get; set; }

    /// <summary>ID bộ điều khiển cần lọc.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Cổng ra đang cắm cần lọc (VwControllerOutput.ID).</summary>
    public string? ControllerOutputId { get; set; }

    /// <summary>Serial màn cần lọc.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>true: đang cắm cổng; false: trong kho / đã tháo.</summary>
    public bool? Installed { get; set; }

    /// <summary>Loại tấm nền cần lọc.</summary>
    public string? PanelType { get; set; }

    /// <summary>Trạng thái hoạt động cần lọc.</summary>
    public VwStatus? Status { get; set; }
}
