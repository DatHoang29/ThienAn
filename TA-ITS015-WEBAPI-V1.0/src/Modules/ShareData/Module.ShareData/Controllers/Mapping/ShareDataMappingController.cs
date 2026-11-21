using Module.ShareData.Core.Dto.Mapping;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Controllers.Mapping;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Phễu lọc
/// Created date: 2026-08-21
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 640)]
public class ShareDataMappingController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách phễu lọc theo phân trang
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get ShareDataMapping page list")]
    public virtual async Task<SqlSugarPagedList<ShareDataPageMappingOutput>> Page(ShareDataPageMappingInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ShareDataPageMappingOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách phễu lọc theo tiêu chí lọc (không phân trang)
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Get ShareDataMapping list")]
    public async Task<List<ShareDataMappingOutput>> GetList([FromQuery] ShareDataMappingInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataMappingOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 phễu lọc theo ID (kèm danh sách dòng ánh xạ đã giải mã)
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Get ShareDataMapping by id")]
    public async Task<ShareDataMappingOutput> GetById([FromQuery] ShareDataIdMappingInput input)
    {
        return await MessBus.InvokeAsync<ShareDataMappingOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ShareDataMapping")]
    public async Task<ShareDataMappingOutput> AddShareDataMapping(ShareDataAddMappingInput input)
    {
        return await MessBus.InvokeAsync<ShareDataMappingOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ShareDataMapping")]
    public async Task<ShareDataMappingOutput> UpdateShareDataMapping(ShareDataUpdateMappingInput input)
    {
        return await MessBus.InvokeAsync<ShareDataMappingOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 phễu lọc (xóa mềm)
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ShareDataMapping")]
    public async Task DeleteShareDataMapping(ShareDataDeleteMappingInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều phễu lọc (xóa mềm)
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple ShareDataMapping")]
    public async Task BatchDeleteShareDataMapping(List<ShareDataDeleteMappingInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Bật / tắt 1 phễu lọc. Bật thì tự tắt bản đang dùng cùng bộ ba trong 1 transaction.
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "SetActive"), HttpPost]
    [DisplayName("Set ShareDataMapping active")]
    public async Task<ShareDataMappingOutput> SetActiveShareDataMapping(ShareDataSetActiveMappingInput input)
    {
        return await MessBus.InvokeAsync<ShareDataMappingOutput>(input);
    }
}
