using Modules.TMS.Controllers.TrafficPlanWorkDetail.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.TrafficPlanWorkDetail;

/// <summary>
/// System TmsTrafficPlanWorkDetailition 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsTrafficPlanWorkDetailController : BaseController
{
    public TmsTrafficPlanWorkDetailController()
    {

    }

    /// <summary>
    /// Get TmsTrafficPlanWorkDetailition pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageTmsTrafficPlanWorkDetailOutput>> Page(PageTmsTrafficPlanWorkDetailInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsTrafficPlanWorkDetailOutput>>(input);
    }

    /// <summary>
    /// Get TmsTrafficPlanWorkDetailition list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<TrafficPlanWorkDetailOutput>> GetList([FromQuery] TrafficPlanWorkDetailInput input)
    {
        return await MessBus.InvokeAsync<List<TrafficPlanWorkDetailOutput>>(input);
    }

    /// <summary>
    /// Add TmsTrafficPlanWorkDetailition
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsTrafficPlanWorkDetail(AddTmsTrafficPlanWorkDetailInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update TmsTrafficPlanWorkDetailition 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsTrafficPlanWorkDetail(UpdateTmsTrafficPlanWorkDetailInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete TmsTrafficPlanWorkDetailition 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsTrafficPlanWorkDetail(DeleteTmsTrafficPlanWorkDetailInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete TmsTrafficPlanWorkDetailitions 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsTrafficPlanWorkDetail(List<DeleteTmsTrafficPlanWorkDetailInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: Đăng Khoa
    /// Description: Hàm list danh sách Traffic Plan theo điều kiện tìm kiếm
    /// Created date: 29/08/2024
    /// </summary>
    /// <param name="input">Đối tượng PageTmsTrafficPlanWorkDetailInput</param>
    /// <returns>Danh sách TmsTrafficPlanWorkDetailOutput</returns>
    [HttpGet("DataList/{id}")]
    [DisplayName("Retrieve a list of Traffic Plan based on field code")]
    public async Task<SqlSugarPagedList<PageTmsTrafficPlanWorkDetailOutput>> PageByID(string id)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsTrafficPlanWorkDetailOutput>>(new PageTmsTrafficPlanWorkDetailInput() { TrafficPlanId = id }); ;
    }
}
