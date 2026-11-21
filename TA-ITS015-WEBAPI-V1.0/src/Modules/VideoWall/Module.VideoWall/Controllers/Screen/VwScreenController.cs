using Module.VideoWall.Core.Dto.Screen;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.Screen;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Màn hình trong tường (VwScreen)
/// Created date: 2026-08-01
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 520)]
public class VwScreenController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách VwScreen theo phân trang
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get VwScreen page list")]
    public virtual async Task<SqlSugarPagedList<VwPageScreenOutput>> Page(VwPageScreenInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageScreenOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách VwScreen (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwScreen list")]
    public async Task<List<VwScreenOutput>> GetList([FromQuery] VwScreenInput input)
    {
        return await MessBus.InvokeAsync<List<VwScreenOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 VwScreen theo ID
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwScreen by id")]
    public async Task<VwScreenOutput> GetById([FromQuery] VwIdScreenInput input)
    {
        return await MessBus.InvokeAsync<VwScreenOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwScreen")]
    public async Task AddVwScreen(VwAddScreenInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwScreen")]
    public async Task UpdateVwScreen(VwUpdateScreenInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete VwScreen")]
    public async Task DeleteVwScreen(VwDeleteScreenInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple VwScreen")]
    public async Task BatchDeleteVwScreen(List<VwDeleteScreenInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Lưu sơ đồ màn (đặt màn lên ô + gán cổng ra cho màn) trong 1 lần.
    /// Created date: 07/11/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "SaveWallMap"), HttpPost]
    [DisplayName("Save VwScreen wall map")]
    public async Task SaveWallMap(VwSaveWallMapInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
