namespace Services.Vds.Features.ZoneStatus;

/// <summary>
/// Author: KienLT
/// Description: Tham số cập nhật trạng thái giao thông của vùng (section <c>"Service:ZoneStatus"</c>).
///
/// <para>
/// <b>Hai lớp công tắc.</b> <see cref="Enabled"/> là ý muốn của người vận hành; <see cref="IsUsable"/> là
/// điều kiện đủ để ghi. Thiếu bộ mã <c>Condition</c> mà vẫn ghi thì bảng FE đang đọc nhận chuỗi bịa, hiển
/// thị sai còn tệ hơn không hiển thị — nên fail-closed.
/// </para>
///
/// <para>
/// <b>Nguồn trạng thái (đổi 07/10/2026).</b> Mặc định trạng thái vùng CHỈ theo sự kiện của thiết bị: bản tin
/// <c>Congestion</c> ⇒ nghẽn, bản tin <c>Slow Moving</c> ⇒ chậm. Tốc độ xe KHÔNG còn sinh nghẽn; nó chỉ sinh
/// được bậc chậm, và chỉ khi bật <see cref="SpeedSlow"/>. Bản trước so tốc độ với ba ngưỡng cố định
/// 30/50/60 km/h, lệch hẳn với ngưỡng min/max của tuyến nên xe chạy đúng luật vẫn làm vùng báo chậm.
/// </para>
/// Created date: 26/08/2026
/// </summary>
public sealed class ZoneStatusOptions
{
    /// <summary>
    /// Cập nhật bảng trạng thái vùng.
    ///
    /// <b>Mặc định TẮT</b>: bảng <c>TmsZoneStatus</c> là bảng DÙNG CHUNG, mỗi vùng chỉ có một dòng. Chạy
    /// song song với worker cũ mà hai bên cùng bật thì hai máy trạng thái độc lập sẽ ghi đè lẫn nhau và
    /// trạng thái vùng nhảy qua lại — bản cũ mặc định BẬT vì lúc đó nó là bên duy nhất.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Chuỗi ghi vào cột <c>Condition</c> cho vùng thông thoáng. BẮT BUỘC khai đúng giá trị FE đang dùng;
    /// để rỗng thì tính năng tự tắt. Lấy đúng bộ mã bằng
    /// <c>SELECT DISTINCT Condition FROM TmsZoneStatus</c>.
    /// </summary>
    public string NormalCondition { get; set; } = "";

    /// <summary>Chuỗi ghi vào cột <c>Condition</c> cho vùng nghẽn. BẮT BUỘC khai.</summary>
    public string CongestedCondition { get; set; } = "";

    /// <summary>
    /// Chuỗi cho bậc CHẬM. KHÔNG bắt buộc: để rỗng thì bậc chậm gộp về <see cref="NormalCondition"/> — máy
    /// trạng thái vẫn tính ba bậc nhưng DB chỉ thấy hai mã. Thà thiếu một bậc còn hơn tắt cả tính năng hoặc
    /// ghi chuỗi rỗng vào cột FE đang đọc.
    /// </summary>
    public string SlowCondition { get; set; } = "";

    /// <summary>
    /// Chốt nghẽn theo sự kiện <c>Congestion</c> tự tắt sau bao nhiêu giây kể từ bản tin GẦN NHẤT. Nguồn chính
    /// là <c>VdsConfig</c> khoá <c>vds_zone_congestion_event_ttl</c>; thiếu khoá thì dùng giá trị này.
    /// </summary>
    public int CongestionEventTtlSeconds { get; set; } = 60;

    /// <summary>Chốt chậm theo sự kiện <c>Slow Moving</c> tự tắt sau bao nhiêu giây kể từ bản tin GẦN NHẤT.</summary>
    public int SlowEventTtlSeconds { get; set; } = 60;

    /// <summary>
    /// Cửa sổ trượt (giây) của mẫu tốc độ: dùng tính tốc độ trung bình ghi vào <c>TmsZoneStatus</c>, và là
    /// cửa sổ đánh giá của <see cref="SpeedSlow"/> khi bật. Cửa sổ rỗng ⇒ tốc độ trung bình để trống.
    /// </summary>
    public int AvgWindowSeconds { get; set; } = 300;

    /// <summary>Trần số mẫu giữ mỗi vùng — chặn phình RAM khi lưu lượng đột biến.</summary>
    public int AvgMaxSamples { get; set; } = 500;

    /// <summary>Bỏ mẫu có thời điểm phát hiện cũ hơn số giây này so với giờ hệ thống.</summary>
    public int SampleMaxAgeSeconds { get; set; } = 120;

    /// <summary>Nhịp ghi DB tối thiểu khi trạng thái KHÔNG đổi (đổi trạng thái thì ghi ngay).</summary>
    public int MinWriteIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Nhịp quét theo giờ (giây). Quét là BẮT BUỘC, không phải tuỳ chọn: chốt sự kiện hết hạn và cửa sổ tốc
    /// độ trôi qua đều theo THỜI GIAN nên không có bản tin nào tới để kích hoạt. Thiếu quét thì vùng nghẽn
    /// rồi vắng xe sẽ treo trạng thái nghẽn vĩnh viễn.
    /// </summary>
    public int SweepIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Hạn quá hạn của dòng trạng thái vùng (giây): dòng cũ hơn mức này bị API TMS và FE coi là quá hạn và hạ
    /// về thông thoáng. Nguồn chính là <c>VdsConfig</c> nhóm <c>vds_config_default</c> khoá
    /// <c>vds_zone_stale_seconds</c> — cùng khoá API TMS đọc, để hai bên không lệch nhau.
    ///
    /// <para>Worker chỉ ghi khi có thay đổi, trong khi chốt sự kiện giữ trạng thái lâu hơn hạn này. Nên khi
    /// vùng đang chậm/nghẽn, worker GHI GIỮ NHỊP (cập nhật lại <c>UpdateTime</c>) trước khi dòng quá hạn — xem
    /// <see cref="KeepAliveSeconds"/>. Hạn này vẫn là cách phát hiện worker dừng: dừng thì hết ghi giữ.</para>
    /// </summary>
    public int StaleSeconds { get; set; } = 30;

    /// <summary>Đánh giá vùng CHẬM theo tốc độ — tính năng mở rộng, mặc định tắt.</summary>
    public ZoneSpeedSlowOptions SpeedSlow { get; set; } = new();

    /// <summary>
    /// Biên an toàn (giây) giữa lần ghi giữ và hạn quá hạn: phủ nhịp đọc DB của SSE TMS (2s) và độ lệch giờ
    /// giữa máy worker với máy chạy API.
    /// </summary>
    public const int KeepAliveSafetySeconds = 5;

    /// <summary>
    /// Hạn quá hạn tối thiểu để ghi giữ nhịp còn tác dụng: hai nhịp quét tối thiểu (5s) cộng biên an toàn.
    /// Ngắn hơn thì dòng vẫn quá hạn giữa hai lượt ghi — worker cảnh báo khi nạp cấu hình.
    /// </summary>
    public const int MinKeepAliveStaleSeconds = 2 * 5 + KeepAliveSafetySeconds;

    /// <summary>
    /// Nhịp quét thực tế: không dài hơn nửa <see cref="StaleSeconds"/> (nhưng không dưới 5s), để mỗi hạn quá
    /// hạn có ít nhất hai lượt quét kịp ghi giữ nhịp khi <see cref="StaleSeconds"/> ≥
    /// <see cref="MinKeepAliveStaleSeconds"/>. Ghi đè lịch trong <c>Service:Jobs:Schedules</c> chỉ được rút
    /// ngắn nhịp này, không kéo dài.
    /// </summary>
    public int EffectiveSweepIntervalSeconds =>
        Math.Clamp(Math.Min(SweepIntervalSeconds, Math.Max(1, StaleSeconds / 2)), 5, 300);

    /// <summary>
    /// Vùng đang chậm/nghẽn mà lần ghi gần nhất đã cũ bấy nhiêu giây thì ghi lại (giữ nhịp). Tính để lượt
    /// quét kế tiếp vẫn còn trước hạn quá hạn: <c>StaleSeconds − nhịp quét − biên an toàn</c>, tối thiểu 1s.
    /// Mặc định 30s hạn, 15s quét ⇒ 10s, tức mỗi lượt quét ghi lại một lần.
    /// </summary>
    public int KeepAliveSeconds =>
        Math.Max(1, StaleSeconds - EffectiveSweepIntervalSeconds - KeepAliveSafetySeconds);

    /// <summary>
    /// Đủ tham số để ghi: có hai mã BẮT BUỘC (thông/nghẽn). Tham số của <see cref="SpeedSlow"/> sai thì chỉ
    /// tắt riêng phần đó (xem <see cref="ZoneSpeedSlowOptions.IsUsableFor"/>), không tắt cả tính năng.
    /// </summary>
    public bool IsUsable =>
        !string.IsNullOrWhiteSpace(NormalCondition)
        && !string.IsNullOrWhiteSpace(CongestedCondition);

    public ZoneStatusOptions Clone() => new()
    {
        Enabled = Enabled,
        NormalCondition = NormalCondition,
        CongestedCondition = CongestedCondition,
        SlowCondition = SlowCondition,
        CongestionEventTtlSeconds = CongestionEventTtlSeconds,
        SlowEventTtlSeconds = SlowEventTtlSeconds,
        AvgWindowSeconds = AvgWindowSeconds,
        AvgMaxSamples = AvgMaxSamples,
        SampleMaxAgeSeconds = SampleMaxAgeSeconds,
        MinWriteIntervalSeconds = MinWriteIntervalSeconds,
        SweepIntervalSeconds = SweepIntervalSeconds,
        StaleSeconds = StaleSeconds,
        SpeedSlow = SpeedSlow.Clone(),
    };
}

/// <summary>
/// Author: KienLT
/// Description: Đánh giá vùng CHẬM theo tốc độ tức thời và số lượng xe (section
/// <c>"Service:ZoneStatus:SpeedSlow"</c>). Chỉ sinh bậc CHẬM, không bao giờ sinh nghẽn — nghẽn chỉ theo sự
/// kiện <c>Congestion</c>.
///
/// <para><b>Công thức</b>, trên các lượt xe có tốc độ trong cửa sổ <c>AvgWindowSeconds</c> của vùng, so với
/// <c>V_min</c> = ngưỡng tối thiểu của vùng (<c>TmsZone.MinSpeed</c>, thiếu thì mặc định theo hướng):
/// <list type="bullet">
/// <item><c>N</c> = số xe, <c>V_tb</c> = tốc độ trung bình, <c>p</c> = tỉ lệ xe có tốc độ &lt; <c>V_min</c>.</item>
/// <item><b>VÀO chậm</b> khi đủ cả ba: <c>N ≥ MinVehicles</c>, <c>V_tb &lt; V_min</c>, <c>p ≥ SlowShare</c>.</item>
/// <item><b>RA khỏi chậm</b> khi <c>V_tb ≥ V_min + ClearMarginKmh</c>, hoặc cửa sổ không còn xe nào.</item>
/// <item>Ở giữa hai điều kiện ⇒ GIỮ bậc đang có (vùng trễ, tránh rung quanh <c>V_min</c>).</item>
/// </list></para>
///
/// <para><b>Vì sao cần đủ ba điều kiện để vào</b>: <c>N</c> chặn kết luận từ một hai xe lẻ (ban đêm một xe tải
/// chạy chậm không phải là đường chậm); <c>V_tb</c> nói cả dòng xe chậm; <c>p</c> chặn trường hợp vài xe rất
/// chậm kéo trung bình xuống trong khi số đông vẫn chạy bình thường. Điều kiện ra chỉ cần trung bình vượt
/// hẳn <c>V_min</c>, không cần đủ <c>N</c>: đường vắng sau giờ cao điểm phải về thông thoáng được.</para>
/// Created date: 07/10/2026
/// </summary>
public sealed class ZoneSpeedSlowOptions
{
    /// <summary>Bật đánh giá chậm theo tốc độ. Mặc định TẮT: trạng thái vùng chỉ theo sự kiện.</summary>
    public bool Enabled { get; set; }

    /// <summary>Số xe tối thiểu trong cửa sổ để được kết luận vùng chậm; không được vượt AvgMaxSamples.</summary>
    public int MinVehicles { get; set; } = 5;

    /// <summary>Tỉ lệ tối thiểu (0..1) số xe chạy dưới <c>V_min</c> để kết luận vùng chậm.</summary>
    public double SlowShare { get; set; } = 0.6;

    /// <summary>Tốc độ trung bình phải vượt <c>V_min</c> thêm bấy nhiêu km/h mới hết chậm.</summary>
    public double ClearMarginKmh { get; set; } = 5;

    /// <summary>Tham số dùng được; sai thì phần đánh giá theo tốc độ tự tắt (trạng thái vẫn theo sự kiện).</summary>
    public bool IsUsable => MinVehicles > 0
                            && double.IsFinite(SlowShare) && SlowShare is > 0 and <= 1
                            && double.IsFinite(ClearMarginKmh) && ClearMarginKmh >= 0;

    /// <summary>Kiểm tra thêm trần mẫu của vùng: MinVehicles vượt trần thì không thể đủ mẫu để vào chậm.</summary>
    public bool IsUsableFor(int avgMaxSamples) => IsUsable && MinVehicles <= avgMaxSamples;

    public ZoneSpeedSlowOptions Clone() => new()
    {
        Enabled = Enabled,
        MinVehicles = MinVehicles,
        SlowShare = SlowShare,
        ClearMarginKmh = ClearMarginKmh,
    };
}
