using Modules.TMS.Core.Dto.EquipmentEventRule;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.EquipmentEventRule;

/// <summary>
/// Author: ChuongLP
/// Description: TMS Equipment Event Rule Controller
///              Quản lý cấu hình hiển thị VMS/LCS theo loại sự kiện (trước/sau sự cố).
/// Created date: 16/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 455)]
public class TmsEquipmentEventRuleController : BaseController
{
    public TmsEquipmentEventRuleController()
    {
    }

    /// <summary>
    /// Get Tms Equipment Event Rule pagination list
    /// </summary>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageEquipmentEventRuleOutput>> Page(PageEquipmentEventRuleInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageEquipmentEventRuleOutput>>(input);
    }

    /// <summary>
    /// Get Tms Equipment Event Rule list
    /// </summary>
    [DisplayName("Get  list")]
    public async Task<List<EquipmentEventRuleOutput>> GetList([FromQuery] EquipmentEventRuleInput input)
    {
        return await MessBus.InvokeAsync<List<EquipmentEventRuleOutput>>(input);
    }

    /// <summary>
    /// Add Tms Equipment Event Rule
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsEquipmentEventRule(AddEquipmentEventRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Tms Equipment Event Rule
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsEquipmentEventRule(UpdateEquipmentEventRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Delete Tms Equipment Event Rule
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsEquipmentEventRule(DeleteEquipmentEventRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Batch delete Tms Equipment Event Rules
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsEquipmentEventRule(List<DeleteEquipmentEventRuleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
