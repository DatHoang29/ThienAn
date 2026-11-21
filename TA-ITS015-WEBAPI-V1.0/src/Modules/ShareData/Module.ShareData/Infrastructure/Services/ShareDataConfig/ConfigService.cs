using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Helpers;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Wolverine;

namespace Module.ShareData.Infrastructure.Services.ShareDataConfig
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Dịch vụ cấu hình vận hành của phân hệ ShareData (WebAPI).
    ///              Đọc SysOpConfig nhóm sharedata_config qua IMessageBus theo nếp ConfigService,
    ///              kèm giá trị dự phòng và khoảng an toàn tuyệt đối khai tại ShareDataConst.
    /// Created date: 18/10/2026
    /// Modified date: 20/10/2026 (Đổi tên thành ConfigService theo Rule 19.73 tránh lặp tiền tố thư mục)
    /// </summary>
    public class ConfigService : IScoped
    {
        private static readonly Dictionary<string, ShareDataConst.ConfigSpec> _specMap =
            ShareDataConst.WebApiSpecs.ToDictionary(s => s.Code, s => s, StringComparer.OrdinalIgnoreCase);

        public static readonly Values Defaults = CreateDefaults();

        private readonly IMessageBus _bus;
        private readonly ILogger<ConfigService> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Bản ghi bất biến chứa bộ tham số giới hạn vận hành của WebAPI ShareData.
        /// </summary>
        public sealed record Values(
            (int Min, int Max, int Default) SqlSampleTop,
            (int Min, int Max, int Default) MappingSampleTop,
            int LogListDefaultTop,
            (int Min, int Max) SubscriptionInterval,
            (int Min, int Max) SubscriptionPriority,
            (int Min, int Max) SubscriptionDebounceSec);

        public ConfigService(
            IMessageBus bus,
            ILogger<ConfigService> logger,
            IConfiguration configuration)
        {
            _bus = bus;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Lấy toàn bộ cấu hình nhóm sharedata_config dạng Dictionary qua bus (một lần gọi bus cho cả nhóm).
        /// </summary>
        public async Task<Dictionary<string, string>> GetGroup(CancellationToken ct = default)
        {
            var groupCode = ResolveGroupCode();
            try
            {
                var result = await _bus.InvokeAsync<List<SysOpConfigOutput>>(
                    new SysOpConfigInput { GroupCode = groupCode },
                    cancellation: ct);

                return result?
                    .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                    .GroupBy(x => x.Code!)
                    .ToDictionary(g => g.Key, g => g.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Không đọc được cấu hình SysOpConfig nhóm '{GroupCode}' qua bus; sử dụng bộ giá trị dự phòng.",
                    groupCode);
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Đọc các tham số giới hạn đã kiểm tra hợp lệ từ bảng SysOpConfig qua bus kèm cơ chế fallback an toàn.
        /// </summary>
        public async Task<Values> Get(CancellationToken ct = default)
        {
            var rawGroup = await GetGroup(ct);
            var values = ValidateRawValues(rawGroup);

            var sqlTop = ResolveTriplet(values, ShareDataConst.ConfigKeySqlSampleTopMin, ShareDataConst.ConfigKeySqlSampleTopMax, ShareDataConst.ConfigKeySqlSampleTopDefault);
            var mappingTop = ResolveTriplet(values, ShareDataConst.ConfigKeyMappingSampleTopMin, ShareDataConst.ConfigKeyMappingSampleTopMax, ShareDataConst.ConfigKeyMappingSampleTopDefault);
            var logTop = values.TryGetValue(ShareDataConst.ConfigKeyLogListDefaultTop, out var configuredLogTop)
                ? configuredLogTop
                : Defaults.LogListDefaultTop;
            var subInterval = ResolvePair(values, ShareDataConst.ConfigKeySubscriptionIntervalSecMin, ShareDataConst.ConfigKeySubscriptionIntervalSecMax);
            var subPriority = ResolvePair(values, ShareDataConst.ConfigKeySubscriptionPriorityMin, ShareDataConst.ConfigKeySubscriptionPriorityMax);
            var subDebounce = ResolvePair(values, ShareDataConst.ConfigKeySubscriptionDebounceSecMin, ShareDataConst.ConfigKeySubscriptionDebounceSecMax);

            return new Values(
                SqlSampleTop: sqlTop,
                MappingSampleTop: mappingTop,
                LogListDefaultTop: logTop,
                SubscriptionInterval: subInterval,
                SubscriptionPriority: subPriority,
                SubscriptionDebounceSec: subDebounce
            );
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Mã nhóm cấu hình: lấy theo khóa ConfigGroupOverrideKey nếu có khai, không thì dùng sharedata_config.
        /// Created date: 19/10/2026
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
        /// Description: Đổi giá trị cấu hình thô sang số bằng ConfigValueParser, giá trị không hợp lệ thì ghi cảnh báo.
        /// Created date: 19/10/2026
        /// </summary>
        private Dictionary<string, int> ValidateRawValues(Dictionary<string, string> rawGroup)
        {
            var groupCode = ResolveGroupCode();
            var parseResult = ConfigValueParser.Parse(
                rawGroup.Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value)),
                _specMap);

            foreach (var issue in parseResult.Issues)
            {
                if (issue.Parsed == null)
                {
                    _logger.LogWarning(
                        "Cấu hình SysOpConfig [{GroupCode}.{Code}] có giá trị '{RawValue}' không phải số nguyên hợp lệ; dùng giá trị dự phòng {Fallback}.",
                        groupCode,
                        issue.Code,
                        issue.RawValue,
                        issue.Spec.Default);
                }
                else
                {
                    _logger.LogWarning(
                        "Cấu hình SysOpConfig [{GroupCode}.{Code}] = {Parsed} nằm ngoài khoảng an toàn tuyệt đối [{SafeMin}..{SafeMax}]; dùng giá trị dự phòng {Fallback}.",
                        groupCode,
                        issue.Code,
                        issue.Parsed,
                        issue.Spec.Min,
                        issue.Spec.Max,
                        issue.Spec.Default);
                }
            }

            return (Dictionary<string, int>)parseResult.Values;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lấy cặp nhỏ nhất, lớn nhất; lớn nhất nhỏ hơn nhỏ nhất thì ghi cảnh báo và quay về nguyên bộ dự phòng.
        /// Created date: 19/10/2026
        /// </summary>
        private (int Min, int Max) ResolvePair(
            Dictionary<string, int> values,
            string minCode,
            string maxCode)
        {
            var fallbackMin = _specMap[minCode].Default;
            var fallbackMax = _specMap[maxCode].Default;

            var min = values.GetValueOrDefault(minCode, fallbackMin);
            var max = values.GetValueOrDefault(maxCode, fallbackMax);

            if (max < min)
            {
                _logger.LogWarning(
                    "Bộ cấu hình [{MinCode}={Min}, {MaxCode}={Max}] không hợp lệ (max < min); quay về nguyên bộ dự phòng ({FallbackMin}..{FallbackMax}).",
                    minCode,
                    min,
                    maxCode,
                    max,
                    fallbackMin,
                    fallbackMax);
                return (fallbackMin, fallbackMax);
            }

            return (min, max);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lấy bộ nhỏ nhất, lớn nhất, mặc định; ba giá trị lệch nhau thì ghi cảnh báo và quay về nguyên bộ dự phòng.
        /// Created date: 19/10/2026
        /// </summary>
        private (int Min, int Max, int Default) ResolveTriplet(
            Dictionary<string, int> values,
            string minCode,
            string maxCode,
            string defaultCode)
        {
            var fallbackMin = _specMap[minCode].Default;
            var fallbackMax = _specMap[maxCode].Default;
            var fallbackDefault = _specMap[defaultCode].Default;

            var min = values.GetValueOrDefault(minCode, fallbackMin);
            var max = values.GetValueOrDefault(maxCode, fallbackMax);
            var def = values.GetValueOrDefault(defaultCode, fallbackDefault);

            if (max < min || def < min || def > max)
            {
                _logger.LogWarning(
                    "Bộ cấu hình [{MinCode}={Min}, {MaxCode}={Max}, {DefaultCode}={Default}] lệch nhau; quay về nguyên bộ dự phòng ({FallbackMin}, {FallbackMax}, {FallbackDefault}).",
                    minCode,
                    min,
                    maxCode,
                    max,
                    defaultCode,
                    def,
                    fallbackMin,
                    fallbackMax,
                    fallbackDefault);
                return (fallbackMin, fallbackMax, fallbackDefault);
            }

            return (min, max, def);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dựng bộ giá trị mặc định từ khai báo ShareDataConst.WebApiSpecs.
        /// Created date: 19/10/2026
        /// </summary>
        private static Values CreateDefaults()
        {
            var map = ShareDataConst.WebApiSpecs.ToDictionary(s => s.Code, s => s.Default, StringComparer.OrdinalIgnoreCase);
            return new Values(
                SqlSampleTop: (map[ShareDataConst.ConfigKeySqlSampleTopMin], map[ShareDataConst.ConfigKeySqlSampleTopMax], map[ShareDataConst.ConfigKeySqlSampleTopDefault]),
                MappingSampleTop: (map[ShareDataConst.ConfigKeyMappingSampleTopMin], map[ShareDataConst.ConfigKeyMappingSampleTopMax], map[ShareDataConst.ConfigKeyMappingSampleTopDefault]),
                LogListDefaultTop: map[ShareDataConst.ConfigKeyLogListDefaultTop],
                SubscriptionInterval: (map[ShareDataConst.ConfigKeySubscriptionIntervalSecMin], map[ShareDataConst.ConfigKeySubscriptionIntervalSecMax]),
                SubscriptionPriority: (map[ShareDataConst.ConfigKeySubscriptionPriorityMin], map[ShareDataConst.ConfigKeySubscriptionPriorityMax]),
                SubscriptionDebounceSec: (map[ShareDataConst.ConfigKeySubscriptionDebounceSecMin], map[ShareDataConst.ConfigKeySubscriptionDebounceSecMax])
            );
        }
    }
}
