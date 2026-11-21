namespace Services.Vds.Features.Events;

/// <summary>
/// Author: KienLT
/// Description: Tham số của phần trạng thái lưu ra đĩa (section <c>"Service:StateStore"</c>).
///
/// Thư mục gốc lấy từ <c>Service:StateDirectory</c>, không khai lại ở đây: mọi thứ service ghi ra đĩa phải
/// nằm cùng một chỗ để sao lưu/dọn dẹp chỉ phải nhớ một đường dẫn.
///
/// <para><b>Từ 10/09/2026 các tham số này chỉ còn áp cho cửa SKIP</b> (<c>VehicleDedupStateStore</c> →
/// <c>{StateDirectory}/vehicle-skip/{loại}.json</c>). Cửa chống trùng khi TẠO SỰ CỐ đã bỏ hẳn cache/file và
/// chuyển sang truy vấn bảng sự cố (cột <c>TmsIncident.ConditionKey</c> + <c>State</c>), nên thư mục
/// <c>recent/</c> không còn được ghi nữa — xoá được nếu còn sót từ bản cũ.</para>
/// Created date: 26/08/2026
/// </summary>
public sealed class StateStoreOptions
{
    /// <summary>
    /// Chu kỳ ghi trạng thái cửa SKIP ra đĩa (giây).
    ///
    /// Không ghi ngay mỗi lần đổi vì mỗi bản tin đều đụng vào trạng thái này; ghi theo nhịp là đánh đổi có
    /// chủ đích: mất tối đa một nhịp nếu tiến trình bị kill, và hệ quả của mất là một bản tin trùng có thể
    /// được ghi thêm một lần — chấp nhận được so với việc ghi đĩa trên mỗi bản tin.
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Thời gian giữ một mục cửa SKIP sau lần xuất hiện cuối (giờ). Hết hạn thì bị dọn.
    ///
    /// Cần thiết vì mỗi tổ hợp (camera, loại, biển) mới sinh ra một mục: không dọn thì file và bộ nhớ phình
    /// theo thời gian chạy. Phải LỚN HƠN cửa sổ SKIP lớn nhất, nếu không mục bị dọn trước khi hết cửa sổ và
    /// cùng một bản tin sẽ được ghi lại.
    /// </summary>
    public int RecentRetentionHours { get; set; } = 24;

    /// <summary>
    /// Trần số mục cửa SKIP giữ cho MỖI loại bản tin. Vượt trần thì bỏ mục cũ nhất theo lần xuất hiện cuối.
    /// Chốt an toàn cho trường hợp dữ liệu bất thường sinh ra rất nhiều tổ hợp khoá.
    /// </summary>
    public int RecentMaxEntriesPerType { get; set; } = 5_000;

    public int EffectiveFlushIntervalSeconds => Math.Clamp(FlushIntervalSeconds, 1, 300);

    public int EffectiveRecentRetentionHours => Math.Clamp(RecentRetentionHours, 1, 24 * 30);

    public int EffectiveRecentMaxEntriesPerType => Math.Clamp(RecentMaxEntriesPerType, 100, 1_000_000);

    public StateStoreOptions Clone() => new()
    {
        FlushIntervalSeconds = FlushIntervalSeconds,
        RecentRetentionHours = RecentRetentionHours,
        RecentMaxEntriesPerType = RecentMaxEntriesPerType,
    };
}
