using Shared.Utility.Serialization;
namespace Modules.VDS.Controllers.TrafficStatistic.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: Output số liệu thống kê chu kỳ. Khớp cột <c>VdsTrafficStatistic</c> trên QuestDB.
    ///
    /// <para><b>Occupy/Headway là <c>double?</c></b>, không phải <c>string</c> như
    /// <c>TmsTrafficStatisticResponse</c> cũ: entity QuestDB đã là số, giữ string là nhân bản lỗi kiểu và
    /// buộc bên nhận tự parse (bản cũ có cả <c>AvgSpaceOccupy</c> parse tay trong getter).</para>
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficStatisticOutput
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime FromTime { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
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

    /// <summary>
    /// Author: KienLT
    /// Description: Bản thống kê mới nhất theo camera/làn kèm trạng thái quá hạn.
    /// Created date: 08/09/2026
    /// </summary>
    public class StatisticSnapshotOutput
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime ComputedAt { get; set; }
        public string Source { get; set; } = "VdsTrafficStatistic";
        public List<StatisticSnapshotRowOutput> Rows { get; set; } = new();
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Bản thống kê và cờ stale tính theo chu kỳ thiết bị.
    ///
    /// <para><b>Không còn trả entity từ 22/09/2026:</b> <c>Data</c> trước đây là entity
    /// <c>VdsTrafficStatistic</c> nên lộ <c>TenantId</c>, <c>IsDelete</c>, <c>RowStatus</c> ra ngoài API. Nay
    /// dùng <see cref="TrafficStatisticOutput"/> — đã có sẵn và khớp đúng 24 cột nghiệp vụ của entity.</para>
    /// Created date: 08/09/2026
    /// </summary>
    public class StatisticSnapshotRowOutput
    {
        public TrafficStatisticOutput Data { get; set; } = new();
        public bool Stale { get; set; }
    }
}
