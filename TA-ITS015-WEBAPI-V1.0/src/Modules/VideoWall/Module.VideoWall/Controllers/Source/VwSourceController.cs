using Module.VideoWall.Core.Dto.Source;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.Source;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Nguồn tín hiệu đầu vào (VwSource)
/// Created date: 2026-08-01
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 540)]
public class VwSourceController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách VwSource theo phân trang
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get VwSource page list")]
    public virtual async Task<SqlSugarPagedList<VwPageSourceOutput>> Page(VwPageSourceInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageSourceOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách VwSource (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwSource list")]
    public async Task<List<VwSourceOutput>> GetList([FromQuery] VwSourceInput input)
    {
        return await MessBus.InvokeAsync<List<VwSourceOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 VwSource theo ID
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwSource by id")]
    public async Task<VwSourceOutput> GetById([FromQuery] VwIdSourceInput input)
    {
        return await MessBus.InvokeAsync<VwSourceOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwSource")]
    public async Task AddVwSource(VwAddSourceInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwSource")]
    public async Task UpdateVwSource(VwUpdateSourceInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete VwSource")]
    public async Task DeleteVwSource(VwDeleteSourceInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều VwSource
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple VwSource")]
    public async Task BatchDeleteVwSource(List<VwDeleteSourceInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
