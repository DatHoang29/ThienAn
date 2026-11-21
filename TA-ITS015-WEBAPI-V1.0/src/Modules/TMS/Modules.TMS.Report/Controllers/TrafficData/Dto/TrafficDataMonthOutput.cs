namespace Modules.TMS.Report.Controllers.TrafficData.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin danh sách sự cố cho report
    /// Created date: 04/11/2024
    /// </summary>
    public class TrafficDataMonthReport
    {
        public TrafficDataMonthParameters Parameters { get; set; }
        public TrafficDataMonthColumnNames Columns { get; set; }
        public List<TrafficDataDayOutput> TrafficDataDayList { get; set; }

        public int? TotalByMonth { get; set; } // Tổng tất cả theo tháng
        public int? TotalByMonthAndShortLength { get; set; } // Tổng theo tháng theo loại xe < 6m
        public int? TotalByMonthAndMediumLength { get; set; } // Tổng theo tháng theo loại xe 6m-12m
        public int? TotalByMonthAndLongLength { get; set; } // Tổng theo tháng theo loại xe > 12m
        public int? TotalByMonthAndVeryLongLength { get; set; } // Tổng theo tháng theo loại xe rất dài
        public int? TotalByMonthAndExtraLongLength { get; set; } // Tổng theo tháng theo loại xe rất rất dài
        public TrafficDataMonthReport()
        {
            this.Parameters = new TrafficDataMonthParameters();
            this.Columns = new TrafficDataMonthColumnNames();
            this.TrafficDataDayList = new List<TrafficDataDayOutput>();
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created Date: 04/11/2024
    /// </summary>
    public class TrafficDataMonthParameters
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
    public class TrafficDataMonthColumnNames
    {
        public string Day { get; set; } = "Day";
        public string Time { get; set; } = "Time";
        public string Direction { get; set; } = "Direction";
        public string ShortLength { get; set; } = "ShortLength";
        public string MediumLength { get; set; } = "MediumLength";
        public string LongLength { get; set; } = "LongLength";
        public string DirTotal { get; set; } = "DirTotal";
        public string ShiftTotal { get; set; } = "DirTotal";
        public string DayTotal { get; set; } = "DayTotal";
        public string MonthTotal { get; set; } = "MonthTotal";
    }
}
