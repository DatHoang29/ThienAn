using Shared.Core.Utilities;

namespace Modules.VDS.Controllers.TrafficData.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: DTO phân trang danh sách traffic data
    /// Created date: 24/08/2026
    /// </summary>
    public class PageTrafficDataInput : BasePageInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Lane { get; set; }
        public string? Direction { get; set; }
        public string? EquipmentId { get; set; }
        public string? ZoneId { get; set; }
        public string? EventType { get; set; }
        public string? VehicleSize { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleColor { get; set; }
        public int? LengthFrom { get; set; }
        public int? LengthTo { get; set; }
        public double? SpeedFrom { get; set; }
        public double? SpeedTo { get; set; }
        public bool EventsOnly { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO tìm kiếm traffic data (không phân trang)
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataListInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Lane { get; set; }
        public string? Direction { get; set; }
        public string? EquipmentId { get; set; }
        public string? ZoneId { get; set; }
        public string? EventType { get; set; }
        public string? VehicleSize { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleType { get; set; }
        public string? VehicleColor { get; set; }
        public int? LengthFrom { get; set; }
        public int? LengthTo { get; set; }
        public double? SpeedFrom { get; set; }
        public double? SpeedTo { get; set; }
        public bool EventsOnly { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO tham số lưu lượng xe trong một giờ
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataHourVolumeInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Direction { get; set; }
        public string? ZoneId { get; set; }
        public string? Lane { get; set; }
        public string? EquipmentId { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO tham số lưu lượng xe trong một ngày
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataDayVolumeInput : TrafficDataHourVolumeInput
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO tham số tốc độ trung bình
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataAverageInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Direction { get; set; }
        public string? ZoneId { get; set; }
        public string? EquipmentId { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO tham số biểu đồ ngày hiện tại
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataToDayChartInput
    {
        public string? ZoneId { get; set; }
        public string? EquipmentId { get; set; }
        public string? Direction { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: DTO đếm xe theo giờ tách direction × vehicleSize — cho report ngày/tháng.
    /// FromDate/ToDate bắt buộc (validator).
    /// Created date: 03/09/2026
    /// </summary>
    /// <summary>
    /// Author: KienLT
    /// Description: DTO đếm xe theo giờ tách direction × vehicleSize — cho report ngày/tháng.
    /// FromDate/ToDate bắt buộc (validator).
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficDataVolumeBreakdownInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool CountAllEventTypes { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Khóa kép tra chi tiết một bản tin thô.
    ///
    /// <para>QuestDB không có ID tự sinh: một bản tin được định danh bằng CẶP <c>vendorEventId</c> +
    /// <c>detectTime</c>. DTO này KHÔNG lộ ra swagger — action giữ hai tham số vô hướng trên query string,
    /// controller tự dựng rồi dispatch.</para>
    /// <para><b>Không còn <c>[Required]</c> (bỏ 23/09/2026):</b> value type không nullable luôn có giá trị nên
    /// attribute không chặn được <c>0</c> hay <c>0001-01-01</c>. Miền hợp lệ do <c>VdsEventKeyRules</c> định
    /// nghĩa và <c>VdsRecordReaderService</c> kiểm, phủ cả đường HTTP và đường message bus.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class TrafficDataDetailInput
    {
        public long EventId { get; set; }
        public DateTime DetectTime { get; set; }
    }
}
