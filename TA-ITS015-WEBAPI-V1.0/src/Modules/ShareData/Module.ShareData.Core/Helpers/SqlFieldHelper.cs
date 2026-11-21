using Module.ShareData.Core.Dto.Packet;
using Newtonsoft.Json;

namespace Module.ShareData.Core.Helpers
{
    /// <summary>
    /// Kết quả gộp cột dò được từ câu SQL với danh sách trường đang lưu.
    /// </summary>
    /// <param name="Fields">Danh sách trường sau khi gộp: theo thứ tự cột của câu SQL, khoá đã mất xếp cuối.</param>
    /// <param name="Added">Khoá mới (hoặc xuất hiện lại sau khi mất).</param>
    /// <param name="Lost">Khoá vừa mất khỏi câu SQL.</param>
    /// <param name="TypeChanged">Khoá đổi kiểu, dạng "khoá: kiểu cũ → kiểu mới".</param>
    public sealed record SqlFieldMergeResult(List<SqlFieldItem> Fields, List<string> Added, List<string> Lost, List<string> TypeChanged);

    /// <summary>
    /// Author: DatHQ
    /// Description: Đọc/ghi danh sách trường của gói tin (ShareDataSql.FieldsJson), quy đổi kiểu CSDL và gộp kết quả dò cột với bản đang lưu.
    ///              Khoá và kiểu theo câu SQL; nhãn, bắt buộc, bỏ qua, nhóm, ghi chú của người dùng được giữ nguyên khi dò lại.
    /// Created date: 24/10/2026
    /// </summary>
    public static class SqlFieldHelper
    {
        /// <summary>Độ dài tối đa của nhãn, nhóm.</summary>
        public const int LabelMaxLength = 128;

        /// <summary>Độ dài tối đa của ghi chú.</summary>
        public const int RemarkMaxLength = 256;

        private static readonly HashSet<string> NumberDbTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "int", "bigint", "smallint", "tinyint", "decimal", "numeric", "float", "real", "money", "smallmoney"
        };

        private static readonly HashSet<string> DateTimeDbTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "date", "datetime", "datetime2", "smalldatetime", "datetimeoffset", "time"
        };

        /// <summary>
        /// Đọc FieldsJson; trống hoặc hỏng trả danh sách rỗng.
        /// </summary>
        public static List<SqlFieldItem> Read(string? fieldsJson)
        {
            if (string.IsNullOrWhiteSpace(fieldsJson)) return [];

            try
            {
                return JsonConvert.DeserializeObject<List<SqlFieldItem>>(fieldsJson!)?
                    .Where(f => !string.IsNullOrWhiteSpace(f?.Key))
                    .ToList() ?? [];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Ghi danh sách trường thành FieldsJson; danh sách rỗng trả null.
        /// </summary>
        public static string? Write(IReadOnlyCollection<SqlFieldItem>? fields)
        {
            return fields == null || fields.Count == 0 ? null : JsonConvert.SerializeObject(fields);
        }

        /// <summary>
        /// Cột hệ thống (bắt đầu bằng "__", VD __lastKey) không phải trường của gói tin.
        /// </summary>
        public static bool IsSystemColumn(string? name) => name != null && name.StartsWith("__", StringComparison.Ordinal);

        /// <summary>
        /// Quy kiểu CSDL (system_type_name, VD "nvarchar(128)") về kiểu trường: string | number | datetime | bool.
        /// </summary>
        public static string MapDbType(string? dbType)
        {
            if (string.IsNullOrWhiteSpace(dbType)) return SqlFieldType.String;

            var baseType = dbType.Trim();
            var paren = baseType.IndexOf('(');
            if (paren > 0) baseType = baseType[..paren].Trim();

            if (NumberDbTypes.Contains(baseType)) return SqlFieldType.Number;
            if (DateTimeDbTypes.Contains(baseType)) return SqlFieldType.DateTime;
            if (string.Equals(baseType, "bit", StringComparison.OrdinalIgnoreCase)) return SqlFieldType.Bool;
            return SqlFieldType.String;
        }

        /// <summary>
        /// Gộp cột dò được với danh sách trường đang lưu (so khoá không phân biệt hoa thường):
        /// khoá mới thêm (nhãn, bắt buộc, nhóm, ghi chú lấy theo inheritFrom nếu có, không thì nhãn gợi ý từ mô tả cột nguồn);
        /// khoá còn cập nhật kiểu và thông tin dò, giữ phần người dùng khai; khoá mất bị xoá khỏi danh sách (như wireframe), chỉ ghi vào Lost.
        /// Modified date: 27/10/2026 (không giữ trường mất cột)
        /// Cột hệ thống bị bỏ qua. Cột không có thông tin kiểu (dò tĩnh, không có CSDL) giữ kiểu đang lưu.
        /// </summary>
        /// <param name="current">Danh sách trường đang lưu của dòng SQL.</param>
        /// <param name="columns">Cột dò được từ câu SQL.</param>
        /// <param name="inheritFrom">Danh sách trường lấy phần người dùng khai cho khoá mới (VD trường mẫu chung khi dựng trường cho SQL riêng của đối tác).</param>
        public static SqlFieldMergeResult Merge(
            IReadOnlyList<SqlFieldItem>? current,
            IReadOnlyList<SqlColumnInfo> columns,
            IReadOnlyList<SqlFieldItem>? inheritFrom = null)
        {
            var currentMap = ToKeyMap(current);
            var inheritMap = ToKeyMap(inheritFrom);

            var fields = new List<SqlFieldItem>();
            var added = new List<string>();
            var lost = new List<string>();
            var typeChanged = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var col in columns.Where(c => !IsSystemColumn(c.Name)).OrderBy(c => c.Ordinal))
            {
                if (!seen.Add(col.Name)) continue;

                var hasDbInfo = !string.IsNullOrWhiteSpace(col.DbType);
                var source = !string.IsNullOrWhiteSpace(col.SourceTable) && !string.IsNullOrWhiteSpace(col.SourceColumn)
                    ? $"{col.SourceTable}.{col.SourceColumn}"
                    : null;

                if (currentMap.TryGetValue(col.Name, out var old))
                {
                    // Người dùng đã chọn kiểu thì giữ kiểu đó, không báo đổi kiểu
                    var newType = old.TypeOverride ?? (hasDbInfo ? MapDbType(col.DbType) : old.Type);
                    if (old.Lost)
                    {
                        added.Add(col.Name);
                    }
                    else if (!string.Equals(old.Type, newType, StringComparison.OrdinalIgnoreCase))
                    {
                        typeChanged.Add($"{col.Name}: {old.Type} → {newType}");
                    }

                    fields.Add(new SqlFieldItem
                    {
                        Key = col.Name,
                        Type = newType,
                        TypeOverride = old.TypeOverride,
                        DbType = hasDbInfo ? col.DbType : old.DbType,
                        Nullable = hasDbInfo ? col.IsNullable : old.Nullable,
                        Source = hasDbInfo ? source : old.Source,
                        Lost = false,
                        Label = old.Label,
                        Required = old.Required,
                        Skip = old.Skip,
                        Group = old.Group,
                        Remark = old.Remark
                    });
                    continue;
                }

                inheritMap.TryGetValue(col.Name, out var inherited);
                added.Add(col.Name);
                fields.Add(new SqlFieldItem
                {
                    Key = col.Name,
                    Type = inherited?.TypeOverride ?? MapDbType(col.DbType),
                    TypeOverride = inherited?.TypeOverride,
                    DbType = col.DbType,
                    Nullable = col.IsNullable,
                    Source = source,
                    Lost = false,
                    Label = inherited?.Label ?? Truncate(col.Description, LabelMaxLength),
                    Required = inherited?.Required ?? false,
                    Skip = false,
                    Group = inherited?.Group,
                    Remark = inherited?.Remark
                });
            }

            foreach (var old in current ?? [])
            {
                if (string.IsNullOrWhiteSpace(old.Key) || seen.Contains(old.Key)) continue;
                seen.Add(old.Key);

                // Như wireframe: trường không còn trong câu SQL bị xoá hẳn khỏi danh sách (không giữ dòng "mất cột");
                // vẫn trả khoá bị mất để nơi gọi hỏi xác nhận khi hồ sơ ánh xạ còn dùng.
                if (!old.Lost) lost.Add(old.Key);
            }

            return new SqlFieldMergeResult(fields, added, lost, typeChanged);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lập bảng tra trường theo khóa, không phân biệt hoa thường; khóa trùng giữ dòng đầu.
        /// Created date: 24/10/2026
        /// </summary>
        private static Dictionary<string, SqlFieldItem> ToKeyMap(IReadOnlyList<SqlFieldItem>? fields)
        {
            var map = new Dictionary<string, SqlFieldItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in fields ?? [])
            {
                if (!string.IsNullOrWhiteSpace(f.Key)) map.TryAdd(f.Key, f);
            }
            return map;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cắt khoảng trắng và cắt bớt theo độ dài tối đa; rỗng trả null.
        /// Created date: 24/10/2026
        /// </summary>
        private static string? Truncate(string? value, int maxLength)
        {
            var text = value?.Trim();
            if (string.IsNullOrEmpty(text)) return null;
            return text.Length <= maxLength ? text : text[..maxLength];
        }
    }
}
