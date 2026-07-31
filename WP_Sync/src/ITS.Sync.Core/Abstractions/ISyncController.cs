using System;
using System.Threading;
using System.Threading.Tasks;
using ITS.Sync.Core.Models;

namespace ITS.Sync.Core.Abstractions;

/// <summary>
/// Bộ điều khiển đồng bộ dùng chung cho mọi host (WPF/Worker).
/// Sở hữu vòng lặp chạy định kỳ (run-loop) để host chỉ cần Init/Start/Stop,
/// và nhận báo cáo/lỗi qua các callback <see cref="IProgress{T}"/>.
/// KHÔNG phụ thuộc bất kỳ kiểu UI nào — thread-safety do host quyết định
/// bằng cách tạo <see cref="Progress{T}"/> trên UI thread của mình.
/// </summary>
public interface ISyncController
{
    /// <summary>True khi vòng lặp định kỳ đang hoạt động (đã Start và chưa bị Stop).</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Chuẩn bị trước khi chạy: kiểm tra kết nối DB nguồn + đích.
    /// Trả về <see cref="SyncInitResult"/>; host nên chặn Start khi <c>Ok == false</c>.
    /// </summary>
    Task<SyncInitResult> InitAsync(CancellationToken ct = default);

    /// <summary>
    /// Chạy MỘT chu kỳ đồng bộ. Kết quả báo về qua <paramref name="onCycle"/>,
    /// lỗi (nếu có) báo về qua <paramref name="onError"/> — không để exception thoát ra ngoài.
    /// </summary>
    Task RunOnceAsync(IProgress<SyncRunReport>? onCycle = null, IProgress<Exception>? onError = null, CancellationToken ct = default);

    /// <summary>
    /// Bắt đầu vòng lặp đồng bộ định kỳ: chạy ngay một chu kỳ, sau đó lặp lại mỗi
    /// <paramref name="intervalSeconds"/> giây cho tới khi <see cref="Stop"/>.
    /// </summary>
    void Start(int intervalSeconds, IProgress<SyncRunReport>? onCycle = null, IProgress<Exception>? onError = null);

    /// <summary>Dừng vòng lặp định kỳ (hủy chu kỳ đang chờ/đang chạy).</summary>
    void Stop();

    /// <summary>
    /// Chạy đồng bộ CHỈ MỘT bảng theo tên — dùng cho đồng bộ theo tín hiệu (event-driven),
    /// độc lập với vòng lặp định kỳ. Kết quả báo qua <paramref name="onCycle"/>,
    /// lỗi qua <paramref name="onError"/> — không để exception thoát ra ngoài.
    /// Các lần chạy được tuần tự hóa để không ghi chồng lấn với chu kỳ định kỳ.
    /// </summary>
    Task RunTableOnceAsync(
        string tableName,
        IProgress<SyncRunReport>? onCycle = null,
        IProgress<Exception>? onError = null,
        CancellationToken ct = default);
}
