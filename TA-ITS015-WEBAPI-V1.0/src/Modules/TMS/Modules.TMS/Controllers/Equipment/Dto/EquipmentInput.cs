using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.Equipment.Dto;

public class PageEquipmentInput : BasePageInput
{

    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Ip { get; set; }
    public string? Port { get; set; }
    public string? DirectionId { get; set; }
    public string? EquipmentState { get; set; }
    public string? Location { get; set; }
    public string? EquipmentTypeId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
    public int? EquipmentVisible { get; set; }


}

public class EquipmentInput
{

    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Ip { get; set; }
    public string? Port { get; set; }
    public string? DirectionId { get; set; }
    public string? EquipmentState { get; set; }
    public string? Location { get; set; }
    public string? EquipmentTypeId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
    public int? EquipmentVisible { get; set; }


}

public class AddEquipmentInput : TmsEquipment
{
}

public class UpdateEquipmentInput : AddEquipmentInput
{

}

public class DeleteEquipmentInput : BaseIdInput
{

}

/// <summary>
/// Author: ThangNN
/// Description: DTO chứa id và code
/// </summary>
public class TmsEquipmentSingleInput
{
    public string? Id { set; get; }
    public string? Code { set; get; }
}

/// <summary>
/// Author: ThangNN
/// Description: DTO chứa danh sách equipment-id
/// </summary>
public class TmsEquipmentIdListInput
{
    public string? EquipmentIdList { get; set; }
}
