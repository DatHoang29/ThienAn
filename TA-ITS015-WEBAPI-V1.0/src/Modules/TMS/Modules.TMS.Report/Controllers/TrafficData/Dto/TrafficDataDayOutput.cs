namespace Modules.TMS.Report.Controllers.TrafficData.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin danh sách sự cố cho report
    /// Created date: 04/11/2024
    /// </summary>
    public class TrafficDataDayReport
    {
        public TrafficDataDayParameters Parameters { get; set; }
        public TrafficDataDayColumnNames Columns { get; set; }
        public List<TrafficDataHourOutput> TrafficDataList { get; set; }
        public int TotalByDayAndShortLength { get; set; } // Tổng theo loại short < 6m
        public int TotalByDayAndMediumLength { get; set; } // Tổng theo loại medium 6m-12m
        public int TotalByDayAndLongLength { get; set; } // Tổng theo loại long > 12m
        public int TotalByDayAndVeryLongLength { get; set; } // Tổng theo loại very-long
        public int TotalByDayAndExtraLongLength { get; set; } // Tổng theo extra-long
        public int TotalByDay { get; set; } // Tổng tất cả
        public TrafficDataDayReport()
        {
            this.Parameters = new TrafficDataDayParameters();
            this.Columns = new TrafficDataDayColumnNames();
            this.TrafficDataList = new List<TrafficDataHourOutput>();
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created Date: 04/11/2024
    /// </summary>

    public class TrafficDataDayParameters
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
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
    public class TrafficDataDayColumnNames
    {
        public string Time { get; set; } = "Time";
        public string Direction { get; set; } = "Direction";
        public string ShortLength { get; set; } = "ShortLength";
        public string MediumLength { get; set; } = "MediumLength";
        public string LongLength { get; set; } = "LongLength";
        public string ShiftTotal { get; set; } = "Total";
        public string HourTotal { get; set; } = "HourTotal";

    }
}