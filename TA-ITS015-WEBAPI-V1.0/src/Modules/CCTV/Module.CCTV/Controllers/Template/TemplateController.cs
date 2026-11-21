using Modules.CCTV.Controllers;
using Modules.CCTV.Controllers.Template.Dto;

namespace Module.CCTV.Controllers.Template;

[ApiDescriptionSettings(GroupName, Order = 460)]
public class TemplateController : BaseController
{
    [DisplayName("Get Template list")]
    public virtual async Task<List<TemplateCctvOutput>> GetTemplateList([FromQuery] TemplateInput input)
    {
        return await MessBus.InvokeAsync<List<TemplateCctvOutput>>(input);
    }

    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    public virtual async Task<TemplateCctvOutput> AddTemplate(AddTemplateInput input)
    {
         return await MessBus.InvokeAsync<TemplateCctvOutput>(input);
    }

    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    public virtual async Task<TemplateCctvOutput> UpdateTemplate(UpdateTemplateInput input)
    {
        return await MessBus.InvokeAsync<TemplateCctvOutput>(input);
    }
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    public virtual async Task DeleteTemplate(DeleteTemplateInput input)
    {
        await MessBus.InvokeAsync(input);

    }
}
