namespace Shared.QuestHa.Interfaces;

/// <summary>
/// Author: KienLT
/// Description: Điều khiển tiến trình QuestDB của một node; adapter chịu trách nhiệm dừng và khởi động tiến trình.
/// Core chỉ gọi interface này trong backup/restore.
/// Created date: 09/10/2026
/// </summary>
public interface INodeControl
{
    /// <summary>Trùng <see cref="QuestNodeOptions.Name"/>.</summary>
    string Node { get; }

    /// <summary>Root QuestDB của node (thư mục chứa <c>conf/</c>, <c>db/</c> trên máy node).</summary>
    string RootPath { get; }

    /// <summary>Khởi động; trả về khi tiến trình đã chạy (không bắt buộc đã nhận kết nối).</summary>
    Task StartAsync(CancellationToken ct = default);

    // cleanup backup phải chờ STOP_PENDING kết thúc trước khi coi start đã thành công.
    /// <summary>Đảm bảo trạng thái chạy ổn định; adapter SCM phải chờ STOP_PENDING xong rồi start, không nhầm là đã chạy.</summary>
    async Task EnsureStartedAsync(CancellationToken ct = default)
    {
        if (!(await StatusFreshAsync(ct)).IsRunning) await StartAsync(ct);
    }

    /// <summary>Tắt sạch (flush xong mới thoát); trả về khi tiến trình đã thoát.</summary>
    Task StopCleanAsync(CancellationToken ct = default);

    /// <summary>Giết ngay đúng tiến trình java của root này.</summary>
    Task KillAsync(CancellationToken ct = default);

    /// <summary>Trạng thái tiến trình QuestDB của node (có thể dùng cache).</summary>
    Task<NodeProcessOutput> StatusAsync(CancellationToken ct = default);

    /// <summary>
    /// trạng thái tiến trình đọc MỚI, KHÔNG dùng cache (kiểm node đang chạy đầu backup, trước/sau
    /// khi dừng). Mặc định = <see cref="StatusAsync"/> — triển khai có cache PHẢI override.
    /// </summary>
    Task<NodeProcessOutput> StatusFreshAsync(CancellationToken ct = default) => StatusAsync(ct);
}
