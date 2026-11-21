using Modules.TMS.Controllers.WorkDetail.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.WorkDetail;

/// <summary>
/// Tms Work Detail 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsWorkDetailController : BaseController
{


    /// <summary>
    /// Get Tms Work Detail pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageWorkDetailOutput>> Page(PageWorkDetailInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageWorkDetailOutput>>(input);
    }

    /// <summary>
    /// Get Tms Work Detail list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get list")]
    public async Task<List<WorkDetailOutput>> GetList([FromQuery] WorkDetailInput input)
    {
        return await MessBus.InvokeAsync<List<WorkDetailOutput>>(input);
    }
    ///// <summary>
    ///// Author: Đăng Khoa SonTH
    ///// Description: Hàm list danh sách WorkDetail theo điều kiện tìm kiếm
    ///// Created date: 29/08/2024
    ///// </summary>
    ///// <param name="input">Đối tượng WorkDetailInput</param>
    ///// <returns>Danh sách WorkDetailOutput</returns>
    //[HttpGet("DataList/{id}")]
    //[DisplayName("Get list of WorkDetail")]
    //public async Task<List<WorkDetailOutput>> GetListId(string id)
    //{
    //    return await this.MessBus.InvokeAsync<List<WorkDetailOutput>>(new WorkDetailInput() { ID = id });
    //}
    /// <summary>
    /// Add Tms Work Detail
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsWorkDetail(AddWorkDetailInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Tms Work Detail 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsWorkDetail(UpdateWorkDetailInput input)
    {
        await MessBus.InvokeAsync(input);

    }


    /// <summary>
    /// Delete Tms Work Detail 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsWorkDetail(DeleteWorkDetailInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete Tms Work Details 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsWorkDetail(List<DeleteWorkDetailInput> input)
    {
        await MessBus.InvokeAsync(input);
    }



}
