namespace Modules.TMS.Report.Controllers.Incident.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin chi tiết sự cố cho report
    /// Created date: 04/11/2024
    /// </summary>
    public class IncidentDetailReport
    {
        public IncidentDetailParameters Parameters { get; set; }
        public IncidentDetailColumnNames IncidentColumns { get; set; }
        public WorkExecuteColumnNames WorkExecuteColumns { get; set; }
        public IncidentOutput Incident { get; set; }

        public IncidentDetailReport()
        {
            this.Parameters = new IncidentDetailParameters();
            this.IncidentColumns = new IncidentDetailColumnNames();
            this.WorkExecuteColumns = new WorkExecuteColumnNames();
            this.Incident = new IncidentOutput();
        }
    }

    public class IncidentDetailParameters
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportLogo { get; set; } = string.Empty;
        public string ReportSign { get; set; } = string.Empty;
        public string DataDate { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
        public string PageLabel { get; set; } = string.Empty;
        public string ReportFooter { get; set; } = string.Empty;
        public string IncidentSectionTitle { get; set; } = string.Empty;
        public string WorkExecSectionTitle { get; set; } = string.Empty;
    }

    public class IncidentDetailColumnNames
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
        public string Information { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
    }

    public class WorkExecuteColumnNames
    {
        public string Idx { get; set; } = "Idx";
        public string Step { get; set; } = "Step";
        public string Name { get; set; } = "Name";
        public string State { get; set; } = "State";
        public string WorkMode { get; set; } = "WorkMode";
        public string ControlType { get; set; } = "ControlType";
        public string ExecuteBy { get; set; } = "ExecuteBy";
        public string ExecuteOrg { get; set; } = "ExecuteOrg";
        public string ExecuteDate { get; set; } = "ExecuteDate";
        public string Description { get; set; } = "Description";
        public string Remark { get; set; } = "Remark";
    }
}
