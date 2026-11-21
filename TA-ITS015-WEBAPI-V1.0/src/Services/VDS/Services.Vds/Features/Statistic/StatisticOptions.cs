namespace Services.Vds.Features.Statistic;

/// <summary>
/// Author: KienLT
/// Description: Xử lý bản tin thống kê chu kỳ do thiết bị tự tổng hợp (section <c>"Service:Statistic"</c>).
/// Created date: 26/08/2026
/// </summary>
public sealed class StatisticOptions
{
    /// <summary>
    /// Ghi bản tin thống kê xuống QuestDB.
    ///
    /// <b>Phải TẮT khi bảng <c>VdsTrafficStatistic</c> chưa được tạo.</b> Bảng do DDL tay tạo (service
    /// không tự đổi lược đồ); bật khi bảng chưa có thì mọi bản tin thống kê sẽ lỗi ghi, Wolverine retry
    /// liên tục và làm nghẽn queue tuần tự — kéo theo cả dữ liệu phương tiện đang chạy.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Chu kỳ (phút) mà thiết bị ĐƯỢC KỲ VỌNG gửi. Dùng cho hai việc:
    /// <list type="bullet">
    /// <item>cảnh báo khi bản tin lệch chu kỳ — <b>không</b> dùng để sửa số: trên môi trường thật đã từng
    /// gặp lẫn cả chu kỳ 3 phút và 5 phút, không phát hiện thì số liệu tổng hợp lệch mà không có dấu hiệu
    /// nào. 0 = không kiểm;</item>
    /// <item>giá trị đẩy lên thiết bị khi <see cref="PushPeriodToDevice"/> bật.</item>
    /// </list>
    ///
    /// CỐ TÌNH chỉ có MỘT con số cho cả hai việc (bản cũ có hai chỗ khai riêng). Đẩy lên thiết bị một chu
    /// kỳ rồi lại kỳ vọng một chu kỳ khác thì cảnh báo lệch chu kỳ sẽ kêu vì chính cấu hình của mình.
    ///
    /// Nguồn ưu tiên là <c>VdsConfig</c> khoá <c>vds_statistic_period_minutes</c> (người vận hành sửa
    /// trên web); giá trị khai ở đây là mặc định khi DB chưa có khoá đó.
    /// </summary>
    public int ExpectedPeriodMinutes { get; set; } = 5;

    /// <summary>
    /// Đẩy chu kỳ lên VDS AID lúc khởi động (<c>PUT /api/v1/vision-traffic/stats-config</c>).
    ///
    /// <b>Mặc định TẮT vì đây là lệnh ghi vào THIẾT BỊ DÙNG CHUNG.</b> Thiết bị chỉ có một chu kỳ cho mọi
    /// bên nhận, nên khi còn chạy song song với worker cũ mà hai bên khai lệch nhau thì bên khởi động sau
    /// sẽ lặng lẽ đổi chu kỳ của bên kia. Chỉ bật khi service này là bên duy nhất quản lý chu kỳ.
    /// </summary>
    public bool PushPeriodToDevice { get; set; }

    public StatisticOptions Clone() => new()
    {
        Enabled = Enabled,
        ExpectedPeriodMinutes = ExpectedPeriodMinutes,
        PushPeriodToDevice = PushPeriodToDevice,
    };
}
