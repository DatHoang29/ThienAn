using Shared.Utility.Helpers;

namespace Modules.TMS.Core.Constants;

/// <summary>
/// Author: KienLT
/// Description: Hạn dùng của <c>TmsZoneStatus.AverageSpeed</c> — TÁCH khỏi hạn của trạng thái vùng.
/// Created date: 08/10/2026
///
/// <para><b>Vì sao tách:</b> worker VDS ghi lại dòng khi trạng thái hoặc tốc độ TB ĐỔI; riêng vùng đang
/// chậm/nghẽn thì ghi thêm nhịp giữ (tính từ <c>vds_zone_stale_seconds</c>) để không bị quá hạn giữa chừng. Nên
/// vùng thông thoáng "lâu không cập nhật" thường chỉ có nghĩa là số không đổi — trạng thái vẫn quá hạn sau
/// <c>vds_zone_stale_seconds</c> như cũ (quá hạn cũng là thông), còn tốc độ thì giữ số cuối.</para>
///
/// <para><b>Vì sao vẫn cần hạn:</b> worker còn sống thì chắc chắn ghi lại trong khoảng một cửa sổ tính TB
/// (mẫu cũ rơi khỏi cửa sổ làm số đổi, hết mẫu thì ghi null). Quá cửa sổ + biên mà không có lần ghi nào ⇒
/// worker ngừng cập nhật (dừng, tắt tính năng, ghi DB lỗi) ⇒ số không còn đáng tin, phải ẩn.</para>
/// </summary>
public static class ZoneSpeedFreshnessConst
{
    /// <summary>Cửa sổ khi chưa khai <c>vds_zone_slow_window</c> — bằng mặc định <c>ZoneStatus:AvgWindowSeconds</c> của service.</summary>
    public const int DefaultWindowSeconds = 300;

    /// <summary>Biên cho nhịp quét (15s) + nhịp ghi tối thiểu (15s) của worker và độ lệch giờ.</summary>
    public const int MarginSeconds = 60;

    /// <summary>Tuổi tối đa của tốc độ TB, từ giá trị thô của <c>vds_zone_slow_window</c>; sai/thiếu ⇒ mặc định.</summary>
    public static int MaxAgeSeconds(string? windowValue)
        => (BaseHelper.DurationHp.TryParseSeconds(windowValue) is int window && window > 0 ? window : DefaultWindowSeconds)
           + MarginSeconds;

    /// <summary>Còn dùng được: có mốc, không ở tương lai (đồng hồ lệch ⇒ không tin) và chưa quá tuổi tối đa.</summary>
    public static bool IsFresh(DateTime? updateTime, DateTime serverNow, int maxAgeSeconds)
        => updateTime is DateTime time
           && time <= serverNow
           && (serverNow - time).TotalSeconds < maxAgeSeconds;
}
