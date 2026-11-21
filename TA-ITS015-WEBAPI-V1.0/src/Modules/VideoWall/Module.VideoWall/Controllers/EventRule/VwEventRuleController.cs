using Module.VideoWall.Core.Dto.EventRule;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.EventRule;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Luật kích hoạt kịch bản theo sự kiện (VwEventRule)
/// Created date: 2026-08-01
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 570)]
public class VwEventRuleController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách VwEventRule theo phân trang
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get VwEventRule page list")]
    public virtual async Task<SqlSugarPagedList<VwPageEventRuleOutput>> Page(VwPageEventRuleInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageEventRuleOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách VwEventRule (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwEventRule list")]
    public async Task<List<VwEventRuleOutput>> GetList([FromQuery] VwEventRuleInput input)
    {
        return await MessBus.InvokeAsync<List<VwEventRuleOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 VwEventRule theo ID
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwEventRule by id")]
    public async Task<VwEventRuleOutput> GetById([FromQuery] VwIdEventRuleInput input)
    {
        return await MessBus.InvokeAsync<VwEventRuleOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwEventRule")]
    public async Task AddVwEventRule(VwAddEventRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwEventRule")]
    public async Task UpdateVwEventRule(VwUpdateEventRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete VwEventRule")]
    public async Task DeleteVwEventRule(VwDeleteEventRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple VwEventRule")]
    public async Task BatchDeleteVwEventRule(List<VwDeleteEventRuleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
