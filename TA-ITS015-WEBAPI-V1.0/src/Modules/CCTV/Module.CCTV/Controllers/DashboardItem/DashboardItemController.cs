using Modules.CCTV.Controllers;
using Modules.CCTV.Controllers.DashboardItem.Dto;

namespace Module.CCTV.Controllers.DashboardItem;

[ApiDescriptionSettings(GroupName, Order = 460)]
public class DashboardItemController : BaseController
{
    [DisplayName("Get DashboardItem list")]
    public virtual async Task<List<DashboardItemOutput>> GetListItem([FromQuery] DashboardItemInput input)
    {
        return await MessBus.InvokeAsync<List<DashboardItemOutput>>(input);
    }

    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add DashboardItem")]
    public virtual async Task AddDashboardItem(AddDashboardItemInput input)
    {
        await MessBus.InvokeAsync(input);
    }


    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update DashboardItem")]
    public virtual async Task UpdateDashboardItem(UpdateDashboardItemInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete DashboardItem")]
    public virtual async Task DeleteDashboardItem(DeleteDashboardItemInput input)
    {
        await MessBus.InvokeAsync(input);

    }
}
