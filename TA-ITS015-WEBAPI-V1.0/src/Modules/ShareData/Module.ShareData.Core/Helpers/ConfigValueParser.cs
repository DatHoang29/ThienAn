using System.Globalization;
using Module.ShareData.Core.Constants;

namespace Module.ShareData.Core.Helpers
{
    /// <summary>
    /// Bản ghi kết quả phân tích giá trị cấu hình theo ConfigSpec.
    /// </summary>
    public sealed record ConfigParseResult(
        IReadOnlyDictionary<string, int> Values,
        IReadOnlyList<ConfigIssue> Issues);

    /// <summary>
    /// Bản ghi mô tả sự cố phân tích cấu hình (không phải số nguyên hoặc ngoài khoảng an toàn).
    /// </summary>
    public sealed record ConfigIssue(
        string Code,
        string? RawValue,
        int? Parsed,
        ShareDataConst.ConfigSpec Spec);

    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ phân tích và kiểm tra giá trị cấu hình SysOpConfig theo ConfigSpec.
    ///              Lớp static thuần: không log, không I/O.
    /// Created date: 18/10/2026
    /// </summary>
    public static class ConfigValueParser
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc giá trị cấu hình theo bảng khai báo: bỏ qua khóa lạ; không phải số nguyên hoặc ngoài khoảng an toàn thì đưa vào Issues.
        /// Created date: 19/10/2026
        /// </summary>
        public static ConfigParseResult Parse(
            IEnumerable<KeyValuePair<string, string?>> rows,
            IReadOnlyDictionary<string, ShareDataConst.ConfigSpec> specMap)
        {
            var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var issues = new List<ConfigIssue>();

            if (rows == null || specMap == null)
            {
                return new ConfigParseResult(values, issues);
            }

            foreach (var kvp in rows)
            {
                var code = kvp.Key?.Trim();
                if (string.IsNullOrEmpty(code) || !specMap.TryGetValue(code, out var spec))
                {
                    continue;
                }

                var rawValue = kvp.Value?.Trim();
                if (!int.TryParse(
                        rawValue,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var parsed))
                {
                    values.Remove(code);
                    issues.Add(new ConfigIssue(code, rawValue, null, spec));
                    continue;
                }

                if (parsed < spec.Min || parsed > spec.Max)
                {
                    values.Remove(code);
                    issues.Add(new ConfigIssue(code, rawValue, parsed, spec));
                    continue;
                }

                values[code] = parsed;
            }

            return new ConfigParseResult(values, issues);
        }
    }
}
