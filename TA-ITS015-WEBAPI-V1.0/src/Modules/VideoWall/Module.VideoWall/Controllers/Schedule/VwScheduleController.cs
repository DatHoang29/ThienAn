using Module.VideoWall.Core.Dto.Schedule;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.Schedule;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Lịch phát kịch bản tự động (VwSchedule)
/// Created date: 2026-08-01
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 560)]
public class VwScheduleController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách VwSchedule theo phân trang
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get VwSchedule page list")]
    public virtual async Task<SqlSugarPagedList<VwPageScheduleOutput>> Page(VwPageScheduleInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageScheduleOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách VwSchedule (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwSchedule list")]
    public async Task<List<VwScheduleOutput>> GetList([FromQuery] VwScheduleInput input)
    {
        return await MessBus.InvokeAsync<List<VwScheduleOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 VwSchedule theo ID
    /// Created date: 2026-08-01
    /// </summary>
    [DisplayName("Get VwSchedule by id")]
    public async Task<VwScheduleOutput> GetById([FromQuery] VwIdScheduleInput input)
    {
        return await MessBus.InvokeAsync<VwScheduleOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add VwSchedule")]
    public async Task AddVwSchedule(VwAddScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update VwSchedule")]
    public async Task UpdateVwSchedule(VwUpdateScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete VwSchedule")]
    public async Task DeleteVwSchedule(VwDeleteScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple VwSchedule")]
    public async Task BatchDeleteVwSchedule(List<VwDeleteScheduleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Chạy ngay 1 VwSchedule (thực hiện hành động + ghi LastRun), không ảnh hưởng job Hangfire
    /// Created date: 06/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "RunNow"), HttpPost]
    [DisplayName("Run VwSchedule now")]
    public async Task RunNowVwSchedule(VwRunNowScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
