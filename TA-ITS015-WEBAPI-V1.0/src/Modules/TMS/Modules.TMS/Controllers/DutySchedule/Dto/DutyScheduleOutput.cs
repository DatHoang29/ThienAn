using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.DutySchedule.Dto;

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: Output for DutySchedule
/// </summary>
public class DutyScheduleOutput : TmsDutySchedule
{
    /// <summary>
    /// Danh sách chi tiết người dùng
    /// </summary>
    public List<DutyScheduleDetailOutput>? Details { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: Output for DutyScheduleDetail
/// </summary>
public class DutyScheduleDetailOutput : TmsDutyScheduleDetail
{
}
