using Modules.CCTV.Core.Entities;

namespace Modules.CCTV.Controllers.DashboardItem.Dto;

public class DashboardItemOutput : CctvDashboardItem
{
    /// <summary>IP thiết bị (enrich từ EquipmentId → TmsEquipment). Không lưu DB.</summary>
    public string? Ip { get; set; }
}
