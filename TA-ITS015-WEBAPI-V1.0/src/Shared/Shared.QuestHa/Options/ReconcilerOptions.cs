namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình đối soát journal ↔ node (độ trễ ổn định, chờ WAL apply, cửa sổ nhìn lại, chu kỳ tự động).
/// Created date: 09/10/2026
/// </summary>
public sealed class ReconcilerOptions
{
    /// <summary>Bỏ qua phút quá gần hiện tại (WAL QuestDB apply bất đồng bộ).</summary>
    public TimeSpan SettleDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Chờ tối đa writerTxn = sequencerTxn trước khi đọc node.</summary>
    public TimeSpan WalApplyWait { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Cửa sổ mặc định khi chạy tự động: [now − Lookback, now − SettleDelay).</summary>
    public TimeSpan Lookback { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Chu kỳ chạy tự động (hosted service). null = chỉ chạy khi gọi tay. Lab 30 s, production 1 giờ.</summary>
    public TimeSpan? AutoInterval { get; set; } = TimeSpan.FromSeconds(30);
}
