namespace Modules.VMS.Report.Controllers.EquipmentHistory.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO thông tin danh sách EquipmentHistory cho report VMS/LCS (map đúng cấu trúc FastReport template EquipmentHistoryListReport.frx)
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryListReport
    {
        public VmsEquipmentHistoryParameters Parameters { get; set; }
        public VmsEquipmentHistoryColumnNames Columns { get; set; }
        public List<VmsEquipmentHistoryData> EquipHistoryList { get; set; }

        public VmsEquipmentHistoryListReport()
        {
            this.Parameters = new VmsEquipmentHistoryParameters();
            this.Columns = new VmsEquipmentHistoryColumnNames();
            this.EquipHistoryList = new List<VmsEquipmentHistoryData>();
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Tham số đầu trang/cuối trang cho báo cáo EquipmentHistory
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryParameters
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
    /// Description: Tên cột hiển thị cho báo cáo EquipmentHistory
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryColumnNames
    {
        public string Idx { get; set; } = "Idx";
        public string Name { get; set; } = "Name";
        public string EquipmentType { get; set; } = "EquipmentType";
        public string OldState { get; set; } = "OldState";
        public string NewState { get; set; } = "NewState";
        public string Remark { get; set; } = "Remark";
        public string DateTime { get; set; } = "DateTime";
    }
}
