using Modules.TMS.Core.Entities;

namespace Modules.TMS.Core.Dto.EquipmentEventRule;

/// <summary>
/// Author: ChuongLP
/// Description: Output item danh sách rule (kèm tên loại sự kiện + tên loại thiết bị để hiển thị grid)
/// Created date: 16/07/2026
/// </summary>
public class EquipmentEventRuleOutput : TmsEquipmentEventRule
{
    /// <summary>
    /// Tên loại sự kiện (TmsEventType.Name)
    /// </summary>
    public string? EventTypeName { get; set; }

    /// <summary>
    /// Tên loại thiết bị (TmsEquipmentType.Name)
    /// </summary>
    public string? EquipmentTypeName { get; set; }
}

/// <summary>
/// Author: ChuongLP
/// Description: Output phân trang rule (kèm tên loại sự kiện + tên loại thiết bị)
/// Created date: 16/07/2026
/// </summary>
public class PageEquipmentEventRuleOutput : EquipmentEventRuleOutput
{
}
