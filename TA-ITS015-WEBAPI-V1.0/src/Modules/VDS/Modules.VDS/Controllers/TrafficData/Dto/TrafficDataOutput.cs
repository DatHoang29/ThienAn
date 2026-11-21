using Shared.Utility.Serialization;
using Shared.Utility.Helpers;

namespace Modules.VDS.Controllers.TrafficData.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: DTO phân trang danh sách traffic data
    /// Created date: 24/08/2026
    /// </summary>
    public class PageTrafficDataOutput
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime DetectTime { get; set; }
        public long VendorEventId { get; set; }
        public string? CameraIp { get; set; }
        public string? EquipmentId { get; set; }
        public string? Direction { get; set; }
        public string? ZoneId { get; set; }
        public string? Lane { get; set; }
        public string? EventType { get; set; }
        public string? AidEventType { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleColor { get; set; }
        public string? VehicleSize { get; set; }
        public double? Speed { get; set; }
        public int? LengthMm { get; set; }
        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }
        public int? SpeedMin { get; set; }
        public int? SpeedMax { get; set; }
        public string? PlateColor { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO danh sách traffic data (không phân trang)
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataOutput
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime DetectTime { get; set; }
        public long VendorEventId { get; set; }
        public string? CameraIp { get; set; }
        public string? EquipmentId { get; set; }
        public string? Direction { get; set; }
        public string? ZoneId { get; set; }
        public string? Lane { get; set; }
        public string? EventType { get; set; }
        public string? AidEventType { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleColor { get; set; }
        public string? VehicleSize { get; set; }
        public double? Speed { get; set; }
        public int? LengthMm { get; set; }
        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }
        public int? SpeedMin { get; set; }
        public int? SpeedMax { get; set; }
        public string? PlateColor { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO lưu lượng xe theo vùng/hướng/làn
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataVolumeOutput
    {
        public string? Zone { get; set; }
        public string? Direction { get; set; }
        public string? Lane { get; set; }
        public string? VehicleSize { get; set; }
        public int VehicleCount { get; set; }
        public int? DetectHour { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime BucketTime { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO tốc độ trung bình theo vùng/hướng
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataAverageOutput
    {
        public string? Zone { get; set; }
        public string? Direction { get; set; }
        public double? AvgSpeed { get; set; }
        public int TotalVehicle { get; set; }
        public int ShortVehicle { get; set; }
        public int MediumVehicle { get; set; }
        public int LongVehicle { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO biểu đồ traffic ngày hiện tại
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataToDayChartOutput : BaseHelper.EChartHp.EChartResponse
    {
    }
}
