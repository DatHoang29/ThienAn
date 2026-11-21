namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình replicator từng node (chu kỳ poll, cỡ lô, lùi con trỏ khi node bật lại, backoff, ngưỡng Ready).
/// Created date: 09/10/2026
/// </summary>
public sealed class ReplicatorOptions
{
    /// <summary>Chu kỳ poll journal khi không có việc. Giá trị tham chiếu: 200–500 ms.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Số dòng journal tối đa mỗi lô. Giá trị tham chiếu: 500.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Lùi cursor bao xa khi node chuyển Down → kết nối được. Lab 2 phút, tham chiếu production 10 phút.</summary>
    public TimeSpan RewindOnReconnect { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan BackoffInitial { get; set; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan BackoffMax { get; set; } = TimeSpan.FromSeconds(10);

    // cùng ngưỡng cho tuổi journal pending và tuổi mốc WAL từng bảng, không yêu cầu gap luôn bằng 0.
    /// <summary>Ngưỡng Ready cho tuổi journal pending và mốc WAL chờ apply; mặc định 2 giây.</summary>
    public TimeSpan ReadyMaxLag { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Chu kỳ ghi heartbeat/lag vào QuestNodeStatus.</summary>
    public TimeSpan StatusWriteInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Chu kỳ thăm dò node (SELECT 1 + wal_tables) khi không có gì để giao.</summary>
    public TimeSpan ProbeInterval { get; set; } = TimeSpan.FromSeconds(2);
}
