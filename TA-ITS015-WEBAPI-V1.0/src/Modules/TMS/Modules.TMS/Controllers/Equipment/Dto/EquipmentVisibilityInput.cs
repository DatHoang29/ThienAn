using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.Equipment.Dto;

/// <summary>
/// Author: HieuNV
/// Description: DTO dùng để bật hiển thị (visible) hàng loạt cho thiết bị
/// Created date: 09/08/2024
/// </summary>
public class UpdateEquipmentVisibleInput : BaseIdInput
{
    public int? EquipmentVisible { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: DTO dùng để ẩn (invisible) hàng loạt cho thiết bị
/// Created date: 09/08/2024
/// </summary>
public class UpdateEquipmentInVisibleInput : BaseIdInput
{
    public int? EquipmentVisible { get; set; }
}
