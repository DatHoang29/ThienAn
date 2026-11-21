using System.Globalization;
using Module.ShareData.Core.Constants;
using DbType = System.Data.DbType;

namespace Module.ShareData.Core.Helpers
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ dựng tham số SQL trích xuất dùng chung giữa Worker và WebAPI (chạy thử, xem dữ liệu mẫu).
    ///              Không còn biến hệ thống: câu SQL chỉ dùng các biến đã khai ở bảng tham số câu truy vấn (ShareDataSqlVariable).
    /// Created date: 14/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    public static class SqlParameterHelper
    {
        /// <summary>
        /// Description: Mốc nối đuôi hiệu lực cho lượt chạy: mốc khóa đã lưu ở ShareDataLastSend (chưa có thì để trống).
        ///              Truy vấn chụp toàn bộ thì không có mốc. Không còn giới hạn quét lùi (bỏ MaxLookBackDays 26/10/2026).
        /// Modified date: 03/11/2026
        /// </summary>
        public static string? ResolveCursor(bool isSnapshot, string? lastKey) => isSnapshot ? null : lastKey ?? string.Empty;

        /// <summary>
        /// Description: Tham số cho các biến đã khai ở bảng tham số câu truy vấn (ShareDataSqlVariable).
        ///              Hỗ trợ biến mốc (Builtin lastKey) và giá trị nhập cố định (Const: String, Int, Decimal).
        /// Created date: 25/10/2026
        /// Modified date: 09/11/2026
        /// </summary>
        public static SugarParameter[] BuildParameters(SqlVariableSet? variables, SqlVariableContext context)
        {
            return variables == null ? [] : [.. variables.Items.Select(v => BuildVariableParameter(v, context))];
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dựng tham số cho một biến. Giá trị cố định đổi theo kiểu đã khai, đọc không được thì DBNull.
        ///              Biến mốc lấy mốc đang chạy; chưa có mốc thì dùng mốc khởi tạo của biến.
        /// Created date: 26/10/2026
        /// Modified date: 09/11/2026
        /// </summary>
        private static SugarParameter BuildVariableParameter(EffectiveSqlVariable variable, SqlVariableContext context)
        {
            var name = "@" + variable.Code;
            if (string.Equals(variable.ValueKind, ShareDataConst.SqlVariableKindConst, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(variable.DataType, ShareDataConst.SqlVariableTypeInt, StringComparison.OrdinalIgnoreCase))
                {
                    return int.TryParse(variable.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal)
                        ? new SugarParameter(name, intVal, DbType.Int32)
                        : new SugarParameter(name, DBNull.Value, DbType.Int32);
                }

                if (string.Equals(variable.DataType, ShareDataConst.SqlVariableTypeDecimal, StringComparison.OrdinalIgnoreCase))
                {
                    return decimal.TryParse(variable.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decVal)
                        ? new SugarParameter(name, decVal, DbType.Decimal)
                        : new SugarParameter(name, DBNull.Value, DbType.Decimal);
                }

                return new SugarParameter(name, variable.Value ?? string.Empty, DbType.String);
            }

            if (variable.ValueKind != ShareDataConst.SqlVariableKindBuiltin)
                return new SugarParameter(name, DBNull.Value);

            var source = variable.Value ?? string.Empty;
            // Biến lastKey_<nhóm> lấy mốc của nhóm đó; chưa có mốc thì dùng mốc khởi tạo của biến
            var group = SqlVariableSet.CursorGroupOf(variable.Code);
            var lastKey = context.LastKey;
            if (group != null)
            {
                lastKey = context.Groups != null && context.Groups.TryGetValue(group, out var groupKey) ? groupKey : null;
            }

            if (source.Equals(ShareDataConst.SqlVariableSourceLastKey, StringComparison.OrdinalIgnoreCase))
                return new SugarParameter(name, string.IsNullOrEmpty(lastKey) ? (variable.DefaultValue ?? string.Empty) : lastKey, DbType.String);
            return new SugarParameter(name, DBNull.Value);
        }

        /// <summary>
        /// Description: Khai kiểu các biến đã khai cho sys.dm_exec_describe_first_result_set (dò cột không chạy dữ liệu),
        ///              ví dụ "@since datetime, @afterKey nvarchar(max), @pageSize int". Không có biến nào thì trả null.
        /// Created date: 26/10/2026
        /// </summary>
        public static string? BuildDeclaration(SqlVariableSet? variables)
        {
            if (variables == null || variables.Items.Count == 0)
                return null;

            return string.Join(", ", variables.Items.Select(v =>
            {
                var sqlType = v.DataType switch
                {
                    var dt when string.Equals(dt, ShareDataConst.SqlVariableTypeDateTime, StringComparison.OrdinalIgnoreCase) => " datetime",
                    var dt when string.Equals(dt, ShareDataConst.SqlVariableTypeInt, StringComparison.OrdinalIgnoreCase) => " int",
                    var dt when string.Equals(dt, ShareDataConst.SqlVariableTypeDecimal, StringComparison.OrdinalIgnoreCase) => " decimal(38,10)",
                    _ => " nvarchar(max)"
                };
                return "@" + v.Code + sqlType;
            }));
        }
    }

    /// <summary>
    /// Giá trị lúc chạy dùng để tính biến mốc: mốc khóa nối đuôi của nhóm mặc định (ShareDataLastSend).
    /// Groups: mốc khóa của các nhóm mốc có tên (ShareDataLastSend có CursorKey) theo tên nhóm, không phân biệt hoa thường.
    /// </summary>
    public sealed record SqlVariableContext(
        string? LastKey,
        IReadOnlyDictionary<string, string?>? Groups = null);
}
