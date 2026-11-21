using System;
using System.Collections.Generic;
using System.Linq;

namespace ITS.Sync.Core.Models;

/// <summary>
/// Báo cáo tổng hợp của MỘT chu kỳ đồng bộ (gồm kết quả từng bảng).
/// </summary>
public sealed class SyncRunReport
{
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }

    /// <summary>Kết quả của từng bảng đã chạy trong chu kỳ.</summary>
    public List<SyncResult> Results { get; } = new();

    public int TotalInserted => Results.Sum(r => r.Inserted);
    public int TotalUpdated => Results.Sum(r => r.Updated);
    public int TotalDeleted => Results.Sum(r => r.Deleted);
    public bool AllSucceeded => Results.All(r => r.Success);
}
