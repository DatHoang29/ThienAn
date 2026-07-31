using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITS.Sync.Core.Enums;
using ITS.Sync.Core.Models;
using ITS.Sync.Infrastructure.Persistence;
using Shared.Core.Domain;
using SqlSugar;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Bộ "bước dùng chung" cho đồng bộ MỘT CHIỀU (nguồn → đích), phát hiện thay đổi theo UpdateTime.
///
/// Tối ưu băng thông: chỉ đọc (ID, UpdateTime) của cả hai bên để phân loại, rồi CHỈ load đầy đủ
/// các cột cho những bản ghi thực sự cần Insert/Update (và load bản ghi đích cần Delete để log).
///
/// - <see cref="ReadKeysAsync{T}"/>: đọc nhẹ (ID + UpdateTime).
/// - <see cref="LoadByIdsAsync{T}"/>: load đầy đủ cột theo danh sách ID (chia lô để tránh giới hạn tham số).
/// - <see cref="WriteAsync{T}"/>: ghi Thêm/Sửa/Xóa xuống đích trong 1 transaction.
/// - <see cref="ContentCompareAsync{T}"/>: pipeline hoàn chỉnh (dùng cho mọi bảng).
/// </summary>
public sealed class SyncSteps
{
    private readonly SyncDbAccessor _db;

    public SyncSteps(SyncDbAccessor db) => _db = db;

    /// <summary>Khóa nhẹ để so sánh: chỉ gồm ID và UpdateTime.</summary>
    private sealed class SyncKey
    {
        public string? Id { get; set; }
        public DateTime? Ut { get; set; }
    }

    /// <summary>
    /// Đọc CHỈ (ID, UpdateTime) của toàn bộ bảng (kể cả bản ghi đã xóa mềm nhờ ClearFilter).
    /// </summary>
    public async Task<List<(string Id, DateTime? UpdateTime)>> ReadKeysAsync<T>(
        BaseRepository<T> repo) where T : EntityTenant, new()
    {
        var keys = await repo.AsQueryable().ClearFilter()
            .Select(it => new SyncKey { Id = it.ID, Ut = it.UpdateTime })
            .ToListAsync();

        return keys.Select(k => (k.Id ?? string.Empty, k.Ut)).ToList();
    }

    /// <summary>Load ĐẦY ĐỦ cột cho các bản ghi theo danh sách ID (chia lô 1000 để tránh giới hạn IN).</summary>
    public async Task<List<T>> LoadByIdsAsync<T>(
        BaseRepository<T> repo, IReadOnlyList<string> ids, CancellationToken ct = default)
        where T : EntityTenant, new()
    {
        const int batchSize = 1000;
        var result = new List<T>(ids.Count);

        for (var i = 0; i < ids.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = ids.Skip(i).Take(batchSize).ToList();
            var rows = await repo.AsQueryable().ClearFilter()
                .Where(it => chunk.Contains(it.ID))
                .ToListAsync();
            result.AddRange(rows);
        }

        return result;
    }

    /// <summary>Ghi Thêm/Sửa/Xóa xuống ĐÍCH trong MỘT transaction; trả <see cref="SyncResult"/>.</summary>
    public async Task<SyncResult> WriteAsync<T>(
        string tableName,
        SyncPlan<T> plan,
        string[]? ignore = null,
        CancellationToken ct = default) where T : EntityTenant, new()
    {
        ignore ??= Array.Empty<string>();
        var stopwatch = Stopwatch.StartNew();
        var result = new SyncResult { TableName = tableName };
        result.Changes.AddRange(plan.Changes);

        try
        {
            var targetDb = _db.TargetDb;
            var target = new BaseRepository<T>(targetDb);

            if (plan.ToInsert.Count > 0 || plan.ToUpdate.Count > 0 || plan.ToDeleteIds.Count > 0)
            {
                try
                {
                    targetDb.Ado.BeginTran();

                    if (plan.ToInsert.Count > 0)
                        await target.AsInsertable(plan.ToInsert).ExecuteCommandAsync();

                    if (plan.ToUpdate.Count > 0)
                    {
                        var up = target.AsUpdateable(plan.ToUpdate).WhereColumns(it => it.ID);
                        if (ignore.Length > 0) up = up.IgnoreColumns(ignore);
                        await up.ExecuteCommandAsync();
                    }

                    if (plan.ToDeleteIds.Count > 0)
                        await target.AsDeleteable().Where(it => plan.ToDeleteIds.Contains(it.ID)).ExecuteCommandAsync();

                    targetDb.Ado.CommitTran();
                }
                catch
                {
                    targetDb.Ado.RollbackTran();
                    throw;
                }
            }

            result.Inserted = plan.ToInsert.Count;
            result.Updated = plan.ToUpdate.Count;
            result.Deleted = plan.ToDeleteIds.Count;
            result.Unchanged = plan.Unchanged;
            result.Success = true;
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = "Đã hủy bởi người dùng.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Pipeline CHUẨN: đọc (ID, UpdateTime) 2 bên → phân loại → chỉ load full cột cho bản ghi
    /// cần Insert/Update (và bản ghi đích cần Delete để log) → ghi. Phát hiện thay đổi bằng UpdateTime:
    /// - Cùng ID, UpdateTime khác nhau (nguồn ≠ đích) → UPDATE (đích lấy toàn bộ giá trị nguồn).
    /// - Có ở nguồn, chưa có ở đích → INSERT.
    /// - Có ở đích, không còn ở nguồn → xử lý theo <paramref name="missingRowAction"/>.
    ///
    /// <paramref name="missingRowAction"/>:
    /// - <see cref="MissingRowAction.HardDelete"/> (mặc định) = DELETE khỏi đích (mirror).
    /// - <see cref="MissingRowAction.Keep"/> = upsert-only, giữ nguyên bản ghi riêng của đích.
    ///
    /// Lưu ý: IsDelete được đối xử như một CỘT BÌNH THƯỜNG — nguồn xóa mềm (set IsDelete +
    /// UpdateTime) thì thay đổi đó lan sang đích qua bước UPDATE, không cần xử lý riêng.
    /// </summary>
    public async Task<SyncResult> ContentCompareAsync<T>(
        string tableName,
        string[]? ignore = null,
        CancellationToken ct = default,
        Func<T, string>? describeInsert = null,
        Func<T, T, string>? describeUpdate = null,
        Func<T, string>? describeDelete = null,
        MissingRowAction missingRowAction = MissingRowAction.HardDelete) where T : EntityTenant, new()
    {
        var stopwatch = Stopwatch.StartNew();
        SyncResult result;
        try
        {
            // 1) CHỈ lấy (ID, UpdateTime) của cả hai bên.
            var sourceKeys = await ReadKeysAsync<T>(_db.Source<T>());
            var targetKeys = await ReadKeysAsync<T>(_db.Target<T>());

            var targetUtById = new Dictionary<string, DateTime?>(StringComparer.Ordinal);
            foreach (var (id, ut) in targetKeys)
                if (!string.IsNullOrEmpty(id)) targetUtById[id] = ut;

            // 2) Phân loại bằng UpdateTime.
            var insertIds = new List<string>();
            var updateIds = new List<string>();
            // UpdateTime cũ (đích) và mới (nguồn) của các bản ghi cần Update — chỉ dùng để log.
            var updateOldUt = new Dictionary<string, DateTime?>(StringComparer.Ordinal);
            var updateNewUt = new Dictionary<string, DateTime?>(StringComparer.Ordinal);
            var unchanged = 0;
            var sourceIdSet = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (id, ut) in sourceKeys)
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(id)) continue;
                sourceIdSet.Add(id);

                if (targetUtById.TryGetValue(id, out var tgtUt))
                {
                    // null == null → không đổi; null vs có giá trị → coi là đổi.
                    if (Nullable.Equals(ut, tgtUt)) unchanged++;
                    else
                    {
                        updateIds.Add(id);
                        updateOldUt[id] = tgtUt;   // giá trị đang có ở đích
                        updateNewUt[id] = ut;      // giá trị sẽ ghi (lấy từ nguồn)
                    }
                }
                else insertIds.Add(id);
            }

            // Bản ghi CHỈ CÓ Ở ĐÍCH → xóa cứng, hoặc giữ nguyên nếu ở chế độ upsert-only.
            var deleteIds = new List<string>();
            if (missingRowAction == MissingRowAction.HardDelete)
            {
                foreach (var (id, _) in targetKeys)
                    if (!string.IsNullOrEmpty(id) && !sourceIdSet.Contains(id)) deleteIds.Add(id);
            }

            // 3) CHỈ load full cột cho bản ghi cần Insert/Update (từ nguồn).
            var needFullIds = new List<string>(insertIds.Count + updateIds.Count);
            needFullIds.AddRange(insertIds);
            needFullIds.AddRange(updateIds);

            var fullSource = needFullIds.Count > 0
                ? await LoadByIdsAsync<T>(_db.Source<T>(), needFullIds, ct)
                : new List<T>();
            var srcById = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var r in fullSource)
                if (!string.IsNullOrEmpty(r.ID)) srcById[r.ID!] = r;

            // Load full ĐÍCH cho các ID cần xóa (cứng/mềm, thường ít) để log describeDelete dùng được Code...
            var delTargetById = new Dictionary<string, T>(StringComparer.Ordinal);
            if (deleteIds.Count > 0)
            {
                var fullDel = await LoadByIdsAsync<T>(_db.Target<T>(), deleteIds, ct);
                foreach (var r in fullDel)
                    if (!string.IsNullOrEmpty(r.ID)) delTargetById[r.ID!] = r;
            }

            // 4) Dựng SyncPlan.
            var plan = new SyncPlan<T> { Unchanged = unchanged };

            foreach (var id in insertIds)
            {
                if (!srcById.TryGetValue(id, out var s)) continue; // an toàn: bản ghi vừa bị xóa ở nguồn giữa 2 truy vấn
                plan.ToInsert.Add(s);
                plan.Changes.Add(new SyncChange
                {
                    Action = "Thêm",
                    Id = id,
                    Detail = describeInsert?.Invoke(s) ?? $"Code={s.Code}"
                });
            }

            foreach (var id in updateIds)
            {
                if (!srcById.TryGetValue(id, out var s)) continue;
                plan.ToUpdate.Add(s);
                var oldUt = updateOldUt.TryGetValue(id, out var uOld) ? uOld : null;
                var newUt = updateNewUt.TryGetValue(id, out var uNew) ? uNew : null;
                plan.Changes.Add(new SyncChange
                {
                    Action = "Sửa",
                    Id = id,
                    // Không load full bản ghi đích cho update nên mặc định log theo UpdateTime.
                    Detail = $"UpdateTime: '{Fmt(oldUt)}' -> '{Fmt(newUt)}'"
                });
            }

            foreach (var id in deleteIds)
            {
                plan.ToDeleteIds.Add(id);
                delTargetById.TryGetValue(id, out var tRow);
                plan.Changes.Add(new SyncChange
                {
                    Action = "Xóa",
                    Id = id,
                    Detail = (tRow != null ? describeDelete?.Invoke(tRow) : null)
                             ?? "Không còn ở nguồn -> xóa khỏi đích"
                });
            }

            // 5) Ghi xuống đích.
            result = await WriteAsync(tableName, plan, ignore, ct);
        }
        catch (OperationCanceledException)
        {
            result = new SyncResult { TableName = tableName, Success = false, Error = "Đã hủy bởi người dùng." };
        }
        catch (Exception ex)
        {
            result = new SyncResult { TableName = tableName, Success = false, Error = ex.Message };
        }

        stopwatch.Stop();
        if (result.Duration == default) result.Duration = stopwatch.Elapsed;
        return result;
    }

    // ================== helper nội bộ ==================

    /// <summary>Định dạng UpdateTime cho message log.</summary>
    private static string Fmt(DateTime? dt) => dt?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "(null)";
}
