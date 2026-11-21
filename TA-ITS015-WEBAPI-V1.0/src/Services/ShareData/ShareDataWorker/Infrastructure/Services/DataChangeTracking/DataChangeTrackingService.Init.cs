using ShareDataWorker.Infrastructure.Logging;

namespace ShareDataWorker.Infrastructure.Services.DataChangeTracking
{
    // Phần partial chứa logic khởi tạo môi trường và tự động kích hoạt SQL Server Change Tracking (DDL).
    // Khối XML summary của lớp nằm ở DataChangeTrackingService.cs.
    public partial class DataChangeTrackingService
    {
        /// <summary>
        /// Description: Mốc hẹn thử lại trọn lượt khởi tạo khi Change Tracking chưa dùng được, để không dội
        ///              lệnh DDL mỗi giây. Null nghĩa là thử ngay lượt tới.
        /// Created date: 02/10/2026
        /// </summary>
        private DateTime? NextInitRetryTime { get; set; }

        /// <summary>
        /// Description: Đã ghi cảnh báo Change Tracking không dùng được cho tiến trình này hay chưa, để không
        ///              ghi lại mỗi lượt thử.
        /// Created date: 02/10/2026
        /// </summary>
        private bool IsTrackingUnavailableReported { get; set; }

        /// <summary>
        /// Description: Bảo đảm Change Tracking đã bật ở cấp CSDL và trên các bảng nguồn chưa khởi tạo, cắm mốc version gốc
        ///              riêng cho từng bảng đó, đồng thời dọn nhật ký hạ tầng quá hạn.
        /// Created date: 28/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private async Task TryInitChangeTracking(
            ISqlSugarClient db,
            IReadOnlyList<ShareDataSourceTable> uninitializedRows,
            CancellationToken cancelToken,
            DataWorkerConfig settings)
        {
            var currentSettings = settings;
            var tableRetryInterval = TimeSpan.FromMinutes(currentSettings.TrackingTableRetryMin);

            var isDbReady = await EnsureDbTrackingEnabled(db, currentSettings.TrackingChangeRetentionDays);
            if (!isDbReady)
            {
                NextInitRetryTime = DateTime.UtcNow + tableRetryInterval;

                const string dbNotReadyIssue = "Change Tracking chưa được kích hoạt ở cấp CSDL.";
                foreach (var row in uninitializedRows)
                {
                    await db.Updateable<ShareDataSourceTable>()
                        .SetColumns(x => x.LastIssue == dbNotReadyIssue)
                        .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                        .Where(x => x.ID == row.ID && x.IsDelete == null)
                        .ExecuteCommandAsync(cancelToken);
                }

                _logger.LogWarning("⚠️ Change Tracking chưa được kích hoạt ở cấp CSDL. Polling Change Tracking tạm dừng cho đến khi DBA thực thi script kích hoạt. Thử lại sau {Minutes:F0} phút.",
                    tableRetryInterval.TotalMinutes);

                if (!IsTrackingUnavailableReported)
                {
                    IsTrackingUnavailableReported = true;

                    await ShareDataTransferLog.WriteTrackingAsync(db, ShareDataAlertCode.Tracking.NotReady,
                        "Change Tracking chưa kích hoạt ở cấp CSDL hoặc không có bảng nguồn nào sẵn sàng — polling tạm dừng.",
                        detailJson: $"{{\"trackedTotal\":{TrackedTables.Count}}}",
                        cancelToken: cancelToken);
                }
                return;
            }

            var baselineVersion = await GetCurrentVersion(db);
            if (!baselineVersion.HasValue)
            {
                _logger.LogWarning("⚠️ Không thể lấy mốc Version Change Tracking ban đầu. Tạm dừng chu kỳ khởi tạo.");
                return;
            }

            var trackedTableVersions = await GetTrackedTableVersions(db);
            var newlyMissing = new List<string>();
            var initializedCount = 0;

            foreach (var row in uninitializedRows)
            {
                var table = row.TableName?.Trim();
                if (string.IsNullOrWhiteSpace(table))
                    continue;

                var (isReady, issue) = await TryEnsureTableReadyForInit(db, trackedTableVersions, table);
                if (!isReady)
                {
                    newlyMissing.Add(table);
                    MissingTables[table] = DateTime.UtcNow + tableRetryInterval;

                    var issueText = issue ?? "Bảng nguồn chưa bật được Change Tracking.";
                    await db.Updateable<ShareDataSourceTable>()
                        .SetColumns(x => x.LastIssue == issueText)
                        .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                        .Where(x => x.ID == row.ID && x.IsDelete == null)
                        .ExecuteCommandAsync(cancelToken);
                    continue;
                }

                MissingTables.Remove(table);
                var lastVersion = baselineVersion.Value;
                await db.Updateable<ShareDataSourceTable>()
                    .SetColumns(x => x.LastVersion == lastVersion)
                    .SetColumns(x => x.LastIssue == null)
                    .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                    .Where(x => x.ID == row.ID
                             && x.IsDelete == null
                             && (x.LastVersion == null || x.LastVersion < 0))
                    .ExecuteCommandAsync(cancelToken);
                initializedCount++;
            }

            RebuildActiveChangesSql();

            if (newlyMissing.Count > 0 && MissingTables.Count >= TrackedTables.Count)
            {
                NextInitRetryTime = DateTime.UtcNow + tableRetryInterval;

                _logger.LogWarning(
                    "⚠️ Change Tracking chưa sẵn sàng cho toàn bộ {Count} bảng nguồn.",
                    TrackedTables.Count);

                if (!IsTrackingUnavailableReported)
                {
                    IsTrackingUnavailableReported = true;

                    await ShareDataTransferLog.WriteTrackingAsync(
                        db,
                        ShareDataAlertCode.Tracking.NotReady,
                        "Change Tracking chưa kích hoạt ở cấp CSDL hoặc không có bảng nguồn nào sẵn sàng — polling tạm dừng.",
                        detailJson: $"{{\"trackedTotal\":{TrackedTables.Count}}}",
                        cancelToken: cancelToken);
                }
                return;
            }

            if (newlyMissing.Count > 0)
            {
                _logger.LogWarning(
                    "⚠️ Change Tracking chưa sẵn sàng cho {Count} bảng: {Tables}. Đã cô lập các bảng này và ghi LastIssue, tiếp tục giám sát {HealthyCount} bảng còn lại.",
                    newlyMissing.Count,
                    string.Join(", ", newlyMissing),
                    TrackedTables.Count - MissingTables.Count);
            }

            NextInitRetryTime = null;
            IsTrackingUnavailableReported = false;

            if (initializedCount > 0)
            {
                _logger.LogInformation(
                    "✅ Đã khởi tạo mốc Change Tracking Version={Version} cho {Count} bảng nguồn.",
                    baselineVersion.Value,
                    initializedCount);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm CSDL đã bật Change Tracking chưa, chưa thì tự chạy ALTER DATABASE. Không có quyền thì trả false, không ném lỗi;
        ///              TryInitChangeTracking sẽ ghi LastIssue, cảnh báo ESH-1603 một lần và thử lại sau TrackingTableRetryMin phút.
        /// Created date: 19/10/2026
        /// </summary>
        private async Task<bool> EnsureDbTrackingEnabled(
            ISqlSugarClient db,
            int retentionDays)
        {
            var isDbTrackingEnabled = await db.Ado.GetIntAsync(SqlCheckDbTracking) > 0;
            if (!isDbTrackingEnabled)
            {
                var dbName = await db.Ado.GetStringAsync(SqlGetDbName);
                isDbTrackingEnabled = await TryEnableTracking(
                    db,
                    SqlEnableDbTracking(dbName, retentionDays),
                    async () => await db.Ado.GetIntAsync(SqlCheckDbTracking) > 0,
                    dbName);
            }

            return isDbTrackingEnabled;
        }

        /// <summary>
        /// Description: Thử chạy DDL kích hoạt Change Tracking (cấp CSDL hoặc cấp bảng) rồi kiểm tra lại kết quả; nuốt lỗi và ghi cảnh báo nếu thất bại (không có quyền DBA, hoặc lỗi tạm thời).
        /// Created date: 25/09/2026
        /// </summary>
        private async Task<bool> TryEnableTracking(
            ISqlSugarClient db,
            string enableSql,
            Func<Task<bool>> checkEnabledAsync,
            string label)
        {
            try
            {
                await db.Ado.ExecuteCommandAsync(enableSql);
                var enabled = await checkEnabledAsync();
                if (enabled)
                    _logger.LogInformation("✅ [AutoEnableChangeTracking] Đã kích hoạt Change Tracking thành công cho [{Label}].", label);
                return enabled;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ [AutoEnableChangeTracking] Không thể tự kích hoạt Change Tracking cho [{Label}]: {Message}", label, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Description: Bảo đảm một bảng nguồn sẵn sàng để cắm mốc khởi tạo hoặc khôi phục: tồn tại trong CSDL
        ///              và đã bật Change Tracking.
        /// Created date: 02/10/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private async Task<(bool IsReady, string? Issue)> TryEnsureTableReadyForInit(
            ISqlSugarClient db,
            Dictionary<string, long> trackedTableVersions,
            string table)
        {
            if (trackedTableVersions.ContainsKey(table))
                return (true, null);

            if (!db.DbMaintenance.IsAnyTable(table, false))
            {
                _logger.LogWarning(
                    "⚠️ Bảng nguồn [{Table}] khai trong ShareDataSourceTable nhưng KHÔNG tồn tại trong CSDL — gói tin tương ứng sẽ không có trigger. Kiểm tra lại tên bảng.",
                    table);
                return (false, $"Bảng nguồn '{table}' không tồn tại trong CSDL.");
            }

            var isEnabled = await TryEnableTracking(
                db,
                SqlEnableTableTracking(table),
                async () => await db.Ado.GetIntAsync(SqlCheckTableTracking, new { table }) > 0,
                table);

            if (!isEnabled)
                return (false, $"Không thể bật Change Tracking cho bảng '{table}'.");

            return (true, null);
        }
    }
}
