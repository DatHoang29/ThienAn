using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.Schedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageScheduleInput : BasePageInput
    {
        public string? Name { get; set; }
        public string? TargetSceneId { get; set; }
        public string? Action { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách VwSchedule (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwScheduleInput
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? TargetSceneId { get; set; }
        public string? Action { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 VwSchedule theo ID
    /// Created date: 2026-08-01
    /// </summary>
    public class VwIdScheduleInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddScheduleInput : VwSchedule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateScheduleInput : VwAddScheduleInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteScheduleInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO chạy ngay 1 VwSchedule (nút "Chạy thử")
    /// Created date: 06/10/2026
    /// </summary>
    public class VwRunNowScheduleInput : BaseIdInput { }
}
