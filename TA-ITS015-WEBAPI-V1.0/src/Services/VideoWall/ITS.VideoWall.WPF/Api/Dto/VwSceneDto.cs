using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Kịch bản hiển thị (VwScene). Lưu ý GridCols/GridRows ở đây là SỐ NHIỀU,
/// khác với GridCol/GridRow (số ít) của VwScreen.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSceneDto : VwEntityDto
{
    /// <summary>Tên kịch bản hiển thị.</summary>
    public string? Name { get; set; }

    /// <summary>Cờ đánh dấu kịch bản mặc định.</summary>
    public VwDefault? IsDefault { get; set; }

    /// <summary>Bỏ trống là kịch bản toàn tường — chỉ SuperAdmin được phép.</summary>
    public string? ControllerId { get; set; }

    /// <summary>SID kịch bản trên thiết bị, phải nằm trong 1..maxSceneNums do Probe trả về.</summary>
    public string? OutputId { get; set; }

    /// <summary>Số cột lưới ma trận chia màn hình của kịch bản.</summary>
    public int? GridCols { get; set; }

    /// <summary>Số hàng lưới ma trận chia màn hình của kịch bản.</summary>
    public int? GridRows { get; set; }

    /// <summary>Ảnh thu nhỏ (thumbnail) xem trước của kịch bản.</summary>
    public string? Thumbnail { get; set; }

    /// <summary>Trạng thái hoạt động của kịch bản (Enable/Disable).</summary>
    public VwStatus? Status { get; set; }

    /// <summary>Cờ kịch bản đang được kích hoạt chiếu trên tường.</summary>
    public VwActiveScene? ActiveScene { get; set; }

    /// <summary>Thứ tự sắp xếp hiển thị của kịch bản.</summary>
    public int? OrderNo { get; set; }

    /// <summary>Ghi chú hoặc mô tả bổ sung cho kịch bản.</summary>
    public string? Remark { get; set; }

    /// <summary>Tên hiển thị kèm tiền tố SID phục vụ giao diện người dùng.</summary>
    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            var rawName = !string.IsNullOrWhiteSpace(Name) ? Name.Trim() : (Code ?? string.Empty);
            var sid = !string.IsNullOrWhiteSpace(OutputId) ? OutputId.Trim() : (OrderNo?.ToString() ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(sid))
            {
                var prefix = $"{sid}_";
                if (rawName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return rawName;
                return $"{prefix} {rawName}";
            }
            return rawName;
        }
    }
}

/// <summary>
/// Author: Đạt
/// Description: Bộ lọc cho GET /api/videowall/vwscene/list. Backend KHÔNG hỗ trợ lọc theo Code,
/// nên muốn tìm bản ghi vừa thêm phải lọc theo Name rồi tự đối chiếu Code phía client.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSceneFilter
{
    /// <summary>Mã định danh duy nhất của kịch bản.</summary>
    [JsonPropertyName("id")]
    public string? ID { get; set; }

    /// <summary>Tên kịch bản hiển thị cần lọc.</summary>
    public string? Name { get; set; }

    /// <summary>SID kịch bản trên thiết bị cần lọc.</summary>
    public string? OutputId { get; set; }

    /// <summary>Mã bộ điều khiển tường ghép cần lọc.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Trạng thái kịch bản mặc định.</summary>
    public VwDefault? IsDefault { get; set; }

    /// <summary>Trạng thái hoạt động của kịch bản.</summary>
    public VwStatus? Status { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Tham số phân trang kịch bản cho POST /api/videowall/vwscene/page.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwScenePageInput : PageInput
{
    /// <summary>Tên kịch bản cần tìm kiếm phân trang.</summary>
    public string? Name { get; set; }

    /// <summary>SID kịch bản trên thiết bị cần lọc.</summary>
    public string? OutputId { get; set; }

    /// <summary>Mã bộ điều khiển cần lọc.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Trạng thái hoạt động của kịch bản.</summary>
    public VwStatus? Status { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Body của POST /api/videowall/vwscene/active — kích hoạt theo Code kịch bản
/// hoặc để backend suy ra kịch bản từ EventTypeId qua VwEventRule.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwActiveSceneInput
{
    /// <summary>Mã Code của kịch bản cần kích hoạt.</summary>
    public string? Code { get; set; }

    /// <summary>Mã loại sự kiện kích hoạt (dùng khi backend suy luận kịch bản qua rule).</summary>
    public string? EventTypeId { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Kết quả kích hoạt kịch bản — là VwScene kèm thông tin luật sự kiện đã khớp.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwActiveSceneOutput : VwEntityDto
{
    /// <summary>Tên kịch bản hiển thị đã kích hoạt.</summary>
    public string? Name { get; set; }

    /// <summary>Mã bộ điều khiển của kịch bản.</summary>
    public string? ControllerId { get; set; }

    /// <summary>SID kịch bản trên thiết bị.</summary>
    public string? OutputId { get; set; }

    /// <summary>Trạng thái hoạt động của kịch bản.</summary>
    public VwStatus? Status { get; set; }

    /// <summary>Trạng thái kích hoạt chiếu trên tường.</summary>
    public VwActiveScene? ActiveScene { get; set; }

    /// <summary>Mã luật sự kiện (RuleId) đã khớp.</summary>
    public string? RuleId { get; set; }

    /// <summary>Mã loại sự kiện (EventTypeId) kích hoạt.</summary>
    public string? EventTypeId { get; set; }

    /// <summary>Mã nhật ký kích hoạt sự kiện (TriggerLogId) được tạo.</summary>
    public string? TriggerLogId { get; set; }

    /// <summary>Thời điểm kích hoạt sự kiện.</summary>
    public DateTime? TriggeredAt { get; set; }
}

/// <summary>
/// Description: Thông tin vùng màn hình và kịch bản đang kích hoạt theo bộ điều khiển
/// Created date: 25/08/2026
/// </summary>
public sealed class VwActiveRegionOutput
{
    /// <summary>Mã bộ điều khiển tường ghép.</summary>
    public string ControllerId { get; set; } = null!;

    /// <summary>Mã Code của bộ điều khiển.</summary>
    public string? ControllerCode { get; set; }

    /// <summary>Tên bộ điều khiển.</summary>
    public string? ControllerName { get; set; }

    /// <summary>Mã định danh kịch bản (SceneId) đang kích hoạt.</summary>
    public string? SceneId { get; set; }

    /// <summary>Mã Code của kịch bản đang kích hoạt.</summary>
    public string? SceneCode { get; set; }

    /// <summary>Tên kịch bản đang kích hoạt.</summary>
    public string? SceneName { get; set; }

    /// <summary>Thời điểm kích hoạt kịch bản.</summary>
    public DateTime? ActivatedAt { get; set; }
}
