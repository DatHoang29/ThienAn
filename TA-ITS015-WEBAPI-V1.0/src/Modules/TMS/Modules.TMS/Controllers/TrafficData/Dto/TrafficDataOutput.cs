using Modules.TMS.Core.Entities;
using Shared.Utility.Helpers;

namespace Modules.TMS.Controllers.TrafficData.Dto
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto chứa thông tin traffic 
    /// Created date: 14/11/2024
    /// </summary>
    public class PageTmsTrafficDataOutput : TmsTrafficData
    {
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin traffic
    /// Created date: 12/11/2024
    /// </summary>
    public class TrafficDataOutput : TmsTrafficData
    {
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin lưu lượng
    /// Created date: 12/11/2024
    /// </summary>
    public class TmsTrafficDataVolume
    {
        public string? Zone { get; set; }
        public string? Direction { get; set; }
        public string? Lane { get; set; }
        public string? VehicleType { get; set; }
        public int? VehicleCount { get; set; }
        public int? DetectHour { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO dữ liệu traffic trung bình
    /// Created date: 12/11/2024
    /// </summary>
    public class TmsTrafficDataAverage : TmsTrafficDataVolume
    {
        public int? AverageSpeed { get; set; }
        public int? SpeedLimit { get; set; }
        public bool? IsSlow { get; set; }
        public bool? IsStop { get; set; }
        public bool? IsWay { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO dữ liệu traffic theo zone
    /// Created date: 12/11/2024
    /// </summary>
    public class TmsZoneTrafficData : TmsTrafficDataAverage { }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 12/11/2024
    /// </summary>
    public class TmsTrafficDataToDayChartOutput : BaseHelper.EChartHp.EChartResponse
    {
    }
}
