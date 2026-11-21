namespace Modules.VMS.Report.Controllers.OperationLogHistory.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO danh sách báo cáo lịch sử điều khiển thiết bị VMS/LCS (map đúng cấu trúc FastReport template VmsOperationLogHistoryListReport.frx)
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsOperationLogHistoryListReport
    {
        public VmsOperationLogHistoryParameters Parameters { get; set; }
        public VmsOperationLogHistoryColumnNames Columns { get; set; }
        public List<VmsOperationLogHistoryData> LogHistoryList { get; set; }

        public VmsOperationLogHistoryListReport()
        {
            this.Parameters = new VmsOperationLogHistoryParameters();
            this.Columns = new VmsOperationLogHistoryColumnNames();
            this.LogHistoryList = new List<VmsOperationLogHistoryData>();
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Tham số tiêu đề và thông tin chung trên báo cáo lịch sử điều khiển
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsOperationLogHistoryParameters
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportLogo { get; set; } = string.Empty;
        public string ReportSign { get; set; } = string.Empty;
        public string DataDate { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
        public string PageLabel { get; set; } = string.Empty;
        public string ReportFooter { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Tiêu đề các cột trên báo cáo lịch sử điều khiển (STT, Thời gian, Thiết bị, Trước tác động, Sau tác động, Trạng thái, Người thực hiện)
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsOperationLogHistoryColumnNames
    {
        public string Idx { get; set; } = "STT";
        public string ExecuteTime { get; set; } = "Thời gian thực thi";
        public string EquipmentName { get; set; } = "Thiết bị";
        public string OldData { get; set; } = "Trước tác động";
        public string NewData { get; set; } = "Sau tác động";
        public string State { get; set; } = "Trạng thái";
        public string ExecuteBy { get; set; } = "Người thực hiện";
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Dữ liệu từng dòng của báo cáo lịch sử điều khiển thiết bị
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsOperationLogHistoryData
    {
        public string? Idx { get; set; }
        public string? ExecuteTime { get; set; }
        public string? EquipmentName { get; set; }
        public string? EquipmentPosition { get; set; }
        public string? EquipmentDisplayName { get; set; }
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        public string? OldImage { get; set; }
        public string? NewImage { get; set; }
        public string? StateName { get; set; }
        public string? ExecuteBy { get; set; }
    }
}
