namespace Services.Vds.Features.VehicleFilter;

/// <summary>
/// Author: KienLT
/// Description: Cảnh báo biển số theo danh sách đen/trắng (section <c>"Service:VehicleFilter"</c>).
///
/// Danh sách do người vận hành khai TRÊN WEB (bảng <c>TmsVehicleFilter</c>); service chỉ đọc lên RAM rồi
/// đối chiếu từng lượt xe.
/// Created date: 26/08/2026
/// </summary>
public sealed class VehicleFilterOptions
{
    /// <summary>
    /// Đối chiếu biển số với danh sách đen/trắng.
    ///
    /// <b>Mặc định TẮT</b>: bật cùng lúc với worker cũ thì mỗi lượt xe khớp sẽ ghi HAI dòng lịch sử và bắn
    /// HAI thông báo cho người trực, vì cửa sổ chống dội của mỗi tiến trình nằm riêng.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Khoảng nghỉ tối đa (giây) giữa hai lượt cùng biển trong MỘT chuyến. Các lượt đi TIẾN theo tuyến
    /// (A → B → C) cách nhau không quá khoảng này là một chuyến: ghi cùng <c>TripId</c> và chỉ thông báo một lần.
    ///
    /// <b>KHÔNG khai trong appsettings</b>: nguồn là <c>VdsConfig</c> nhóm <c>vds_config_default</c> khoá
    /// <c>vds_vehicle_filter_trip_gap</c> (sửa trên web), áp qua <c>ConfigParamApplier.VehicleFilterTripGap</c>.
    /// Giá trị ở đây chỉ là mặc định khi DB chưa có dòng đó.
    /// </summary>
    public int TripGapSeconds { get; set; } = 600;

    /// <summary>
    /// Trần số biển giữ trong bộ nhớ theo dõi chuyến — chặn phình RAM khi lưu lượng đột biến. Vượt trần
    /// thì dọn mục đã hết khoảng nghỉ trước, còn thiếu thì bỏ mục cũ nhất.
    /// </summary>
    public int TripMaxPlates { get; set; } = 20_000;

    /// <summary>
    /// Độ dài tối thiểu của biển số (sau chuẩn hoá) để được đối chiếu. Dưới ngưỡng ⇒ coi là rác nhận dạng
    /// và bỏ qua.
    ///
    /// <b>CỐ TÌNH không dùng mẫu biển Việt Nam để lọc</b>: danh sách trong DB là nguồn quyết định, người vận
    /// hành hoàn toàn có thể khai biển nước ngoài hoặc biển đặc biệt không khớp mẫu — chặn theo mẫu thì
    /// những biển đó nằm trong danh sách mà không bao giờ báo, và không có dấu hiệu nào để thấy.
    /// </summary>
    public int MinPlateLength { get; set; } = 4;

    /// <summary>
    /// Chu kỳ nạp lại danh sách từ DB (giây). Cần vì người vận hành thêm/xoá biển trên web mà service giữ
    /// danh sách trong RAM — không nạp lại thì phải khởi động lại service mới thấy.
    /// </summary>
    public int ReloadSeconds { get; set; } = 300;

    /// <summary>
    /// Giá trị cột cảnh báo của dòng danh sách được coi là BẬT (so sánh đã trim, không phân biệt hoa
    /// thường). Ngoài danh sách này ⇒ CHỈ ghi lịch sử, KHÔNG bắn thông báo.
    /// </summary>
    public List<string> AlarmOnValues { get; set; } = new() { "1", "true", "on" };

    /// <summary>Khoảng nghỉ gom chuyến dạng <see cref="TimeSpan"/>; kẹp 1 phút..24 giờ (cùng biên luật lưu trên web).</summary>
    public TimeSpan TripGap => TimeSpan.FromSeconds(Math.Clamp(TripGapSeconds, 60, 86_400));

    public int EffectiveMinPlateLength => Math.Clamp(MinPlateLength, 1, 32);

    public int EffectiveTripMaxPlates => Math.Max(64, TripMaxPlates);

    public int EffectiveReloadSeconds => Math.Clamp(ReloadSeconds, 30, 86_400);

    /// <summary>
    /// Cột cảnh báo của một dòng có bật thông báo hay không. Rỗng/null ⇒ false (fail-closed: thà không báo
    /// còn hơn báo sai — dòng đó vẫn được ghi vào lịch sử nên không mất vết).
    /// </summary>
    public bool IsAlarmOn(string? alarm)
    {
        if (string.IsNullOrWhiteSpace(alarm)) return false;

        var value = alarm.Trim();
        return AlarmOnValues.Any(v => string.Equals(v?.Trim(), value, StringComparison.OrdinalIgnoreCase));
    }

    public VehicleFilterOptions Clone() => new()
    {
        Enabled = Enabled,
        TripGapSeconds = TripGapSeconds,
        TripMaxPlates = TripMaxPlates,
        MinPlateLength = MinPlateLength,
        ReloadSeconds = ReloadSeconds,
        AlarmOnValues = new List<string>(AlarmOnValues),
    };
}
