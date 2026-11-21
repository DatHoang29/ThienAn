using Module.VideoWall.Core.Dto.WindowScene;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.WindowScene;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Cửa sổ hiển thị trong kịch bản (VwWindowScene)
/// Created date: 2026-08-01
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 550)]
public class VwWindowSceneController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách VwWindowScene theo phân trang
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get VwWindowScene page list")]
    public virtual async Task<SqlSugarPagedList<VwPageWindowSceneOutput>> Page(VwPageWindowSceneInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageWindowSceneOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách VwWindowScene (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwWindowScene list")]
    public async Task<List<VwWindowSceneOutput>> GetList([FromQuery] VwWindowSceneInput input)
    {
        return await MessBus.InvokeAsync<List<VwWindowSceneOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 VwWindowScene theo ID
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwWindowScene by id")]
    public async Task<VwWindowSceneOutput> GetById([FromQuery] VwIdWindowSceneInput input)
    {
        return await MessBus.InvokeAsync<VwWindowSceneOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwWindowScene")]
    public async Task AddVwWindowScene(VwAddWindowSceneInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwWindowScene")]
    public async Task UpdateVwWindowScene(VwUpdateWindowSceneInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete VwWindowScene")]
    public async Task DeleteVwWindowScene(VwDeleteWindowSceneInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple VwWindowScene")]
    public async Task BatchDeleteVwWindowScene(List<VwDeleteWindowSceneInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Đổi nguồn tín hiệu trực tiếp cho 1 cửa sổ trên Video Wall
    /// Created date: 17/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "SwitchSource"), HttpPost]
    [DisplayName("Switch window signal source")]
    public async Task SwitchSource(VwSwitchWindowSourceInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Điều chỉnh thứ tự xếp lớp Z-Order (Top/Bottom) của cửa sổ trên Video Wall
    /// Created date: 17/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "SetLayer"), HttpPost]
    [DisplayName("Set window layer order")]
    public async Task SetLayer(VwSetWindowLayerInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
