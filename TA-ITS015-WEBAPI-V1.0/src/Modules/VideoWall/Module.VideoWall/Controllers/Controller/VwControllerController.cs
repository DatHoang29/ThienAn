using Module.VideoWall.Core.Dto.Controller;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.Controller;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý bộ điều khiển Video Wall (VwController)
/// Created date: 2026-08-01
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 500)]
public class VwControllerController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách VwController theo phân trang
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get VwController page list")]
    public virtual async Task<SqlSugarPagedList<VwPageControllerOutput>> Page(VwPageControllerInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageControllerOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách VwController (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwController list")]
    public async Task<List<VwControllerOutput>> GetList([FromQuery] VwControllerInput input)
    {
        return await MessBus.InvokeAsync<List<VwControllerOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 VwController theo ID
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwController by id")]
    public async Task<VwControllerOutput> GetById([FromQuery] VwIdControllerInput input)
    {
        return await MessBus.InvokeAsync<VwControllerOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwController")]
    public async Task AddVwController(VwAddControllerInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwController")]
    public async Task UpdateVwController(VwUpdateControllerInput input)
    {
        await MessBus.InvokeAsync<VwUpdateControllerOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete VwController")]
    public async Task DeleteVwController(VwDeleteControllerInput input)
    {
        await MessBus.InvokeAsync<VwDeleteControllerOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều VwController
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple VwController")]
    public async Task BatchDeleteVwController(List<VwDeleteControllerInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Lấy danh sách kênh tín hiệu đầu vào video từ thiết bị controller vật lý
    /// Created date: 17/08/2026
    /// </summary>
    [DisplayName("Get controller input channels")]
    public async Task<List<VwGetInputChannelsControllerOutput>> GetInputChannels([FromQuery] VwGetInputChannelsControllerInput input)
    {
        return await MessBus.InvokeAsync<List<VwGetInputChannelsControllerOutput>>(input);
    }
}
