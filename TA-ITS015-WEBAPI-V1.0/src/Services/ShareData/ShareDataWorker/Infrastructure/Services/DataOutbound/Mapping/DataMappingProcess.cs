using ShareDataWorker.Core.Models.DataOutbound;
using System.Globalization;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Infrastructure.Logging;

namespace ShareDataWorker.Infrastructure.Services.DataOutbound.Mapping
{
    /// <summary>
    /// Bước 2: Xử lý và Ánh xạ dữ liệu (Data Mapping & Transformation)
    /// Áp dụng phễu lọc ShareDataMapping, tính toán biểu thức, quy đổi bộ mã và serialize trực tiếp JSON
    /// Created date: 16/09/2026
    /// </summary>
    public static partial class DataMappingProcess
    {
        private const int MaxShapeDepth = 64;
        private static readonly JsonSerializerOptions PayloadJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// Description: Tuần tự hoá payload đối tượng thành mảng byte UTF-8 theo camelCase.
        /// Created date: 16/09/2026
        /// </summary>
        public static byte[] SerializePayload(object payload)
        {
            return JsonSerializer.SerializeToUtf8Bytes(payload, PayloadJsonOptions);
        }

        /// <summary>
        /// Description: Thực thi Bước 2 - Ánh xạ và chuyển đổi dữ liệu, cô lập lỗi nếu thiếu trường bắt buộc.
        /// Created date: 16/09/2026
        /// </summary>
        public static async Task<DataMappingResult> Map(
            ISqlSugarClient db,
            DataOutboundExtractionResult extraction,
            DataOutboundContext ctx,
            Func<string, BaseEnums.AlertSeverity, BaseEnums.AlertSource, string, string?, Task>? logAlert = null,
            Action<string, object?[]>? logWarning = null)
        {
            if (extraction.RawRows.Count == 0)
            {
                return new DataMappingResult(true, null, 0);
            }

            var packet = ctx.Packet;
            var mapping = ctx.Mapping;
            var sub = ctx.Subscription;

            if (mapping == null || string.IsNullOrWhiteSpace(mapping.TargetShapeJson))
            {
                if (logAlert != null)
                {
                    await logAlert(ShareDataAlertCode.Outbound.ShapeInvalid, BaseEnums.AlertSeverity.Error, BaseEnums.AlertSource.Funnel, $"Cấu hình ánh xạ [{mapping?.ID}] rỗng hoặc không có TargetShapeJson. Không thể kết xuất dữ liệu.", JsonSerializer.Serialize(new { packetCode = packet.Code, mappingId = mapping?.ID }));
                }
                return new DataMappingResult(false, null, 0, "TargetShapeJson rỗng");
            }

            try
            {
                using var testDoc = JsonDocument.Parse(mapping.TargetShapeJson);
            }
            catch (Exception ex)
            {
                if (logAlert != null)
                {
                    await logAlert(ShareDataAlertCode.Outbound.ShapeInvalid, BaseEnums.AlertSeverity.Error, BaseEnums.AlertSource.Funnel, $"Cấu hình ánh xạ [{mapping.Name ?? mapping.Code}] có TargetShapeJson sai cú pháp JSON: {ex.Message}", JsonSerializer.Serialize(new { packetCode = packet.Code, mappingId = mapping.ID, error = ex.Message }));
                }
                return new DataMappingResult(false, null, 0, "TargetShapeJson sai cú pháp JSON");
            }

            // Kiểm tra tính hợp lệ tĩnh của các biểu thức trong TargetShapeJson (nếu có)
            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.TargetShapeJson))
            {
                var bindings = ReadShapeExpressionBindings(mapping.TargetShapeJson);
                var invalidBindings = new List<object>();
                foreach (var (fieldKey, expr) in bindings)
                {
                    if (!IsStaticallyValid(expr, out var staticErr))
                    {
                        invalidBindings.Add(new { fieldKey, expression = expr, error = staticErr });
                    }
                }

                if (invalidBindings.Count > 0 && logAlert != null)
                {
                    await logAlert(
                        ShareDataAlertCode.Outbound.ExpressionFailed,
                        BaseEnums.AlertSeverity.Warning,
                        BaseEnums.AlertSource.Funnel,
                        $"Cấu hình ánh xạ [{mapping.ID}] có {invalidBindings.Count} biểu thức expression không hợp lệ (không qua kiểm tra an toàn), sẽ giữ nguyên giá trị nguồn.",
                        JsonSerializer.Serialize(new { mappingId = mapping.ID, failedExpressions = invalidBindings }));
                }
            }

            // Thu thập danh sách bộ mã cần nạp từ TargetShapeJson
            var neededCodeSets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.TargetShapeJson))
            {
                var (_, shapeCodeSets) = ReadShape(mapping.TargetShapeJson);

                foreach (var cs in shapeCodeSets)
                {
                    if (!string.IsNullOrWhiteSpace(cs))
                        neededCodeSets.Add(cs.Trim());
                }
            }

            var codeSetsDict = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase);
            if (neededCodeSets.Count > 0)
            {
                List<ShareDataCodeSet> dbCodeSets;
                if (ctx.CodeSets != null)
                {
                    dbCodeSets = ctx.CodeSets
                        .Where(c => c.IsDelete == null && c.Code != null && neededCodeSets.Contains(c.Code))
                        .ToList();
                }
                else
                {
                    var neededList = neededCodeSets.ToList();
                    dbCodeSets = await db.Queryable<ShareDataCodeSet>()
                        .Where(c => c.IsDelete == null)
                        .In(c => c.Code, neededList)
                        .ToListAsync();
                }

                foreach (var cs in dbCodeSets)
                {
                    if (!string.IsNullOrWhiteSpace(cs.Code))
                    {
                        codeSetsDict[cs.Code] = PacketJsonParser.ParseCodeSet(cs.ValuesJson);
                    }
                }

                foreach (var needed in neededCodeSets)
                {
                    if (!codeSetsDict.ContainsKey(needed) && logAlert != null)
                    {
                        await logAlert(
                            ShareDataAlertCode.Outbound.CodeSetNotFound,
                            BaseEnums.AlertSeverity.Warning,
                            BaseEnums.AlertSource.Funnel,
                            $"Gói tin [{packet.Code}] khai báo bộ mã [{needed}] nhưng không tìm thấy trong CSDL (ShareDataCodeSet). Giá trị thô sẽ được giữ nguyên.",
                            JsonSerializer.Serialize(new { missingCodeSet = needed, packetCode = packet.Code }));
                    }
                }
            }

            var esh1203RuntimeWarnings = new Dictionary<string, (string Expression, string Error)>(StringComparer.OrdinalIgnoreCase);

            var now = ctx.ExportedAt;
            var metaValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Now"] = now,
                ["PartnerCode"] = ctx.Partner?.Code,
                ["PacketCode"] = packet.Code,
                ["Serial"] = sub?.SerialNbr
            };

            List<object> transformedRows;
            try
            {
                transformedRows = Transform(
                    extraction.RawRows,
                    mapping?.TargetShapeJson,
                    codeSetsDict,
                    (codeSet, val) =>
                    {
                        logWarning?.Invoke("Gói tin [{0}] bộ mã [{1}] không tìm thấy ánh xạ cho giá trị '{2}'. Giá trị thô sẽ được giữ nguyên.", [packet.Code, codeSet, val]);
                    },
                    (fieldKey, expression, error) =>
                    {
                        if (error.Contains("thôi dùng", StringComparison.OrdinalIgnoreCase) ||
                            error.Contains("dữ liệu hỏng", StringComparison.OrdinalIgnoreCase))
                        {
                            logWarning?.Invoke("Gói tin [{0}] token [{1}] cảnh báo: {2}", [packet.Code, expression, error]);
                        }
                        else
                        {
                            esh1203RuntimeWarnings.TryAdd(fieldKey, (expression, error));
                        }
                    },
                    metaValues);
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("Thiếu trường bắt buộc", StringComparison.OrdinalIgnoreCase))
            {
                return new DataMappingResult(false, null, 0, ex.Message);
            }

            if (logAlert != null)
            {
                foreach (var (fieldKey, (expr, err)) in esh1203RuntimeWarnings)
                {
                    await logAlert(
                        ShareDataAlertCode.Outbound.ExpressionFailed,
                        BaseEnums.AlertSeverity.Warning,
                        BaseEnums.AlertSource.Funnel,
                        $"Trường [{fieldKey}] tính toán biểu thức [{expr}] thất bại: {err}. Đã giữ nguyên giá trị nguồn.",
                        JsonSerializer.Serialize(new { fieldKey, expression = expr, error = err, packetCode = packet.Code }));
                }
            }

            if (transformedRows.Count == 0)
            {
                return new DataMappingResult(true, null, 0);
            }

            // Bỏ PDU Envelope: tuần tự hóa trực tiếp kết quả Transform
            byte[] finalBytes;
            if (!string.IsNullOrWhiteSpace(mapping?.TargetShapeJson))
            {
                try
                {
                    using var shapeDoc = JsonDocument.Parse(mapping.TargetShapeJson);
                    if (ShapeHasRepeatBlock(shapeDoc.RootElement) && transformedRows.Count == 1)
                    {
                        // Gói dùng $each: root là Aggregate Object { header: ..., data: [...] }
                        finalBytes = SerializePayload(transformedRows[0]);
                    }
                    else
                    {
                        finalBytes = SerializePayload(transformedRows);
                    }
                }
                catch
                {
                    finalBytes = SerializePayload(transformedRows);
                }
            }
            else
            {
                // Mapping phẳng: mảng các bản ghi JSON
                finalBytes = SerializePayload(transformedRows);
            }

            return new DataMappingResult(true, finalBytes, transformedRows.Count);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ánh xạ và chuyển đổi danh sách bản ghi thô theo cấu hình Shape, bộ mã CodeSet, biểu thức tính toán và từ điển metaValues (§3.5).
        /// Created date: 18/09/2026
        /// </summary>
        public static List<object> Transform(
            IEnumerable<object>? rawRows,
            string? targetShapeJson = null,
            object? codeSets = null,
            Action<string, object?>? onUnmappedCode = null,
            Action<string, string, string>? onExpressionEvalFailed = null,
            IReadOnlyDictionary<string, object?>? metaValues = null)
        {
            if (rawRows == null)
                return [];

            var normalizedCodeSets = NormalizeCodeSets(codeSets);

            JsonDocument? shapeDoc = null;
            if (!string.IsNullOrWhiteSpace(targetShapeJson))
            {
                try
                {
                    shapeDoc = JsonDocument.Parse(targetShapeJson);
                }
                catch
                {
                    shapeDoc = null;
                }
            }

            if (shapeDoc == null)
                return [];

            using (shapeDoc)
            {
                var result = new List<object>();
                var missingRequiredFieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var missingRowCount = 0;
                var totalRowCount = 0;

                if (ShapeHasRepeatBlock(shapeDoc.RootElement))
                {
                    var allRowDicts = new List<IDictionary<string, object?>>();
                    foreach (var item in rawRows)
                    {
                        totalRowCount++;
                        var d = ExtractDictionary(item);
                        if (d != null)
                        {
                            allRowDicts.Add(d);
                        }
                    }

                    var renderedRoot = RenderShapeAggregate(
                        shapeDoc.RootElement,
                        allRowDicts,
                        normalizedCodeSets,
                        onUnmappedCode,
                        onExpressionEvalFailed,
                        missingRequiredFieldKeys,
                        0,
                        metaValues);

                    if (missingRequiredFieldKeys.Count > 0)
                    {
                        throw new InvalidOperationException($"Thiếu trường bắt buộc [{string.Join(", ", missingRequiredFieldKeys)}] trong khối dữ liệu {totalRowCount} bản ghi.");
                    }

                    if (renderedRoot != null && !ReferenceEquals(renderedRoot, ExcludedMarker.Instance))
                    {
                        result.Add(renderedRoot);
                    }

                    return result;
                }

                foreach (var item in rawRows)
                {
                    totalRowCount++;
                    var rowDict = ExtractDictionary(item);
                    if (rowDict == null)
                    {
                        result.Add(item);
                        continue;
                    }

                    var hasRowMissingRequired = false;

                    var rendered = RenderShapeNode(
                        shapeDoc.RootElement,
                        rowDict,
                        normalizedCodeSets,
                        onUnmappedCode,
                        onExpressionEvalFailed,
                        missingRequiredFieldKeys,
                        ref hasRowMissingRequired,
                        0,
                        metaValues);

                    if (hasRowMissingRequired)
                        missingRowCount++;

                    if (rendered != null && !ReferenceEquals(rendered, ExcludedMarker.Instance))
                    {
                        result.Add(rendered);
                    }
                }

                if (missingRequiredFieldKeys.Count > 0)
                {
                    throw new InvalidOperationException($"Thiếu trường bắt buộc [{string.Join(", ", missingRequiredFieldKeys)}] trên {missingRowCount}/{totalRowCount} bản ghi.");
                }

                return result;
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra một node JSON (Object hoặc Array) có chứa ràng buộc dữ liệu động ($field)
        ///              ở bất kỳ cấp độ sâu nào hay không. Dùng để nhận diện khuôn mẫu bản ghi lặp (record template)
        ///              trong cấu hình Shape của phễu lọc ShareDataMapping.
        ///              Có chốt chặn an toàn độ sâu (depth > MaxShapeDepth) để chống tràn ngăn xếp.
        ///              LƯU Ý: Phải giữ đúng đồng bộ ngữ nghĩa với HasFieldBinding bên chiều Inbound (DataInboundService.Parse).
        /// Created date: 16/09/2026
        /// </summary>
        /// <param name="node">Node JSON cần duyệt kiểm tra</param>
        /// <param name="depth">Độ sâu đệ quy hiện tại (mặc định = 0, tối đa MaxShapeDepth = 32)</param>
        /// <returns>True nếu node hoặc bất kỳ node con nào chứa thuộc tính "$field", ngược lại False.</returns>
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
        /// Author: Đạt
        /// Description: Kiểm tra một mảng JSON có phải là mảng khuôn mẫu bản ghi lặp (record template) hay không
        ///              bằng cách duyệt các phần tử và phát hiện sự hiện diện của thuộc tính "$field".
        ///              Guard / Safety limit: Duyệt nông 1 cấp qua các phần tử mảng và uỷ quyền kiểm tra đệ quy cho HasFieldBinding.
        ///              Cross-pipeline Sync: Giữ đồng bộ với cơ chế nhận diện template lặp ở chiều Inbound.
        /// Created date: 16/09/2026
        /// </summary>
        /// <param name="node">Node mảng JSON cần kiểm tra</param>
        /// <returns>True nếu mảng chứa ít nhất một phần tử có binding trường "$field", ngược lại False.</returns>
        private static bool IsRecordTemplateArray(JsonElement node)
        {
            if (node.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var item in node.EnumerateArray())
            {
                if (HasFieldBinding(item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra cấu hình cây Shape có chứa khối lặp bản ghi ($each = true hoặc mảng khuôn mẫu bản ghi)
        ///              ở bất kỳ nhánh cây nào hay không để quyết định chế độ render tổng hợp (RenderShapeAggregate)
        ///              thay vì render phẳng từng bản ghi.
        ///              Guard / Safety limit: Chốt chặn an toàn độ sâu đệ quy depth > MaxShapeDepth chống tràn stack.
        ///              Cross-pipeline Sync: Đồng bộ cấu trúc nhận diện khối lặp giữa luồng gửi (Outbound) và luồng nhận (Inbound).
        /// Created date: 16/09/2026
        /// </summary>
        /// <param name="node">Node JSON cây Shape cần duyệt</param>
        /// <param name="depth">Độ sâu đệ quy hiện tại (mặc định = 0)</param>
        /// <returns>True nếu tìm thấy cấu hình khối lặp, ngược lại False.</returns>
        private static bool ShapeHasRepeatBlock(JsonElement node, int depth = 0)
        {
            if (depth > MaxShapeDepth)
                return false;

            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    if (node.TryGetProperty("$each", out var eachElem) && eachElem.ValueKind == JsonValueKind.True)
                        return true;

                    foreach (var prop in node.EnumerateObject())
                    {
                        if (ShapeHasRepeatBlock(prop.Value, depth + 1))
                            return true;
                    }
                    break;

                case JsonValueKind.Array:
                    if (IsRecordTemplateArray(node))
                        return true;

                    foreach (var item in node.EnumerateArray())
                    {
                        if (ShapeHasRepeatBlock(item, depth + 1))
                            return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Giải giá trị token meta ($meta) từ từ điển metaValues theo §3.5 bản bàn giao.
        ///              Chỉ hỗ trợ đúng 4 token: Now, PartnerCode, PacketCode, Serial.
        ///              Bỏ qua $extend nếu có kèm theo (coi là dữ liệu hỏng).
        /// Created date: 18/09/2026
        /// </summary>
        /// <param name="node">Node JSON chứa thuộc tính $meta</param>
        /// <param name="metaElem">Phần tử $meta</param>
        /// <param name="metaValues">Từ điển giá trị meta hệ thống</param>
        /// <param name="onWarning">Callback cảnh báo khi dữ liệu hỏng</param>
        /// <returns>Giá trị meta tương ứng hoặc null nếu token không hợp lệ / không có.</returns>
        private static object? ResolveMetaValue(
            JsonElement node,
            JsonElement metaElem,
            IReadOnlyDictionary<string, object?>? metaValues,
            Action<string, string, string>? onWarning)
        {
            var token = metaElem.ValueKind == JsonValueKind.String ? metaElem.GetString() : metaElem.ToString();
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = token.Trim();

            if (!string.Equals(token, "Now", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(token, "PartnerCode", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(token, "PacketCode", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(token, "Serial", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (node.TryGetProperty("$extend", out _))
            {
                onWarning?.Invoke("$meta", token, "Node $meta đi kèm $extend là dữ liệu hỏng, đã bỏ qua $extend.");
            }

            if (metaValues != null)
            {
                var matchedKey = metaValues.Keys.FirstOrDefault(k => string.Equals(k, token, StringComparison.OrdinalIgnoreCase));
                if (matchedKey != null)
                {
                    return metaValues[matchedKey];
                }
            }

            return null;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Render cây Shape ở chế độ gom nhóm / tổng hợp (envelope chung chứa một hoặc nhiều mảng bản ghi),
        ///              duyệt qua toàn bộ danh sách bản ghi nguồn để đổ vào các mảng khuôn mẫu lặp tương ứng.
        ///              Guard / Safety limit: Chốt chặn độ sâu đệ quy depth > MaxShapeDepth (trả về null nếu vượt ngưỡng).
        ///              Cross-pipeline Sync: Đồng bộ với cấu trúc gói tin phân cấp ISO 14827 bên ngoài và schema JSON của đối tác.
        /// Created date: 16/09/2026
        /// </summary>
        /// <param name="node">Node JSON cây Shape hiện tại đang render</param>
        /// <param name="allRows">Toàn bộ danh sách dòng dữ liệu nguồn đã trích xuất</param>
        /// <param name="codeSets">Từ điển quy đổi bộ mã</param>
        /// <param name="onUnmappedCode">Callback xử lý mã chưa được map</param>
        /// <param name="onExpressionEvalFailed">Callback khi biểu thức tính toán lỗi</param>
        /// <param name="missingRequiredFieldKeys">Tập các trường bắt buộc bị thiếu</param>
        /// <param name="depth">Độ sâu đệ quy hiện tại</param>
        /// <returns>Đối tượng đã render thành cây dữ liệu tổng hợp.</returns>
        private static object? RenderShapeAggregate(
            JsonElement node,
            IReadOnlyList<IDictionary<string, object?>> allRows,
            IReadOnlyDictionary<string, CodeSetDto>? codeSets,
            Action<string, object?>? onUnmappedCode,
            Action<string, string, string>? onExpressionEvalFailed,
            HashSet<string> missingRequiredFieldKeys,
            int depth = 0,
            IReadOnlyDictionary<string, object?>? metaValues = null)
        {
            if (depth > MaxShapeDepth)
                return null;

            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    if (node.TryGetProperty("$exclude", out var excludeElem) && excludeElem.ValueKind == JsonValueKind.True)
                    {
                        return ExcludedMarker.Instance;
                    }

                    if (node.TryGetProperty("$meta", out var metaElem))
                    {
                        return ResolveMetaValue(node, metaElem, metaValues, onExpressionEvalFailed);
                    }

                    if (node.TryGetProperty("$each", out var eachElem) && eachElem.ValueKind == JsonValueKind.True)
                    {
                        if (node.TryGetProperty("$as", out var asElem))
                        {
                            var items = new List<object?>();
                            foreach (var row in allRows)
                            {
                                var dummyMissing = false;
                                var renderedChild = RenderShapeNode(
                                    asElem,
                                    row,
                                    codeSets,
                                    onUnmappedCode,
                                    onExpressionEvalFailed,
                                    missingRequiredFieldKeys,
                                    ref dummyMissing,
                                    depth + 1,
                                    metaValues);

                                if (!ReferenceEquals(renderedChild, ExcludedMarker.Instance))
                                {
                                    items.Add(renderedChild);
                                }
                            }
                            return items;
                        }
                    }

                    if (node.TryGetProperty("$field", out _))
                    {
                        var dummyMissing = false;
                        var firstRow = allRows.Count > 0 ? allRows[0] : new Dictionary<string, object?>();
                        return RenderShapeNode(
                            node,
                            firstRow,
                            codeSets,
                            onUnmappedCode,
                            onExpressionEvalFailed,
                            missingRequiredFieldKeys,
                            ref dummyMissing,
                            depth + 1,
                            metaValues);
                    }

                    var objDict = new Dictionary<string, object?>();
                    foreach (var prop in node.EnumerateObject())
                    {
                        var child = RenderShapeAggregate(
                            prop.Value,
                            allRows,
                            codeSets,
                            onUnmappedCode,
                            onExpressionEvalFailed,
                            missingRequiredFieldKeys,
                            depth + 1,
                            metaValues);

                        if (!ReferenceEquals(child, ExcludedMarker.Instance))
                        {
                            objDict[prop.Name] = child;
                        }
                    }
                    return objDict;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in node.EnumerateArray())
                    {
                        if (HasFieldBinding(item))
                        {
                            foreach (var row in allRows)
                            {
                                var dummyMissing = false;
                                var renderedChild = RenderShapeNode(
                                    item,
                                    row,
                                    codeSets,
                                    onUnmappedCode,
                                    onExpressionEvalFailed,
                                    missingRequiredFieldKeys,
                                    ref dummyMissing,
                                    depth + 1,
                                    metaValues);

                                if (!ReferenceEquals(renderedChild, ExcludedMarker.Instance))
                                {
                                    list.Add(renderedChild);
                                }
                            }
                        }
                        else
                        {
                            var child = RenderShapeAggregate(
                                item,
                                allRows,
                                codeSets,
                                onUnmappedCode,
                                onExpressionEvalFailed,
                                missingRequiredFieldKeys,
                                depth + 1,
                                metaValues);

                            if (!ReferenceEquals(child, ExcludedMarker.Instance))
                            {
                                list.Add(child);
                            }
                        }
                    }
                    return list;

                case JsonValueKind.String:
                    return node.GetString();
                case JsonValueKind.Number:
                    if (node.TryGetInt64(out var l))
                        return l;
                    if (node.TryGetDouble(out var d))
                        return d;
                    return node.GetRawText();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return node.GetRawText();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Render một node đơn lẻ trong cây Shape cho một dòng dữ liệu cụ thể,
        ///              thực hiện đọc trường nguồn ($field), tính toán biểu thức ($extend.expression),
        ///              quy đổi bộ mã ($extend.codeSet) và ép kiểu dữ liệu đích.
        ///              Guard / Safety limit: Giới hạn độ sâu đệ quy depth > MaxShapeDepth chống tràn bộ nhớ/ngăn xếp.
        ///              Cross-pipeline Sync: Đảm bảo quy tắc giải token ($field, $extend) nhất quán giữa các phân hệ.
        /// Created date: 16/09/2026
        /// </summary>
        /// <param name="node">Node JSON cây Shape đang xử lý</param>
        /// <param name="rowDict">Dữ liệu một dòng bản ghi nguồn dạng IDictionary</param>
        /// <param name="codeSets">Từ điển bộ mã</param>
        /// <param name="onUnmappedCode">Callback cảnh báo giá trị mã chưa định nghĩa</param>
        /// <param name="onExpressionEvalFailed">Callback cảnh báo biểu thức lỗi</param>
        /// <param name="missingRequiredFieldKeys">Tập lưu trữ các trường bắt buộc bị thiếu</param>
        /// <param name="hasRowMissingRequired">Cờ đánh dấu dòng dữ liệu có thiếu trường bắt buộc</param>
        /// <param name="depth">Độ sâu đệ quy hiện tại</param>
        /// <returns>Đối tượng giá trị đã ánh xạ tương ứng cho node.</returns>
        private static object? RenderShapeNode(
            JsonElement node,
            IDictionary<string, object?> rowDict,
            IReadOnlyDictionary<string, CodeSetDto>? codeSets,
            Action<string, object?>? onUnmappedCode,
            Action<string, string, string>? onExpressionEvalFailed,
            HashSet<string> missingRequiredFieldKeys,
            ref bool hasRowMissingRequired,
            int depth = 0,
            IReadOnlyDictionary<string, object?>? metaValues = null)
        {
            if (depth > MaxShapeDepth)
                return null;

            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    if (node.TryGetProperty("$exclude", out var excludeElem) && excludeElem.ValueKind == JsonValueKind.True)
                    {
                        return ExcludedMarker.Instance;
                    }

                    if (node.TryGetProperty("$meta", out var metaElem))
                    {
                        return ResolveMetaValue(node, metaElem, metaValues, onExpressionEvalFailed);
                    }

                    if (node.TryGetProperty("$each", out var eachElem) && eachElem.ValueKind == JsonValueKind.True)
                    {
                        if (node.TryGetProperty("$as", out var asElem))
                        {
                            var singleChild = RenderShapeNode(
                                asElem,
                                rowDict,
                                codeSets,
                                onUnmappedCode,
                                onExpressionEvalFailed,
                                missingRequiredFieldKeys,
                                ref hasRowMissingRequired,
                                depth + 1,
                                metaValues);

                            return ReferenceEquals(singleChild, ExcludedMarker.Instance)
                                ? new List<object?>()
                                : new List<object?> { singleChild };
                        }
                    }

                    if (node.TryGetProperty("$field", out var fieldElem))
                    {
                        var sourceFieldKey = fieldElem.ValueKind == JsonValueKind.String ? fieldElem.GetString() : fieldElem.ToString();
                        if (string.IsNullOrWhiteSpace(sourceFieldKey))
                            return null;

                        object? val = null;
                        var matchedKey = rowDict.Keys.FirstOrDefault(k => string.Equals(k, sourceFieldKey, StringComparison.OrdinalIgnoreCase));
                        if (matchedKey != null)
                        {
                            val = rowDict[matchedKey];
                        }

                        string? targetType = null;
                        string? codeSet = null;
                        string? dateFormat = null;
                        string? numberFormat = null;
                        string? defaultPartnerValue = null;
                        bool? requiredFlag = null;
                        string? expression = null;

                        if (node.TryGetProperty("$extend", out var extendElem) && extendElem.ValueKind == JsonValueKind.Object)
                        {
                            if (extendElem.TryGetProperty("targetType", out var ttElem) && ttElem.ValueKind == JsonValueKind.String)
                                targetType = ttElem.GetString();
                            if (extendElem.TryGetProperty("codeSet", out var csElem) && csElem.ValueKind == JsonValueKind.String)
                                codeSet = csElem.GetString();
                            if (extendElem.TryGetProperty("dateFormat", out var dfElem) && dfElem.ValueKind == JsonValueKind.String)
                                dateFormat = dfElem.GetString();
                            if (extendElem.TryGetProperty("numberFormat", out var nfElem) && nfElem.ValueKind == JsonValueKind.String)
                                numberFormat = nfElem.GetString();
                            if (extendElem.TryGetProperty("defaultPartnerValue", out var dpvElem))
                                defaultPartnerValue = dpvElem.ValueKind == JsonValueKind.String ? dpvElem.GetString() : dpvElem.GetRawText();
                            if (extendElem.TryGetProperty("required", out var rqElem) && (rqElem.ValueKind == JsonValueKind.True || rqElem.ValueKind == JsonValueKind.False))
                                requiredFlag = rqElem.GetBoolean();
                            if (extendElem.TryGetProperty("expression", out var exElem) && exElem.ValueKind == JsonValueKind.String)
                                expression = exElem.GetString();
                        }

                        // §3.4 & D8: expression không còn sinh ra từ FE — service bỏ qua không tính toán, ghi nhận cảnh báo
                        if (!string.IsNullOrWhiteSpace(expression))
                        {
                            onExpressionEvalFailed?.Invoke(sourceFieldKey, expression, "Biểu thức expression đã thôi dùng, bỏ qua không tính toán");
                        }

                        // B1 & B2: Thứ tự biến đổi — §4.1:
                        // giá trị nội bộ → quy đổi BỘ MÃ (sourceValue → partnerValue) → nếu rỗng lấy mặc định → ép kiểu + định dạng
                        var hasCodeSet = !string.IsNullOrWhiteSpace(codeSet);
                        if (hasCodeSet)
                        {
                            CodeSetDto? partnerCodeSet = null;
                            var foundCodeSet = codeSets != null && codeSets.TryGetValue(codeSet!, out partnerCodeSet);
                            var codeSetDefault = partnerCodeSet?.DefaultPartnerValue;

                            var isValEmpty = val == null || (val is string s && string.IsNullOrWhiteSpace(s));
                            if (isValEmpty)
                            {
                                // D3: Giá trị rỗng + có codeSet: ưu tiên mặc định của BỘ MÃ; nếu bộ mã không có mặc định mới lấy defaultPartnerValue của lá
                                if (!string.IsNullOrWhiteSpace(codeSetDefault))
                                {
                                    val = codeSetDefault;
                                }
                                else if (partnerCodeSet?.Values.FirstOrDefault(cv => cv.IsDefault == true) is { } defEntry && !string.IsNullOrWhiteSpace(defEntry.PartnerValue))
                                {
                                    val = defEntry.PartnerValue;
                                }
                                else if (!string.IsNullOrWhiteSpace(defaultPartnerValue))
                                {
                                    val = defaultPartnerValue;
                                }
                                else
                                {
                                    val = null;
                                }
                            }
                            else
                            {
                                // D4: Giá trị có: quy đổi qua bộ mã, nếu không khớp lấy defaultPartnerValue của bộ mã
                                if (foundCodeSet && partnerCodeSet != null)
                                {
                                    val = MapCode(partnerCodeSet.Values, val, codeSet, onUnmappedCode, codeSetDefault);
                                }
                                else
                                {
                                    onUnmappedCode?.Invoke(codeSet ?? string.Empty, val?.ToString());
                                }
                            }

                            // B2: ĐÃ QUA BỘ MÃ THÌ KHÔNG ÉP KIỂU NỮA — giá trị bộ mã trả ra là giá trị cuối cùng (D5)
                        }
                        else
                        {
                            // Trường không có bộ mã:
                            // D6: Nếu giá trị rỗng thì lấy defaultPartnerValue của lá
                            if ((val == null || (val is string s && string.IsNullOrWhiteSpace(s))) && !string.IsNullOrWhiteSpace(defaultPartnerValue))
                            {
                                val = defaultPartnerValue;
                            }

                            // Ép kiểu theo targetType + định dạng theo dateFormat / numberFormat (D1, D2)
                            val = ConvertDataType(val, targetType, dateFormat, numberFormat);
                        }

                        var isRequired = requiredFlag == true;
                        if (isRequired && (val == null || (val is string sRequired && string.IsNullOrWhiteSpace(sRequired))))
                        {
                            missingRequiredFieldKeys.Add(sourceFieldKey);
                            hasRowMissingRequired = true;
                        }

                        return val;
                    }

                    var objDict = new Dictionary<string, object?>();
                    foreach (var prop in node.EnumerateObject())
                    {
                        var child = RenderShapeNode(
                            prop.Value,
                            rowDict,
                            codeSets,
                            onUnmappedCode,
                            onExpressionEvalFailed,
                            missingRequiredFieldKeys,
                            ref hasRowMissingRequired,
                            depth + 1,
                            metaValues);

                        if (!ReferenceEquals(child, ExcludedMarker.Instance))
                        {
                            objDict[prop.Name] = child;
                        }
                    }
                    return objDict;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in node.EnumerateArray())
                    {
                        var child = RenderShapeNode(
                            item,
                            rowDict,
                            codeSets,
                            onUnmappedCode,
                            onExpressionEvalFailed,
                            missingRequiredFieldKeys,
                            ref hasRowMissingRequired,
                            depth + 1,
                            metaValues);

                        if (!ReferenceEquals(child, ExcludedMarker.Instance))
                        {
                            list.Add(child);
                        }
                    }
                    return list;

                case JsonValueKind.String:
                    return node.GetString();
                case JsonValueKind.Number:
                    if (node.TryGetInt64(out var l))
                        return l;
                    if (node.TryGetDouble(out var d))
                        return d;
                    return node.GetRawText();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return node.GetRawText();
            }
        }

        private sealed class ExcludedMarker
        {
            /// <summary>
            /// Description: Đối tượng đánh dấu singleton cho biết trường dữ liệu bị loại trừ khỏi kết quả đầu ra.
            /// </summary>
            public static readonly ExcludedMarker Instance = new();
        }

        /// <summary>
        /// Description: Đọc cây Shape JSON và trích xuất danh sách FieldKey cùng CodeSet được sử dụng.
        /// Created date: 16/09/2026
        /// </summary>
        public static (List<string> FieldKeys, List<string> CodeSets) ReadShape(string? shapeJson)
        {
            var fieldKeys = new List<string>();
            var codeSets = new List<string>();

            if (string.IsNullOrWhiteSpace(shapeJson))
                return (fieldKeys, codeSets);

            try
            {
                using var doc = JsonDocument.Parse(shapeJson);
                WalkShapeDoc(doc.RootElement, fieldKeys, codeSets, null, null, 0);
            }
            catch
            {
                fieldKeys.Clear();
                codeSets.Clear();
            }

            return (fieldKeys, codeSets);
        }


        /// <summary>
        /// Description: Đọc cây Shape JSON và trích xuất danh sách các cặp (FieldKey, Expression).
        /// Created date: 16/09/2026
        /// </summary>
        public static List<(string FieldKey, string Expression)> ReadShapeExpressionBindings(string? shapeJson)
        {
            var bindings = new List<(string FieldKey, string Expression)>();
            if (string.IsNullOrWhiteSpace(shapeJson))
                return bindings;

            try
            {
                using var doc = JsonDocument.Parse(shapeJson);
                WalkShapeDoc(doc.RootElement, null, null, null, bindings, 0);
            }
            catch
            {
                bindings.Clear();
            }

            return bindings;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Duyệt đệ quy cây JSON Document cấu hình Shape để thu thập metadata trường,
        ///              danh sách bộ mã và các biểu thức tính toán được khai báo trong template.
        ///              Guard / Safety limit: Chốt chặn đệ quy depth > MaxShapeDepth để tránh treo hoặc tràn ngăn xếp.
        ///              Cross-pipeline Sync: Duyệt đầy đủ các cấu trúc Object, Array và nhận diện token $field / $extend.
        /// Created date: 16/09/2026
        /// </summary>
        /// <param name="node">Node JSON hiện tại</param>
        /// <param name="fieldKeys">Danh sách lưu các FieldKey phát hiện</param>
        /// <param name="codeSets">Danh sách lưu các CodeSet phát hiện</param>
        /// <param name="fieldsWithExpr">Danh sách trường có biểu thức</param>
        /// <param name="expressionBindings">Danh sách cặp (FieldKey, Expression)</param>
        /// <param name="depth">Độ sâu đệ quy hiện tại</param>
        private static void WalkShapeDoc(
            JsonElement node,
            List<string>? fieldKeys,
            List<string>? codeSets,
            List<string>? fieldsWithExpr,
            List<(string FieldKey, string Expression)>? expressionBindings = null,
            int depth = 0)
        {
            if (depth > MaxShapeDepth)
                return;

            switch (node.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var item in node.EnumerateArray())
                    {
                        WalkShapeDoc(item, fieldKeys, codeSets, fieldsWithExpr, expressionBindings, depth + 1);
                    }
                    break;

                case JsonValueKind.Object:
                    if (node.TryGetProperty("$exclude", out var ex) && ex.ValueKind == JsonValueKind.True)
                    {
                        return;
                    }

                    if (node.TryGetProperty("$meta", out _))
                    {
                        return;
                    }

                    if (node.TryGetProperty("$field", out var f))
                    {
                        var fStr = f.ValueKind == JsonValueKind.String ? f.GetString() : f.ToString();
                        if (!string.IsNullOrWhiteSpace(fStr) && fieldKeys != null)
                        {
                            fieldKeys.Add(fStr);
                        }

                        if (node.TryGetProperty("$extend", out var ext) && ext.ValueKind == JsonValueKind.Object)
                        {
                            if (ext.TryGetProperty("codeSet", out var cs) && cs.ValueKind == JsonValueKind.String)
                            {
                                var csStr = cs.GetString();
                                if (!string.IsNullOrWhiteSpace(csStr) && codeSets != null)
                                {
                                    codeSets.Add(csStr);
                                }
                            }

                            if (ext.TryGetProperty("expression", out var expr) && expr.ValueKind == JsonValueKind.String)
                            {
                                var exprStr = expr.GetString();
                                if (!string.IsNullOrWhiteSpace(exprStr) && !string.IsNullOrWhiteSpace(fStr))
                                {
                                    fieldsWithExpr?.Add(fStr);
                                    expressionBindings?.Add((fStr, exprStr));
                                }
                            }
                        }
                        return;
                    }

                    foreach (var prop in node.EnumerateObject())
                    {
                        WalkShapeDoc(prop.Value, fieldKeys, codeSets, fieldsWithExpr, expressionBindings, depth + 1);
                    }
                    break;
            }
        }

        /// <summary>
        /// Description: Ép kiểu giá trị sang kiểu dữ liệu đích tương ứng theo cấu hình trường (decimal, int, string, bool...)
        ///              và áp dụng khuôn định dạng ngày (dateFormat) hoặc định dạng số (numberFormat) nếu có (§3.4).
        /// Created date: 16/09/2026
        /// </summary>
        public static object? ConvertDataType(object? value, string? dataType, string? dateFormat = null, string? numberFormat = null)
        {
            if (value == null)
                return null;

            if (string.IsNullOrWhiteSpace(dataType))
            {
                if (value is DateTime dt && !string.IsNullOrWhiteSpace(dateFormat))
                    return FormatDateTime(dt, dateFormat);

                if (!string.IsNullOrWhiteSpace(numberFormat))
                    return FormatNumber(value, "number", numberFormat);

                return value;
            }

            var targetType = dataType.Trim().ToLowerInvariant();

            try
            {
                return targetType switch
                {
                    "decimal" or "number" or "int" or "integer" or "long" or "double" or "float" =>
                        FormatNumber(value, targetType, numberFormat),
                    "bool" or "boolean" => value is bool b ? b : (value is string strBool ? (strBool == "1" || strBool.Equals("true", StringComparison.OrdinalIgnoreCase)) : Convert.ToBoolean(value)),
                    "guid" => value is Guid ? value : (Guid.TryParse(value.ToString(), out var g) ? g : value),
                    "string" => value is DateTime dt
                        ? FormatDateTime(dt, dateFormat)
                        : (!string.IsNullOrWhiteSpace(numberFormat) ? FormatNumber(value, "string", numberFormat)?.ToString() : value.ToString()),
                    "datetime" or "datetime2" or "date" or "time" => FormatDateTime(value, dateFormat),
                    _ => value
                };
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Xử lý định dạng và làm tròn số theo numberFormat và targetType (§3.4, §9.3).
        /// Created date: 18/09/2026
        /// </summary>
        private static object? FormatNumber(object value, string targetType, string? numberFormat)
        {
            decimal dec;
            if (value is decimal d) dec = d;
            else if (value is double dbl) dec = (decimal)dbl;
            else if (value is float fl) dec = (decimal)fl;
            else if (value is int i) dec = i;
            else if (value is long l) dec = l;
            else
            {
                var str = value.ToString()?.Trim();
                if (string.IsNullOrEmpty(str) || !decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out dec))
                {
                    return value;
                }
            }

            if (!string.IsNullOrWhiteSpace(numberFormat))
            {
                var formattedStr = dec.ToString(numberFormat, CultureInfo.InvariantCulture);
                if (decimal.TryParse(formattedStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var formattedDec))
                {
                    dec = formattedDec;
                }
            }

            return targetType switch
            {
                "int" or "integer" => Convert.ToInt32(dec),
                "long" => Convert.ToInt64(dec),
                "double" => Convert.ToDouble(dec),
                "float" => (float)dec,
                "decimal" or "number" => dec,
                "string" when !string.IsNullOrWhiteSpace(numberFormat) => dec.ToString(numberFormat, CultureInfo.InvariantCulture),
                _ => dec
            };
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ép kiểu và định dạng thời gian. Trả về chuẩn ISO-8601 nếu định dạng rỗng hoặc lỗi.
        /// Created date: 17/09/2026
        /// Updated date: 18/09/2026
        /// </summary>
        private static object? FormatDateTime(object? value, string? dateFormat)
        {
            if (value is not DateTime dt)
            {
                if (value is not string sv || !DateTime.TryParse(sv, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    return value;
            }

            try
            {
                return string.IsNullOrWhiteSpace(dateFormat)
                    ? dt.ToString("o", CultureInfo.InvariantCulture)
                    : dt.ToString(dateFormat, CultureInfo.InvariantCulture);
            }
            catch
            {
                return dt.ToString("o", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Description: Trích xuất một đối tượng bất kỳ thành IDictionary để phục vụ ánh xạ trường dữ liệu.
        /// Created date: 16/09/2026
        /// </summary>
        private static IDictionary<string, object?>? ExtractDictionary(object item)
        {
            if (item is IDictionary<string, object?> dict)
                return dict;

            if (item is JsonElement je && je.ValueKind == JsonValueKind.Object)
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText());

            try
            {
                var json = JsonSerializer.Serialize(item);
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Description: Chuẩn hoá các dạng đối tượng codeSets đầu vào thành IReadOnlyDictionary<string, CodeSetDto>.
        /// Created date: 18/09/2026
        /// </summary>
        private static IReadOnlyDictionary<string, CodeSetDto>? NormalizeCodeSets(object? codeSets)
        {
            if (codeSets == null)
                return null;

            if (codeSets is IReadOnlyDictionary<string, CodeSetDto> roDictDto)
                return roDictDto;

            if (codeSets is IDictionary<string, CodeSetDto> dictDto)
                return new Dictionary<string, CodeSetDto>(dictDto, StringComparer.OrdinalIgnoreCase);

            if (codeSets is IReadOnlyDictionary<string, List<CodeValueDto>> roDictList)
            {
                return roDictList.ToDictionary(
                    k => k.Key,
                    k => new CodeSetDto { Values = k.Value },
                    StringComparer.OrdinalIgnoreCase);
            }

            if (codeSets is IDictionary<string, List<CodeValueDto>> dictList)
            {
                return dictList.ToDictionary(
                    k => k.Key,
                    k => new CodeSetDto { Values = k.Value },
                    StringComparer.OrdinalIgnoreCase);
            }

            return null;
        }

        /// <summary>
        /// Description: Ánh xạ giá trị mã nội bộ sang giá trị của đối tác theo bảng CodeSet.
        ///              Thứ tự ưu tiên khi không khớp: directionDefault (cấu trúc mới) -> IsDefault (cấu trúc cũ) -> giữ nguyên giá trị thô và gọi onUnmapped.
        /// Created date: 18/09/2026
        /// Modified date: 24/09/2026
        /// </summary>
        private static object? MapCode(
            List<CodeValueDto>? codeValues,
            object? rawValue,
            string? codeSetCode = null,
            Action<string, object?>? onUnmapped = null,
            string? directionDefault = null)
        {
            if (codeValues == null || codeValues.Count == 0)
            {
                if (!string.IsNullOrEmpty(directionDefault))
                    return directionDefault;
                return rawValue;
            }

            var rawStr = rawValue?.ToString();

            var matched = codeValues.FirstOrDefault(cv =>
                cv.SourceValue != null &&
                string.Equals(cv.SourceValue, rawStr, StringComparison.OrdinalIgnoreCase));

            if (matched != null)
                return matched.PartnerValue;

            if (!string.IsNullOrEmpty(directionDefault))
                return directionDefault;

            var defaultEntry = codeValues.FirstOrDefault(cv => cv.IsDefault == true);
            if (defaultEntry != null)
                return defaultEntry.PartnerValue;

            onUnmapped?.Invoke(codeSetCode ?? string.Empty, rawStr);
            return rawValue;
        }
    }
}

