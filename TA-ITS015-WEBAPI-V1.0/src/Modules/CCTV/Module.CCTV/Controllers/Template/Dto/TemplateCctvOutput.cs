using Modules.TMS.Core.Entities;
using Shared.DTO.Enums;

namespace Modules.CCTV.Controllers.Template.Dto;

public class TemplateCctvOutput
{
    public string? ID { get; set; }
    public string? Name { get; set; }
    public string? Remark { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
    public List<TemplateSlot> Items { get; set; } = new();
    public DateTime? CreateTime { get; set; }
    public string? CreateUId { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? UpdateUId { get; set; }
}
