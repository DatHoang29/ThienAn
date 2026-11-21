using Module.VideoWall.Core.Dto.WallTopology;

namespace Module.VideoWall.Controllers.WallTopology;

/// <summary>
/// Description: API cấu hình tường màn hình (VwWallTopology) — số hàng, số cột và kích thước
///              một panel. Web đọc endpoint này để dựng lưới và quy đổi toạ độ cửa sổ, thay vì
///              gắn cứng hằng số phía client rồi lệch với backend.
/// Created date: 09/09/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 505)]
public class VwWallTopologyController : BaseController
{
    /// <summary>
    /// Description: Lấy cấu hình tường đang dùng. Chưa cấu hình thì trả giá trị mặc định
    ///              kèm cờ IsDefault = true (không ghi CSDL).
    /// Created date: 09/09/2026
    /// </summary>
    [DisplayName("Get current VwWallTopology")]
    public async Task<VwWallTopologyOutput> GetCurrent([FromQuery] VwCurrentWallTopologyInput input)
    {
        return await MessBus.InvokeAsync<VwWallTopologyOutput>(input);
    }

    /// <summary>
    /// Description: Danh sách bố cục tường (đang dùng trước) kèm khối, số kịch bản và số phân quyền.
    /// Created date: 22/10/2026 — cập nhật 26/10/2026 (quản lý nhiều bố cục)
    /// </summary>
    [DisplayName("Get VwWallTopology list")]
    public async Task<List<VwWallTopologyOutput>> GetList([FromQuery] VwListWallTopologyInput input)
    {
        return await MessBus.InvokeAsync<List<VwWallTopologyOutput>>(input);
    }

    /// <summary>
    /// Description: Lấy 1 bố cục tường theo ID (VD: vẽ kịch bản thuộc bố cục khác bố cục đang dùng).
    /// Created date: 22/10/2026
    /// </summary>
    [DisplayName("Get VwWallTopology by id")]
    public async Task<VwWallTopologyOutput> GetById([FromQuery] VwIdWallTopologyInput input)
    {
        return await MessBus.InvokeAsync<VwWallTopologyOutput>(input);
    }

    /// <summary>
    /// Description: Thêm bố cục tường (mã, tên, lưới khối, kích thước ô).
    /// Created date: 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwWallTopology")]
    public async Task AddVwWallTopology(VwAddWallTopologyInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Sửa bố cục tường.
    /// Created date: 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwWallTopology")]
    public async Task UpdateVwWallTopology(VwUpdateWallTopologyInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Xoá bố cục tường (không phải bố cục đang dùng) cùng kịch bản, lập lịch, liên kết sự kiện, phân quyền của nó.
    /// Created date: 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete VwWallTopology")]
    public async Task DeleteVwWallTopology(VwDeleteWallTopologyInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Đặt 1 bố cục làm bố cục đang dùng; kịch bản / lập lịch / liên kết sự kiện của bố cục khác chuyển Không dùng.
    /// Created date: 22/10/2026 — cập nhật 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "SetCurrent"), HttpPost]
    [DisplayName("Set current VwWallTopology")]
    public async Task SetCurrent(VwSetCurrentWallTopologyInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Lưu bố cục tường trong 1 transaction: thông tin chung + màn hình mẫu, toàn bộ khối,
    ///              và các màn hình thêm / sửa / xoá trên lưới. Hệ thống giữ đúng một bản ghi tường, sửa tại chỗ;
    ///              thu nhỏ lưới đè lên dữ liệu đang dùng bị chặn (xem VwWallTopologyCommandHandler).
    /// Created date: 22/10/2026 (thay API Upsert 09/09/2026) — cập nhật 26/10/2026 (bỏ phiên bản)
    /// </summary>
    [ApiDescriptionSettings(Name = "SaveLayout"), HttpPost]
    [DisplayName("Save VwWallTopology layout")]
    public async Task SaveLayout(VwSaveWallLayoutInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
