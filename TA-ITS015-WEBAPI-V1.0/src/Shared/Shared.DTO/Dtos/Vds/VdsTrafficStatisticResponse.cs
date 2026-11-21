using System;
using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Vds
{
    /// <summary>
    /// Author: KienLT
    /// Description: Request thống kê chu kỳ VDS (QuestDB) — cross-module in-process qua message bus.
    /// FromDate/ToDate bắt buộc (QuestDB phân vùng theo ngày).
    /// Created date: 03/09/2026
    /// </summary>
    public class VdsTrafficStatisticRequest : BaseEntityDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? ZoneId { get; set; }
        public string? EquipmentId { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Response thống kê chu kỳ VDS. Occupy/Headway là <c>double?</c> (không phải string như
    /// <c>TmsTrafficStatisticResponse</c> cũ) — entity QuestDB đã là số, giữ string là nhân bản lỗi kiểu.
    /// Created date: 03/09/2026
    /// </summary>
    public class VdsTrafficStatisticResponse
    {
        public DateTime FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public short? PeriodMinutes { get; set; }

        public long VendorEventId { get; set; }
        public string? CameraIp { get; set; }
        public string? EquipmentId { get; set; }
        public string? Direction { get; set; }
        public string? ZoneId { get; set; }
        public string? Lane { get; set; }

        public int? TotalVehicle { get; set; }
        public int? ShortVehicle { get; set; }
        public int? MediumVehicle { get; set; }
        public int? LongVehicle { get; set; }
        public int? OtherVehicle { get; set; }

        public double? AvgSpeed { get; set; }
        public double? SpaceMeanKmh { get; set; }
        public int? FlowVph { get; set; }
        public double? DensityVpkm { get; set; }
        public double? SpaceOccupy { get; set; }
        public double? TimeOccupy { get; set; }
        public double? SpaceHeadway { get; set; }
        public double? TimeHeadway { get; set; }

        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }
    }
}
