using System.Globalization;
using System.Text.Json;
using ShareDataWorker.Core.Dto;

namespace ShareDataWorker.Infrastructure.Services.DataInbound
{
    /// <summary>
    /// Bóc gói tin PDU và gỡ bộ khung phễu lọc để lấy lại các cặp fieldKey → giá trị
    /// Author: HieuNV
    /// Created date: 07/09/2026
    /// </summary>
    public partial class DataInboundService
    {
        private const int MaxShapeDepth = 64;

        /// <summary>
        /// Author: HieuNV
        /// Description: Bóc envelope PDU và trả về danh sách bản ghi dạng fieldKey → giá trị.
        /// Created date: 07/09/2026
        /// </summary>
        private static List<Dictionary<string, object?>> ParsePayloadRecords(
            string content,
            string? inboundShapeJson,
            IReadOnlyDictionary<string, string>? fieldCodeSets,
            IReadOnlyDictionary<string, CodeSetDto>? codeSets,
            out string? parseError)
        {
            parseError = null;
            var records = new List<Dictionary<string, object?>>();

            var trimmed = content.TrimStart();
            if (trimmed.StartsWith('<'))
            {
                parseError = "Gói tin ở định dạng XML — luồng nhận hiện chỉ hỗ trợ JSON.";
                return records;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(content);
            }
            catch (JsonException ex)
            {
                parseError = $"Nội dung không phải JSON hợp lệ: {ex.Message}";
                return records;
            }

            using (doc)
            {
                var root = doc.RootElement;

                // Không có phễu lọc chiều nhận: không có gì tả cấu trúc gói tin, chỉ còn cách đoán
                // theo khoá quy ước. Đây là đường lùi, không phải đường chạy chính.
                if (string.IsNullOrWhiteSpace(inboundShapeJson))
                {
                    if (!TryFindFallbackArray(root, out var flatArray))
                    {
                        parseError = "Chưa khai phễu lọc chiều nhận (TargetShapeJson) và gói tin cũng không có "
                            + "mảng bản ghi ở khoá quy ước (payload/data) — không biết đọc dữ liệu ở đâu.";
                        return records;
                    }

                    foreach (var element in flatArray.EnumerateArray())
                    {
                        if (element.ValueKind != JsonValueKind.Object)
                            continue;

                        var flat = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        foreach (var prop in element.EnumerateObject())
                            flat[prop.Name] = JsonToObject(prop.Value);

                        ApplyReverseCodeSets(flat, null, fieldCodeSets, codeSets);
                        records.Add(flat);
                    }

                    return records;
                }

                // Có phễu lọc: BỘ KHUNG quyết định mảng bản ghi nằm ở nhánh nào (EachPath) và mỗi fieldKey
                // đọc ở đâu trong một bản ghi. Khoá trong gói tin đối tác KHÔNG cố định nên ở đây tuyệt đối
                // không giả định tên khoá nào — kể cả "payload".
                ShapeBinding binding;
                try
                {
                    binding = BuildShapeBinding(inboundShapeJson!);
                }
                catch (JsonException ex)
                {
                    parseError = $"TargetShapeJson của phễu lọc chiều nhận không hợp lệ: {ex.Message}";
                    return records;
                }

                if (binding.FieldPaths.Count == 0)
                {
                    parseError = "Phễu lọc chiều nhận không khai trường nào ($field), không biết đọc dữ liệu ở đâu.";
                    return records;
                }

                // Bộ khung không có khối lặp: cả gói tin là MỘT bản ghi.
                var eachPath = binding.EachPath;
                if (eachPath == null)
                {
                    records.Add(ReadRecord(root, binding, fieldCodeSets, codeSets));
                    return records;
                }

                var eachPathText = eachPath.Length == 0 ? "(gốc)" : string.Join(".", eachPath);

                if (!TryNavigate(root, eachPath, out var arrayNode))
                {
                    parseError = $"Gói tin không có nhánh [{eachPathText}] — phễu lọc khai đây là nơi chứa mảng bản ghi.";
                    return records;
                }

                if (arrayNode.ValueKind != JsonValueKind.Array)
                {
                    parseError = $"Nhánh [{eachPathText}] trong gói tin phải là một mảng bản ghi.";
                    return records;
                }

                foreach (var element in arrayNode.EnumerateArray())
                    records.Add(ReadRecord(element, binding, fieldCodeSets, codeSets));
            }

            return records;
        }

        /// <summary>Đọc một khoá chuỗi trong $extend; khoá không phải chuỗi thì lấy nguyên văn JSON.</summary>
        private static string? ReadExtendString(JsonElement extendNode, string key)
        {
            if (!extendNode.TryGetProperty(key, out var node))
                return null;

            var text = node.ValueKind == JsonValueKind.String ? node.GetString() : node.GetRawText();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Gỡ ngược ép kiểu / định dạng của chiều gửi (§3.4) — đảo của
        ///              DataMappingProcess.ConvertDataType. Đưa giá trị đối tác gửi về dạng chuẩn để
        ///              OPENJSON trong ShareDataSql.WriteSql ép kiểu được chắc chắn, không phải đoán
        ///              theo ngôn ngữ của máy chủ SQL.
        ///              Gỡ không được thì GIỮ NGUYÊN giá trị gốc — thà để câu ghi báo lỗi còn hơn
        ///              âm thầm ghi sai dữ liệu.
        /// Created date: 20/09/2026
        /// </summary>
        private static object? ConvertInboundValue(object? value, string? targetType, string? dateFormat, string? numberFormat)
        {
            if (value == null)
                return null;

            var type = targetType?.Trim().ToLowerInvariant();

            try
            {
                // Không khai targetType: chỉ gỡ khi bộ khung có khai khuôn định dạng.
                if (string.IsNullOrWhiteSpace(type))
                {
                    if (!string.IsNullOrWhiteSpace(dateFormat))
                        return ParseDateTimeBack(value, dateFormat) ?? value;

                    if (!string.IsNullOrWhiteSpace(numberFormat))
                        return ParseDecimalBack(value) ?? value;

                    return value;
                }

                switch (type)
                {
                    case "datetime":
                    case "date":
                    case "time":
                        return ParseDateTimeBack(value, dateFormat) ?? value;

                    case "int":
                    case "integer":
                        return ParseDecimalBack(value) is { } di ? Convert.ToInt32(di) : value;

                    case "long":
                        return ParseDecimalBack(value) is { } dl ? Convert.ToInt64(dl) : value;

                    case "double":
                        return ParseDecimalBack(value) is { } dd ? Convert.ToDouble(dd) : value;

                    case "float":
                        return ParseDecimalBack(value) is { } df ? (float)df : value;

                    case "decimal":
                    case "number":
                        return ParseDecimalBack(value) ?? (object?)value;

                    case "bool":
                    case "boolean":
                        if (value is bool)
                            return value;
                        return bool.TryParse(value.ToString()?.Trim(), out var b)
                            ? b
                            : ParseDecimalBack(value) is { } dbv ? dbv != 0 : value;

                    case "string":
                        return value as string ?? value.ToString();

                    default:
                        return value;
                }
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// Description: Chuỗi ngày đối tác gửi → chuỗi ISO-8601. Ưu tiên đọc ĐÚNG khuôn dateFormat của
        ///              bộ khung (khuôn kiểu dd/MM/yyyy mà đọc tự do là hiểu sai ngày thành tháng).
        /// Created date: 20/09/2026
        /// </summary>
        private static string? ParseDateTimeBack(object? value, string? dateFormat)
        {
            if (value is DateTime already)
                return already.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);

            var raw = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(raw))
                return null;

            if (!string.IsNullOrWhiteSpace(dateFormat)
                && DateTime.TryParseExact(raw, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            {
                return exact.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            }

            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose)
                ? loose.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)
                : null;
        }

        /// <summary>
        /// Description: Chuỗi số đối tác gửi → decimal. NumberStyles.Any để gỡ được dấu phân cách nghìn
        ///              mà numberFormat của chiều gửi sinh ra.
        /// Created date: 20/09/2026
        /// </summary>
        private static decimal? ParseDecimalBack(object? value)
        {
            switch (value)
            {
                case null: return null;
                case decimal dec: return dec;
                case double dbl: return (decimal)dbl;
                case float f: return (decimal)f;
                case int i: return i;
                case long l: return l;
            }

            var raw = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(raw))
                return null;

            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đường lùi khi KHÔNG có phễu lọc — dò mảng bản ghi ở gốc hoặc ở các khoá quy ước.
        ///              Chỉ dùng cho hồ sơ chưa khai bộ khung; có bộ khung thì luôn đi theo EachPath.
        /// Created date: 20/09/2026
        /// </summary>
        private static bool TryFindFallbackArray(JsonElement root, out JsonElement array)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                array = root;
                return true;
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    if (string.Equals(prop.Name, "payload", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                    {
                        array = prop.Value;
                        return true;
                    }
                }
            }

            array = default;
            return false;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đọc một bản ghi theo bản đồ vị trí của bộ khung: lấy giá trị từng trường, gỡ ép kiểu rồi mới tra ngược bộ mã.
        ///              Thứ tự này đảo đúng chiều gửi, đổi lại sẽ làm mã "05" thành 5 khi tra. Trường không tìm thấy trong gói thì nhận null.
        /// Created date: 07/09/2026
        /// </summary>
        private static Dictionary<string, object?> ReadRecord(
            JsonElement element,
            ShapeBinding binding,
            IReadOnlyDictionary<string, string>? fieldCodeSets,
            IReadOnlyDictionary<string, CodeSetDto>? codeSets)
        {
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var (fieldKey, path) in binding.FieldPaths)
            {
                var raw = TryNavigate(element, path, out var value)
                    ? JsonToObject(value)
                    : null;

                // Gỡ ép kiểu / định dạng của đối tác TRƯỚC khi tra ngược bộ mã — đúng thứ tự đảo của
                // chiều gửi (§4.1: bộ mã → mặc định → ép kiểu + định dạng). Lá có bộ mã không có khuôn
                // trong Formats (xem Walk) nên giữ nguyên chuỗi đối tác gửi để tra ngược.
                if (binding.Formats.TryGetValue(fieldKey, out var format))
                    raw = ConvertInboundValue(raw, format.TargetType, format.DateFormat, format.NumberFormat);

                record[fieldKey] = raw;
            }

            ApplyReverseCodeSets(record, binding, fieldCodeSets, codeSets);
            return record;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đổi giá trị chuẩn nhận về thành giá trị nguồn của hệ thống
        /// Created date: 07/09/2026
        /// </summary>
        private static void ApplyReverseCodeSets(
            Dictionary<string, object?> record,
            ShapeBinding? binding,
            IReadOnlyDictionary<string, string>? fieldCodeSets,
            IReadOnlyDictionary<string, CodeSetDto>? codeSets)
        {
            foreach (var fieldKey in record.Keys.ToList())
            {
                // Bộ mã khai ở bộ khung ($extend.codeSet) trước, rồi mới tới bộ mã khai theo trường.
                string? codeSetCode = null;
                if (binding != null && binding.CodeSets.TryGetValue(fieldKey, out var shapeCode))
                    codeSetCode = shapeCode;
                else if (fieldCodeSets != null && fieldCodeSets.TryGetValue(fieldKey, out var fieldCode))
                    codeSetCode = fieldCode;

                // Lá CÓ bộ mã: tra ngược, và mặc định lấy từ chính bộ mã đó.
                if (codeSetCode != null
                    && codeSets != null
                    && codeSets.TryGetValue(codeSetCode, out var codeSet))
                {
                    record[fieldKey] = MapCodeReverse(codeSet, record[fieldKey]);
                    continue;
                }

                // Lá KHÔNG có bộ mã: $extend.defaultSourceValue chỉ để lấp chỗ trống.
                if (binding != null
                    && IsEmptyValue(record[fieldKey])
                    && binding.DefaultSourceValues.TryGetValue(fieldKey, out var leafDefault))
                {
                    record[fieldKey] = leafDefault;
                }
            }
        }

        /// <summary>Giá trị coi như trống: null hoặc chuỗi rỗng/toàn khoảng trắng.</summary>
        private static bool IsEmptyValue(object? value)
            => value == null || (value is string s && string.IsNullOrWhiteSpace(s));

        /// <summary>
        /// Author: HieuNV
        /// Description: Tra ngược bộ mã — partnerValue → sourceValue. Chiều ngược của MapCode.
        /// Created date: 07/09/2026
        /// </summary>
        public static object? MapCodeReverse(CodeSetDto? codeSet, object? value)
        {
            if (codeSet == null)
                return value;

            var fallback = string.IsNullOrWhiteSpace(codeSet.DefaultSourceValue)
                ? null
                : codeSet.DefaultSourceValue;

            var valText = value?.ToString()?.Trim();

            // D3 đảo — đối tác bỏ trống: lấy mặc định của bộ mã, không có thì lấy dòng đánh dấu isDefault.
            if (string.IsNullOrEmpty(valText))
            {
                if (fallback != null)
                    return fallback;

                var defEntry = codeSet.Values.FirstOrDefault(c => c.IsDefault == true);
                return string.IsNullOrWhiteSpace(defEntry?.SourceValue) ? value : defEntry!.SourceValue;
            }

            var match = codeSet.Values.FirstOrDefault(c =>
                string.Equals(c.PartnerValue?.Trim(), valText, StringComparison.OrdinalIgnoreCase));

            if (match != null)
                return match.SourceValue;

            // D4 đảo — mã lạ chưa khai trong bộ mã: đổi sang mặc định nội bộ.
            // Bộ mã không khai mặc định thì giữ nguyên giá trị đối tác để còn lần ra dữ liệu bẩn.
            return fallback ?? value;
        }

        /// <summary>Bản đồ vị trí đọc dữ liệu, dựng một lần từ TargetShapeJson chiều nhận.</summary>
        private sealed class ShapeBinding
        {
            /// <summary>Đường dẫn tới nút mảng $each, null nếu bộ khung không dùng $each.</summary>
            public string[]? EachPath { get; set; }

            /// <summary>fieldKey → đường dẫn TƯƠNG ĐỐI tính từ mỗi bản ghi.</summary>
            public Dictionary<string, string[]> FieldPaths { get; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>fieldKey → mã bộ mã khai tại $extend.codeSet, để tra ngược khi đọc.</summary>
            public Dictionary<string, string> CodeSets { get; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// fieldKey → $extend.defaultSourceValue khai tại lá. CHỈ dùng cho lá không khai codeSet;
            /// lá có codeSet thì mặc định lấy từ defaultSourceValue của chính bộ mã đó.
            /// </summary>
            public Dictionary<string, string> DefaultSourceValues { get; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>fieldKey → khai báo ép kiểu / định dạng ở $extend, để gỡ ngược lúc đọc.</summary>
            public Dictionary<string, ShapeFormat> Formats { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Khai báo ép kiểu và định dạng của một lá trong bộ khung ($extend).</summary>
        private sealed record ShapeFormat(string? TargetType, string? DateFormat, string? NumberFormat)
        {
            public bool HasAny => !string.IsNullOrWhiteSpace(TargetType)
                || !string.IsNullOrWhiteSpace(DateFormat)
                || !string.IsNullOrWhiteSpace(NumberFormat);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Dựng bản đồ vị trí đọc dữ liệu từ TargetShapeJson của chiều nhận. JSON hỏng thì ném JsonException, nơi gọi đổi thành lỗi đọc gói.
        /// Created date: 08/09/2026
        /// </summary>
        private static ShapeBinding BuildShapeBinding(string shapeJson)
        {
            var binding = new ShapeBinding();
            using var doc = JsonDocument.Parse(shapeJson);
            Walk(doc.RootElement, new List<string>(), binding, 0);
            return binding;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Duyệt đệ quy bộ khung, gom vào binding nhánh mảng bản ghi và đường dẫn, bộ mã, mặc định, định dạng của từng trường.
        ///              Gặp $exclude thì bỏ cả nhánh. Gặp $each hoặc mảng khuôn thì đường dẫn tính lại từ bản ghi.
        ///              Một trường ở nhiều nơi thì lấy vị trí đầu tiên. Sâu quá MaxShapeDepth thì dừng.
        /// Created date: 08/09/2026
        /// </summary>
        private static void Walk(JsonElement node, List<string> path, ShapeBinding binding, int depth)
        {
            if (depth > MaxShapeDepth)
                return;

            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    if (node.TryGetProperty("$exclude", out var excl) && excl.ValueKind == JsonValueKind.True)
                        return;

                    if (node.TryGetProperty("$each", out var each)
                        && each.ValueKind == JsonValueKind.True
                        && node.TryGetProperty("$as", out var asNode))
                    {
                        // Từ đây trở xuống là cấu trúc của MỘT bản ghi, nên đường dẫn tính lại từ đầu.
                        binding.EachPath = path.ToArray();
                        Walk(asNode, new List<string>(), binding, depth + 1);
                        return;
                    }

                    if (node.TryGetProperty("$field", out var fieldNode))
                    {
                        var fieldKey = fieldNode.ValueKind == JsonValueKind.String
                            ? fieldNode.GetString()
                            : fieldNode.ToString();

                        // Một trường có thể xuất hiện nhiều nơi; lấy vị trí đầu tiên gặp được.
                        if (!string.IsNullOrWhiteSpace(fieldKey) && !binding.FieldPaths.ContainsKey(fieldKey!))
                        {
                            binding.FieldPaths[fieldKey!] = path.ToArray();

                            // Bộ mã nằm cạnh $field trong bộ khung — phải nhớ ngay lúc duyệt,
                            // vì tới lúc đọc dữ liệu sẽ không còn thấy cấu trúc bộ khung nữa.
                            if (node.TryGetProperty("$extend", out var extendNode)
                                && extendNode.ValueKind == JsonValueKind.Object)
                            {
                                if (extendNode.TryGetProperty("codeSet", out var csNode)
                                    && csNode.ValueKind == JsonValueKind.String)
                                {
                                    var codeSet = csNode.GetString();
                                    if (!string.IsNullOrWhiteSpace(codeSet))
                                        binding.CodeSets[fieldKey!] = codeSet!;
                                }

                                // Mặc định khai ngay tại lá — dành cho lá KHÔNG có bộ mã.
                                if (extendNode.TryGetProperty("defaultSourceValue", out var dsvNode))
                                {
                                    var leafDefault = dsvNode.ValueKind == JsonValueKind.String
                                        ? dsvNode.GetString()
                                        : dsvNode.GetRawText();

                                    if (!string.IsNullOrWhiteSpace(leafDefault))
                                        binding.DefaultSourceValues[fieldKey!] = leafDefault!;
                                }

                                // Ép kiểu / định dạng — nhớ lại để lúc đọc còn gỡ ngược đúng khuôn đối tác gửi.
                                // Lá có bộ mã thì bỏ qua, đối xứng với chiều gửi (B2/D5: đã qua bộ mã thì không ép kiểu):
                                // đối tác gửi nguyên partnerValue, ép kiểu trước sẽ làm lệch mã ("05" → 5) khi tra ngược.
                                var format = new ShapeFormat(
                                    ReadExtendString(extendNode, "targetType"),
                                    ReadExtendString(extendNode, "dateFormat"),
                                    ReadExtendString(extendNode, "numberFormat"));

                                if (format.HasAny && !binding.CodeSets.ContainsKey(fieldKey!))
                                    binding.Formats[fieldKey!] = format;
                            }
                        }

                        return;
                    }

                    foreach (var prop in node.EnumerateObject())
                    {
                        if (prop.Name.StartsWith('$'))
                            continue;

                        path.Add(prop.Name);
                        Walk(prop.Value, path, binding, depth + 1);
                        path.RemoveAt(path.Count - 1);
                    }
                    return;

                case JsonValueKind.Array:
                    // Mảng khuôn bản ghi — [ { "$field": ... } ] — là khối lặp NGẦM, không cần khai $each.
                    // Chiều gửi coi đúng như vậy (ShapeHasRepeatBlock → IsRecordTemplateArray) nên nó nhân
                    // khuôn ra cho mọi dòng; chiều nhận phải hiểu giống, nếu không đường dẫn trường sẽ thành
                    // "data.0.<field>" và chỉ đọc được dòng ĐẦU TIÊN, bỏ toàn bộ các dòng còn lại.
                    if (binding.EachPath == null && IsRecordTemplateArray(node))
                    {
                        binding.EachPath = path.ToArray();

                        // Chỉ phần tử đầu mang cấu trúc của một bản ghi; đường dẫn tính lại từ đầu.
                        Walk(node[0], new List<string>(), binding, depth + 1);
                        return;
                    }

                    var index = 0;
                    foreach (var item in node.EnumerateArray())
                    {
                        path.Add(index.ToString());
                        Walk(item, path, binding, depth + 1);
                        path.RemoveAt(path.Count - 1);
                        index++;
                    }
                    return;

                default:
                    // Hằng số trong bộ khung (VD "source":"ITS") không mang dữ liệu, bỏ qua.
                    return;
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Mảng khuôn bản ghi hay không — phần tử có ràng buộc $field ở bất kỳ độ sâu nào.
        ///              Giữ ĐÚNG ngữ nghĩa của IsRecordTemplateArray/HasFieldBinding bên DataOutboundService;
        ///              hai bên lệch nhau là gói tin gửi ra và gói tin đọc vào hiểu khác nhau.
        /// Created date: 08/09/2026
        /// </summary>
        private static bool IsRecordTemplateArray(JsonElement node)
        {
            if (node.ValueKind != JsonValueKind.Array || node.GetArrayLength() == 0)
                return false;

            foreach (var item in node.EnumerateArray())
            {
                if (HasFieldBinding(item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Có ràng buộc $field ở bất kỳ độ sâu nào hay không. Giữ cùng ngữ nghĩa với hàm cùng tên ở DataMappingProcess.
        /// Created date: 09/09/2026
        /// </summary>
        private static bool HasFieldBinding(JsonElement node, int depth = 0)
        {
            if (depth > MaxShapeDepth)
                return false;

            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    if (node.TryGetProperty("$field", out _))
                        return true;

                    foreach (var prop in node.EnumerateObject())
                    {
                        if (HasFieldBinding(prop.Value, depth + 1))
                            return true;
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in node.EnumerateArray())
                    {
                        if (HasFieldBinding(item, depth + 1))
                            return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đi theo đường dẫn trong JSON. Đoạn số là chỉ số mảng; khóa thử đúng hoa thường trước rồi mới không phân biệt, vì đối tác có thể đổi hoa thường.
        ///              Không thấy thì trả false, không ném lỗi.
        /// Created date: 08/09/2026
        /// </summary>
        private static bool TryNavigate(JsonElement start, string[] path, out JsonElement result)
        {
            result = start;

            foreach (var segment in path)
            {
                if (result.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index))
                {
                    if (index < 0 || index >= result.GetArrayLength())
                        return false;

                    result = result[index];
                    continue;
                }

                if (result.ValueKind != JsonValueKind.Object)
                    return false;

                if (!result.TryGetProperty(segment, out var next))
                {
                    // Đối tác có thể đổi hoa thường của khoá — thử đối chiếu không phân biệt.
                    var matched = false;
                    foreach (var prop in result.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, segment, StringComparison.OrdinalIgnoreCase))
                        {
                            next = prop.Value;
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                        return false;
                }

                result = next;
            }

            return true;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đổi giá trị JSON sang kiểu .NET: số ưu tiên long, rồi decimal, rồi double; object và mảng giữ nguyên chuỗi JSON.
        /// Created date: 08/09/2026
        /// </summary>
        private static object? JsonToObject(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l)
                ? l
                : element.TryGetDecimal(out var m) ? m : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }
}

