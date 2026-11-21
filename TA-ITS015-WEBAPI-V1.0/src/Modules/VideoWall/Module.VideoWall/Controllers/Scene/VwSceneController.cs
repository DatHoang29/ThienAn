using Module.VideoWall.Core.Dto.Scene;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.Scene;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Kịch bản hiển thị (VwScene)
/// Created date: 2026-08-01
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 510)]
public class VwSceneController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách VwScene theo phân trang
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get VwScene page list")]
    public virtual async Task<SqlSugarPagedList<VwPageSceneOutput>> Page(VwPageSceneInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageSceneOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách VwScene (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwScene list")]
    public async Task<List<VwSceneOutput>> GetList([FromQuery] VwSceneInput input)
    {
        return await MessBus.InvokeAsync<List<VwSceneOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 VwScene theo ID
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwScene by id")]
    public async Task<VwSceneOutput> GetById([FromQuery] VwIdSceneInput input)
    {
        return await MessBus.InvokeAsync<VwSceneOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwScene")]
    public async Task AddVwScene(VwAddSceneInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwScene")]
    public async Task UpdateVwScene(VwUpdateSceneInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete VwScene")]
    public async Task DeleteVwScene(VwDeleteSceneInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều VwScene
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple VwScene")]
    public async Task BatchDeleteVwScene(List<VwDeleteSceneInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Đặt 1 kịch bản làm mặc định (bỏ cờ mặc định ở các kịch bản khác). Chỉ SuperAdmin.
    /// Created date: 28/09/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "SetDefault"), HttpPost]
    [DisplayName("Set default VwScene")]
    public async Task SetDefaultVwScene(VwSetDefaultSceneInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Kích hoạt 1 kịch bản hiển thị.
    ///              Truyền SceneId để kích hoạt trực tiếp, hoặc truyền EventTypeId để hệ thống
    ///              tra VwEventRule tìm ra kịch bản đích. Sau khi cập nhật DB, hệ thống phát
    ///              bản tin lên kênh NATS ta.its.data.videowallScene để FE đổi giao diện.
    /// Created date: 2026-08-03
    /// </summary>
    [ApiDescriptionSettings(Name = "Active"), HttpPost]
    [DisplayName("Active VwScene")]
    public async Task<VwActiveSceneOutput> ActiveVwScene(VwActiveSceneInput input)
    {
        return await MessBus.InvokeAsync<VwActiveSceneOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy kịch bản đang được kích hoạt kèm danh sách cửa sổ.
    ///              FE gọi khi tải lại trang hoặc khi client mới kết nối để dựng lại giao diện.
    /// Created date: 2026-08-03
    /// </summary>
    [DisplayName("Get active VwScene")]
    public async Task<VwGetActiveSceneOutput> GetActive([FromQuery] VwGetActiveSceneInput input)
    {
        return await MessBus.InvokeAsync<VwGetActiveSceneOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy kịch bản đang chiếu của TỪNG vùng màn hình (mỗi bộ điều khiển một dòng).
    ///              Đây là trạng thái thật của tường khi các vùng chạy kịch bản khác nhau; endpoint
    ///              GetActive chỉ trả được một kịch bản nên chỉ đúng với kịch bản toàn tường.
    /// Created date: 2026-08-13
    /// </summary>
    [DisplayName("Get active VwScene by region")]
    public async Task<List<VwActiveRegionOutput>> GetActiveRegions([FromQuery] VwGetActiveRegionInput input)
    {
        return await MessBus.InvokeAsync<List<VwActiveRegionOutput>>(input);
    }
}
