
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.TrafficData.Dto
{

    /// <summary>
    /// Author: KhoaND
    /// Description: DTO phân trang
    /// Created date: 14/11/2024
    /// </summary>
    public class PageTmsTrafficDataInput : BasePageInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Lane { get; set; }
        public string? Direction { get; set; }
        public string? EquipmentId { get; set; }
        public string? Zone { get; set; }
        public string? Type { get; set; }
        public string? LicensePlate { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 12/11/2024
    /// Description: DTO tìm kiếm
    /// </summary>
    public class TrafficDataInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Lane { get; set; }
        public string? Direction { get; set; }
        public string? EquipmentId { get; set; }
        public string? Zone { get; set; }
        public string? Type { get; set; }
        public string? LicensePlate { get; set; }

        public bool? IsCount {get;set;}
    }


    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số dữ liệu trong một giờ
    /// Created date: 12/11/2024
    /// </summary>
    public class TmsTrafficDataHourVolumeInput
    {
        public DateTime? ValueDate { get; set; }
        public string? Direction { get; set; }
        public string? Zone { get; set; }
        public string? Lane { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số dữ liệu trong một ngày
    /// Created date: 12/11/2024
    /// </summary>
    public class TmsTrafficDataDayVolumeInput : TmsTrafficDataHourVolumeInput
    {
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số dữ liệu trung bình
    /// Created date: 12/11/2024
    /// </summary>
    public class TmsTrafficAverageChartInput
    {
        public DateTime? ValueDate { get; set; }
        public string? Direction { get; set; }
        public string? Zone { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số dữ liệu ngày hiện tại
    /// Created date: 12/11/2024
    /// </summary>
    public class TmsTrafficDataToDayChartInput
    {
        public string? Zone { get; set; }
    }
}
