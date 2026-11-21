using System.Text.Json;

namespace Modules.Integration.Delivery.Base.Helpers
{
    /// <summary>
    /// Description: Bộ đọc cấu hình kết nối chuẩn hoá hai chiều, không phân biệt hoa thường.
    ///              Thuần BCL, dùng an toàn ở cả WebAPI và Worker mà không phụ thuộc hạ tầng ngoài.
    /// Created date: 14/10/2026
    /// </summary>
    public sealed class ConnectConfigReader
    {
        private readonly Dictionary<string, string> _dict;

        /// <summary>
        /// Author: DatHQ
        /// Description: Chép cấu hình sang dictionary không phân biệt hoa thường, bỏ qua khóa trống.
        /// Created date: 14/10/2026
        /// </summary>
        public ConnectConfigReader(IDictionary<string, string>? dictionary)
        {
            _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (dictionary != null)
            {
                foreach (var (k, v) in dictionary)
                {
                    if (!string.IsNullOrWhiteSpace(k))
                    {
                        _dict[k.Trim()] = v ?? string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Tạo bộ đọc từ chuỗi JSON (ví dụ ConnectionConfig).
        /// </summary>
        public static ConnectConfigReader FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ConnectConfigReader(null);
            }

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new ConnectConfigReader(null);
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var valStr = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => prop.Value.GetRawText()
                };
                dict[prop.Name.Trim()] = valStr;
            }

            return new ConnectConfigReader(dict);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc khóa chính rồi đến các tên thay thế, lấy giá trị đầu tiên có nội dung và đã cắt khoảng trắng.
        /// Created date: 14/10/2026
        /// </summary>
        public string? GetString(string key, params string[] aliases)
        {
            if (_dict.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
            {
                return val.Trim();
            }

            foreach (var alias in aliases)
            {
                if (_dict.TryGetValue(alias, out var aVal) && !string.IsNullOrWhiteSpace(aVal))
                {
                    return aVal.Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Lấy giá trị chuỗi nếu key/alias có trong cấu hình (kể cả rỗng/khoảng trắng).
        /// Nếu không có key nào tồn tại thì trả về defaultValue.
        /// </summary>
        public string? GetRawString(string key, string? defaultValue = null, bool trim = false, params string[] aliases)
        {
            if (_dict.TryGetValue(key, out var val))
            {
                return trim ? val.Trim() : val;
            }

            foreach (var alias in aliases)
            {
                if (_dict.TryGetValue(alias, out var aVal))
                {
                    return trim ? aVal.Trim() : aVal;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc giá trị bool; không có hoặc sai định dạng thì trả defaultValue.
        /// Created date: 14/10/2026
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false, params string[] aliases)
        {
            var str = GetString(key, aliases);
            if (str == null)
            {
                return defaultValue;
            }

            return bool.TryParse(str, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc giá trị số nguyên; không có hoặc sai định dạng thì trả defaultValue.
        /// Created date: 14/10/2026
        /// </summary>
        public int GetInt(string key, int defaultValue = 0, params string[] aliases)
        {
            var str = GetString(key, aliases);
            if (str == null)
            {
                return defaultValue;
            }

            return int.TryParse(str, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc giá trị enum, không phân biệt hoa thường; không có hoặc sai định dạng thì trả defaultValue.
        /// Created date: 14/10/2026
        /// </summary>
        public T GetEnum<T>(string key, T defaultValue = default, params string[] aliases) where T : struct, Enum
        {
            var str = GetString(key, aliases);
            if (str == null)
            {
                return defaultValue;
            }

            return Enum.TryParse<T>(str, true, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Parse enum dạng tùy chọn (nullable): nếu vắng hoặc parse không được thì trả về null.
        /// </summary>
        public T? GetOptionalEnum<T>(string key, params string[] aliases) where T : struct, Enum
        {
            var str = GetString(key, aliases);
            if (str != null && Enum.TryParse<T>(str, true, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cấu hình có khai khóa này không, kể cả khi giá trị trống.
        /// Created date: 14/10/2026
        /// </summary>
        public bool ContainsKey(string key) => _dict.ContainsKey(key);

        /// <summary>
        /// Author: DatHQ
        /// Description: Trả toàn bộ cấu hình dạng chỉ đọc.
        /// Created date: 14/10/2026
        /// </summary>
        public IReadOnlyDictionary<string, string> AsReadOnly() => _dict;
    }
}
