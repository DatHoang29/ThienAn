using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Chạy lần lượt tất cả các bảng (ITableSync) trong một chu kỳ và gom kết quả.
/// Ngoài ra hỗ trợ chạy RIÊNG một bảng theo tên (dùng cho đồng bộ theo tín hiệu NATS).
/// Dùng chung cho WPF lẫn Worker.
/// </summary>
public sealed class SyncManager : ISyncManager
{
    private readonly IEnumerable<ITableSync> _tables;

    /// <summary>Tra bảng theo tên, không phân biệt hoa/thường.</summary>
    private readonly Dictionary<string, ITableSync> _byName;

    public SyncManager(IEnumerable<ITableSync> tables)
    {
        _tables = tables;

        _byName = new Dictionary<string, ITableSync>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tables)
            if (!string.IsNullOrWhiteSpace(t.TableName)) _byName[t.TableName] = t;
    }

    public IReadOnlyCollection<string> TableNames => _byName.Keys.ToList();

    public async Task<SyncRunReport> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var report = new SyncRunReport { StartedAt = DateTime.Now };

        foreach (var table in _tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            report.Results.Add(await table.SyncAsync(cancellationToken));
        }

        report.FinishedAt = DateTime.Now;
        return report;
    }

    public async Task<SyncRunReport> RunTableOnceAsync(string tableName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Tên bảng không được rỗng.", nameof(tableName));

        if (!_byName.TryGetValue(tableName.Trim(), out var table))
            throw new KeyNotFoundException(
                $"Bảng '{tableName}' chưa được đăng ký đồng bộ. Các bảng hợp lệ: {string.Join(", ", _byName.Keys)}");

        var report = new SyncRunReport { StartedAt = DateTime.Now };
        cancellationToken.ThrowIfCancellationRequested();
        report.Results.Add(await table.SyncAsync(cancellationToken));
        report.FinishedAt = DateTime.Now;
        return report;
    }
}
