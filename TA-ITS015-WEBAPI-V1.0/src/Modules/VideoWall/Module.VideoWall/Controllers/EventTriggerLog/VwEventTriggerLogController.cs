using Module.VideoWall.Core.Dto.EventTriggerLog;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.EventTriggerLog;

/// <summary>
/// Author: Đạt
/// Description: API nhật ký kích hoạt kịch bản và thao tác thiết bị Video Wall (VwEventTriggerLog).
///
/// Bảng CHỈ GHI THÊM: chỉ có Add + Page/GetList, KHÔNG có Update/Delete/BatchDelete. Muốn dọn log cũ
/// thì làm job xoá theo TriggeredAt, không mở qua API — log đấu nối/kích hoạt là bằng chứng, không phải
/// dữ liệu nghiệp vụ sửa được.
///
/// Toàn bộ endpoint đọc và ghi đều tuân theo pipeline xác thực JWT mặc định của hệ thống.
/// Created date: 26/08/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 480)]
public class VwEventTriggerLogController : BaseController
{
    /// <summary>
    /// Author: Đạt
    /// Description: Lấy nhật ký kích hoạt kịch bản và thao tác thiết bị theo phân trang, mới nhất lên đầu.
    /// Created date: 26/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get VwEventTriggerLog page list")]
    public async Task<SqlSugarPagedList<VwPageEventTriggerLogOutput>> Page(VwPageEventTriggerLogInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageEventTriggerLogOutput>>(input);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Lấy nhật ký kích hoạt kịch bản và thao tác thiết bị không phân trang, có trần số bản ghi (input.Take).
    /// Created date: 26/08/2026
    /// </summary>
    [DisplayName("Get VwEventTriggerLog list")]
    public async Task<List<VwEventTriggerLogOutput>> GetList([FromQuery] VwEventTriggerLogInput input)
    {
        return await MessBus.InvokeAsync<List<VwEventTriggerLogOutput>>(input);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Ghi 1 dòng nhật ký thao tác thiết bị hoặc kích hoạt kịch bản.
    ///              OrgId / ControllerName / TriggeredAt / OperatorName do server đóng dấu.
    /// Created date: 26/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwEventTriggerLog")]
    public async Task AddVwEventTriggerLog(VwAddEventTriggerLogInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
