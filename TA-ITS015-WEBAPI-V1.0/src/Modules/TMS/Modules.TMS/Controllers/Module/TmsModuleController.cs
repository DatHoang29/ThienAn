using Modules.TMS.Controllers.Module.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.Module;

/// <summary>
/// System Tms Module 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsModulesController : BaseController
{

    /// <summary>
    /// Get Tms Module pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get pagination list")]
    public async Task<SqlSugarPagedList<PageTmsModuleOutput>> Page(PageTmsModuleInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsModuleOutput>>(input);
    }

    /// <summary>
    /// Get Tms Module list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<ModuleOutput>> GetList([FromQuery] ModuleInput input)
    {
        return await MessBus.InvokeAsync<List<ModuleOutput>>(input);
    }

    /// <summary>
    /// Add Tms Module
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsModule(AddTmsModuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Tms Module 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsModule(UpdateTmsModuleInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete Tms Module 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsModule(DeleteTmsModuleInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete Tms Modules 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsModule(List<DeleteTmsModuleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }



}
