using Modules.TMS.Controllers.WorkContact.Dto;
using Modules.TMS.Controllers.WorkDetailContact.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.WorkDetailContact;

/// <summary>
/// System TmsWorkDetailContactition 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsWorkDetailContactController : BaseController
{
    public TmsWorkDetailContactController()
    {

    }

    /// <summary>
    /// Get TmsWorkDetailContactition pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageTmsWorkDetailContactOutput>> Page(PageTmsWorkDetailContactInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsWorkDetailContactOutput>>(input);
    }

    /// <summary>
    /// Get TmsWorkDetailContactition list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<TmsWorkDetailContactOutput>> GetList([FromQuery] TmsWorkDetailContactInput input)
    {
        return await MessBus.InvokeAsync<List<TmsWorkDetailContactOutput>>(input);
    }

    /// <summary>
    /// Add TmsWorkDetailContactition
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsWorkDetailContact(AddTmsWorkDetailContactInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update TmsWorkDetailContactition 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsWorkDetailContact(UpdateTmsWorkDetailContactInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete TmsWorkDetailContactition 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsWorkDetailContact(DeleteTmsWorkDetailContactInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete TmsWorkDetailContactitions 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsWorkDetailContact(List<DeleteTmsWorkDetailContactInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// Author: Đăng Khoa
    /// Description: Hàm list danh sách Work Detail theo điều kiện tìm kiếm
    /// Created date: 29/08/2024
    /// </summary>
    /// <param name="input">Đối tượng PageTmsWorkDetailContactInput</param>
    /// <returns>Danh sách PageTmsWorkDetailContactOutput</returns>
    [HttpGet("DataList/{id}")]
    [DisplayName("Retrieve a list of Work deatil based on field code")]
    public async Task<SqlSugarPagedList<PageTmsWorkDetailContactOutput>> PageByID(string id)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsWorkDetailContactOutput>>(new PageTmsWorkDetailContactInput() { WorkDetailId = id }); ;
    }


    /// <summary>
    /// Author: Ngọc Thắng
    /// Created Date: 23/10/2024
    /// Description: Lấy danh sách thông tin liên hệ bởi danh sách workdetail-id
    /// </summary>
    [HttpPost("ListWorkContactByWorkDetailIds")]
    [DisplayName("Retrieve the list of work-contact based on the list of work-detail-id")]
    public async Task<List<TmsWorkContactOutput>> HandleAsync(TmsWorkDetailIdsInput input)
    {
        return await MessBus.InvokeAsync<List<TmsWorkContactOutput>>(input); ;
    }
}
