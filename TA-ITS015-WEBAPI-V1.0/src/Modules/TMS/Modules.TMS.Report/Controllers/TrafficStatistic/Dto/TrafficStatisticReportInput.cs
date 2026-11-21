using Modules.Report.Base.Core.Dto;

namespace Modules.TMS.Report.Controllers.TrafficStatistic.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số render danh sách dữ liệu
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticSectionRenderInput : BaseReportInput
    {
        public string? ZoneId { get; set; }
        public string? Cycle { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số export danh sách dữ liệu
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticSectionExportInput : TrafficStatisticSectionRenderInput
    {
        public string? Format { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số render danh sách dữ liệu
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticZoneRenderInput : BaseReportInput
    {
        public string? Cycle { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string[] ZoneIdList { get; set; }
        public string[] EquipmentIdList { get; set; }
        public string? ZoneType { get; set; }
        public string? EquipmentType { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số export danh sách dữ liệu
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticZoneExportInput : TrafficStatisticZoneRenderInput
    {
        public string? Format { get; set; }
    }
}
