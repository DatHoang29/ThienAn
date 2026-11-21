using System.Text;
using System.Text.Json;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Models.Formats;

namespace Modules.Integration.Delivery.Formats
{
    /// <summary>
    /// Description: Định dạng CSV chuẩn RFC 4180 phục vụ xuất bản File/FTP.
    ///              Thuần BCL.
    /// Created date: 14/10/2026
    /// </summary>
    public class CsvFormat
    {
        public const string Crlf = "\r\n";

        public string Name => "Csv";

        /// <summary>
        /// Author: DatHQ
        /// Description: Đổi JSON sang CSV để truyền; khai CsvIncludeBom thì thêm BOM UTF-8.
        /// Created date: 14/10/2026
        /// </summary>
        public TransportPayload ToTransport(string json, FormatContext ctx)
        {
            var includeBom = false;
            if (ctx.Metadata != null && ctx.Metadata.TryGetValue(ConnectKeys.CsvIncludeBom, out var bomStr))
            {
                bool.TryParse(bomStr, out includeBom);
            }

            var csvText = ConvertJsonToCsv(json, includeBom);
            var bytes = includeBom
                ? Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvText)).ToArray()
                : Encoding.UTF8.GetBytes(csvText);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["content-type"] = "text/csv; charset=utf-8"
            };

            return new TransportPayload(bytes, "text/csv", ".csv", headers);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chiều nhận chưa hỗ trợ CSV nên luôn ném NotSupportedException.
        /// Created date: 14/10/2026
        /// </summary>
        public string FromTransport(byte[] bytes, FormatContext ctx)
        {
            throw new NotSupportedException("Chiều nhận hiện chỉ hỗ trợ định dạng JSON. Định dạng CSV chưa hỗ trợ ở chiều nhận.");
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đổi JSON sang CSV, xuống dòng CRLF: mảng object lấy hợp các khóa làm tiêu đề, object đơn thành một dòng, kiểu khác trả rỗng.
        /// Created date: 14/10/2026
        /// </summary>
        public static string ConvertJsonToCsv(string jsonPayload, bool includeBom = false)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
                return string.Empty;

            using var doc = JsonDocument.Parse(jsonPayload);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var elements = root.EnumerateArray().ToList();
                if (elements.Count == 0)
                    return string.Empty;

                var headers = new List<string>();
                foreach (var el in elements)
                {
                    if (el.ValueKind != JsonValueKind.Object) continue;
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (!headers.Contains(prop.Name))
                            headers.Add(prop.Name);
                    }
                }

                if (headers.Count == 0)
                    return string.Empty;

                var sb = new StringBuilder();
                if (includeBom)
                {
                    sb.Append('\uFEFF');
                }

                sb.Append(string.Join(",", headers.Select(EscapeCsvField))).Append(Crlf);

                foreach (var el in elements)
                {
                    if (el.ValueKind != JsonValueKind.Object) continue;
                    var rowValues = new List<string>(headers.Count);

                    foreach (var header in headers)
                    {
                        if (el.TryGetProperty(header, out var prop))
                        {
                            rowValues.Add(EscapeCsvField(FormatJsonElementValue(prop)));
                        }
                        else
                        {
                            rowValues.Add(string.Empty);
                        }
                    }

                    sb.Append(string.Join(",", rowValues)).Append(Crlf);
                }

                return sb.ToString();
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                var headers = root.EnumerateObject().Select(p => p.Name).ToList();
                var values = root.EnumerateObject().Select(p => EscapeCsvField(FormatJsonElementValue(p.Value))).ToList();

                var sb = new StringBuilder();
                if (includeBom)
                {
                    sb.Append('\uFEFF');
                }
                sb.Append(string.Join(",", headers.Select(EscapeCsvField))).Append(Crlf);
                sb.Append(string.Join(",", values)).Append(Crlf);
                return sb.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đổi giá trị JSON thành nội dung ô CSV: null thành rỗng, object và mảng giữ nguyên văn JSON.
        /// Created date: 14/10/2026
        /// </summary>
        private static string FormatJsonElementValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => element.GetRawText()
            };
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ô có dấu phẩy, nháy kép hoặc xuống dòng thì bọc nháy kép và nhân đôi nháy bên trong.
        /// Created date: 14/10/2026
        /// </summary>
        public static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            var mustQuote = field.Contains(',') || field.Contains('"') || field.Contains('\r') || field.Contains('\n');
            if (mustQuote)
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}
