using Modules.CCTV.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.CCTV.Controllers.DashboardItem.Dto;

public class DashboardItemInput
{
    public string? ID { get; set; }
    public string? DashboardId { get; set; }
    public string? EquipmentId { get; set; }
    /// <summary>Lọc theo IP thiết bị (BE tự quy đổi Ip → EquipmentId để lọc).</summary>
    public string? Ip { get; set; }
}

public class AddDashboardItemInput
{
    public string DashboardId { get; set; } = null!;
    public List<DashboardItemPayload> Items { get; set; } = new();
}

public class UpdateDashboardItemInput : AddDashboardItemInput
{

}

public class DeleteDashboardItemInput : BaseIdInput
{
}

// Input cho từng item (dùng chung Add/Update)
public class DashboardItemPayload
{
    public string? ID { get; set; }         // Có ID → item đang tồn tại
    public string? EquipmentId { get; set; }
    /// <summary>
    /// IP thiết bị. Nếu FE truyền Ip mà bỏ trống EquipmentId,
    /// BE tự quy đổi Ip → EquipmentId (khóa lưu trữ) trước khi lưu.
    /// </summary>
    public string? Ip { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public int? W { get; set; }
    public int? H { get; set; }
}