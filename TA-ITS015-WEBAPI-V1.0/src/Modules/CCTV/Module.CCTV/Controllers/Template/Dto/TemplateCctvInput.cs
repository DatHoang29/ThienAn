using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.CCTV.Controllers.Template.Dto;

// Input filter cho query list
public class TemplateInput
{
    public string? Name { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

// Add: nhận list slot trực tiếp, không nhận string JSON
public class AddTemplateInput
{
    public string? Name { get; set; }
    public string? Remark { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
    public List<TemplateSlot>? Items { get; set; }  = new();
}

public class UpdateTemplateInput : AddTemplateInput
{
    public string ID { get; set; } = null!;
}

public class DeleteTemplateInput : BaseIdInput
{
}

public class TemplateSlot
{
    public int SlotNo { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}