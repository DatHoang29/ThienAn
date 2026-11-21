using Services.Shared.Messaging;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace ShareDataWorker.Infrastructure.Services.DataChangeTracking
{
    /// <summary>
    /// Description: Dịch vụ giám sát SQL Server Change Tracking (Polling kiểm tra thay đổi).
    ///              Khi phát hiện dữ liệu bảng nguồn thay đổi, phát tín hiệu trigger lên NATS subject thông báo dữ liệu mới.
    ///              https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/about-change-tracking-sql-server?view=sql-server-ver17 
    /// Created date: 28/09/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public partial class DataChangeTrackingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataChangeTrackingService> _logger;
        private readonly TransportManager _transport;
        private readonly DataWorkerConfigService _workerConfig;
        private DateTime? _nextConfigReloadTime;
        private bool _isFirstPoll = true;

        /// <summary>
        /// Description: NATS subject mặc định dùng để phát và nhận thông báo có dữ liệu mới.
        /// Created date: 25/09/2026
        /// </summary>
        public const string DEFAULT_NATS_SUBJECT = "ta.its.event.sharedata.newdata";

        /// <summary>
        /// Description: Bảng ánh xạ giữa bảng dữ liệu nguồn SQL và danh sách các mã gói tin tương ứng cần kích hoạt.
        ///              Nạp động từ bảng ShareDataSourceTable.
        /// Created date: 27/09/2026
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> TablePacketMap { get; private set; }

        /// <summary>
        /// Description: Danh sách các bảng cần giám sát Change Tracking.
        ///              Được suy ra từ cấu hình TablePacketMap.
        /// </summary>
        public IReadOnlyList<string> TrackedTables { get; private set; }

        /// <summary>
        /// Description: Câu lệnh SQL kiểm tra thay đổi đang áp dụng — chỉ gồm các bảng nguồn còn lành mạnh.
        ///              Dựng lại qua RebuildActiveChangesSql() mỗi khi tập bảng lành thay đổi.
        /// Created date: 28/09/2026
        /// </summary>
        public string ActiveChangesSql { get; private set; }

        /// <summary>
        /// Description: Chủ đề NATS để gửi tin nhắn về dữ liệu mới.
        /// Created date: 28/09/2026
        /// </summary>
        public string NatsSubject { get; }

        /// <summary>
        /// Description: Đã ghi cảnh báo cấu hình rỗng cho tiến trình này hay chưa, để không ghi lại mỗi giây.
        /// Created date: 02/10/2026
        /// </summary>
        private bool IsEmptyConfigReported { get; set; }

        /// <summary>
        /// Description: Một dòng của DMV sys.change_tracking_tables — tên bảng và mốc version tối thiểu còn
        ///              hỏi được của bảng đó.
        /// Created date: 02/10/2026
        /// </summary>
        private sealed record TrackedTableVersion
        {
            public string? TableName       { get; init; }
            public long?   MinValidVersion { get; init; }
        }

        /// <summary>
        /// Description: Khởi tạo Service: cấu hình NATS subject và chu kỳ nạp lại cấu hình bảng giám sát.
        /// Created date: 28/09/2026
        /// </summary>
        public DataChangeTrackingService(
            IServiceScopeFactory scopeFactory,
            ILogger<DataChangeTrackingService> logger,
            TransportManager transport,
            DataWorkerConfigService workerConfig,
            IConfiguration? configuration = null)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _transport = transport;
            _workerConfig = workerConfig;

            NatsSubject = configuration?["Nats:NewDataSubject"] ?? configuration?["Nats:Subject"] ?? DEFAULT_NATS_SUBJECT;

            TablePacketMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            TrackedTables = [];
            ActiveChangesSql = string.Empty;
        }

        #region Public Methods

        /// <summary>
        /// Description: Chu kỳ polling kiểm tra mốc version mới theo từng bảng trong ShareDataSourceTable
        ///              và phát sinh trigger NATS nếu có thay đổi.
        /// Created date: 25/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        public async Task PollChanges(CancellationToken cancelToken = default)
        {
            cancelToken.ThrowIfCancellationRequested();

            var settings = await _workerConfig.Get(cancelToken);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            using var db = baseClient.CopyNew();

            await CheckAndReloadConfig(db, cancelToken);

            if (TrackedTables.Count == 0)
            {
                await ReportEmptyConfigOnce(db, cancelToken);
                return;
            }

            var currentVersion = await GetCurrentVersion(db);
            var activeRows = await db.Queryable<ShareDataSourceTable>()
                .Where(x => x.IsDelete == null && x.IsTracked == true)
                .OrderBy(x => x.OrderNo)
                .OrderBy(x => x.TableName)
                .ToListAsync(cancelToken);

            activeRows = activeRows
                .Where(r => !string.IsNullOrWhiteSpace(r.TableName) && TablePacketMap.ContainsKey(r.TableName.Trim()))
                .ToList();

            if (activeRows.Count == 0)
                return;

            if (!currentVersion.HasValue)
            {
                var hadInitialized = activeRows.Any(r => r.LastVersion.HasValue && r.LastVersion.Value >= 0);
                if (hadInitialized)
                {
                    var maxOldVer = activeRows
                        .Where(r => r.LastVersion.HasValue && r.LastVersion.Value >= 0)
                        .Max(r => r.LastVersion!.Value);
                    await ResetAllWhenDbVersionNull(db, maxOldVer, cancelToken);
                    return;
                }

                if (NextInitRetryTime.HasValue && DateTime.UtcNow < NextInitRetryTime.Value)
                    return;

                await TryInitChangeTracking(db, activeRows, cancelToken, settings);
                return;
            }

            var curVer = currentVersion.Value;

            if (MissingTables.Values.Any(retryAt => DateTime.UtcNow >= retryAt))
                await TryRestoreMissingTables(db, curVer, settings);

            var uninitializedRows = activeRows
                .Where(r => !r.LastVersion.HasValue || r.LastVersion.Value < 0)
                .ToList();

            var futureRows = activeRows
                .Where(r => r.LastVersion.HasValue && r.LastVersion.Value > curVer)
                .ToList();

            var validRows = activeRows
                .Where(r => r.LastVersion.HasValue && r.LastVersion.Value >= 0 && r.LastVersion.Value <= curVer)
                .ToList();

            if (uninitializedRows.Count > 0)
            {
                if (!NextInitRetryTime.HasValue || DateTime.UtcNow >= NextInitRetryTime.Value)
                    await TryInitChangeTracking(db, uninitializedRows, cancelToken, settings);
            }

            if (futureRows.Count > 0)
                await ResetFutureVersionTables(db, curVer, cancelToken);

            var healthyValidRows = validRows
                .Where(r => !MissingTables.ContainsKey(r.TableName!.Trim()))
                .ToList();

            if (healthyValidRows.Count == 0 || string.IsNullOrWhiteSpace(ActiveChangesSql))
                return;

            if (healthyValidRows.All(r => r.LastVersion!.Value >= curVer))
                return;

            await HandleStaleTables(db, healthyValidRows, curVer, cancelToken);

            var candidateRows = healthyValidRows
                .Where(r => !MissingTables.ContainsKey(r.TableName!.Trim()) && r.LastVersion!.Value < curVer)
                .ToList();

            if (candidateRows.Count == 0)
                return;

            var tablesToCheck = await AdvanceTableVersions(db, candidateRows, curVer, cancelToken);
            if (tablesToCheck.Count == 0)
                return;

            var changedTables = await QueryChangedTables(db, tablesToCheck);

            await UpdatePostQueryTrackState(db, tablesToCheck, changedTables, cancelToken);

            var detectedPackets = GetTriggeredPackets(changedTables, TablePacketMap);

            if (!_transport.IsConnected)
            {
                if (detectedPackets.Count > 0)
                    _logger.LogWarning("⚠️ NATS chưa kết nối. Bỏ qua publish {Count} gói [{PacketCodes}] tới '{Subject}', Worker định kỳ sẽ quét bù.",
                        detectedPackets.Count, string.Join(", ", detectedPackets), NatsSubject);
                return;
            }

            foreach (var packetCode in detectedPackets)
            {
                await _transport.PublishAsync(NatsSubject, new
                {
                    PacketCode = packetCode,
                    Type = packetCode,
                    Version = curVer,
                    TriggeredAt = DateTime.UtcNow
                }, cancelToken);
                _logger.LogInformation("📢 [NATS Trigger] Phát gói [{PacketCode}] tới subject duy nhất '{Subject}'", packetCode, NatsSubject);
            }
        }

        /// <summary>
        /// Description: Ánh xạ danh sách bảng bị thay đổi sang danh sách các mã gói tin tương ứng dựa theo bảng ánh xạ chỉ định.
        ///              Tự động chuẩn hoá mã gói về mã chuẩn (Canonical Packet Code) và loại bỏ trùng lặp.
        /// Created date: 25/09/2026
        /// </summary>
        public static IReadOnlyList<string> GetTriggeredPackets(
            IEnumerable<string> changedTables,
            IReadOnlyDictionary<string, IReadOnlyList<string>>? tablePacketMap)
        {
            if (changedTables == null || tablePacketMap == null || tablePacketMap.Count == 0)
                return Array.Empty<string>();

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in changedTables)
            {
                if (string.IsNullOrWhiteSpace(table))
                    continue;

                if (!tablePacketMap.TryGetValue(table, out var packets) || packets == null)
                    continue;

                foreach (var packet in packets)
                {
                    var normalized = PacketHelper.NormalizePacketCode(packet);
                    if (!string.IsNullOrWhiteSpace(normalized))
                        result.Add(normalized);
                }
            }

            return result.ToList();
        }

        /// <summary>
        /// Description: Ép nạp lại cấu hình bảng giám sát Change Tracking từ bảng ShareDataSourceTable ngay lập tức.
        /// Created date: 16/10/2026
        /// Modified date: 17/10/2026
        /// </summary>
        public async Task ReloadConfig(ISqlSugarClient db, CancellationToken cancelToken = default)
        {
            await ReloadConfigInternal(db, cancelToken);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Description: Cập nhật nguyên tử mốc LastVersion lên currentVersion cho từng bảng bằng Updateable kèm điều kiện so mốc (Optimistic Concurrency).
        ///              Chỉ một instance cập nhật thành công (kết quả = 1) nhận bảng trong cùng chu kỳ version.
        /// Created date: 17/10/2026
        /// Modified date: 19/10/2026
        /// </summary>
        private static async Task<List<ShareDataSourceTable>> AdvanceTableVersions(
            ISqlSugarClient db,
            IReadOnlyList<ShareDataSourceTable> candidateRows,
            long currentVersion,
            CancellationToken cancelToken = default)
        {
            var accepted = new List<ShareDataSourceTable>();
            foreach (var row in candidateRows)
            {
                if (string.IsNullOrWhiteSpace(row.TableName) || !row.LastVersion.HasValue)
                    continue;

                var oldVersion = row.LastVersion.Value;
                var updated = await db.Updateable<ShareDataSourceTable>()
                    .SetColumns(x => x.LastVersion == currentVersion)
                    .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                    .Where(x => x.ID == row.ID
                             && x.IsDelete == null
                             && x.IsTracked == true
                             && x.LastVersion == oldVersion
                             && x.LastVersion >= 0
                             && x.LastVersion < currentVersion)
                    .ExecuteCommandAsync(cancelToken);

                if (updated == 1)
                {
                    accepted.Add(new ShareDataSourceTable
                    {
                        ID = row.ID,
                        TableName = row.TableName,
                        Name = row.Name,
                        PacketCodes = row.PacketCodes,
                        IsTracked = row.IsTracked,
                        LastVersion = oldVersion,
                        LastIssue = row.LastIssue
                    });
                }
            }

            return accepted;
        }

        /// <summary>
        /// Description: Cập nhật LastChangeTime cho các bảng phát hiện có thay đổi và xóa LastIssue cho các bảng đã truy vấn thành công.
        /// Created date: 17/10/2026
        /// </summary>
        private async Task UpdatePostQueryTrackState(
            ISqlSugarClient db,
            IReadOnlyList<ShareDataSourceTable> tablesToCheck,
            IReadOnlyList<string> changedTables,
            CancellationToken cancelToken)
        {
            var changedSet = new HashSet<string>(changedTables, StringComparer.OrdinalIgnoreCase);
            if (changedSet.Count > 0)
            {
                var changedList = changedSet.ToList();
                await db.Updateable<ShareDataSourceTable>()
                    .SetColumns(x => x.LastChangeTime == SqlFunc.GetDate())
                    .SetColumns(x => x.LastIssue == null)
                    .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                    .Where(x => x.IsDelete == null && changedList.Contains(x.TableName!))
                    .ExecuteCommandAsync(cancelToken);
            }

            var healthyClearedIds = tablesToCheck
                .Where(r => !string.IsNullOrWhiteSpace(r.TableName)
                         && !MissingTables.ContainsKey(r.TableName)
                         && !changedSet.Contains(r.TableName)
                         && r.LastIssue != null)
                .Select(r => r.ID)
                .ToList();

            if (healthyClearedIds.Count > 0)
            {
                await db.Updateable<ShareDataSourceTable>()
                    .SetColumns(x => x.LastIssue == null)
                    .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                    .Where(x => x.IsDelete == null && healthyClearedIds.Contains(x.ID))
                    .ExecuteCommandAsync(cancelToken);
            }
        }

        /// <summary>
        /// Description: Truy vấn danh sách bảng nguồn có thay đổi theo mốc riêng của từng bảng cần kiểm tra.
        ///              Tự cô lập bảng lỗi trong RAM và ghi LastIssue khi bảng mất Change Tracking.
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private async Task<List<string>> QueryChangedTables(
            ISqlSugarClient db,
            IReadOnlyList<ShareDataSourceTable> tablesToCheck,
            string? customSql = null)
        {
            string sql;
            SugarParameter[] parameters;

            if (!string.IsNullOrWhiteSpace(customSql))
            {
                sql = customSql;
                BuildPerTableChangesSql(tablesToCheck, out parameters);
                if (parameters.Length == 0)
                    parameters = [new SugarParameter("@lastVer", 0L)];
            }
            else
            {
                sql = BuildPerTableChangesSql(tablesToCheck, out parameters);
                if (string.IsNullOrWhiteSpace(sql))
                    return [];
            }

            try
            {
                return await db.Ado.SqlQueryAsync<string>(sql, parameters);
            }
            catch (Exception ex)
            {
                var skippedCount = await TrySkipBrokenTables(db, ex);
                if (skippedCount > 0)
                {
                    var healthyTables = tablesToCheck
                        .Where(r => !string.IsNullOrWhiteSpace(r.TableName) && !MissingTables.ContainsKey(r.TableName.Trim()))
                        .ToList();

                    var retrySql = BuildPerTableChangesSql(healthyTables, out var retryParams);
                    if (string.IsNullOrWhiteSpace(retrySql))
                        return [];

                    _logger.LogWarning("⚠️ [Fault-Tolerance] Đã tạm bỏ qua {Count} bảng chưa bật Change Tracking. Thử lại truy vấn với {HealthyCount} bảng lành mạnh còn lại...",
                        skippedCount, healthyTables.Count);

                    try
                    {
                        return await db.Ado.SqlQueryAsync<string>(retrySql, retryParams);
                    }
                    catch (Exception retryEx)
                    {
                        await WriteQueryFailed(db, retryEx, retrySql);
                        throw;
                    }
                }

                await WriteQueryFailed(db, ex, sql);
                throw;
            }
        }

        /// <summary>
        /// Description: Lấy mốc version Change Tracking hiện tại của DB.
        ///              Trả null nếu CSDL chưa kích hoạt Change Tracking.
        /// Created date: 25/09/2026
        /// </summary>
        private static async Task<long?> GetCurrentVersion(ISqlSugarClient db)
        {
            var val = await db.Ado.GetScalarAsync(SqlCurrentVersion);
            long? version = (val != null && val != DBNull.Value && long.TryParse(val.ToString(), out var parsed))
                ? parsed
                : null;

            return version;
        }

        /// <summary>
        /// Description: Ghi cảnh báo ESH-1603 cho cấu hình bảng giám sát rỗng, đúng 1 lần cho mỗi tiến trình.
        /// Created date: 02/10/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private async Task ReportEmptyConfigOnce(ISqlSugarClient db, CancellationToken cancelToken = default)
        {
            if (IsEmptyConfigReported)
                return;

            IsEmptyConfigReported = true;

            _logger.LogError("❌ Cấu hình bảng giám sát Change Tracking (bảng ShareDataSourceTable) rỗng hoặc toàn bộ bảng bị tắt — không có bảng nguồn nào được giám sát, toàn bộ luồng trigger tức thì ngừng hoạt động. Luồng quét định kỳ vẫn gửi dữ liệu bình thường.");

            await ShareDataTransferLog.WriteTrackingAsync(db, ShareDataAlertCode.Tracking.NotReady,
                "Cấu hình bảng giám sát Change Tracking (bảng ShareDataSourceTable) rỗng hoặc toàn bộ bảng bị tắt — không có bảng nguồn nào được giám sát, luồng trigger tức thì ngừng hoạt động.",
                detailJson: "{\"trackedTotal\":0}",
                cancelToken: cancelToken);
        }

        /// <summary>
        /// Description: Ghi 1 dòng ESH-1602 cho lỗi truy vấn CHANGETABLE, kèm câu SQL trong DetailJson để lần ra
        ///              bảng gây lỗi.
        /// Created date: 02/10/2026
        /// </summary>
        private Task WriteQueryFailed(ISqlSugarClient db, Exception ex, string sql) =>
            ShareDataTransferLog.WriteTrackingAsync(db, ShareDataAlertCode.Tracking.QueryFailed,
                "Truy vấn CHANGETABLE lỗi.",
                errorMessage: ex.Message,
                detailJson: $"{{\"exceptionType\":\"{ex.GetType().Name}\",\"sql\":{JsonSerializer.Serialize(sql)}}}");

        /// <summary>
        /// Description: Hạ mốc về -1 cho toàn bộ các bảng đang bật khi Change Tracking cấp CSDL bị tắt (CurrentVersion là null).
        /// Created date: 17/10/2026
        /// </summary>
        private async Task ResetAllWhenDbVersionNull(
            ISqlSugarClient db,
            long lastVersion,
            CancellationToken cancelToken = default)
        {
            const string issue = "Change Tracking cấp CSDL bị tắt (CURRENT_VERSION = NULL), đã đặt lại mốc về -1 để khởi tạo lại.";
            var affectedRows = await db.Updateable<ShareDataSourceTable>()
                .With(SqlWith.TabLockX)
                .SetColumns(x => x.LastVersion == -1)
                .SetColumns(x => x.LastIssue == issue)
                .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                .Where(x => x.IsDelete == null
                         && x.IsTracked == true
                         && x.LastVersion != null
                         && x.LastVersion >= 0)
                .ExecuteCommandAsync(cancelToken);

            if (affectedRows <= 0)
                return;

            _logger.LogWarning(
                "⚠️ [Reset] Change Tracking cấp CSDL bị tắt (CurrentVersion=NULL). Đã hạ mốc {Count} bảng trong ShareDataSourceTable về -1 để chu kỳ sau khởi tạo lại.",
                affectedRows);

            await ShareDataTransferLog.WriteTrackingAsync(
                db,
                ShareDataAlertCode.Tracking.VersionReset,
                $"Mốc Change Tracking đang giữ (LastVersion={lastVersion}) không còn tương thích CSDL (CurrentVersion=null), đã hạ về -1 trên {affectedRows} bảng để đi lại đường khởi tạo. Khoảng dữ liệu phát sinh tới khi khởi tạo xong chỉ được luồng quét định kỳ gửi bù, không qua trigger.",
                detailJson: $"{{\"lastVersion\":{lastVersion},\"currentVersion\":null,\"trackingEnabled\":false,\"affectedTables\":{affectedRows}}}",
                cancelToken: cancelToken);
        }

        /// <summary>
        /// Description: Hạ mốc về -1 cho các bảng có LastVersion lớn hơn version hiện tại của CSDL bằng Updateable kèm điều kiện so mốc.
        ///              Bảo đảm chỉ instance nào cập nhật thành công (kết quả = 1) mới ghi cảnh báo ESH-1604 khi đua.
        /// Created date: 02/10/2026
        /// Modified date: 19/10/2026
        /// </summary>
        private async Task ResetFutureVersionTables(
            ISqlSugarClient db,
            long currentVersion,
            CancellationToken cancelToken = default)
        {
            var futureCandidates = await db.Queryable<ShareDataSourceTable>()
                .Where(x => x.IsDelete == null && x.IsTracked == true && x.LastVersion > currentVersion)
                .Select(x => new { x.ID, x.TableName, x.LastVersion })
                .ToListAsync(cancelToken);

            if (futureCandidates == null || futureCandidates.Count == 0)
                return;

            var issue = $"Mốc LastVersion vượt quá version hiện tại của CSDL ({currentVersion}), đã đặt lại về -1 để khởi tạo lại.";
            var resetTables = new List<ShareDataSourceTable>();

            foreach (var candidate in futureCandidates)
            {
                if (!candidate.LastVersion.HasValue)
                    continue;

                var oldVer = candidate.LastVersion.Value;
                var updated = await db.Updateable<ShareDataSourceTable>()
                    .SetColumns(x => x.LastVersion == -1)
                    .SetColumns(x => x.LastIssue == issue)
                    .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                    .Where(x => x.ID == candidate.ID && x.IsDelete == null && x.LastVersion == oldVer)
                    .ExecuteCommandAsync(cancelToken);

                if (updated == 1)
                {
                    resetTables.Add(new ShareDataSourceTable
                    {
                        ID = candidate.ID,
                        TableName = candidate.TableName,
                        LastVersion = oldVer,
                        LastIssue = issue
                    });
                }
            }

            if (resetTables.Count == 0)
                return;

            var maxOldVer = resetTables.Max(r => r.LastVersion ?? 0L);
            var tableNames = resetTables
                .Where(r => !string.IsNullOrWhiteSpace(r.TableName))
                .Select(r => r.TableName!)
                .ToList();

            _logger.LogWarning(
                "⚠️ [Reset] Mốc Change Tracking đang giữ (LastVersion={LastVer}) trên {Count} bảng [{Tables}] vượt quá CSDL (CurrentVersion={CurrentVer}). Đã hạ mốc về -1 để chu kỳ sau khởi tạo lại.",
                maxOldVer,
                tableNames.Count,
                string.Join(", ", tableNames),
                currentVersion);

            await ShareDataTransferLog.WriteTrackingAsync(
                db,
                ShareDataAlertCode.Tracking.VersionReset,
                $"Mốc Change Tracking đang giữ (LastVersion={maxOldVer}) không còn tương thích CSDL (CurrentVersion={currentVersion}), đã hạ về -1 để đi lại đường khởi tạo. Khoảng dữ liệu phát sinh tới khi khởi tạo xong chỉ được luồng quét định kỳ gửi bù, không qua trigger.",
                detailJson: $"{{\"lastVersion\":{maxOldVer},\"currentVersion\":{currentVersion},\"trackingEnabled\":true,\"resetTables\":{JsonSerializer.Serialize(tableNames)}}}",
                cancelToken: cancelToken);
        }

        /// <summary>
        /// Description: Kiểm tra và thực hiện nạp lại cấu hình bảng giám sát Change Tracking từ ShareDataSourceTable
        ///              định kỳ theo cấu hình ReloadIntervalSec hoặc ở chu kỳ poll đầu tiên.
        /// Created date: 16/10/2026
        /// </summary>
        private async Task CheckAndReloadConfig(ISqlSugarClient db, CancellationToken cancelToken = default)
        {
            var isDue = _isFirstPoll || (_nextConfigReloadTime.HasValue && DateTime.UtcNow >= _nextConfigReloadTime.Value);
            if (!isDue)
                return;

            _isFirstPoll = false;
            var settings = await _workerConfig.Get(cancelToken);
            _nextConfigReloadTime = DateTime.UtcNow.AddSeconds(settings.ReloadIntervalSec);

            await ReloadConfigInternal(db, cancelToken);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc lại danh sách bảng theo dõi từ ShareDataSourceTable. Đọc lỗi thì giữ cấu hình đang chạy, chỉ ghi log.
        ///              Có thay đổi thì cập nhật danh sách bảng, gỡ bảng đã bỏ khỏi MissingTables và dựng lại câu truy vấn thay đổi.
        /// Created date: 19/10/2026
        /// </summary>
        private async Task ReloadConfigInternal(ISqlSugarClient db, CancellationToken cancelToken = default)
        {
            Dictionary<string, IReadOnlyList<string>> nextMap;
            try
            {
                nextMap = await LoadTrackerMap(db, cancelToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancelToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    ex,
                    "⚠️ Không thể đọc cấu hình bảng giám sát từ ShareDataSourceTable; giữ cấu hình đang chạy: {Message}",
                    ex.Message);
                return;
            }

            if (AreMapsEqual(TablePacketMap, nextMap))
                return;

            var currentTables = TrackedTables;
            var nextTables = nextMap.Keys.ToList();
            var removedTables = currentTables.Except(nextTables, StringComparer.OrdinalIgnoreCase).ToList();
            var addedTables = nextTables.Except(currentTables, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var removed in removedTables)
                MissingTables.Remove(removed);

            TablePacketMap = nextMap;
            TrackedTables = [.. nextTables];

            if (TrackedTables.Count > 0)
                IsEmptyConfigReported = false;

            RebuildActiveChangesSql();

            _logger.LogInformation(
                "🔄 Đã cập nhật cấu hình bảng giám sát Change Tracking từ ShareDataSourceTable: {Count} bảng đang theo dõi (Thêm mới: {AddedCount}, Gỡ bỏ: {RemovedCount}).",
                TrackedTables.Count,
                addedTables.Count,
                removedTables.Count);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: So hai bản đồ bảng và gói tin, không phân biệt hoa thường và thứ tự gói.
        /// Created date: 19/10/2026
        /// </summary>
        private static bool AreMapsEqual(
            IReadOnlyDictionary<string, IReadOnlyList<string>>? current,
            IReadOnlyDictionary<string, IReadOnlyList<string>>? next)
        {
            if (current == null && next == null)
                return true;
            if (current == null || next == null)
                return false;
            if (current.Count != next.Count)
                return false;

            foreach (var (table, packets) in current)
            {
                if (!next.TryGetValue(table, out var nextPackets))
                    return false;

                if (packets.Count != nextPackets.Count)
                    return false;

                var set1 = new HashSet<string>(packets, StringComparer.OrdinalIgnoreCase);
                var set2 = new HashSet<string>(nextPackets, StringComparer.OrdinalIgnoreCase);
                if (!set1.SetEquals(set2))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Description: Đọc danh sách cấu hình bảng giám sát từ bảng ShareDataSourceTable.
        ///              Chỉ lấy các bản ghi chưa xóa mềm và có IsTracked = true.
        ///              Trả về Dictionary rỗng nếu bảng rỗng hoặc toàn bộ dòng đều tắt (IsTracked != true).
        /// Created date: 16/10/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private static async Task<Dictionary<string, IReadOnlyList<string>>> LoadTrackerMap(
            ISqlSugarClient db,
            CancellationToken cancelToken)
        {
            var rows = await db.Queryable<ShareDataSourceTable>()
                .Where(x => x.IsDelete == null && x.IsTracked == true)
                .OrderBy(x => x.OrderNo)
                .OrderBy(x => x.TableName)
                .ToListAsync(cancelToken);

            var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            if (rows == null || rows.Count == 0)
                return map;

            foreach (var row in rows)
            {
                var tableName = row.TableName?.Trim();
                if (string.IsNullOrWhiteSpace(tableName))
                    continue;

                if (!SqlIdentifierRegex().IsMatch(tableName))
                {
                    const string issue = "Tên bảng nguồn không hợp lệ (không đúng định danh SQL).";
                    await db.Updateable<ShareDataSourceTable>()
                        .SetColumns(x => x.LastIssue == issue)
                        .SetColumns(x => x.UpdateTime == SqlFunc.GetDate())
                        .Where(x => x.ID == row.ID && x.IsDelete == null)
                        .ExecuteCommandAsync(cancelToken);
                    continue;
                }

                var packets = (row.PacketCodes ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                map[tableName] = packets;
            }

            return map;
        }

        #endregion
    }
}
