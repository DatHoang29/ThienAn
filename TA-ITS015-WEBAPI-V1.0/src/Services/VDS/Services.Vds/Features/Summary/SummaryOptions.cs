namespace Services.Vds.Features.Summary;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình job tổng hợp số liệu lâu dài (section <c>Service:Summary</c>).
///
/// <para>Job đọc số đếm từ QuestDB theo giờ vừa kết thúc rồi ghi vào <c>TmsTrafficSummary</c> trên SQL
/// SERVER — làm nguồn báo cáo DỰ PHÒNG khi QuestDB lỗi. Ngày/tháng gộp từ chính bảng summary, không đụng
/// QuestDB.</para>
///
/// <para>MẶC ĐỊNH BẬT: đây là fallback cho báo cáo nên nên chạy sẵn. Tắt bằng
/// <c>Service:Summary:Enabled = false</c> khi chạy song song với worker cũ đang cũng ghi cùng bảng.</para>
/// </summary>
public sealed class SummaryOptions
{
    /// <summary>Bật/tắt job tổng hợp.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Nhịp chạy job (giây). Mặc định 3600 = mỗi giờ. Bên trong mỗi lượt tự xét mốc ngày/tháng đã kết
    /// thúc, nên chạy dày hơn cũng không sinh trùng (upsert luỹ đẳng), chỉ tốn truy vấn.
    /// </summary>
    public int IntervalSeconds { get; set; } = 3600;

    /// <summary>
    /// Số giờ tối đa nhìn lại khi tổng hợp mức GIỜ. Mặc định 3: mỗi lượt bù luôn vài giờ gần nhất phòng
    /// khi một lượt job trước lỗi hoặc service vừa khởi động lại — upsert nên chạy lại không hại.
    /// </summary>
    public int HourLookbackHours { get; set; } = 3;

    public int EffectiveIntervalSeconds => IntervalSeconds > 0 ? IntervalSeconds : 3600;

    public int EffectiveHourLookbackHours => Math.Clamp(HourLookbackHours, 1, 48);

    public SummaryOptions Clone() => new()
    {
        Enabled = Enabled,
        IntervalSeconds = IntervalSeconds,
        HourLookbackHours = HourLookbackHours,
    };
}
