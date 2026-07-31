using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITS.Sync.Core.Models;

namespace ITS.Sync.Core.Abstractions;

/// <summary>
/// Bộ chạy đồng bộ: gọi lần lượt tất cả các bảng (ITableSync) trong một chu kỳ.
/// Đây là điểm mà host (WPF/Worker) gọi vào.
/// </summary>
public interface ISyncManager
{
    /// <summary>Chạy MỘT chu kỳ đồng bộ cho toàn bộ bảng, trả về báo cáo tổng hợp.</summary>
    Task<SyncRunReport> RunOnceAsync(CancellationToken cancellationToken = default);

    /// <summary>Tên các bảng đã đăng ký (dùng để kiểm tra tín hiệu đồng bộ theo bảng).</summary>
    IReadOnlyCollection<string> TableNames { get; }

    /// <summary>
    /// Chạy đồng bộ CHỈ MỘT bảng theo tên (không phân biệt hoa/thường).
    /// Trả về báo cáo chứa đúng kết quả của bảng đó; ném <see cref="KeyNotFoundException"/>
    /// nếu tên bảng không được đăng ký.
    /// </summary>
    Task<SyncRunReport> RunTableOnceAsync(string tableName, CancellationToken cancellationToken = default);
}
