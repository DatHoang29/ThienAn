using Shared.Utility.Counting;

namespace Services.Vds.Features.Counting;

/// <summary>
/// Author: KienLT
/// Description: Phạm vi và bộ lọc của số ĐẾM XE (section <c>"Service:Counting"</c>).
///
/// Số đếm không còn được cộng dồn trong RAM rồi chốt xuống bảng riêng: nó được TÍNH LẠI từ bảng dữ liệu
/// thô bằng <c>SAMPLE BY</c> mỗi lần cần. Vì vậy các tham số ở đây là bộ lọc của truy vấn, không phải
/// tham số của một bộ đếm — đổi tham số là số liệu đổi ngay ở lượt phát tiếp theo, không phải chờ tích luỹ.
/// Created date: 26/08/2026
/// </summary>
public sealed class CountingOptions : ICountQueryFilter, CountSliceFolder.IEquipmentScope
{
    /// <summary>
    /// Hạt thời gian của số đếm (phút). Được chuẩn hoá về bội số của 5 và tối thiểu 5 để khớp hạt mà phía
    /// WebAPI đang dùng — hai bên lệch hạt thì không đối chiếu được số nào với số nào.
    /// </summary>
    public int PeriodMinutes { get; set; } = TrafficCountMath.DefaultPeriodMinutes;

    /// <summary>
    /// Loại bản tin được ĐẾM. Mặc định chỉ <c>Normal</c>: đó là lượt xe đi qua bình thường, còn các loại
    /// sự cố là sự kiện chứ không phải lưu lượng nên cộng vào sẽ làm phồng số đếm.
    /// </summary>
    public List<string> CountedEventTypes { get; set; } = new() { "Normal" };

    /// <summary>
    /// Đếm MỌI loại bản tin, bỏ qua <see cref="CountedEventTypes"/>. Chỉ dùng khi cần đối chiếu tổng số
    /// bản tin nhận được.
    /// </summary>
    public bool CountsAllEventTypes { get; set; }

    /// <summary>
    /// Chỉ đếm lượt xe ĐỌC ĐƯỢC biển số. Mặc định false: bảng dữ liệu thô giữ cả xe không đọc được biển,
    /// và với lưu lượng thì một xe không biển vẫn là một xe.
    /// </summary>
    public bool RequirePlate { get; set; }

    /// <summary>
    /// Giới hạn camera được đếm theo <c>TmsEquipment.ID</c>. Rỗng = mọi camera trong danh mục.
    /// </summary>
    public List<string> CountingEquipmentIds { get; set; } = new();

    /// <summary>
    /// Trần khoảng thời gian một truy vấn đếm được phép quét (giờ). Chặn an toàn: payload cần rộng nhất là
    /// 24h, một cấu hình sai làm truy vấn quét vài tháng sẽ bị QuestDB tự huỷ và không có số nào.
    /// </summary>
    public int MaxQueryHours { get; set; } = 48;

    public int EffectivePeriodMinutes => TrafficCountMath.NormalizePeriod(PeriodMinutes);

    public int EffectiveMaxQueryHours => Math.Clamp(MaxQueryHours, 1, 24 * 30);

    /// <summary>Danh sách loại bản tin đã làm sạch; rỗng khi <see cref="CountsAllEventTypes"/> bật.</summary>
    public List<string> EffectiveCountedEventTypes => CountsAllEventTypes
        ? new List<string>()
        : CountedEventTypes
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // ==================== ICountQueryFilter (hợp đồng dùng chung với CountQueryBuilder) ====================
    // Ánh xạ sang tên trung lập của interface; giữ nguyên các property lịch sử ở trên để không phải sửa
    // cấu hình/bind.
    IReadOnlyList<string> ICountQueryFilter.EffectiveEventTypes => EffectiveCountedEventTypes;

    bool ICountQueryFilter.CountAllEventTypes => CountsAllEventTypes;

    /// <summary>Camera có nằm trong phạm vi đếm hay không; danh sách rỗng nghĩa là tất cả.</summary>
    public bool IsCountingEquipment(string? equipmentId)
    {
        if (CountingEquipmentIds.Count == 0) return true;
        if (string.IsNullOrWhiteSpace(equipmentId)) return false;

        return CountingEquipmentIds.Any(
            id => string.Equals(id?.Trim(), equipmentId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public CountingOptions Clone() => new()
    {
        PeriodMinutes = PeriodMinutes,
        CountedEventTypes = new List<string>(CountedEventTypes),
        CountsAllEventTypes = CountsAllEventTypes,
        RequirePlate = RequirePlate,
        CountingEquipmentIds = new List<string>(CountingEquipmentIds),
        MaxQueryHours = MaxQueryHours,
    };
}
