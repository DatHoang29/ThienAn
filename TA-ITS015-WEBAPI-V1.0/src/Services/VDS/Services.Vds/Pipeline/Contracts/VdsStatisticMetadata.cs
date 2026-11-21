using Newtonsoft.Json;
using Services.Vds.Shared.Parsing;

namespace Services.Vds.Pipeline.Contracts;

/// <summary>
/// Author: KienLT
/// Description: Metadata của bản tin thống kê (<c>type = Statistic</c>) — schema RIÊNG, không dùng chung
/// với bản tin phương tiện: số liệu nằm trong <c>data_list[]</c> theo từng làn.
/// Created date: 26/08/2026
/// </summary>
public sealed class VdsStatisticMetadata
{
    [JsonProperty("event_type")] public string? EventType { get; set; }
    [JsonProperty("device_name")] public string? DeviceName { get; set; }

    /// <summary>Đầu chu kỳ theo giờ THIẾT BỊ (không kèm timezone) — chỉ dùng để tính độ dài chu kỳ.</summary>
    [JsonProperty("start_time")] public string? StartTime { get; set; }

    /// <summary>Cuối chu kỳ theo giờ THIẾT BỊ (không kèm timezone) — chỉ dùng để tính độ dài chu kỳ.</summary>
    [JsonProperty("end_time")] public string? EndTime { get; set; }

    [JsonProperty("period_minutes")] public int? PeriodMinutes { get; set; }
    [JsonProperty("computed_by")] public string? ComputedBy { get; set; }

    [JsonProperty("data_list")] public List<VdsStatisticLaneItem> DataList { get; set; } = new();
}

/// <summary>Số liệu một làn trong bản tin thống kê.</summary>
public sealed class VdsStatisticLaneItem
{
    [JsonProperty("detection_region")] public string? DetectionRegion { get; set; }
    [JsonProperty("lane_no.")] public string? LaneNo { get; set; }
    [JsonProperty("average_speed")] public string? AverageSpeed { get; set; }
    [JsonProperty("vehicle_num")] public string? VehicleNum { get; set; }

    // Ba trường chia nhỏ vehicle_num theo cỡ xe nằm ở CẤP NÀY (ngay sau vehicle_num), không nằm trong
    // ext. Phân nhóm theo chiều dài L: nhỏ L <= 6m, trung bình 6m < L < 12m, lớn L >= 12m.
    // Tổng ba trường LUÔN bằng vehicle_num — đó là bất biến dùng để phát hiện thiết bị đổi ngữ nghĩa.

    [JsonProperty("small_vehicle_num")] public string? SmallVehicleNum { get; set; }
    [JsonProperty("medium_vehicle_num")] public string? MediumVehicleNum { get; set; }

    /// <summary>
    /// Số xe LỚN. Chỉ khác 0 khi module ước lượng chiều dài đang BẬT: không có chiều dài mặc định nào
    /// của bảng loại xe đạt 12 m.
    /// </summary>
    [JsonProperty("large_vehicle_num")] public string? LargeVehicleNum { get; set; }

    [JsonProperty("space_occupy_ration")] public string? SpaceOccupyRation { get; set; }
    [JsonProperty("time_occupy_ration")] public string? TimeOccupyRation { get; set; }
    [JsonProperty("space_headway")] public string? SpaceHeadway { get; set; }
    [JsonProperty("time_headway")] public string? TimeHeadway { get; set; }

    /// <summary>Tên làn do người dùng đặt trên camera; chỉ có khi camera được cấu hình đặt tên làn.</summary>
    [JsonProperty("custom_lane_name")] public string? CustomLaneName { get; set; }

    [JsonProperty("ext")] public VdsStatisticExt? Ext { get; set; }

    /// <summary>Khoá làn của phần tử này; hai trường luôn cùng giá trị, thiếu cả hai ⇒ <c>"1"</c>.</summary>
    [JsonIgnore]
    public string Lane =>
        !string.IsNullOrWhiteSpace(LaneNo) ? LaneNo!.Trim()
        : !string.IsNullOrWhiteSpace(DetectionRegion) ? DetectionRegion!.Trim()
        : "1";

    /// <summary>Số xe cỡ nhỏ: trường mới, lùi về <c>ext.s_vehicle_num</c> của payload cũ.</summary>
    [JsonIgnore]
    public int? SmallVehicleCount => VdsParse.Int(SmallVehicleNum) ?? Ext?.SVehicleNum;

    [JsonIgnore]
    public int? MediumVehicleCount => VdsParse.Int(MediumVehicleNum) ?? Ext?.MVehicleNum;

    [JsonIgnore]
    public int? LargeVehicleCount => VdsParse.Int(LargeVehicleNum) ?? Ext?.LVehicleNum;

    /// <summary>
    /// Số xe KHÔNG phân được cỡ. Payload mới không có nhóm này (tổng ba nhóm luôn bằng
    /// <c>vehicle_num</c>) nên suy bằng phần dư và bình thường phải là 0 — chính con số 0 đó là bằng
    /// chứng bản tin nhất quán. Payload cũ thì lấy <c>ext.o_vehicle_num</c>. Không có căn cứ nào ⇒ null,
    /// để phân biệt với "có số liệu và bằng 0".
    /// </summary>
    [JsonIgnore]
    public int? UnclassifiedVehicleCount
    {
        get
        {
            if (Ext?.OVehicleNum is int legacy) return legacy;
            if (!HasSizeCounts) return null;
            if (VdsParse.Int(VehicleNum) is not int total) return null;

            var remainder = total - SizeCountsTotal;
            return remainder > 0 ? remainder : 0;
        }
    }

    /// <summary>Bản tin có đủ ba trường chia cỡ hay không.</summary>
    [JsonIgnore]
    public bool HasSizeCounts =>
        VdsParse.Int(SmallVehicleNum) is not null
        && VdsParse.Int(MediumVehicleNum) is not null
        && VdsParse.Int(LargeVehicleNum) is not null;

    /// <summary>Tổng ba nhóm cỡ — dùng để kiểm tra bất biến của bản tin.</summary>
    [JsonIgnore]
    public int SizeCountsTotal =>
        (SmallVehicleCount ?? 0) + (MediumVehicleCount ?? 0) + (LargeVehicleCount ?? 0);
}

/// <summary>
/// Nhóm chỉ số mở rộng <c>ext.*</c>. Bốn trường <c>*_vehicle_num</c> là bộ tên NỘI BỘ của payload cũ,
/// giữ lại chỉ để đọc dữ liệu cũ và replay log thô.
/// </summary>
public sealed class VdsStatisticExt
{
    [JsonProperty("flow_vph")] public int? FlowVph { get; set; }
    [JsonProperty("density_vpkm")] public double? DensityVpkm { get; set; }
    [JsonProperty("space_mean_kmh")] public double? SpaceMeanKmh { get; set; }
    [JsonProperty("speed_sample")] public int? SpeedSample { get; set; }

    /// <summary>
    /// Chiều dài dùng để tính hai chỉ số chiếm dụng lấy từ đâu: <c>model</c> = đo riêng từng xe,
    /// <c>class</c> = dùng chiều dài trung bình theo loại xe, <c>mixed</c> = pha trộn. VẮNG khi module
    /// ước lượng chiều dài đang tắt.
    ///
    /// Số chiếm dụng có thể lệch khoảng 10% giữa hai cách tính, nên không có trường này thì về sau không
    /// giải thích được vì sao số liệu nhảy bậc.
    /// </summary>
    [JsonProperty("length_source")] public string? LengthSource { get; set; }

    [JsonProperty("length_scored_pct")] public double? LengthScoredPct { get; set; }
    [JsonProperty("length_model")] public string? LengthModel { get; set; }

    [JsonProperty("s_vehicle_num")] public int? SVehicleNum { get; set; }
    [JsonProperty("m_vehicle_num")] public int? MVehicleNum { get; set; }
    [JsonProperty("l_vehicle_num")] public int? LVehicleNum { get; set; }
    [JsonProperty("o_vehicle_num")] public int? OVehicleNum { get; set; }
}
