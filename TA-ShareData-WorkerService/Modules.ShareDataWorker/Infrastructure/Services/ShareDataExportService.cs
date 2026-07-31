namespace TA_ShareData_WorkerService.Infrastructure.Services
{
    /// <summary>
    /// Background Worker Service xử lý kết xuất & truyền dữ liệu định kỳ theo CronJob (ESHARE V1)
    /// Author: Đạt
    /// Created date: 31/07/2026
    /// </summary>
    public class ShareDataExportService(IServiceScopeFactory scopeFactory, ILogger<ShareDataExportService> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<ShareDataExportService> _logger = logger;

        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);
        private static readonly IReadOnlyList<string> ForbiddenSqlKeywords = ["DROP", "DELETE", "UPDATE", "INSERT", "EXEC", "TRUNCATE", "ALTER", "--", "/*"];
        private const int DefaultIntervalSeconds = 60;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(BaseMsg.Worker.Started, DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchSubscriptionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    LogInformationMsg("❌ Lỗi trong quá trình xử lý kết xuất dữ liệu: {ErrorMessage}", ex.Message);
                }
                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation(BaseMsg.Worker.Stopping, DateTimeOffset.Now);
        }

        public async Task ProcessBatchSubscriptionsAsync(CancellationToken stoppingToken = default)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var now = DateTime.Now;
            var lockTimeoutDate = now.Subtract(LockTimeout);
            var processLockId = Guid.NewGuid().ToString("N");

            var updatedCount = await db.Updateable<EshSubscription>()
                .SetColumns(x => new EshSubscription
                {
                    RunStatus = EshEnums.RunStatus.Running,
                    ProcessLockId = processLockId,
                    UpdateTime = now,
                })
                .Where(s => s.State == EshEnums.SubState.Active
                         && (s.Mode == EshEnums.SubMode.Batch || s.Mode == EshEnums.SubMode.Periodic)
                         && s.Direction == EshEnums.SubDirection.Outbound
                         && (s.RunStatus == null || s.RunStatus != EshEnums.RunStatus.Running || s.UpdateTime <= lockTimeoutDate)
                         && (s.NextTimeRun == null || s.NextTimeRun <= now))
                .ExecuteCommandAsync(stoppingToken);

            if (updatedCount == 0) return;

            var subs = await db.Queryable<EshSubscription>()
                .Where(s => s.ProcessLockId == processLockId && s.RunStatus == EshEnums.RunStatus.Running)
                .ToListAsync(stoppingToken);

            foreach (var sub in subs)
                if (!stoppingToken.IsCancellationRequested) await ExecuteExportAsync(sub);
        }

        private async Task ExecuteExportAsync(EshSubscription sub)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            using var db = baseClient.CopyNew();
            var now = DateTime.Now;

            var mapping = await db.Queryable<EshMappingProfile>().InSingleAsync(sub.MappingProfileId);
            if (mapping == null)
            {
                await LogExportResultAsync(db, sub, 0, 0, null, EshEnums.ExportStatus.Failed, BaseMsg.ExportValidation.ProfileNotFound);
                return;
            }

            var fieldMappings = await db.Queryable<EshFieldMapping>()
                .Where(f => f.MappingProfileId == mapping.ID)
                .OrderBy(f => f.OrderNo)
                .ToListAsync();

            if (fieldMappings.Count == 0)
            {
                await LogExportResultAsync(db, sub, 0, 0, null, EshEnums.ExportStatus.Failed, BaseMsg.ExportValidation.FieldMappingEmpty);
                return;
            }

            var dataSource = await db.Queryable<EshDataSource>().InSingleAsync(sub.DataSourceId);
            if (dataSource == null)
            {
                await LogExportResultAsync(db, sub, 0, 0, null, EshEnums.ExportStatus.Failed, BaseMsg.ExportValidation.DataSourceNotFound);
                return;
            }

            var (sql, errorMessage) = BuildExportSql(dataSource, fieldMappings, sub.LastTimeRun);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await LogExportResultAsync(db, sub, 0, 0, null, EshEnums.ExportStatus.Failed, errorMessage);
                return;
            }

            int topNValue = dataSource.TopN is > 0 and <= 10000 ? dataSource.TopN : 1000;

            try
            {
                var dt = await db.Ado.GetDataTableAsync(sql, new { topN = topNValue, lastTimeRun = sub.LastTimeRun });
                var (exportDataList, transformError) = TransformDataTableToExportPayload(dt, fieldMappings);

                if (!string.IsNullOrEmpty(transformError))
                {
                    await LogExportResultAsync(db, sub, 0, 0, null, EshEnums.ExportStatus.Failed, transformError);
                    return;
                }

                var recordCount = (long)exportDataList.Count;
                if (recordCount == 0)
                {
                    LogInformationMsg($"ℹ️ Không có dữ liệu mới cho Subscription ID {sub.ID}. Bỏ qua xuất file JSON.");
                    await LogExportResultAsync(db, sub, 0, 0, null, EshEnums.ExportStatus.Success, "Không có dữ liệu mới");
                    return;
                }

                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(exportDataList);
                var byteSize = (long)jsonBytes.Length;
                var hashBytes = SHA256.HashData(jsonBytes);
                var fileHash = Convert.ToHexString(hashBytes);

                var partner = await db.Queryable<EshPartner>().InSingleAsync(sub.PartnerId);
                var vendorCode = partner?.Code ?? sub.PartnerId ?? EshEnums.SystemConstants.Unknown;
                var dataTypeId = sub.DatatypeId ?? EshEnums.SystemConstants.Unknown;
                var relativePath = EshExportPathHelper.GenerateExportRelativePath(vendorCode, dataTypeId, now);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
                var directoryPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                await File.WriteAllBytesAsync(fullPath, jsonBytes);
                await LogExportResultAsync(db, sub, recordCount, byteSize, relativePath, EshEnums.ExportStatus.Success, null, fileHash);
            }
            catch (Exception ex)
            {
                LogInformationMsg($"❌ Lỗi kết xuất dữ liệu cho Subscription ID {sub.ID}: {ex.Message}");
                await LogExportResultAsync(db, sub, 0, 0, null, EshEnums.ExportStatus.Failed, ex.Message);
            }
            finally
            {
                var finishedTime = DateTime.Now;
                var intervalSec = sub.IntervalSeconds > 0 ? sub.IntervalSeconds : DefaultIntervalSeconds;
                var nextTimeRun = finishedTime.AddSeconds(intervalSec);

                await db.Updateable<EshSubscription>()
                    .SetColumns(x => new EshSubscription
                    {
                        RunStatus = EshEnums.RunStatus.Idle,
                        ProcessLockId = null,
                        LastTimeRun = now,
                        NextTimeRun = nextTimeRun,
                        UpdateTime = finishedTime
                    })
                    .Where(x => x.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        private static (string Sql, string? ErrorMessage) BuildExportSql(EshDataSource dataSource, List<EshFieldMapping> fieldMappings, DateTime? lastTimeRun)
        {
            var hasLastTime = lastTimeRun.HasValue;

            if (dataSource.Kind == EshEnums.DataSourceKind.FieldPicker)
            {
                if (string.IsNullOrWhiteSpace(dataSource.Table))
                    return (string.Empty, BaseMsg.ExportValidation.TableEmptyForPicker);

                var selectColumns = string.Join(", ", fieldMappings.Select(f => $"[{f.SourceKey}]"));
                var timeCol = hasLastTime ? FindTimeColumn(fieldMappings) : null;
                var whereClause = !string.IsNullOrEmpty(timeCol) ? $" WHERE [{timeCol}] > @lastTimeRun ORDER BY [{timeCol}] ASC" : "";
                return ($"SELECT TOP (@topN) {selectColumns} FROM [{dataSource.Table}]{whereClause}", null);
            }

            if (dataSource.Kind == EshEnums.DataSourceKind.SavedQuery)
            {
                if (string.IsNullOrWhiteSpace(dataSource.QueryText))
                    return (string.Empty, BaseMsg.ExportValidation.QueryEmptyForSavedQuery);

                var trimmedQuery = dataSource.QueryText.Trim();
                if (!trimmedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    return (string.Empty, BaseMsg.ExportValidation.QueryMustStartWithSelect);

                var forbiddenKw = ForbiddenSqlKeywords.FirstOrDefault(kw => (kw == "--" || kw == "/*")
                        ? trimmedQuery.Contains(kw, StringComparison.OrdinalIgnoreCase)
                        : Regex.IsMatch(trimmedQuery, $@"\b{kw}\b", RegexOptions.IgnoreCase));

                if (forbiddenKw != null)
                    return (string.Empty, string.Format(BaseMsg.ExportValidation.ForbiddenKeyword, forbiddenKw));

                if (Regex.IsMatch(trimmedQuery, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase) &&
                    !Regex.IsMatch(trimmedQuery, @"\bTOP\b", RegexOptions.IgnoreCase))
                {
                    trimmedQuery = Regex.Replace(trimmedQuery, @"^SELECT\b", "SELECT TOP 100 PERCENT", RegexOptions.IgnoreCase);
                }

                var timeCol = hasLastTime ? FindTimeColumn(fieldMappings, trimmedQuery) : null;
                var whereClause = !string.IsNullOrEmpty(timeCol) ? $" WHERE temp_query.[{timeCol}] > @lastTimeRun ORDER BY temp_query.[{timeCol}] ASC" : "";
                return ($"SELECT TOP (@topN) * FROM ({trimmedQuery}) AS temp_query{whereClause}", null);
            }

            return (string.Empty, BaseMsg.ExportValidation.UnsupportedDataSource);
        }

        private static string? FindTimeColumn(List<EshFieldMapping> fieldMappings, string? queryText = null)
        {
            var candidates = new[] { "UpdateTime", "CreateTime", "CreatedTime", "LogTime", "UpdatedDate", "CreatedDate", "ExportedAt", "Timestamp", "Time" };

            foreach (var candidate in candidates)
            {
                var mapped = fieldMappings.FirstOrDefault(f => candidate.Equals(f.SourceKey, StringComparison.OrdinalIgnoreCase));
                if (mapped != null && !string.IsNullOrWhiteSpace(mapped.SourceKey))
                    return mapped.SourceKey;
            }

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                foreach (var candidate in candidates)
                {
                    if (Regex.IsMatch(queryText, $@"\b{candidate}\b", RegexOptions.IgnoreCase))
                        return candidate;
                }
            }

            return null;
        }

        private static (List<Dictionary<string, object?>> Data, string? ErrorMessage) TransformDataTableToExportPayload(DataTable dt, List<EshFieldMapping> fieldMappings)
        {
            var existingColumns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingRequiredList = new List<string>();
            var validMappings = fieldMappings.Where(f => !string.IsNullOrWhiteSpace(f.TargetKey)).ToList();

            var exportDataList = dt.Rows.Cast<DataRow>().Select(row => validMappings.ToDictionary(
                f => f.TargetKey ?? string.Empty,
                f =>
                {
                    var sourceKey = f.SourceKey ?? string.Empty;
                    var targetKey = f.TargetKey ?? string.Empty;

                    var hasSource = !string.IsNullOrEmpty(sourceKey) && existingColumns.Contains(sourceKey);
                    var val = (hasSource && row[sourceKey] != DBNull.Value) ? row[sourceKey] : f.DefaultValue;

                    if (f.IsRequired && (val == null || string.IsNullOrWhiteSpace(val.ToString())) && !string.IsNullOrEmpty(targetKey))
                        missingRequiredList.Add(targetKey);

                    return val;
                })
            ).ToList();

            if (missingRequiredList.Count > 0)
                return (exportDataList, string.Format(BaseMsg.ExportValidation.MissingRequiredFields, string.Join(", ", missingRequiredList.Distinct())));

            return (exportDataList, null);
        }

        private static async Task LogExportResultAsync(SqlSugarClient db, EshSubscription sub, long recordCount, long byteSize, string? filePath, string? status, string? errorMessage = null, string? hash = null)
        {
            var exportLog = new EshExportLog
            {
                SubscriptionId = sub.ID,
                MappingId = sub.MappingProfileId,
                PartnerId = sub.PartnerId,
                DatatypeId = sub.DatatypeId,
                ExportedAt = DateTime.Now,
                RecordCount = recordCount,
                ByteSize = byteSize,
                FilePath = filePath,
                Hash = hash,
                Status = status,
                ErrorMessage = errorMessage,
                CreatedBy = "ShareDataExportService"
            };

            await db.Insertable(exportLog).ExecuteCommandAsync();
        }

        private void LogInformationMsg(string message, params object?[] args)
        {
#if DEBUG
            _logger.LogInformation(message, args);
#endif
        }
    }
}
