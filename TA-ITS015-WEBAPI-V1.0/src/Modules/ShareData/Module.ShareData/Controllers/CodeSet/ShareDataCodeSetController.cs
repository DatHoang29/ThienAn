using Module.ShareData.Core.Dto.CodeSet;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Controllers.CodeSet;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Bộ mã quy đổi giá trị (ShareDataCodeSet)
/// Created date: 2026-08-21
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 645)]
public class ShareDataCodeSetController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách bộ mã theo phân trang
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get ShareDataCodeSet page list")]
    public virtual async Task<SqlSugarPagedList<ShareDataPageCodeSetOutput>> Page(ShareDataPageCodeSetInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ShareDataPageCodeSetOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách bộ mã (không phân trang) — dùng cho dropdown màn Phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Get ShareDataCodeSet list")]
    public async Task<List<ShareDataCodeSetOutput>> GetList([FromQuery] ShareDataCodeSetInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataCodeSetOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 bộ mã theo ID (kèm bảng quy đổi đã giải mã)
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Get ShareDataCodeSet by id")]
    public async Task<ShareDataCodeSetOutput> GetById([FromQuery] ShareDataIdCodeSetInput input)
    {
        return await MessBus.InvokeAsync<ShareDataCodeSetOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ShareDataCodeSet")]
    public async Task<ShareDataCodeSetOutput> AddShareDataCodeSet(ShareDataAddCodeSetInput input)
    {
        return await MessBus.InvokeAsync<ShareDataCodeSetOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ShareDataCodeSet")]
    public async Task<ShareDataCodeSetOutput> UpdateShareDataCodeSet(ShareDataUpdateCodeSetInput input)
    {
        return await MessBus.InvokeAsync<ShareDataCodeSetOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 bộ mã (xóa mềm). Chặn khi bộ mã đang được gói tin hoặc phễu lọc tham chiếu.
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ShareDataCodeSet")]
    public async Task DeleteShareDataCodeSet(ShareDataDeleteCodeSetInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều bộ mã (xóa mềm)
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple ShareDataCodeSet")]
    public async Task BatchDeleteShareDataCodeSet(List<ShareDataDeleteCodeSetInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
