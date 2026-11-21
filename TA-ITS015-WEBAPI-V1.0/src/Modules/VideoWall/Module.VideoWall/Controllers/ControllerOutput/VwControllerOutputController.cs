using Module.VideoWall.Core.Dto.ControllerOutput;

namespace Module.VideoWall.Controllers.ControllerOutput;

/// <summary>
/// Description: API cổng ra của bộ điều khiển (VwControllerOutput) — dữ liệu đồng bộ từ thiết bị,
///              chỉ sửa được tên hiển thị / ghi chú / trạng thái. Đồng bộ: VwDeviceSetup/SyncOutputs.
/// Created date: 06/11/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 519)]
public class VwControllerOutputController : BaseController
{
    /// <summary>Danh sách cổng ra kèm bộ điều khiển và màn đang cắm.</summary>
    [DisplayName("Get VwControllerOutput list")]
    public async Task<List<VwControllerOutputOutput>> GetList([FromQuery] VwControllerOutputInput input)
    {
        return await MessBus.InvokeAsync<List<VwControllerOutputOutput>>(input);
    }

    /// <summary>Chi tiết 1 cổng ra.</summary>
    [DisplayName("Get VwControllerOutput by id")]
    public async Task<VwControllerOutputOutput> GetById([FromQuery] VwIdControllerOutputInput input)
    {
        return await MessBus.InvokeAsync<VwControllerOutputOutput>(input);
    }

    /// <summary>Sửa tên hiển thị / ghi chú / trạng thái của cổng ra.</summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwControllerOutput")]
    public async Task Update(VwUpdateControllerOutputInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>Khai báo cổng của bộ phân phối cấp tín hiệu cho bộ hiển thị nào.</summary>
    [ApiDescriptionSettings(Name = "SetFeeds"), HttpPost]
    [DisplayName("Set VwControllerOutput feeds controller")]
    public async Task SetFeeds(VwSetFeedsControllerOutputInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
