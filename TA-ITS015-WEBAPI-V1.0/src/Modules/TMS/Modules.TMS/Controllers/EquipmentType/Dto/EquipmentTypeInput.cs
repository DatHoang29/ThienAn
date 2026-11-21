using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.EquipmentType.Dto;

public class PageEquipmentTypesInput : BasePageInput
{
    public string? ID { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? ModuleId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

public class EquipmentTypeInput
{
    public string? ID { get; set; }
    public string? Code { get; set; }

    public string? Name { get; set; }
    public string? ModuleId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
    public bool IncludeDelete { get; set; }

}

public class AddEquipmentTypesInput : TmsEquipmentType
{
}

public class UpdateEquipmentTypesInput : AddEquipmentTypesInput
{

}

public class DeleteEquipmentTypesInput : BaseIdInput
{

}