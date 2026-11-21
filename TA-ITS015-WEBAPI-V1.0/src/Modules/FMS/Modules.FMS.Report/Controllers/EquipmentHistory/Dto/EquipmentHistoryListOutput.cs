namespace Modules.FMS.Report.Controllers.EquipmentHistory.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin danh sách EquipmentHistory cho report
    /// Created Date: 04/11/2024
    /// </summary>
    public class EquipmentHistoryListReport
    {
        public EquipmentHistoryParameters Parameters { get; set; }
        public EquipmentHistoryColumnNames Columns { get; set; }
        public List<EquipmentHistoryData> EquipHistoryList { get; set; }
        public EquipmentHistoryListReport()
        {
            this.Parameters = new EquipmentHistoryParameters();
            this.Columns = new EquipmentHistoryColumnNames();
            this.EquipHistoryList = new List<EquipmentHistoryData>();
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created Date: 04/11/2024
    /// </summary>
    public class EquipmentHistoryParameters
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
    /// Author: ThangNN
    /// Created Date: 04/11/2024
    /// </summary>
    public class EquipmentHistoryColumnNames
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
