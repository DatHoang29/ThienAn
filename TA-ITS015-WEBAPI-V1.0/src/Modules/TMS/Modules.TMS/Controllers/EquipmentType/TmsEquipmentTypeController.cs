using Modules.TMS.Controllers.EquipmentType.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.EquipmentType;

/// <summary>
/// TMS EquipmentType 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsEquipmentTypeController : BaseController
{

    /// <summary>
    /// Get Tms EquipmentType pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get pagination list")]
    public async Task<SqlSugarPagedList<PageEquipmentTypesOutput>> Page(PageEquipmentTypesInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageEquipmentTypesOutput>>(input);
    }

    /// <summary>
    /// Get Tms EquipmentType list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get list")]
    public async Task<List<EquipmentTypeOutput>> GetList([FromQuery] EquipmentTypeInput input)
    {
        return await MessBus.InvokeAsync<List<EquipmentTypeOutput>>(input);
    }

    /// <summary>
    /// Add Tms EquipmentType
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add Equipment Type")]
    public async Task AddTmsEquipmentType(AddEquipmentTypesInput input)
    {

        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update TmsEquipmentType 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update Equipment Type")]
    public async Task UpdateTmsEquipmentType(UpdateEquipmentTypesInput input)
    {


        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete TmsEquipmentType 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete Equipment Type")]
    public async Task DeleteTmsEquipmentType(DeleteEquipmentTypesInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete TmsEquipmentTypes 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete Equipment Type")]
    public async Task BatchDeleteTmsEquipmentType(List<DeleteEquipmentTypesInput> input)
    {
        await MessBus.InvokeAsync(input);
    }


}
