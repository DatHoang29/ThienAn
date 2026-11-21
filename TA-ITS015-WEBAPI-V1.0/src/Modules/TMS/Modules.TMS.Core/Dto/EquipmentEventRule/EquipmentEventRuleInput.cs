using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Dto.EquipmentEventRule;

/// <summary>
/// Author: ChuongLP
/// Description: DTO phân trang danh sách rule hiển thị VMS/LCS theo sự kiện
/// Created date: 16/07/2026
/// </summary>
public class PageEquipmentEventRuleInput : BasePageInput
{
    public string? EventTypeId { get; set; }
    public string? EquipmentTypeId { get; set; }
    public string? ProcessType { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

/// <summary>
/// Author: ChuongLP
/// Description: DTO lọc danh sách rule
/// Created date: 16/07/2026
/// </summary>
public class EquipmentEventRuleInput
{
    public string? EventTypeId { get; set; }
    public string? EquipmentTypeId { get; set; }
    public string? ProcessType { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

/// <summary>
/// Author: ChuongLP
/// Description: DTO thêm mới rule
/// Created date: 16/07/2026
/// </summary>
public class AddEquipmentEventRuleInput : TmsEquipmentEventRule
{
}

/// <summary>
/// Author: ChuongLP
/// Description: DTO cập nhật rule
/// Created date: 16/07/2026
/// </summary>
public class UpdateEquipmentEventRuleInput : AddEquipmentEventRuleInput
{
}

/// <summary>
/// Author: ChuongLP
/// Description: DTO xóa rule theo ID
/// Created date: 16/07/2026
/// </summary>
public class DeleteEquipmentEventRuleInput : BaseIdInput
{
}
