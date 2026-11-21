namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Tham số tính TỐC ĐỘ TRUNG BÌNH giữa hai camera A→B và nâng sự cố "quá tốc độ trung bình"
/// (section <c>"Service:Journey"</c>).
///
/// <para>Khác với quá tốc độ TỨC THỜI (một bản tin, so tốc độ đo tại một điểm với ngưỡng theo hướng):
/// đây là tốc độ TRUNG BÌNH của cả đoạn = <c>DistanceMeter / TravelSeconds * 3.6</c>, tính bằng cách ghép
/// hai lượt của CÙNG một biển số ở camera A rồi camera B. Đúng yêu cầu demo: dùng tốc độ trung bình để
/// tạo sự cố.</para>
///
/// <para><b>Hai đường ghép</b> (từ 29/09/2026): theo BẢN TIN (<see cref="Trigger"/> = <c>arrival</c>, xe tới
/// camera cuối là tính ngay) và theo CHU KỲ (<see cref="IntervalSeconds"/>) — đường chu kỳ nay là lượt quét
/// đối soát bù những chuyến đường bản tin bỏ sót. Cả hai đọc QuestDB, ghép bằng cùng hàm thuần, ghi luỹ đẳng.</para>
///
/// Mặc định TẮT (<see cref="Enabled"/> = false) để không chạy khi chưa khai danh mục đoạn
/// (<c>TmsTrafficSegment</c>) và chưa muốn ghi sự cố song song với stack cũ.
/// Created date: 04/09/2026
/// </summary>
public sealed class JourneyOptions
{
    /// <summary>
    /// Bật tính năng ghép hành trình A→B và nâng sự cố quá tốc độ trung bình.
    ///
    /// Tắt để chạy song song với pipeline cũ và khi danh mục đoạn chưa sẵn sàng: job không đăng ký,
    /// <c>SegmentManager</c> không nạp.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Chu kỳ chạy job ghép hành trình (giây). Khi <see cref="Trigger"/> = <c>arrival</c> đây là nhịp LƯỢT QUÉT
    /// ĐỐI SOÁT (đề xuất 600). &lt;= 0 ⇒ dùng mặc định 60s.
    /// </summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Số giây nới thêm cửa sổ quét NGOÀI thời gian di chuyển tối đa của đoạn. Cần vì job chạy chu kỳ:
    /// một hành trình vừa hoàn tất ngay sau lượt quét trước phải còn nằm trong cửa sổ của lượt sau.
    /// &lt;= 0 ⇒ dùng mặc định 60s.
    /// </summary>
    public int LookbackPaddingSeconds { get; set; } = 60;

    /// <summary>
    /// Thuộc tính cũ giữ tương thích file cấu hình; không còn dùng để phát hiện vi phạm.
    /// Ngưỡng thực tế lấy theo hướng trong VdsConfig; thiếu ngưỡng thì chỉ ghi hành trình.
    ///
    /// Fail-closed: hành trình không hợp lệ (thời gian di chuyển bất thường — kẹt xe, dừng lâu, hoặc hai
    /// xe khác nhau bị đọc trùng biển) KHÔNG bao giờ nâng sự cố dù tốc độ tính ra rất cao.
    /// </summary>
    public double OverSpeedThresholdKmh { get; set; }

    /// <summary>Giá trị <see cref="Trigger"/>: ghép ngay khi xe tới camera cuối.</summary>
    public const string TriggerArrival = "arrival";

    /// <summary>Giá trị <see cref="Trigger"/>: chỉ ghép theo chu kỳ (hành vi trước 29/09/2026).</summary>
    public const string TriggerInterval = "interval";

    /// <summary>
    /// Cách kích hoạt ghép hành trình. <c>arrival</c> (mặc định): xe tới camera cuối là ghép ngay
    /// (<c>JourneyArrivalWorker</c>), job chu kỳ chỉ còn là lượt quét ĐỐI SOÁT. <c>interval</c>: chỉ job chu kỳ.
    /// Đọc lúc khởi động; đổi phải restart.
    /// </summary>
    public string Trigger { get; set; } = TriggerArrival;

    /// <summary>Trần hàng đợi ghép theo bản tin. &lt;= 0 ⇒ 5000.</summary>
    public int ArrivalQueueCapacity { get; set; } = 5000;

    /// <summary>
    /// Số giây CHỜ sau khi nhận lượt xe rồi mới ghép theo bản tin, để lượt ở camera kia (bản tin trễ, QuestDB áp
    /// WAL) kịp hiện ra — giảm ca chọn nhầm lượt A. &lt; 0 ⇒ 5; 0 = ghép ngay.
    /// </summary>
    public int ArrivalDelaySeconds { get; set; } = 5;

    public int EffectiveArrivalDelaySeconds => ArrivalDelaySeconds < 0 ? 5 : Math.Min(ArrivalDelaySeconds, 300);

    /// <summary>
    /// Đệm lọc chuyến của lượt quét đối soát = max(LookbackPadding, IntervalSeconds): một lượt A đến trễ sau khi
    /// cursor đã tiến vẫn còn nằm trong vùng được ghi ở lượt kế tiếp. Ghi luỹ đẳng nên quét lại không sinh trùng.
    /// </summary>
    public int EffectiveReconcilePaddingSeconds => Math.Max(EffectiveLookbackPaddingSeconds, EffectiveIntervalSeconds);

    public bool IsArrivalTrigger => !string.Equals(Trigger?.Trim(), TriggerInterval, StringComparison.OrdinalIgnoreCase);

    public int EffectiveArrivalQueueCapacity => ArrivalQueueCapacity > 0 ? ArrivalQueueCapacity : 5000;

    public int EffectiveIntervalSeconds => IntervalSeconds > 0 ? IntervalSeconds : 60;

    public int EffectiveLookbackPaddingSeconds => LookbackPaddingSeconds > 0 ? LookbackPaddingSeconds : 60;

    public JourneyOptions Clone() => new()
    {
        Enabled = Enabled,
        IntervalSeconds = IntervalSeconds,
        LookbackPaddingSeconds = LookbackPaddingSeconds,
        OverSpeedThresholdKmh = OverSpeedThresholdKmh,
        Trigger = Trigger,
        ArrivalQueueCapacity = ArrivalQueueCapacity,
        ArrivalDelaySeconds = ArrivalDelaySeconds,
    };
}
