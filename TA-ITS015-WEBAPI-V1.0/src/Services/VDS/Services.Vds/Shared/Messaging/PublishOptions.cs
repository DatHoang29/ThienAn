namespace Services.Vds.Shared.Messaging;

/// <summary>
/// Author: KienLT
/// Description: Một NHÓM HƯỚNG trong payload đếm xe. <c>EquipmentIds</c> là tập <b>ĐIỂM VÀO</b> của
/// hướng, KHÔNG phải mọi camera thuộc hướng đó.
///
/// <b>Vì sao phải là điểm vào</b>: trên tuyến, một xe đi qua lần lượt 4–5 camera của cùng một hướng. Khai
/// hết rồi cộng lại thì mỗi xe bị đếm 4–5 lần và lưu lượng hướng phồng lên theo số camera nó đi qua —
/// sai âm thầm, không có dấu hiệu gì trên payload. Chỉ khai điểm vào thì phép cộng hợp lệ, vì một xe vào
/// tuyến qua ĐÚNG MỘT điểm vào (cửa vào chính, hoặc một nhánh nhập).
///
/// <b>Con số này là số TIÊU BIỂU, không phải tổng chính xác</b>: xe nhập tuyến ở một nhánh không có
/// camera thì không được đếm. Đừng đọc nó như tổng số xe đang trên hướng.
///
/// Camera nào là điểm vào thì tra danh mục: hướng của camera là <c>TmsEquipment.DirectionId</c>, vị trí là
/// <c>KmNumber/MetNumber</c>, và <c>VdsConfig.vds_count_direction_equipment_{1,2}</c> chỉ ra camera đếm của từng
/// hướng. Khai tay thay vì tự suy từ <c>DirectionId</c> chính là để không gom cả hướng rồi cộng.
/// Created date: 26/08/2026
/// </summary>
public sealed class CountDirectionGroup
{
    /// <summary>Mã nhóm dùng làm <c>directionKey</c> trong payload.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Tên hiển thị cho bên nhận.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// <c>TmsEquipment.ID</c> của các camera ở ĐIỂM VÀO của hướng. Thường 1 cái; nhiều hơn khi hướng có
    /// nhiều cửa vào. Đừng khai camera nằm giữa tuyến — xem phần mô tả của class.
    /// </summary>
    public List<string> EquipmentIds { get; set; } = new();

    public CountDirectionGroup Clone() => new()
    {
        Key = Key,
        Name = Name,
        EquipmentIds = new List<string>(EquipmentIds),
    };
}

/// <summary>
/// Author: KienLT
/// Description: Công tắc bật/tắt từng luồng publish NATS (section <c>"Service:Publish"</c>).
///
/// Kết nối và subject do <c>TransportManager</c> + <c>VdsSubjects</c> lo; đây CHỈ là công tắc. Mặc định
/// bật những luồng đã có bên nhận và tắt những luồng chưa ai nhận, để service mới không đẩy bản tin
/// vào hư không.
/// Created date: 26/08/2026
/// </summary>
public sealed class PublishOptions
{
    /// <summary>
    /// Bản tin LƯU LƯỢNG THEO VÙNG (<c>vds.traffic/v2</c>) trên subject <c>ta.its.data.vds.traffic</c>.
    ///
    /// <b>Đổi ngữ nghĩa so với v1</b>: trước đây subject này chở từng lượt xe (một bản tin mỗi xe, có
    /// <c>kind: "vehicle"</c>). Từ nay nó chở số liệu đã gộp theo vùng kèm chart. Bên nhận phân biệt bằng
    /// trường <c>schema</c> — v1 không có trường này.
    /// </summary>
    public bool PublishTraffic { get; set; } = true;

    /// <summary>Trạng thái vùng sau khi ghi thành công.</summary>
    public bool PublishZone { get; set; } = false;

    /// <summary>Số liệu đếm xe theo chu kỳ.</summary>
    public bool PublishCount { get; set; } = false;

    /// <summary>Sự cố VDS (dùng chung subject sự cố của ITS).</summary>
    public bool PublishIncident { get; set; } = true;

    /// <summary>Số liệu thống kê chu kỳ.</summary>
    public bool PublishStatistic { get; set; } = false;

    /// <summary>Cảnh báo biển số khớp danh sách đen/trắng.</summary>
    public bool PublishFilter { get; set; } = true;

    /// <summary>
    /// Cửa sổ GOM sau khi có dữ liệu mới trước khi phát số đếm (ms). Các lượt xe đến trong cửa sổ này gộp
    /// vào MỘT lần phát.
    /// </summary>
    public int CountPublishDebounceMs { get; set; } = 500;

    /// <summary>
    /// Khoảng TỐI THIỂU giữa hai lần phát số đếm (giây).
    ///
    /// Cần thiết vì mỗi lần phát là một truy vấn gộp trên khoảng 24h: phát theo từng lượt xe như bộ đếm
    /// RAM trước đây sẽ biến lưu lượng giờ cao điểm thành hàng chục truy vấn nặng mỗi giây.
    /// </summary>
    public int CountMinPublishIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Chu kỳ phát lại số đếm dù không có dữ liệu mới (giây). Giữ cho chuỗi theo giờ và cửa sổ trượt tiếp
    /// tục tiến khi vắng xe; 0 = chỉ phát khi có dữ liệu mới.
    /// </summary>
    public int CountRefreshIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Giới hạn camera xuất hiện trong payload đếm theo <c>TmsEquipment.ID</c>. Rỗng ⇒ tự chọn: camera CÓ
    /// số trong cửa sổ đang xét, cộng mọi camera khai trong <see cref="CountDirectionGroups"/> để nhóm
    /// hướng luôn đủ thành viên kể cả khi vắng xe.
    ///
    /// Cần vì danh mục có thể tới hàng chục camera; đưa hết vào thì payload phình lên mà gần hết là số 0.
    /// </summary>
    public List<string> CountCameras { get; set; } = new();

    /// <summary>
    /// Nhóm hướng của payload đếm. Rỗng ⇒ payload không có mảng <c>directions</c>.
    ///
    /// Mỗi nhóm khai tập ĐIỂM VÀO của hướng, không phải mọi camera thuộc hướng — xem
    /// <see cref="CountDirectionGroup"/>. Khai cả hướng thì mỗi xe bị đếm lại ở từng camera nó đi qua.
    /// </summary>
    public List<CountDirectionGroup> CountDirectionGroups { get; set; } = new();

    /// <summary>
    /// Kèm khối <c>chart</c> (định dạng ECharts) vào từng camera và từng nhóm hướng của payload đếm, để FE
    /// vẽ được ngay không phải tự gộp.
    ///
    /// Tắt được vì nó làm payload PHÌNH: mỗi chart là tới 24 điểm × 4 series, nhân với số camera trong
    /// payload. Vài chục camera là đã hàng trăm KB mỗi bản tin, mà giới hạn mặc định của NATS là 1 MB.
    /// Tắt thì bên nhận vẫn có đủ số ở <c>flowByHour</c> để tự dựng.
    /// </summary>
    public bool CountIncludeChart { get; set; } = true;

    /// <summary>
    /// Cửa sổ GOM sau khi có xe qua vùng trước khi phát lưu lượng theo vùng (ms).
    /// Cùng vai trò với <see cref="CountPublishDebounceMs"/>.
    /// </summary>
    public int TrafficPublishDebounceMs { get; set; } = 500;

    /// <summary>
    /// Khoảng TỐI THIỂU giữa hai lần phát lưu lượng theo vùng (giây). Mỗi lần phát là một truy vấn gộp
    /// trên QuestDB nên phải có trần tần suất.
    /// </summary>
    public int TrafficMinPublishIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Chu kỳ phát lại lưu lượng theo vùng dù không có xe (giây); 0 = chỉ phát khi có xe.
    /// </summary>
    public int TrafficRefreshIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Kèm khối <c>chart</c> (ECharts) vào từng vùng của payload lưu lượng. Tắt được vì payload phình theo
    /// số vùng × 24 điểm × 4 series; bên nhận vẫn có <c>flowByHour</c> để tự dựng.
    /// </summary>
    public bool TrafficIncludeChart { get; set; } = true;

    public PublishOptions Clone() => new()
    {
        PublishTraffic = PublishTraffic,
        PublishZone = PublishZone,
        PublishCount = PublishCount,
        PublishIncident = PublishIncident,
        PublishStatistic = PublishStatistic,
        PublishFilter = PublishFilter,
        CountPublishDebounceMs = CountPublishDebounceMs,
        CountMinPublishIntervalSeconds = CountMinPublishIntervalSeconds,
        CountRefreshIntervalSeconds = CountRefreshIntervalSeconds,
        CountCameras = new List<string>(CountCameras),
        CountDirectionGroups = CountDirectionGroups.Select(g => g.Clone()).ToList(),
        CountIncludeChart = CountIncludeChart,
        TrafficPublishDebounceMs = TrafficPublishDebounceMs,
        TrafficMinPublishIntervalSeconds = TrafficMinPublishIntervalSeconds,
        TrafficRefreshIntervalSeconds = TrafficRefreshIntervalSeconds,
        TrafficIncludeChart = TrafficIncludeChart,
    };
}
