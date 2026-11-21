using System.Collections.Concurrent;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Helpers;
using Modules.CfgSystem.Core.Entities;
using ShareDataWorker.Core.Configuration;

namespace ShareDataWorker.Infrastructure.Services.WorkerConfig
{
    /// <summary>
    /// Description: Dịch vụ cấu hình vận hành của ShareDataWorker.
    ///              Đọc SysOpConfig nhóm sharedata_config qua ISqlSugarClient/IServiceScopeFactory trực tiếp,
    ///              nạp đệm luồng an toàn SemaphoreSlim(1, 1) và nạp lại định kỳ.
    /// Author: DatHQ
    /// Created date: 18/10/2026
    /// Modified date: 09/11/2026
    /// </summary>
    public class DataWorkerConfigService
    {
        private static readonly Dictionary<string, ShareDataConst.ConfigSpec> _specMap =
            ShareDataConst.WorkerSpecs.ToDictionary(s => s.Code, s => s, StringComparer.OrdinalIgnoreCase);

        private static readonly (string Code, Func<DataWorkerConfig, int> Getter)[] _configAccessors =
        [
            (ShareDataConst.ConfigKeyOutboundCheckIntervalSec, c => c.OutboundCheckIntervalSec),
            (ShareDataConst.ConfigKeyInboundCheckIntervalSec, c => c.InboundCheckIntervalSec),
            (ShareDataConst.ConfigKeyTrackingPollIntervalSec, c => c.TrackingPollIntervalSec),
            (ShareDataConst.ConfigKeyOutboundLockMinSec, c => c.OutboundLockMinSec),
            (ShareDataConst.ConfigKeyOutboundLockIntervalMultiplier, c => c.OutboundLockIntervalMultiplier),
            (ShareDataConst.ConfigKeyOutboundDefaultIntervalSec, c => c.OutboundDefaultIntervalSec),
            (ShareDataConst.ConfigKeyInboundMaxPacketsPerRun, c => c.InboundMaxPacketsPerRun),
            (ShareDataConst.ConfigKeyInboundStuckTimeoutMin, c => c.InboundStuckTimeoutMin),
            (ShareDataConst.ConfigKeyInboundWarnIntervalMin, c => c.InboundWarnIntervalMin),
            (ShareDataConst.ConfigKeyInboundWriteTimeoutSec, c => c.InboundWriteTimeoutSec),
            (ShareDataConst.ConfigKeyTrackingTableRetryMin, c => c.TrackingTableRetryMin),
            (ShareDataConst.ConfigKeyTrackingLogRetentionDays, c => c.TrackingLogRetentionDays),
            (ShareDataConst.ConfigKeyTrackingLogPurgeBatch, c => c.TrackingLogPurgeBatch),
            (ShareDataConst.ConfigKeyTrackingChangeRetentionDays, c => c.TrackingChangeRetentionDays),
            (ShareDataConst.ConfigKeyCleanupIntervalMin, c => c.CleanupIntervalMin),
            (ShareDataConst.ConfigKeyReloadIntervalSec, c => c.ReloadIntervalSec)
        ];

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataWorkerConfigService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ConcurrentDictionary<string, byte> _warnedPairs = new(StringComparer.OrdinalIgnoreCase);

        private DataWorkerConfig _cachedConfig = new();
        private DateTime _lastReloadTime = DateTime.MinValue;

        public DataWorkerConfigService(
            IServiceScopeFactory scopeFactory,
            ILogger<DataWorkerConfigService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Trả cấu hình đang giữ trong bộ nhớ; quá ReloadIntervalSec thì đọc lại từ CSDL.
        ///              Khóa _lock bảo đảm mỗi lần chỉ một luồng đọc, giá trị nào đổi so với bản cũ thì ghi log.
        /// Created date: 18/10/2026
        /// </summary>
        public async Task<DataWorkerConfig> Get(CancellationToken ct = default)
        {
            var reloadSeconds = _cachedConfig.ReloadIntervalSec;
            var now = DateTime.UtcNow;

            if (now - _lastReloadTime < TimeSpan.FromSeconds(reloadSeconds))
            {
                return _cachedConfig;
            }

            await _lock.WaitAsync(ct);
            try
            {
                now = DateTime.UtcNow;
                if (now - _lastReloadTime < TimeSpan.FromSeconds(reloadSeconds))
                {
                    return _cachedConfig;
                }

                var newConfig = await LoadValuesFromDatabase(ct);
                if (_lastReloadTime != DateTime.MinValue)
                {
                    foreach (var (code, getter) in _configAccessors)
                    {
                        var oldVal = getter(_cachedConfig);
                        var newVal = getter(newConfig);
                        if (oldVal != newVal)
                        {
                            _logger.LogInformation(
                                "Cấu hình sharedata_config thay đổi: {Code} {Cu} → {Moi}",
                                code, oldVal, newVal);
                        }
                    }
                }
                _cachedConfig = newConfig;
                _lastReloadTime = now;
                return _cachedConfig;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chờ theo tham số cấu hình do vòng lặp nền truyền vào. Giá trị từ 0 trở xuống tính là 1 giây để vòng lặp không chạy liên tục.
        /// Created date: 18/10/2026
        /// </summary>
        public async Task Delay(Func<DataWorkerConfig, int> intervalSeconds, CancellationToken ct)
        {
            var config = await Get(ct);
            var sec = intervalSeconds(config);
            if (sec <= 0) sec = 1;
            await Task.Delay(TimeSpan.FromSeconds(sec), ct);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Mã nhóm cấu hình: lấy theo khóa ShareDataConst.ConfigGroupOverrideKey trong appsettings nếu có khai, không thì dùng nhóm mặc định sharedata_config.
        /// Created date: 18/10/2026
        /// </summary>
        private string ResolveGroupCode()
        {
            var customGroup = _configuration[ShareDataConst.ConfigGroupOverrideKey];
            return !string.IsNullOrWhiteSpace(customGroup)
                ? customGroup.Trim()
                : ShareDataConst.ConfigGroupCode;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc các dòng SysOpConfig đang bật của nhóm cấu hình rồi kiểm bằng ConfigValueParser.
        ///              Giá trị không phải số hoặc ngoài khoảng an toàn thì dùng giá trị dự phòng và cảnh báo một lần cho mỗi giá trị.
        ///              Lỗi đọc CSDL thì ghi cảnh báo và giữ nguyên cấu hình đang dùng.
        /// Created date: 18/10/2026
        /// </summary>
        private async Task<DataWorkerConfig> LoadValuesFromDatabase(CancellationToken ct)
        {
            var groupCode = ResolveGroupCode();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

                var rows = await db.Queryable<SysOpConfig>()
                    .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable && x.GroupCode == groupCode)
                    .ToListAsync(ct);

                var parseResult = ConfigValueParser.Parse(
                    rows.Select(r => new KeyValuePair<string, string?>(r.Code ?? string.Empty, r.Value)),
                    _specMap);

                foreach (var issue in parseResult.Issues)
                {
                    if (issue.Parsed == null)
                    {
                        var key = $"{issue.Code}:{issue.RawValue}";
                        if (_warnedPairs.TryAdd(key, 0))
                        {
                            _logger.LogWarning(
                                "Cấu hình SysOpConfig [{GroupCode}.{Code}] có giá trị '{RawValue}' không phải số nguyên; dùng giá trị dự phòng {Fallback}.",
                                groupCode, issue.Code, issue.RawValue, issue.Spec.Default);
                        }
                    }
                    else
                    {
                        var key = $"{issue.Code}:{issue.Parsed}";
                        if (_warnedPairs.TryAdd(key, 0))
                        {
                            _logger.LogWarning(
                                "Cấu hình SysOpConfig [{GroupCode}.{Code}] = {Parsed} ngoài khoảng an toàn [{Min}..{Max}]; dùng giá trị dự phòng {Fallback}.",
                                groupCode, issue.Code, issue.Parsed, issue.Spec.Min, issue.Spec.Max, issue.Spec.Default);
                        }
                    }
                }

                return new DataWorkerConfig
                {
                    OutboundCheckIntervalSec = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyOutboundCheckIntervalSec, ShareDataConst.DefaultOutboundCheckIntervalSec),
                    InboundCheckIntervalSec = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyInboundCheckIntervalSec, ShareDataConst.DefaultInboundCheckIntervalSec),
                    TrackingPollIntervalSec = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyTrackingPollIntervalSec, ShareDataConst.DefaultTrackingPollIntervalSec),
                    OutboundLockMinSec = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyOutboundLockMinSec, ShareDataConst.DefaultOutboundLockMinSec),
                    OutboundLockIntervalMultiplier = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyOutboundLockIntervalMultiplier, ShareDataConst.DefaultOutboundLockIntervalMultiplier),
                    OutboundDefaultIntervalSec = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyOutboundDefaultIntervalSec, ShareDataConst.DefaultOutboundDefaultIntervalSec),
                    InboundMaxPacketsPerRun = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyInboundMaxPacketsPerRun, ShareDataConst.DefaultInboundMaxPacketsPerRun),
                    InboundStuckTimeoutMin = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyInboundStuckTimeoutMin, ShareDataConst.DefaultInboundStuckTimeoutMin),
                    InboundWarnIntervalMin = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyInboundWarnIntervalMin, ShareDataConst.DefaultInboundWarnIntervalMin),
                    InboundWriteTimeoutSec = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyInboundWriteTimeoutSec, ShareDataConst.DefaultInboundWriteTimeoutSec),
                    TrackingTableRetryMin = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyTrackingTableRetryMin, ShareDataConst.DefaultTrackingTableRetryMin),
                    TrackingLogRetentionDays = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyTrackingLogRetentionDays, ShareDataConst.DefaultTrackingLogRetentionDays),
                    TrackingLogPurgeBatch = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyTrackingLogPurgeBatch, ShareDataConst.DefaultTrackingLogPurgeBatch),
                    TrackingChangeRetentionDays = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyTrackingChangeRetentionDays, ShareDataConst.DefaultTrackingChangeRetentionDays),
                    CleanupIntervalMin = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyCleanupIntervalMin, ShareDataConst.DefaultCleanupIntervalMin),
                    ReloadIntervalSec = parseResult.Values.GetValueOrDefault(ShareDataConst.ConfigKeyReloadIntervalSec, ShareDataConst.DefaultReloadIntervalSec)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi đọc SysOpConfig nhóm '{GroupCode}'; giữ giá trị đệm hiện tại.", groupCode);
                return _cachedConfig;
            }
        }
    }
}
