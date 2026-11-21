using Shared.DTO.Constants.Application;
using ShareDataWorker.Infrastructure.Logging;

namespace ShareDataWorker.Infrastructure.Services.DataChangeTracking
{
    // Phần partial chứa logic quản lý danh sách bảng nguồn (phát hiện bảng quá hạn, cô lập bảng lỗi, khôi phục bảng).
    // Khối XML summary của lớp nằm ở DataChangeTrackingService.cs.
    public partial class DataChangeTrackingService
    {
        /// <summary>
        /// Description: Các bảng nguồn đang bị cô lập do mất Change Tracking hoặc không tồn tại, kèm mốc hẹn thử lại RIÊNG
        ///              của từng bảng. Giữ ở RAM, còn tóm tắt lỗi lưu tại cột LastIssue của ShareDataSourceTable.
        /// Created date: 27/09/2026
        /// </summary>
        private Dictionary<string, DateTime> MissingTables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Description: Đọc mọi bảng đang bật Change Tracking kèm mốc version tối thiểu còn hỏi được của từng
        ///              bảng, không phân biệt hoa thường để tra cứu nhanh.
        /// Created date: 28/09/2026
        /// </summary>
        private static async Task<Dictionary<string, long>> GetTrackedTableVersions(ISqlSugarClient db)
        {
            var rows = await db.Ado.SqlQueryAsync<TrackedTableVersion>(SqlGetAllTrackedTables);

            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows ?? [])
            {
                if (string.IsNullOrWhiteSpace(row.TableName))
                    continue;

                result[row.TableName] = row.MinValidVersion ?? 0;
            }

            return result;
        }

        /// <summary>
        /// Description: Dựng lại câu lệnh kiểm tra thay đổi đang áp dụng, chỉ gồm các bảng nguồn còn lành mạnh
        ///              (loại các bảng đang bị cô lập trong MissingTables). Khi không có bảng nào bị cô lập thì
        ///              kết quả đúng bằng câu lệnh của toàn bộ bảng đã cấu hình.
        /// Created date: 28/09/2026
        /// </summary>
        private void RebuildActiveChangesSql() =>
            ActiveChangesSql = BuildChangesSql([.. TrackedTables.Where(t => !MissingTables.ContainsKey(t))]);

        /// <summary>
        /// Description: Phát hiện và tạm bỏ qua các bảng nguồn chưa bật Change Tracking hoặc lỗi schema trong RAM,
        ///              đồng thời ghi nguyên nhân vào cột LastIssue của bảng tương ứng trong ShareDataSourceTable.
        ///              Tự động tái tạo ActiveChangesSql từ các bảng lành mạnh còn lại.
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private async Task<int> TrySkipBrokenTables(ISqlSugarClient db, Exception ex)
        {
            try
            {
                var trackedTableVersions = await GetTrackedTableVersions(db);

                var currentlyActive = TrackedTables.Where(t => !MissingTables.ContainsKey(t)).ToList();
                var newlyBroken = currentlyActive.Where(t => !trackedTableVersions.ContainsKey(t)).ToList();

                // Nếu sys.change_tracking_tables vẫn chứa bảng (hoặc lỗi cụ thể ở bảng đó), kiểm tra thông điệp ngoại lệ
                if (newlyBroken.Count == 0)
                {
                    foreach (var table in currentlyActive.OrderByDescending(t => t.Length))
                    {
                        if (ex.Message.Contains(table, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!newlyBroken.Any(b => b.Contains(table, StringComparison.OrdinalIgnoreCase)))
                                newlyBroken.Add(table);
                        }
                    }
                }

                if (newlyBroken.Count > 0)
                {
                    var settings = await _workerConfig.Get();
                    var tableRetryInterval = TimeSpan.FromMinutes(settings.TrackingTableRetryMin);
                    var retryAt = DateTime.UtcNow + tableRetryInterval;
                    var issueText = $"{ShareDataAlertCode.Tracking.QueryFailed}: Bảng nguồn mất Change Tracking hoặc lỗi truy vấn: {ex.Message}";
                    if (issueText.Length > EntityConst.Length256)
                        issueText = issueText[..EntityConst.Length256];

                    foreach (var b in newlyBroken)
                    {
                        MissingTables[b] = retryAt;
                        await db.Updateable<ShareDataSourceTable>()
                            .SetColumns(x => x.LastIssue == issueText)
                            .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                            .Where(x => x.TableName == b && x.IsDelete == null)
                            .ExecuteCommandAsync();
                    }

                    RebuildActiveChangesSql();

                    _logger.LogWarning("⚠️ [Fault-Tolerance] Phát hiện {Count} bảng mất Change Tracking giữa chừng: {Tables}. Đã tạm bỏ qua trong RAM (hẹn quét lại sau {Minutes:F0} phút), tiếp tục phục vụ {HealthyCount} bảng lành mạnh.",
                        newlyBroken.Count, string.Join(", ", newlyBroken), tableRetryInterval.TotalMinutes, TrackedTables.Count - MissingTables.Count);

                    await ShareDataTransferLog.WriteTrackingAsync(db, ShareDataAlertCode.Tracking.QueryFailed,
                        $"Bảng nguồn [{string.Join(", ", newlyBroken)}] mất Change Tracking giữa chừng — đã tự động bỏ qua trong RAM.",
                        errorMessage: ex.Message,
                        detailJson: $"{{\"skippedTables\":{JsonSerializer.Serialize(newlyBroken)},\"healthyRemaining\":{TrackedTables.Count - MissingTables.Count}}}");

                    return newlyBroken.Count;
                }
            }
            catch (Exception checkEx)
            {
                _logger.LogError(checkEx, "❌ Lỗi khi kiểm tra sys.change_tracking_tables để bỏ qua bảng chưa bật Change Tracking: {Message}", checkEx.Message);
            }

            return 0;
        }

        /// <summary>
        /// Description: Quét kiểm tra lại các bảng đang bị cô lập để tự động khôi phục vào ActiveChangesSql
        ///              và xóa LastIssue khi Change Tracking được bật lại.
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private async Task TryRestoreMissingTables(
            ISqlSugarClient db,
            long currentVersion,
            DataWorkerConfig settings)
        {
            var currentSettings = settings;
            var tableRetryInterval = TimeSpan.FromMinutes(currentSettings.TrackingTableRetryMin);

            try
            {
                var trackedTableVersions = await GetTrackedTableVersions(db);

                var now = DateTime.UtcNow;
                var dueTables = MissingTables.Where(kv => now >= kv.Value).Select(kv => kv.Key).ToList();

                var restored = new List<string>();
                foreach (var table in dueTables)
                {
                    var (isReady, issue) = await TryEnsureTableReadyForInit(db, trackedTableVersions, table);
                    if (isReady)
                    {
                        if (!trackedTableVersions.TryGetValue(table, out var minValid))
                        {
                            var refreshed = await GetTrackedTableVersions(db);
                            minValid = refreshed.TryGetValue(table, out var v) ? v : currentVersion;
                        }

                        await db.Updateable<ShareDataSourceTable>()
                            .SetColumns(x => x.LastVersion == SqlFunc.IIF(x.LastVersion == null || x.LastVersion < minValid, (long?)currentVersion, x.LastVersion))
                            .SetColumns(x => x.LastIssue == null)
                            .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                            .Where(x => x.TableName == table && x.IsDelete == null)
                            .ExecuteCommandAsync();

                        restored.Add(table);
                    }
                    else
                    {
                        MissingTables[table] = now + tableRetryInterval;
                        if (!string.IsNullOrWhiteSpace(issue))
                        {
                            await db.Updateable<ShareDataSourceTable>()
                                .SetColumns(x => x.LastIssue == issue)
                                .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                                .Where(x => x.TableName == table && x.IsDelete == null)
                                .ExecuteCommandAsync();
                        }
                    }
                }

                if (restored.Count > 0)
                {
                    foreach (var t in restored)
                        MissingTables.Remove(t);

                    RebuildActiveChangesSql();
                    _logger.LogInformation("✅ [Self-Healing] Đã khôi phục {Count} bảng vào giám sát Change Tracking: {Tables}",
                        restored.Count, string.Join(", ", restored));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [Self-Healing] Lỗi khi quét khôi phục các bảng thiếu Change Tracking: {Message}", ex.Message);

                var retryAt = DateTime.UtcNow + tableRetryInterval;
                foreach (var key in MissingTables.Keys.ToList())
                    MissingTables[key] = retryAt;
            }
        }

        /// <summary>
        /// Description: Đối chiếu min_valid_version của từng bảng đang giám sát với mốc riêng của chính bảng đó.
        ///              Bảng nào có mốc nhỏ hơn min_valid_version thì nhảy cóc RIÊNG bảng đó lên currentVersion,
        ///              ghi LastIssue và cảnh báo ESH-1601; các bảng lành mạnh khác vẫn giữ nguyên mốc để bắt thay đổi.
        /// Created date: 02/10/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private async Task HandleStaleTables(
            ISqlSugarClient db,
            List<ShareDataSourceTable> healthyValidRows,
            long currentVersion,
            CancellationToken cancelToken = default)
        {
            var trackedTableVersions = await GetTrackedTableVersions(db);

            var staleEntries = new List<(ShareDataSourceTable Row, long MinValid)>();
            foreach (var row in healthyValidRows)
            {
                var table = row.TableName?.Trim();
                if (string.IsNullOrWhiteSpace(table) || !row.LastVersion.HasValue)
                    continue;

                if (trackedTableVersions.TryGetValue(table, out var minValid) && minValid > row.LastVersion.Value)
                    staleEntries.Add((row, minValid));
            }

            if (staleEntries.Count == 0)
                return;

            var fastForwardedTables = new List<string>();
            var minFromVersion = long.MaxValue;

            foreach (var (staleRow, minValid) in staleEntries)
            {
                var oldVer = staleRow.LastVersion!.Value;
                var issue = $"{ShareDataAlertCode.Tracking.SelfHeal}: Mốc LastVersion ({oldVer}) quá hạn (min_valid_version={minValid}), đã tự nhảy cóc lên version {currentVersion}.";

                var affected = await db.Updateable<ShareDataSourceTable>()
                    .SetColumns(x => x.LastVersion == currentVersion)
                    .SetColumns(x => x.LastIssue == issue)
                    .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                    .Where(x => x.ID == staleRow.ID
                             && x.IsDelete == null
                             && x.LastVersion < currentVersion)
                    .ExecuteCommandAsync(cancelToken);

                staleRow.LastVersion = currentVersion;
                staleRow.LastIssue = issue;

                if (affected > 0)
                {
                    fastForwardedTables.Add(staleRow.TableName!.Trim());
                    if (oldVer < minFromVersion)
                        minFromVersion = oldVer;
                }
            }

            if (fastForwardedTables.Count == 0)
                return;

            _logger.LogWarning(
                "⚠️ [Self-Healing] Mốc Change Tracking Version={LastVer} đã rơi ra ngoài cửa sổ hợp lệ của {Count} bảng nguồn: {Tables}. Tự động nhảy cóc riêng các bảng này lên Version={CurrentVer}.",
                minFromVersion,
                fastForwardedTables.Count,
                string.Join(", ", fastForwardedTables),
                currentVersion);

            await ShareDataTransferLog.WriteTrackingAsync(
                db,
                ShareDataAlertCode.Tracking.SelfHeal,
                $"Mốc Change Tracking Version={minFromVersion} đã rơi ra ngoài cửa sổ hợp lệ của bảng nguồn [{string.Join(", ", fastForwardedTables)}], đã tự nhảy cóc lên Version={currentVersion}. Khoảng version bị bỏ qua chỉ được luồng quét định kỳ gửi bù, không qua trigger.",
                detailJson: $"{{\"fromVersion\":{minFromVersion},\"toVersion\":{currentVersion},\"skippedRange\":{currentVersion - minFromVersion},\"staleTables\":{JsonSerializer.Serialize(fastForwardedTables)}}}",
                cancelToken: cancelToken);
        }
    }
}
