using Module.ShareData.Core.Helpers;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Logging;

namespace ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction
{
    /// <summary>
    /// Bước 1: Trích xuất dữ liệu thô (Data Extraction) theo kiến trúc Dynamic Query.
    /// Câu truy vấn SQL hiệu lực (partnerSql) do bên gọi đọc từ ShareDataSql: câu riêng của đối tác nếu có, không thì câu mẫu chung.
    /// Hỗ trợ chính sách Snapshot và AlwaysIncremental với mốc khóa __lastKey (và __lastKey_&lt;nhóm&gt; của câu gộp nhiều nguồn).
    /// Created date: 16/09/2026
    /// Modified date: 03/11/2026
    /// </summary>
    public partial class DataOutboundExtractionProcess
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Chạy câu SQL hiệu lực với mốc khóa và bộ biến của lượt, trả dữ liệu của trang kèm mốc cuối trang.
        ///              Câu nối đuôi đặt số dòng bằng biến thì lấy dư một dòng để biết dòng cuối trang có trùng cột mốc với dòng đầu trang kế không; dòng dư không nằm trong kết quả.
        /// Created date: 16/09/2026
        /// Modified date: 09/11/2026
        /// </summary>
        public async Task<DataOutboundExtractionResult> Extract(
            ISqlSugarClient db,
            ShareDataSubscription sub,
            ShareDataPacket packet,
            string? lastKey,
            Action<string, object?[]>? onLogWarning = null,
            CancellationToken cancellationToken = default,
            ShareDataEnum.PacketPolicy? policy = null,
            string? partnerSql = null,
            SqlVariableSet? variables = null,
            IReadOnlyDictionary<string, string?>? groupCursors = null)
        {
            var sql = partnerSql?.Trim();

            var packetPolicy = policy ?? PacketHelper.GetPolicyFromSql(sql, variables?.CursorNames);
            if (packetPolicy == ShareDataEnum.PacketPolicy.NotReady)
            {
                onLogWarning?.Invoke("Gói tin [{PacketCode}] ở trạng thái NotReady — không chạy query.", [packet.Code]);
                return new DataOutboundExtractionResult([], null);
            }
            if (packetPolicy == ShareDataEnum.PacketPolicy.Disabled)
            {
                onLogWarning?.Invoke("Gói tin [{PacketCode}] ở trạng thái Disabled — không chạy query.", [packet.Code]);
                return new DataOutboundExtractionResult([], null);
            }

            var isSnapshot = packetPolicy == ShareDataEnum.PacketPolicy.Snapshot;

            List<object> rawList;
            object? nextPageRow = null;
            if (!string.IsNullOrWhiteSpace(sql))
            {
                var (isValid, errorMsg) = SqlValidatorHelper.Validate(sql, variables);
                if (!isValid)
                {
                    var invalidMsg = $"Gói tin [{packet.Code}] chứa câu truy vấn ExtractSql không an toàn hoặc không hợp lệ: {errorMsg} — từ chối thực thi.";
                    onLogWarning?.Invoke("⚠️ {Message}", [invalidMsg]);
                    throw new ShareDataException(
                        ShareDataAlertCode.Outbound.QueryFailed,
                        BaseEnums.AlertSeverity.Error,
                        BaseEnums.AlertSource.Subscription,
                        invalidMsg);
                }

                // Chỉ các biến đã khai ở bảng tham số câu truy vấn: biến mốc tính theo mốc lần trước, biến khác là giá trị nhập
                var cursorContext = new SqlVariableContext(
                    SqlParameterHelper.ResolveCursor(isSnapshot, lastKey),
                    isSnapshot ? null : groupCursors);
                var sqlParams = SqlParameterHelper.BuildParameters(variables, cursorContext);

                var pageSize = isSnapshot || variables?.CursorGroups.Count > 0 ? null : AskOneExtraRow(sql, sqlParams);
                rawList = await ExecuteDynamicQuery(db, sql, sqlParams);
                if (pageSize != null && rawList.Count > pageSize)
                {
                    nextPageRow = rawList[pageSize.Value];
                    rawList.RemoveRange(pageSize.Value, rawList.Count - pageSize.Value);
                }
            }
            else
            {
                var missingMsg = $"Gói tin [{packet.Code}] ({sub.DatatypeId}) chưa có câu lệnh SQL trích xuất (ExtractSql). Vui lòng chạy script seed mẫu gói tin hoặc cấu hình SQL trong SQL Center.";
                onLogWarning?.Invoke("⚠️ {Message}", [missingMsg]);
                throw new ShareDataException(
                    ShareDataAlertCode.Outbound.ExtractSqlMissing,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Subscription,
                    missingMsg);
            }

            if (rawList == null || rawList.Count == 0)
                return new DataOutboundExtractionResult([], null);

            string? pageLastKey = null;
            object? lastKeyValue = null;

            if (!isSnapshot)
            {
                for (int i = rawList.Count - 1; i >= 0; i--)
                {
                    var dict = ExtractDictionary(rawList[i]);
                    if (dict != null
                        && dict.TryGetValue("__lastKey", out var rVal)
                        && rVal != null
                        && rVal != DBNull.Value
                        && !string.IsNullOrWhiteSpace(rVal.ToString()))
                    {
                        pageLastKey = rVal.ToString();
                        lastKeyValue = rVal;
                        break;
                    }
                }
            }

            // Nhóm mốc có tên (câu gộp nhiều nguồn): mốc của nhóm là dòng cuối của trang có __lastKey_<nhóm>
            Dictionary<string, CursorGroupPosition>? groups = null;
            if (!isSnapshot && variables?.CursorGroups.Count > 0)
            {
                groups = new(StringComparer.OrdinalIgnoreCase);
                var rows = rawList.Select(ExtractDictionary).ToList();
                foreach (var group in variables.CursorGroups)
                {
                    for (var i = rows.Count - 1; i >= 0; i--)
                    {
                        var dict = rows[i];
                        if (dict == null
                            || !dict.TryGetValue($"__lastKey_{group}", out var keyValue)
                            || keyValue == null
                            || keyValue == DBNull.Value
                            || string.IsNullOrWhiteSpace(keyValue.ToString()))
                        {
                            continue;
                        }

                        groups[group] = new CursorGroupPosition(keyValue.ToString()!, keyValue);
                        break;
                    }
                }
            }

            var duplicatedKey = isSnapshot ? null : FindDuplicatedKey(rawList, nextPageRow, variables);
            return new DataOutboundExtractionResult(rawList, pageLastKey, lastKeyValue, groups, duplicatedKey);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tăng biến đặt số dòng thêm 1 để lấy dư một dòng. Trả số dòng gốc của trang; câu không đặt số dòng bằng biến số nguyên thì không đổi gì và trả null.
        /// Created date: 05/11/2026
        /// </summary>
        private static int? AskOneExtraRow(string sql, SugarParameter[] parameters)
        {
            var variable = SqlValidatorHelper.PageSizeVariable(sql);
            if (variable == null)
            {
                return null;
            }

            var parameter = parameters.FirstOrDefault(p => string.Equals(p.ParameterName, $"@{variable}", StringComparison.OrdinalIgnoreCase));
            if (parameter?.Value is not int pageSize || pageSize <= 0)
            {
                return null;
            }

            parameter.Value = pageSize + 1;
            return pageSize;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Giá trị cột mốc bị trùng: hai dòng trong trang cùng giá trị ở một cột mốc, hoặc dòng đầu trang kế bằng mốc cuối trang.
        ///              Trang kế lọc lớn hơn mốc nên các dòng trùng bị bỏ sót. So không phân biệt hoa thường như cách CSDL so chuỗi. Không trùng thì trả null.
        /// Created date: 05/11/2026
        /// </summary>
        private static string? FindDuplicatedKey(List<object> rows, object? nextPageRow, SqlVariableSet? variables)
        {
            var groupColumns = variables?.CursorGroups.Select(group => $"__lastKey_{group}") ?? [];
            var dictionaries = rows.Select(ExtractDictionary).ToList();
            foreach (var column in groupColumns.Prepend("__lastKey"))
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var dictionary in dictionaries)
                {
                    var key = CursorKeyOf(dictionary, column);
                    if (key != null && !seen.Add(key))
                    {
                        return key;
                    }
                }
            }

            if (nextPageRow == null)
            {
                return null;
            }

            var nextPageKey = CursorKeyOf(ExtractDictionary(nextPageRow), "__lastKey");
            var lastRowKey = CursorKeyOf(dictionaries[^1], "__lastKey");
            return nextPageKey != null && string.Equals(nextPageKey, lastRowKey, StringComparison.OrdinalIgnoreCase) ? nextPageKey : null;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Giá trị dạng chuỗi của một cột mốc trong dòng để so trùng; cột ngày giờ giữ đủ mili giây để không báo trùng nhầm. Dòng không có giá trị ở cột đó thì trả null.
        /// Created date: 05/11/2026
        /// </summary>
        private static string? CursorKeyOf(IDictionary<string, object?>? row, string column)
        {
            if (row == null || !row.TryGetValue(column, out var value) || value == null || value == DBNull.Value)
            {
                return null;
            }

            var key = value is DateTime time ? time.ToString("O", System.Globalization.CultureInfo.InvariantCulture) : value.ToString();
            return string.IsNullOrWhiteSpace(key) ? null : key;
        }

        /// <summary>
        /// Description: Trích xuất đối tượng bất kỳ thành IDictionary để dễ dàng truy xuất giá trị trường theo tên.
        /// Created date: 16/09/2026
        /// </summary>
        public static IDictionary<string, object?>? ExtractDictionary(object item)
        {
            if (item is IDictionary<string, object?> dict)
            {
                if (dict is Dictionary<string, object?> d && d.Comparer == StringComparer.OrdinalIgnoreCase)
                    return d;
                return new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase);
            }

            if (item is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText());
                return parsed != null ? new Dictionary<string, object?>(parsed, StringComparer.OrdinalIgnoreCase) : null;
            }

            try
            {
                var json = JsonSerializer.Serialize(item);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                return parsed != null ? new Dictionary<string, object?>(parsed, StringComparer.OrdinalIgnoreCase) : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Chạy câu trích xuất. Dùng GetDataTableAsync vì SqlQueryAsync&lt;dynamic&gt; trả 0 dòng khi câu lỗi lúc chạy (vd. đổi kiểu dữ liệu):
        /// Worker sẽ hiểu là không có dữ liệu mới và đứng im, không có cảnh báo ESH-1302.
        /// Created date: 29/09/2026
        /// </summary>
        private static async Task<List<object>> ExecuteDynamicQuery(ISqlSugarClient db, string sql, SugarParameter[] parameters)
        {
            var table = await db.Ado.GetDataTableAsync(sql, parameters);
            var rows = new List<object>(table.Rows.Count);
            foreach (System.Data.DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object?>(table.Columns.Count, StringComparer.OrdinalIgnoreCase);
                foreach (System.Data.DataColumn column in table.Columns)
                {
                    dict[column.ColumnName] = row[column] is DBNull ? null : row[column];
                }
                rows.Add(dict);
            }
            return rows;
        }
    }
}

