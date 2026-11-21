namespace Modules.TMS.Report.Controllers.Incident.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin danh sách sự cố cho report
    /// Created date: 04/11/2024
    /// </summary>
    public class IncidentListReport
    {
        public IncidentListParameters Parameters { get; set; }
        public IncidentListColumnNames Columns { get; set; }
        public List<IncidentOutput> IncidentList { get; set; }
        public IncidentListReport()
        {
            this.Parameters = new IncidentListParameters();
            this.Columns = new IncidentListColumnNames();
            this.IncidentList = new List<IncidentOutput>();
        }
    }

    public class IncidentListParameters
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportLogo { get; set; } = string.Empty;
        public string ReportSign { get; set; } = string.Empty;
        public string DataDate { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
        public string PageLabel { get; set; } = string.Empty;
        public string ReportFooter { get; set; } = string.Empty;
    }

    public class IncidentListColumnNames
    {
        public string Idx { get; set; } = "Idx";
        public string Name { get; set; } = "Name";
        public string Code { get; set; } = "Code";
        public string StartDate { get; set; } = "StartDate";
        public string FinishedDate { get; set; } = "EndDate";
        public string EventType { get; set; } = "EventType";
        public string Position { get; set; } = "Position";
        public string Zone { get; set; } = "Zone";
        public string Source { get; set; } = "Source";
        public string TrafficPlan { get; set; } = "TrafficPlan";
        public string State { get; set; } = "State";
    }
}
