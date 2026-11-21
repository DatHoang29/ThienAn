using Modules.Report.Base.Core.Dto;

namespace Modules.VMS.Report.Controllers.EquipmentHistory.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO chứa tham số render danh sách dữ liệu lịch sử thiết bị VMS/LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryListRenderInput : BaseReportInput
    {
        public string? EquipmentId { get; set; }
        public string? EquipmentTypeId { get; set; }
        public string? State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO chứa tham số export danh sách dữ liệu lịch sử thiết bị VMS/LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryListExportInput : VmsEquipmentHistoryListRenderInput
    {
        public string? Format { get; set; }
    }
}
