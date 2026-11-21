using Modules.Report.Base.Core.Dto;

namespace Modules.VMS.Report.Controllers.OperationLogHistory.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO chứa tham số kết xuất báo cáo lịch sử điều khiển thiết bị VMS/LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsOperationLogHistoryListRenderInput : BaseReportInput
    {
        public string? ModuleKey { get; set; } = "VMS";
        public string? EquipmentTypeId { get; set; }
        public string? EquipmentId { get; set; }
        public string? State { get; set; }
        public string? ExecuteBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO chứa tham số xuất file báo cáo lịch sử điều khiển thiết bị VMS/LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsOperationLogHistoryListExportInput : VmsOperationLogHistoryListRenderInput
    {
        public string? Format { get; set; }
    }
}
