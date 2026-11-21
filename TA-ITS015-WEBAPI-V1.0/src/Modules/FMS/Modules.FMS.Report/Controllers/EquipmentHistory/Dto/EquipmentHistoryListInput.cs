using Modules.Report.Base.Core.Dto;

namespace Modules.FMS.Report.Controllers.EquipmentHistory.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số render danh sách dữ liệu
    /// Created Date: 04/11/2024
    /// </summary>
    public class EquipmentHistoryListRenderInput : BaseReportInput
    {
        public string? EquipmentId { get; set; }
        public string? EquipmentTypeId { get; set; }
        public string? State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số export danh sách dữ liệu
    /// Created Date: 04/11/2024
    /// </summary>
    public class EquipmentHistoryListExportInput : EquipmentHistoryListRenderInput
    {
        public string? Format { get; set; }
    }
}
