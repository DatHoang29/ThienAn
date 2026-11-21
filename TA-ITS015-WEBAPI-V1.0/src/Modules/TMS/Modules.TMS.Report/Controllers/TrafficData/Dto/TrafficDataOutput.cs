namespace Modules.TMS.Report.Controllers.TrafficData.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO lưu lượng giao thông theo giờ
    /// Created date: 04/11/2024
    /// </summary>
    public class TrafficDataHourOutput
    {
        public int Hour { get; set; }
        public string Title { get; set; }
        public List<TrafficVolumeInfo> TrafficVolumeList { get; set; }

        public TrafficDataHourOutput()
        {
            this.TrafficVolumeList = new List<TrafficVolumeInfo>();
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO lưu lượng giao thông theo ca
    /// Created date: 04/11/2024
    /// </summary>
    public class TrafficDataShiftOutput
    {
        public string Shift { get; set; }
        public string Title { get; set; }
        public int TotalByShift { get; set; } // Tổng theo ca
        public string TotalByShiftStr { get; set; } // Tổng theo ca
        public List<TrafficVolumeInfo> TrafficVolumeList { get; set; }

        public TrafficDataShiftOutput()
        {
            this.TrafficVolumeList = new List<TrafficVolumeInfo>();
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO lưu lượng giao thông theo ngày
    /// Created date: 04/11/2024
    /// </summary>
    public class TrafficDataDayOutput
    {
        public DateTime Day { get; set; }
        public string Title { get; set; }
        public string TotalByLengthTitle { get; set; }
        public int TotalByDayAndShortLength { get; set; } // Tổng theo loại short < 6m
        public int TotalByDayAndMediumLength { get; set; } // Tổng theo loại medium 6m-12m
        public int TotalByDayAndLongLength { get; set; } // Tổng theo loại long > 12m
        public int TotalByDayAndVeryLongLength { get; set; } // Tổng theo loại very-long
        public int TotalByDayAndExtraLongLength { get; set; } // Tổng theo extra-long
        public int TotalByDay { get; set; } // Tổng theo ngày

        public List<TrafficDataShiftOutput> TrafficDataShiftList { get; set; }

        public TrafficDataDayOutput()
        {
            this.TrafficDataShiftList = new List<TrafficDataShiftOutput>();
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin lưu lượng giao thông
    /// </summary>
    public class TrafficVolumeInfo
    {
        public string Direction { get; set; }
        public string DirectionName { get; set; }
        public int ShortLengthVehicleCount { get; set; }
        public int MediumLengthVehicleCount { get; set; }
        public int LongLengthVehicleCount { get; set; }
        public int VeryLongLengthVehicleCount { get; set; }
        public int ExtraLongLengthVehicleCount { get; set; }
        public int TotalVehicleCount { get; set; } // Tổng theo hướng
    }
}
