using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace ShareDataWorker.Infrastructure.Services.DataCleanup
{
    /// <summary>
    /// Description: Triển khai dịch vụ dọn dẹp định kỳ dữ liệu lưu trữ (tập tin xuất bản và nhật ký CSDL) cho phân hệ ShareData.
    /// Author: DatHQ
    /// Created date: 16/10/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public class DataCleanupService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataCleanupService> _logger;
        private readonly DataWorkerConfigService _workerConfig;

        public DataCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<DataCleanupService> logger,
            DataWorkerConfigService workerConfig)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _workerConfig = workerConfig;
        }

        /// <summary>
        /// Description: Dọn các dòng nhật ký hạ tầng Change Tracking (mã ESH-16xx ở cột Remark) cũ hơn số ngày giữ lại.
        ///              Xoá theo lô qua ORM. Điều kiện lọc BẮT BUỘC gồm cả tiền tố mã ShareDataAlertCode.Tracking.Prefix.
        /// </summary>
        public async Task CleanTrackingLogs(CancellationToken ct = default)
        {
            try
            {
                var opt = await _workerConfig.Get(ct);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
                using var db = baseClient.CopyNew();

                var retentionDays = opt.TrackingLogRetentionDays;
                var cutoff = DateTime.Now.AddDays(-Math.Abs(retentionDays));
                var prefix = ShareDataAlertCode.Tracking.Prefix;
                var totalDeleted = 0;
                var effectiveBatchSize = opt.TrackingLogPurgeBatch;

                while (!ct.IsCancellationRequested)
                {
                    var staleIds = await db.Queryable<ShareDataActivityLog>()
                        .Where(x => x.Remark != null && x.Remark.StartsWith(prefix))
                        .Where(x => x.CreateTime < cutoff)
                        .Select(x => x.ID)
                        .Take(effectiveBatchSize)
                        .ToListAsync(ct);

                    if (staleIds.Count == 0)
                        break;

                    var deletedCount = await db.Deleteable<ShareDataActivityLog>()
                        .Where(x => staleIds.Contains(x.ID))
                        .ExecuteCommandAsync(ct);

                    totalDeleted += deletedCount;
                    if (deletedCount < effectiveBatchSize)
                        break;
                }

                if (totalDeleted > 0)
                {
                    _logger.LogInformation(
                        "🧹 Đã dọn {Count} dòng nhật ký hạ tầng Change Tracking cũ hơn {Days} ngày.",
                        totalDeleted,
                        retentionDays);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "⚠️ Không dọn được nhật ký hạ tầng Change Tracking: {Message}", ex.Message);
            }
        }

        // [NGOẠI LỆ ĐÃ XÁC NHẬN BỞI NGƯỜI DÙNG 18/10/2026]: miễn trừ Rule 19.68 cho khối dọn tệp bằng chứng và nhật ký nghiệp vụ dưới đây; chưa kích hoạt, khi kích hoạt cần thêm tham số cleanup_enabled, cleanup_retention_days vào sharedata_config.
        /*
        /// <summary>
        /// Description: Dọn dẹp tập tin lưu trữ quá hạn và nhật ký nghiệp vụ trong CSDL khi cấu hình cleanup_enabled = 1.
        /// </summary>
        public async Task CleanArchive(DataWorkerConfig opt, CancellationToken ct)
        {
            if (opt.CleanupEnabled != 1)
            {
                _logger.LogInformation(
                    "ℹ️ DataCleanupService đang ở trạng thái tắt (cleanup_enabled={Enabled}). Bỏ qua chu kỳ dọn tệp và nhật ký nghiệp vụ.",
                    opt.CleanupEnabled);
                return;
            }

            var retentionDays = opt.CleanupRetentionDays;
            var cutoffDate = DateTime.Now.Date.AddDays(-Math.Abs(retentionDays));

            _logger.LogInformation(
                "🚀 Bắt đầu chu kỳ dọn dẹp dữ liệu lưu trữ ShareData. Mốc lưu trữ: {Days} ngày (trước {Cutoff:yyyy-MM-dd HH:mm:ss}).",
                retentionDays,
                cutoffDate);

            // 1. Dọn dẹp tập tin lưu trữ trên đĩa/NAS
            try
            {
                var configuredBasePath = _configuration[ExportPathHelper.BasePathConfigKey];
                var rootPath = ExportPathHelper.ResolveRootPath(configuredBasePath);
                CleanupArchiveFiles(rootPath, cutoffDate);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "❌ Lỗi xảy ra trong quá trình dọn dẹp tập tin lưu trữ ShareData: {Message}", ex.Message);
            }

            // 2. Dọn dẹp bản ghi nhật ký nghiệp vụ và gói tin nhận đã hoàn tất trong CSDL
            try
            {
                var batchSize = opt.TrackingLogPurgeBatch;
                await CleanupDatabaseLogs(cutoffDate, batchSize, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "❌ Lỗi xảy ra trong quá trình dọn dẹp nhật ký CSDL ShareData: {Message}", ex.Message);
            }

            _logger.LogInformation("✅ Hoàn thành chu kỳ dọn dẹp dữ liệu lưu trữ ShareData.");
        }

        /// <summary>
        /// Description: Xóa các thư mục ngày lưu trữ gói tin đã quá hạn RetentionDays.
        ///              Cấu trúc: {BasePath}/Out/{Partner}/{yyyy}/{MM}/{dd}/{Datatype}/...
        ///              Duyệt qua từng Partner rồi đến yyyy/MM/dd, xóa nguyên thư mục dd và thu hồi thư mục rỗng cha.
        /// </summary>
        private void CleanupArchiveFiles(string rootPath, DateTime cutoffDate)
        {
            if (!Directory.Exists(rootPath))
            {
                _logger.LogWarning("⚠️ Thư mục gốc lưu trữ không tồn tại: {Path}", rootPath);
                return;
            }

            var outPath = Path.Combine(rootPath, "Out");
            if (!Directory.Exists(outPath))
                return;

            var fullOutPath = Path.GetFullPath(outPath);

            foreach (var partnerDir in Directory.GetDirectories(fullOutPath))
            {
                var fullPartnerDir = Path.GetFullPath(partnerDir);
                if (!fullPartnerDir.StartsWith(fullOutPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("⛔ Bỏ qua đường dẫn không an toàn ngoài phạm vi gốc: {Path}", partnerDir);
                    continue;
                }

                foreach (var yearDir in Directory.GetDirectories(fullPartnerDir))
                {
                    var yearName = Path.GetFileName(yearDir);
                    if (!int.TryParse(yearName, out var year) || year < 2000 || year > 2100)
                        continue;

                    foreach (var monthDir in Directory.GetDirectories(yearDir))
                    {
                        var monthName = Path.GetFileName(monthDir);
                        if (!int.TryParse(monthName, out var month) || month < 1 || month > 12)
                            continue;

                        foreach (var dayDir in Directory.GetDirectories(monthDir))
                        {
                            var dayName = Path.GetFileName(dayDir);
                            if (!int.TryParse(dayName, out var day) || day < 1 || day > 31)
                                continue;

                            DateTime folderDate;
                            try
                            {
                                folderDate = new DateTime(year, month, day);
                            }
                            catch
                            {
                                continue;
                            }

                            if (folderDate < cutoffDate)
                            {
                                try
                                {
                                    Directory.Delete(dayDir, recursive: true);
                                    _logger.LogInformation("🗑️ Đã xóa thư mục ngày lưu trữ quá hạn: {DayDir}", dayDir);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "⚠️ Không thể xóa thư mục ngày {DayDir}: {Message}", dayDir, ex.Message);
                                }
                            }
                        }

                        // Thu hồi thư mục tháng nếu đã rỗng sau khi xóa các thư mục ngày
                        try
                        {
                            if (!Directory.EnumerateFileSystemEntries(monthDir).Any())
                            {
                                Directory.Delete(monthDir, recursive: false);
                                _logger.LogInformation("🧹 Đã dọn thư mục tháng rỗng: {MonthDir}", monthDir);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Không thể dọn thư mục tháng {MonthDir}: {Message}", monthDir, ex.Message);
                        }
                    }

                    // Thu hồi thư mục năm nếu đã rỗng sau khi xóa các thư mục tháng
                    try
                    {
                        if (!Directory.EnumerateFileSystemEntries(yearDir).Any())
                        {
                            Directory.Delete(yearDir, recursive: false);
                            _logger.LogInformation("🧹 Đã dọn thư mục năm rỗng: {YearDir}", yearDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Không thể dọn thư mục năm {YearDir}: {Message}", yearDir, ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Description: Dọn các bản ghi nhật ký hoạt động nghiệp vụ, cảnh báo và gói tin nhận đã hoàn tất.
        /// </summary>
        private async Task CleanupDatabaseLogs(DateTime cutoffDate, int batchSize, CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            using var db = baseClient.CopyNew();

            var infrastructurePrefix = ShareDataAlertCode.Tracking.Prefix;

            // 1. Dọn bảng nhật ký nghiệp vụ: ShareDataActivityLog (loại trừ các bản ghi ESH-16xx của hạ tầng)
            var totalDeletedActivityLogs = 0;
            while (!ct.IsCancellationRequested)
            {
                var staleActivityIds = await db.Queryable<ShareDataActivityLog>()
                    .Where(x => x.CreateTime < cutoffDate)
                    .Where(x => x.Remark == null || !x.Remark.StartsWith(infrastructurePrefix))
                    .Select(x => x.ID)
                    .Take(batchSize)
                    .ToListAsync(ct);

                if (staleActivityIds.Count == 0)
                    break;

                var deletedCount = await db.Deleteable<ShareDataActivityLog>()
                    .Where(x => staleActivityIds.Contains(x.ID))
                    .ExecuteCommandAsync(ct);

                totalDeletedActivityLogs += deletedCount;
                if (deletedCount < batchSize)
                    break;
            }

            if (totalDeletedActivityLogs > 0)
            {
                _logger.LogInformation(
                    "🧹 Đã dọn {Count} bản ghi nhật ký hoạt động nghiệp vụ (ShareDataActivityLog) cũ hơn mốc lưu trữ.",
                    totalDeletedActivityLogs);
            }

            // 2. Dọn bảng nhật ký cảnh báo: ShareDataAlertLog
            var totalDeletedAlertLogs = 0;
            while (!ct.IsCancellationRequested)
            {
                var staleAlertIds = await db.Queryable<ShareDataAlertLog>()
                    .Where(x => x.CreateTime < cutoffDate)
                    .Select(x => x.ID)
                    .Take(batchSize)
                    .ToListAsync(ct);

                if (staleAlertIds.Count == 0)
                    break;

                var deletedCount = await db.Deleteable<ShareDataAlertLog>()
                    .Where(x => staleAlertIds.Contains(x.ID))
                    .ExecuteCommandAsync(ct);

                totalDeletedAlertLogs += deletedCount;
                if (deletedCount < batchSize)
                    break;
            }

            if (totalDeletedAlertLogs > 0)
            {
                _logger.LogInformation(
                    "🧹 Đã dọn {Count} bản ghi cảnh báo (ShareDataAlertLog) cũ hơn mốc lưu trữ.",
                    totalDeletedAlertLogs);
            }

            // 3. Dọn bảng ShareDataInboundPacket trên CSDL nhận (DEV_ITS10_Inbound) — chỉ gói đã hoàn tất
            var tenant = baseClient.AsTenant();
            if (tenant.IsAnyConnection(ShareDataWorkerExtensions.InboundConnectionKey))
            {
                var inboundScope = tenant.GetConnectionScope(ShareDataWorkerExtensions.InboundConnectionKey);
                using var inboundDb = inboundScope.CopyNew();

                var completedStates = new[]
                {
                    BaseEnums.InboundProcessState.Done,
                    BaseEnums.InboundProcessState.Failed,
                    BaseEnums.InboundProcessState.NoSubscription
                };

                var totalDeletedInboundPackets = 0;
                while (!ct.IsCancellationRequested)
                {
                    var staleInboundIds = await inboundDb.Queryable<ShareDataInboundPacket>()
                        .Where(x => x.CreateTime < cutoffDate)
                        .Where(x => x.ProcessState != null && completedStates.Contains(x.ProcessState.Value))
                        .Select(x => x.ID)
                        .Take(batchSize)
                        .ToListAsync(ct);

                    if (staleInboundIds.Count == 0)
                        break;

                    var deletedCount = await inboundDb.Deleteable<ShareDataInboundPacket>()
                        .Where(x => staleInboundIds.Contains(x.ID))
                        .ExecuteCommandAsync(ct);

                    totalDeletedInboundPackets += deletedCount;
                    if (deletedCount < batchSize)
                        break;
                }

                if (totalDeletedInboundPackets > 0)
                {
                    _logger.LogInformation(
                        "🧹 Đã dọn {Count} gói tin thô đã xử lý (ShareDataInboundPacket) trên CSDL nhận cũ hơn mốc lưu trữ.",
                        totalDeletedInboundPackets);
                }
            }
        }
        */
    }
}
