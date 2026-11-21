using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Description: Cổng ra của bộ điều khiển (VwControllerOutput) kèm màn đang cắm — đồng bộ từ thiết bị,
///              thay cho VwSlotPort đã bỏ (06/11/2026). Vị trí (BlockNo / GridCol / GridRow) do hệ thống tự tính.
/// Created date: 06/11/2026
/// </summary>
public sealed class VwControllerOutputDto : VwEntityDto
{
    public string? ControllerId { get; set; }

    /// <summary>outputID của thiết bị.</summary>
    public int? DeviceOutputId { get; set; }

    /// <summary>Tên cổng trên thiết bị (VD "Output 7-1").</summary>
    public string? Name { get; set; }

    public int? BoardNo { get; set; }
    public int? PortNo { get; set; }
    public string? PortType { get; set; }
    public string? Resolution { get; set; }
    public int? WallNo { get; set; }

    /// <summary>Ô của cổng trên tường của chính bộ điều khiển (uniform).</summary>
    public int? RectX { get; set; }
    public int? RectY { get; set; }
    public int? RectW { get; set; }
    public int? RectH { get; set; }

    /// <summary>Vị trí trên tường vật lý toàn cục và trong bố cục đang áp dụng.</summary>
    public int? WallCol { get; set; }
    public int? WallRow { get; set; }
    public int? BlockNo { get; set; }
    public int? GridCol { get; set; }
    public int? GridRow { get; set; }

    public string? DisplayName { get; set; }
    public string? Remark { get; set; }
    public VwStatus? Status { get; set; }

    /// <summary>1: có trên thiết bị, 2: không thấy ở lần đồng bộ gần nhất, 3: dữ liệu dựng tay.</summary>
    public int? DeviceState { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    public string? ControllerCode { get; set; }
    public string? ControllerName { get; set; }
    public bool IsMaster { get; set; }

    /// <summary>Màn đang cắm vào cổng.</summary>
    public string? ScreenId { get; set; }
    public string? ScreenCode { get; set; }
    public string? ScreenName { get; set; }
    public string? SerialNumber { get; set; }
    public string? Temperature { get; set; }
    public string? RunTimeHours { get; set; }
    public string? ScreenState { get; set; }

    /// <summary>Nhãn hiển thị — chỉ dùng ở UI.</summary>
    [JsonIgnore]
    public string Label => string.IsNullOrWhiteSpace(DisplayName) ? Name ?? $"#{DeviceOutputId}" : DisplayName;
}

/// <summary>
/// Description: Bộ lọc cho GET /api/videowall/vwcontrolleroutput/list.
/// Created date: 06/11/2026
/// </summary>
public sealed class VwControllerOutputFilter
{
    public string? ControllerId { get; set; }

    /// <summary>true: chỉ cổng đang nằm trong lưới của bố cục đang áp dụng.</summary>
    public bool? OnlyPlaced { get; set; }
}
