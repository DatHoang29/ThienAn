using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.Schedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwSchedule (phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageScheduleOutput : VwSchedule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwSchedule (danh sách / chi tiết)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwScheduleOutput : VwSchedule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddScheduleOutput : VwSchedule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateScheduleOutput : VwSchedule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteScheduleOutput : VwSchedule { }
}
