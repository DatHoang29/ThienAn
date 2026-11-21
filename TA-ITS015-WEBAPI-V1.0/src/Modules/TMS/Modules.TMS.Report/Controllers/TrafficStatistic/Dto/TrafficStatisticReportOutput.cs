namespace Modules.TMS.Report.Controllers.TrafficStatistic.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin danh sách TrafficStatistic cho report
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticReport
    {
        public TrafficStatisticParameters Parameters { get; set; }
        public TrafficStatisticColumnNames Columns { get; set; }
        public List<TrafficStatisticReportOutput> DataList { get; set; }
        public TrafficStatisticReport()
        {
            this.Parameters = new TrafficStatisticParameters();
            this.Columns = new TrafficStatisticColumnNames();
            this.DataList = new List<TrafficStatisticReportOutput>();
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticParameters
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportLogo { get; set; } = string.Empty;
        public string ReportSign { get; set; } = string.Empty;
        public string DataDate { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
        public string PageLabel { get; set; } = string.Empty;
        public string ReportUser { get; set; } = string.Empty;
        public string ReportFooter { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticColumnNames
    {
        public string Section { get; set; } = "Section";
        public string Zone { get; set; } = "Zone";
        public string Camera { get; set; } = "Camera";
        public string AvgSpeed { get; set; } = "AvgSpeed";
        public string TotalVehicle { get; set; } = "TotalVehicle";
        public string Amount { get; set; } = "Amount";
        public string SpaceOccupy { get; set; } = "SpaceOccupy";
        public string Time { get; set; } = "Time";
    }
}
