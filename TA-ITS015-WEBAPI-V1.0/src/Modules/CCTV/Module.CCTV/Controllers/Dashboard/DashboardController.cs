using Modules.CCTV.Controllers;
using Modules.CCTV.Controllers.Dashboard.Dto;

namespace Module.CCTV.Controllers.Dashboard;

[ApiDescriptionSettings(GroupName, Order = 460)]
public class DashboardController : BaseController
{
    [DisplayName("Get Tag list")]
    public virtual async Task<List<DashboardTagOutput>> GetTagList([FromQuery] DashboardTagInput input)
    {
        return await MessBus.InvokeAsync<List<DashboardTagOutput>>(input);
    }

    [DisplayName("Get Dashboard list")]
    public virtual async Task<List<DashboardOutput>> GetList([FromQuery] DashboardInput input)
    {
        return await MessBus.InvokeAsync<List<DashboardOutput>>(input);
    }

    [DisplayName("Get By Id")]
    public virtual async Task<DashboardOutput> GetById([FromQuery] DashboardIdInput input)
    {
        return await MessBus.InvokeAsync<DashboardOutput>(input);
    }

    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add Dashboard")]
    public virtual async Task<string> AddDashboard(AddDashboardInput input)
    {
        // Wolverine tự động hứng giá trị return string từ Handler và trả về đây
        var id = await _bus.InvokeAsync<string>(input);
        return id;
    }


    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update Dashboard")]
    public virtual async Task UpdateDashboard(UpdateDashboardInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete Dashboard")]
    public virtual async Task DeleteDashboard(DeleteDashboardInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    [ApiDescriptionSettings(Name = "Copy"), HttpPost]
    [DisplayName("Copy Dashboard")]
    public virtual async Task<string> CopyDashboard(CopyDashboardInput input)
    {
        return await MessBus.InvokeAsync<string>(input);
    }
}
