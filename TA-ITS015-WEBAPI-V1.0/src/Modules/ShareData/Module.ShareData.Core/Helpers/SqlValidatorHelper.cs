using System.Data;
using DbType = System.Data.DbType;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Furion.FriendlyException;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Exceptions;

namespace Module.ShareData.Core.Helpers
{
    /// <summary>
    /// Kết quả kiểm tra câu lệnh SQL với khóa thông điệp quốc tế hóa (BaseMsg) và tham số định dạng.
    /// </summary>
    public readonly record struct SqlValidationResult(bool IsValid, string? ErrorKey, object[]? ErrorArgs, string? ErrorMessage)
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Kết quả hợp lệ.
        /// Created date: 19/10/2026
        /// </summary>
        public static SqlValidationResult Success() => new(true, null, null, null);
        /// <summary>
        /// Author: DatHQ
        /// Description: Kết quả lỗi theo khóa thông báo; không truyền câu lỗi thì dùng bản tiếng Việt chuẩn.
        /// Created date: 19/10/2026
        /// </summary>
        public static SqlValidationResult Failure(string errorKey, object[]? errorArgs = null, string? fallbackMessage = null)
            => new(false, errorKey, errorArgs, fallbackMessage ?? SqlValidatorHelper.FormatVietnamese(errorKey, errorArgs));

        /// <summary>
        /// Author: DatHQ
        /// Description: Đổi kết quả lỗi thành ngoại lệ chuẩn.
        /// Created date: 28/10/2026
        /// </summary>
        public Exception ToException() => SqlValidatorHelper.ToException(ErrorKey, ErrorArgs);

        /// <summary>
        /// Author: DatHQ
        /// Description: Tách kết quả thành hợp lệ và câu lỗi.
        /// Created date: 19/10/2026
        /// </summary>
        public void Deconstruct(out bool isValid, out string? errorMessage)
        {
            isValid = IsValid;
            errorMessage = ErrorMessage;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tách kết quả thành hợp lệ, khóa lỗi và tham số lỗi.
        /// Created date: 19/10/2026
        /// </summary>
        public void Deconstruct(out bool isValid, out string? errorKey, out object[]? errorArgs)
        {
            isValid = IsValid;
            errorKey = ErrorKey;
            errorArgs = ErrorArgs;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tách kết quả thành hợp lệ, khóa lỗi, tham số lỗi và câu lỗi.
        /// Created date: 19/10/2026
        /// </summary>
        public void Deconstruct(out bool isValid, out string? errorKey, out object[]? errorArgs, out string? errorMessage)
        {
            isValid = IsValid;
            errorKey = ErrorKey;
            errorArgs = ErrorArgs;
            errorMessage = ErrorMessage;
        }

        public static implicit operator (bool IsValid, string? ErrorMessage)(SqlValidationResult res)
            => (res.IsValid, res.ErrorMessage);
    }

    /// <summary>
    /// Kết quả kiểm tra câu lệnh SQL và trích xuất danh sách cột với khóa thông điệp quốc tế hóa (BaseMsg).
    /// </summary>
    public readonly record struct SqlExtractionResult(bool IsValid, string? ErrorKey, object[]? ErrorArgs, string? ErrorMessage, List<string> Columns)
    {
        /// <summary>Thông tin chi tiết từng cột (kiểu, nguồn, mô tả). Khi không có kết nối CSDL chỉ có tên cột.</summary>
        public List<SqlColumnInfo> ColumnInfos { get; init; } = [];

        /// <summary>
        /// Author: HieuNV
        /// Description: Kết quả hợp lệ chỉ có tên cột, các thông tin khác của cột để trống.
        /// Created date: 26/10/2026
        /// </summary>
        public static SqlExtractionResult Success(List<string> columns) => new(true, null, null, null, columns)
        {
            ColumnInfos = columns.Select((c, i) => new SqlColumnInfo(i + 1, c, null, null, null, null, null)).ToList()
        };

        /// <summary>
        /// Author: HieuNV
        /// Description: Kết quả hợp lệ kèm thông tin chi tiết từng cột do CSDL mô tả.
        /// Created date: 26/10/2026
        /// </summary>
        public static SqlExtractionResult Success(List<SqlColumnInfo> columnInfos) => new(true, null, null, null, columnInfos.Select(c => c.Name).ToList())
        {
            ColumnInfos = columnInfos
        };
        /// <summary>
        /// Author: DatHQ
        /// Description: Kết quả lỗi theo khóa thông báo, danh sách cột rỗng.
        /// Created date: 19/10/2026
        /// </summary>
        public static SqlExtractionResult Failure(string errorKey, object[]? errorArgs = null, string? fallbackMessage = null)
            => new(false, errorKey, errorArgs, fallbackMessage ?? SqlValidatorHelper.FormatVietnamese(errorKey, errorArgs), []);

        /// <summary>
        /// Author: DatHQ
        /// Description: Đổi kết quả lỗi thành ngoại lệ chuẩn.
        /// Created date: 28/10/2026
        /// </summary>
        public Exception ToException() => SqlValidatorHelper.ToException(ErrorKey, ErrorArgs);

        /// <summary>
        /// Author: DatHQ
        /// Description: Tách kết quả thành hợp lệ, câu lỗi và danh sách cột.
        /// Created date: 19/10/2026
        /// </summary>
        public void Deconstruct(out bool isValid, out string? errorMessage, out List<string> columns)
        {
            isValid = IsValid;
            errorMessage = ErrorMessage;
            columns = Columns;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tách kết quả thành hợp lệ, khóa lỗi, tham số lỗi và danh sách cột.
        /// Created date: 19/10/2026
        /// </summary>
        public void Deconstruct(out bool isValid, out string? errorKey, out object[]? errorArgs, out List<string> columns)
        {
            isValid = IsValid;
            errorKey = ErrorKey;
            errorArgs = ErrorArgs;
            columns = Columns;
        }

        public static implicit operator (bool IsValid, string? ErrorMessage, List<string> Columns)(SqlExtractionResult res)
            => (res.IsValid, res.ErrorMessage, res.Columns);
    }

    /// <summary>
    /// Thông tin một cột trong kết quả câu SQL do CSDL mô tả (sys.dm_exec_describe_first_result_set).
    /// DbType, IsNullable, SourceTable, SourceColumn, Description là null khi không dò được trên CSDL hoặc cột là biểu thức.
    /// </summary>
    public sealed record SqlColumnInfo(
        int Ordinal,
        string Name,
        string? DbType,
        bool? IsNullable,
        string? SourceTable,
        string? SourceColumn,
        string? Description);

    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ kiểm tra cú pháp và an toàn câu truy vấn SQL (SQL Validator Helper) dùng chung cho WebAPI và Worker.
    ///              Dựa trên Microsoft.SqlServer.TransactSql.ScriptDom (AST Parser TSql160Parser).
    ///              Chỉ cho phép duy nhất câu lệnh đọc dữ liệu (SELECT), chặn multi-statement,
    ///              chặn DDL/DML, thủ tục hệ thống, bảng nhạy cảm và các tham số không hợp lệ.
    ///              Đồng thời trích xuất danh sách cột kết quả và tạo ngoại lệ từ kết quả kiểm tra.
    /// Created date: 13/10/2026
    /// Modified date: 28/10/2026
    /// </summary>
    public static class SqlValidatorHelper
    {
        /// <summary>
        /// Tạo ngoại lệ chuẩn từ BaseMsg cho lỗi kiểm tra câu SQL trích xuất.
        /// </summary>
        public static Exception ToException(string? errorKey, object[]? errorArgs = null)
        {
            var key = errorKey ?? BaseMsg.ShareData.Validation.SqlScriptInvalid;
            var args = errorArgs ?? [];
            return args.Length > 0 ? Oops.Oh(key, args) : Oops.Oh(key);
        }
        private static readonly HashSet<string> RestrictedTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "SysUser", "SysUserRole", "SysUserExtOrg", "SysRole", "SysRoleMenu", "SysRoleOrg",
            "SysTenant", "SysConfig", "SysConfigData", "SysConfigType", "SysOrg", "ShareDataPartner"
        };

        /// <summary>
        /// Câu mô tả cột kết quả: tên, kiểu, cho phép NULL, bảng/cột nguồn và mô tả cột nguồn (MS_Description) để gợi ý nhãn.
        /// Lỗi biên dịch câu SQL không ném ra mà nằm ở cột error_message của dòng kết quả.
        /// </summary>
        private const string DescribeResultSql = @"SELECT d.column_ordinal AS Ordinal, d.name AS Name, d.system_type_name AS DbType, d.is_nullable AS IsNullable,
       d.source_table AS SourceTable, d.source_column AS SourceColumn, CAST(ep.value AS nvarchar(512)) AS Description,
       d.error_number AS ErrorNumber, d.error_message AS ErrorMessage
FROM sys.dm_exec_describe_first_result_set(@tsql, @params, 1) d
LEFT JOIN sys.extended_properties ep
       ON ep.class = 1 AND ep.name = N'MS_Description'
      AND ep.major_id = OBJECT_ID(QUOTENAME(ISNULL(d.source_schema, N'dbo')) + N'.' + QUOTENAME(d.source_table))
      AND ep.minor_id = COLUMNPROPERTY(ep.major_id, d.source_column, 'ColumnId')
WHERE ISNULL(d.is_hidden, 0) = 0
ORDER BY d.column_ordinal";


        private sealed class IncrementalVariableVisitor(HashSet<string> cursorVariables) : TSqlFragmentVisitor
        {
            public bool HasIncrementalVariable { get; private set; }

            /// <summary>
            /// Author: DatHQ
            /// Description: Đánh dấu câu có dùng biến mốc.
            /// Created date: 19/10/2026
            /// </summary>
            public override void Visit(VariableReference node)
            {
                if (node?.Name != null && cursorVariables.Contains(node.Name.TrimStart('@')))
                {
                    HasIncrementalVariable = true;
                }
                base.Visit(node);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Gom cột và tên bảng trong một phần câu SQL, để biết công thức cột mốc lấy cột từ bảng nào.
        /// Created date: 06/11/2026
        /// </summary>
        private sealed class CursorSourceVisitor : TSqlFragmentVisitor
        {
            public List<ColumnReferenceExpression> Columns { get; } = [];
            public HashSet<string> Tables { get; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom cột có trong phần câu đang duyệt.
            /// Created date: 06/11/2026
            /// </summary>
            public override void Visit(ColumnReferenceExpression node)
            {
                Columns.Add(node);
                base.Visit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom tên bảng (bỏ lược đồ) có trong phần câu đang duyệt.
            /// Created date: 06/11/2026
            /// </summary>
            public override void Visit(NamedTableReference node)
            {
                var name = node.SchemaObject?.BaseIdentifier?.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    Tables.Add(name);
                }
                base.Visit(node);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Nhận diện câu SQL có phải là truy vấn nối đuôi hay không bằng AST ScriptDom.
        ///              Trả về true nếu câu SQL có tham chiếu biến đã khai lấy từ mốc lần gửi trước
        ///              (cursorVariableNames — xem SqlVariableSet.CursorNames); không phân biệt hoa thường, không tính chú thích hay chuỗi.
        /// Created date: 22/10/2026
        /// Modified date: 25/10/2026 (Bỏ biến hệ thống @lastTime/@lastKey; chỉ nhận biến đã khai lấy từ mốc lần gửi trước)
        /// </summary>
        public static bool IsIncrementalSql(string? sql, IReadOnlyCollection<string>? cursorVariableNames = null)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false;

            try
            {
                var parser = new TSql160Parser(true);
                using var reader = new StringReader(sql);
                var fragment = parser.Parse(reader, out var errors);

                if (errors != null && errors.Count > 0) return false;
                if (fragment == null) return false;

                if (cursorVariableNames == null || cursorVariableNames.Count == 0) return false;
                var cursorVariables = new HashSet<string>(cursorVariableNames, StringComparer.OrdinalIgnoreCase);
                var visitor = new IncrementalVariableVisitor(cursorVariables);
                fragment.Accept(visitor);
                return visitor.HasIncrementalVariable;
            }
            catch
            {
                return false;
            }
        }

        private sealed class AstSafetyVisitor : TSqlFragmentVisitor
        {
            private static readonly HashSet<string> BlockedFunctionNames = new(StringComparer.OrdinalIgnoreCase)
            {
                "OPENROWSET", "OPENDATASOURCE", "OPENQUERY", "OPENXML"
            };

            private readonly HashSet<string>? _allowedTables;
            private readonly HashSet<string> _cteNames;
            private readonly HashSet<string> _allowedParameters;

            public List<(string ErrorKey, object[]? ErrorArgs)> Violations { get; } = [];

            public AstSafetyVisitor(HashSet<string>? allowedTables, HashSet<string> cteNames, HashSet<string> allowedParameters)
            {
                _allowedTables = allowedTables;
                _cteNames = cteNames;
                _allowedParameters = allowedParameters;
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Chặn các hàm đọc nguồn ngoài và thủ tục hệ thống có tiền tố xp_, sp_.
            /// Created date: 19/10/2026
            /// </summary>
            public override void Visit(FunctionCall node)
            {
                var name = node.FunctionName?.Value ?? string.Empty;
                if (BlockedFunctionNames.Contains(name) ||
                    name.StartsWith("xp_", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("sp_", StringComparison.OrdinalIgnoreCase))
                {
                    Violations.Add((BaseMsg.ShareData.Validation.SqlFunctionCallBlocked, [name]));
                }
                base.Visit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Chặn nguồn dữ liệu OPENROWSET, OPENQUERY, OPENXML, OPENDATASOURCE.
            /// Created date: 19/10/2026
            /// </summary>
            public override void Visit(TableReference node)
            {
                var typeName = node.GetType().Name;
                if (typeName.Contains("OpenRowset", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("OpenQuery", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("OpenXml", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("OpenDataSource", StringComparison.OrdinalIgnoreCase))
                {
                    Violations.Add((BaseMsg.ShareData.Validation.SqlExternalDataSourcesBlocked, null));
                }
                base.Visit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Chặn lệnh EXEC.
            /// Created date: 19/10/2026
            /// </summary>
            public override void Visit(ExecuteStatement node)
            {
                Violations.Add((BaseMsg.ShareData.Validation.SqlExecuteStatementBlocked, null));
                base.Visit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Kiểm từng bảng được đọc: chặn truy cập chéo CSDL, bảng nhạy cảm và bảng ngoài danh mục cho phép.
            /// Created date: 19/10/2026
            /// </summary>
            public override void Visit(NamedTableReference node)
            {
                if (node.SchemaObject?.DatabaseIdentifier != null || node.SchemaObject?.ServerIdentifier != null)
                {
                    Violations.Add((BaseMsg.ShareData.Validation.SqlCrossDatabaseQueryBlocked, [node.SchemaObject?.BaseIdentifier?.Value ?? string.Empty]));
                }

                var schema = node.SchemaObject?.SchemaIdentifier?.Value;
                var table = node.SchemaObject?.BaseIdentifier?.Value;

                if (!string.IsNullOrWhiteSpace(schema) &&
                    (schema.Equals("sys", StringComparison.OrdinalIgnoreCase) ||
                     schema.Equals("information_schema", StringComparison.OrdinalIgnoreCase)))
                {
                    Violations.Add((BaseMsg.ShareData.Validation.SqlSystemSchemaBlocked, [schema]));
                }

                if (!string.IsNullOrWhiteSpace(table))
                {
                    var cleanTable = table.Trim('[', ']', '"');
                    if (RestrictedTables.Contains(cleanTable))
                    {
                        Violations.Add((BaseMsg.ShareData.Validation.SqlSensitiveTableBlocked, [cleanTable]));
                    }
                    else if (string.IsNullOrWhiteSpace(schema) && _cteNames.Contains(cleanTable))
                    {
                        base.Visit(node);
                        return;
                    }
                    else if (_allowedTables != null)
                    {
                        var isAllowedSchema = string.IsNullOrWhiteSpace(schema) ||
                            schema.Trim('[', ']', '"').Equals("dbo", StringComparison.OrdinalIgnoreCase);

                        if (!isAllowedSchema || !_allowedTables.Contains(cleanTable))
                        {
                            Violations.Add((BaseMsg.ShareData.Validation.SqlTableNotAllowed, [cleanTable]));
                        }
                    }
                }
                base.Visit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Chặn biến chưa khai trong bảng tham số.
            /// Created date: 19/10/2026
            /// </summary>
            public override void Visit(VariableReference node)
            {
                var paramName = node.Name?.TrimStart('@');
                if (!string.IsNullOrWhiteSpace(paramName) && !_allowedParameters.Contains(paramName))
                {
                    Violations.Add((BaseMsg.ShareData.Validation.SqlUnsupportedParameter, [paramName]));
                }
                base.Visit(node);
            }
        }

        private sealed class CteNameCollector : TSqlFragmentVisitor
        {
            private readonly HashSet<string> _cteNames;

            public CteNameCollector(HashSet<string> cteNames)
            {
                _cteNames = cteNames;
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom tên CTE để không coi là bảng ngoài danh mục.
            /// Created date: 19/10/2026
            /// </summary>
            public override void Visit(CommonTableExpression node)
            {
                var name = node.ExpressionName?.Value?.Trim('[', ']', '"');
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _cteNames.Add(name);
                }
                base.Visit(node);
            }
        }

        /// <summary>
        /// Description: Kiểm tra nhanh tính an toàn của câu lệnh SQL (không cần kết nối DB).
        ///              Dùng cho worker hoặc trước khi lưu câu SQL.
        /// </summary>
        public static SqlValidationResult Validate(string sql, SqlVariableSet? variables = null)
        {
            var res = ValidateAndExtractColumns(null, sql, variables: variables);
            if (!res.IsValid)
            {
                return SqlValidationResult.Failure(res.ErrorKey!, res.ErrorArgs, res.ErrorMessage);
            }
            return SqlValidationResult.Success();
        }

        /// <summary>
        /// Description: Kiểm tra tính an toàn của câu lệnh SQL bằng AST ScriptDom và trích xuất danh sách tên cột.
        ///              Nếu có ISqlSugarClient, mô tả cột kết quả trên DB bằng sys.dm_exec_describe_first_result_set (kèm kiểu, nguồn) ở ColumnInfos.
        ///              Nếu truyền allowedTables, chỉ cho phép truy vấn các bảng nằm trong tập danh mục đó (ngoài các CTE nội bộ).
        ///              variables: bộ biến đã khai ở bảng tham số câu truy vấn; câu SQL chỉ được dùng các biến này (không có biến hệ thống).
        /// Modified date: 25/10/2026 (Nhận biến đã khai ở ShareDataSqlVariable)
        /// </summary>
        public static SqlExtractionResult ValidateAndExtractColumns(
            ISqlSugarClient? db,
            string sql,
            IReadOnlySet<string>? allowedTables = null,
            SqlVariableSet? variables = null)
        {
            // Không có biến hệ thống: câu SQL chỉ được dùng biến đã khai ở bảng tham số câu truy vấn
            var allowedParameters = new HashSet<string>(variables?.Names ?? [], StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(sql))
            {
                return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlScriptEmpty);
            }

            // 1. Phân tích cú pháp AST bằng TSql160Parser
            var parser = new TSql160Parser(true);
            using var reader = new StringReader(sql);
            var fragment = parser.Parse(reader, out var errors);

            if (errors != null && errors.Count > 0)
            {
                var firstError = errors[0];
                return SqlExtractionResult.Failure(
                    BaseMsg.ShareData.Validation.SqlScriptSyntaxError,
                    [firstError.Line, firstError.Column, firstError.Message]);
            }

            if (fragment is not TSqlScript script)
            {
                return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlScriptInvalid);
            }

            if (script.Batches == null || script.Batches.Count != 1)
            {
                return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlMultipleBatchesNotAllowed);
            }

            var batch = script.Batches[0];
            if (batch.Statements == null || batch.Statements.Count != 1)
            {
                return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlMultipleStatementsNotAllowed);
            }

            var statement = batch.Statements[0];
            if (statement is not SelectStatement selectStatement)
            {
                return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlOnlySelectAllowed);
            }

            if (selectStatement.Into != null)
            {
                return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlSelectIntoNotAllowed);
            }

            // 2. Thu thập danh sách tên CTE trước (duyệt lượt 1) rồi kiểm tra các quy tắc an toàn tĩnh (duyệt lượt 2)
            var cteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            selectStatement.Accept(new CteNameCollector(cteNames));

            HashSet<string>? normalizedAllowedTables = allowedTables != null
                ? new HashSet<string>(allowedTables, StringComparer.OrdinalIgnoreCase)
                : null;

            var visitor = new AstSafetyVisitor(normalizedAllowedTables, cteNames, allowedParameters);
            selectStatement.Accept(visitor);

            if (visitor.Violations.Count > 0)
            {
                var firstViolation = visitor.Violations[0];
                return SqlExtractionResult.Failure(firstViolation.ErrorKey, firstViolation.ErrorArgs);
            }

            // 3. Mô tả cột kết quả trên CSDL nếu có kết nối (không chạy câu truy vấn)
            if (db != null)
            {
                return DescribeResultColumns(db, sql.Trim().TrimEnd(';'), variables);
            }

            // 4. Nếu không có kết nối DB: Phân tích danh sách cột tĩnh
            var staticColumns = ExtractColumnsStatically(sql.Trim().TrimEnd(';'));
            return SqlExtractionResult.Success(staticColumns);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Mô tả cột kết quả của câu SQL trên CSDL bằng sys.dm_exec_describe_first_result_set (thay cho SET FMTONLY ON):
        ///              lấy tên, kiểu, cho phép NULL, bảng/cột nguồn và mô tả cột nguồn. Cột không có tên hoặc trùng tên là lỗi,
        ///              vì tên cột chính là khoá trường của gói tin. Biến trong câu SQL khai kiểu theo bộ biến đã khai (ShareDataSqlVariable).
        /// Created date: 24/10/2026
        /// Modified date: 26/10/2026 (Khai biến theo bộ biến thay cho 5 biến hệ thống)
        /// </summary>
        private static SqlExtractionResult DescribeResultColumns(ISqlSugarClient db, string executableSql, SqlVariableSet? variables)
        {
            DataTable dt;
            try
            {
                var declaration = SqlParameterHelper.BuildDeclaration(variables);
                dt = db.Ado.GetDataTable(DescribeResultSql,
                    new SugarParameter("@tsql", executableSql, DbType.String) { Size = -1 },
                    new SugarParameter("@params", (object?)declaration ?? DBNull.Value, DbType.String) { Size = -1 });
            }
            catch (Exception ex)
            {
                return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlDatabaseExecutionError, [ex.Message]);
            }

            var columns = new List<SqlColumnInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                if (row["ErrorNumber"] is not DBNull)
                {
                    return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlDatabaseExecutionError, [row["ErrorMessage"]?.ToString() ?? string.Empty]);
                }

                var ordinal = row["Ordinal"] is DBNull ? columns.Count + 1 : Convert.ToInt32(row["Ordinal"]);
                var name = row["Name"] is DBNull ? null : row["Name"].ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlColumnUnnamed, [ordinal]);
                }

                // Gom hết tên trùng rồi báo một lần, mỗi tên chỉ ghi một lần dù trùng nhiều chỗ
                if (!seen.Add(name))
                {
                    if (!duplicates.Contains(name, StringComparer.OrdinalIgnoreCase)) duplicates.Add(name);
                    continue;
                }

                columns.Add(new SqlColumnInfo(
                    ordinal,
                    name,
                    row["DbType"] is DBNull ? null : row["DbType"].ToString(),
                    row["IsNullable"] is DBNull ? null : Convert.ToBoolean(row["IsNullable"]),
                    row["SourceTable"] is DBNull ? null : row["SourceTable"].ToString(),
                    row["SourceColumn"] is DBNull ? null : row["SourceColumn"].ToString(),
                    row["Description"] is DBNull ? null : row["Description"].ToString()));
            }

            if (duplicates.Count > 0)
            {
                return SqlExtractionResult.Failure(BaseMsg.ShareData.Validation.SqlColumnDuplicate, [string.Join(", ", duplicates)]);
            }

            if (columns.Count == 0)
            {
                return SqlExtractionResult.Success(ExtractColumnsStatically(executableSql));
            }

            return SqlExtractionResult.Success(columns);
        }

        /// <summary>
        /// Description: Kiểm tra cấu trúc câu lệnh SQL bắt buộc cho chế độ đồng bộ nối đuôi (Incremental).
        ///              Bắt buộc ORDER BY ngoài cùng không DESC và cột __lastKey. TOP không bắt buộc:
        ///              có TOP / OFFSET-FETCH thì Worker lặp nhiều trang mỗi lượt (HasRowLimit), không có thì một lần lấy hết.
        ///              Có bộ biến (variables) thì kiểm thêm từng nhóm mốc có tên, điều kiện WHERE so cột mốc với biến mốc (29/10/2026)
        ///              và cột ORDER BY ngoài cùng ở nhánh của từng nhóm.
        /// Modified date: 04/11/2026
        /// </summary>
        public static SqlValidationResult ValidateIncrementalStructure(string sql, List<string> columns, SqlVariableSet? variables = null)
        {
            var parser = new TSql160Parser(true);
            using var reader = new StringReader(sql);
            var fragment = parser.Parse(reader, out var errors);
            if (errors != null && errors.Count > 0)
            {
                return SqlValidationResult.Failure(
                    BaseMsg.ShareData.Validation.SqlScriptSyntaxError,
                    [errors[0].Line, errors[0].Column, errors[0].Message]);
            }

            if (fragment is not TSqlScript script || script.Batches == null || script.Batches.Count != 1 ||
                script.Batches[0].Statements == null || script.Batches[0].Statements.Count != 1 ||
                script.Batches[0].Statements[0] is not SelectStatement selectStmt)
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlMultipleStatementsNotAllowed);
            }

            var querySpec = selectStmt.QueryExpression as QuerySpecification;

            var hasOrderBy = querySpec?.OrderByClause != null;

            if (!hasOrderBy)
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalMissingOrderBy);
            }

            if (querySpec?.OrderByClause?.OrderByElements != null)
            {
                var hasDesc = querySpec.OrderByClause.OrderByElements.Any(e => e.SortOrder == SortOrder.Descending);
                if (hasDesc)
                {
                    return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalOrderByDescNotAllowed);
                }
            }

            // Nhóm mốc mặc định (cột __lastKey không hậu tố): câu chỉ dùng biến của nhóm có tên thì không cần
            var usesDefault = variables == null || IsIncrementalSql(sql, variables.DefaultCursorNames);
            if (usesDefault)
            {
                var defaultResult = ValidateDefaultCursor(querySpec!, columns);
                if (!defaultResult.IsValid)
                {
                    return defaultResult;
                }
            }

            if (variables == null)
            {
                return SqlValidationResult.Success();
            }

            var groupResult = ValidateCursorGroups(selectStmt, columns, variables);
            if (!groupResult.IsValid)
            {
                return groupResult;
            }

            var groupOrderResult = ValidateGroupOrder(selectStmt, querySpec!, variables);
            if (!groupOrderResult.IsValid)
            {
                return groupOrderResult;
            }

            return usesDefault
                ? ValidateCursorFilter(selectStmt, variables.LastKeyNames, "__lastKey")
                : SqlValidationResult.Success();
        }

        /// <summary>Cột và ORDER BY ngoài cùng của nhóm mốc mặc định: bắt buộc cột __lastKey, sắp trước hết theo cột mốc.</summary>
        private static SqlValidationResult ValidateDefaultCursor(QuerySpecification querySpec, List<string> columns)
        {
            var hasLastKey = columns.Any(c => c.Equals("__lastKey", StringComparison.OrdinalIgnoreCase));
            if (!hasLastKey)
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalMissingLastKey);
            }

            // Worker lấy mốc từ dòng cuối trang: trang phải sắp theo __lastKey, sắp theo cột khác thì dòng cuối trang không phải mốc lớn nhất và sẽ bỏ sót dòng.
            var orderBy = querySpec.OrderByClause!.OrderByElements;
            if (orderBy.Count < 1 || !IsOrderByColumn(orderBy[0].Expression, "__lastKey", querySpec))
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalOrderByKeyMismatch);
            }

            return SqlValidationResult.Success();
        }

        /// <summary>
        /// Nhóm mốc có tên của câu gộp nhiều nguồn: biến @lastKey_&lt;nhóm&gt; lấy nguồn mốc khóa, câu có cột __lastKey_&lt;nhóm&gt;,
        /// điều kiện WHERE của nguồn so cột mốc của nhóm với biến mốc.
        /// Modified date: 03/11/2026
        /// </summary>
        private static SqlValidationResult ValidateCursorGroups(SelectStatement statement, List<string> columns, SqlVariableSet variables)
        {
            foreach (var group in variables.CursorGroups)
            {
                var keyVariable = $"lastKey_{group}";
                var keyColumn = $"__lastKey_{group}";
                if (!columns.Contains(keyColumn, StringComparer.OrdinalIgnoreCase))
                {
                    return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalGroupColumnMissing, [group, $"[{keyColumn}]"]);
                }

                var filter = ValidateCursorFilter(statement, [keyVariable], keyColumn);
                if (!filter.IsValid)
                {
                    return filter;
                }
            }

            return SqlValidationResult.Success();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: ORDER BY ngoài cùng của câu có nhóm mốc có tên: ở nhánh của từng nhóm (SELECT có WHERE dùng @lastKey_&lt;nhóm&gt;),
        ///              cột đang sắp phải cùng công thức với cột mốc __lastKey_&lt;nhóm&gt;. Sắp theo cột khác thì dòng của nguồn đó
        ///              không ra đúng thứ tự mốc, dòng cuối trang không phải mốc lớn nhất và lượt sau bỏ sót dòng.
        ///              ORDER BY không phải tên cột, hoặc không dò được cột ở nhánh (SELECT *, đổi tên qua lớp con) thì không so.
        /// Created date: 04/11/2026
        /// </summary>
        private static SqlValidationResult ValidateGroupOrder(SelectStatement statement, QuerySpecification querySpec, SqlVariableSet variables)
        {
            var orderBy = querySpec.OrderByClause?.OrderByElements;
            if (variables.CursorGroups.Count == 0 || orderBy is not { Count: > 0 }
                || StripParentheses(orderBy[0].Expression) is not ColumnReferenceExpression { MultiPartIdentifier.Identifiers: { Count: > 0 } parts })
            {
                return SqlValidationResult.Success();
            }

            // ORDER BY trỏ tới bí danh của SELECT ngoài cùng thì lấy tên cột gốc của bí danh đó
            var orderColumn = parts[^1].Value;
            var outer = parts.Count == 1
                ? querySpec.SelectElements.OfType<SelectScalarExpression>()
                    .FirstOrDefault(e => string.Equals(e.ColumnName?.Value, orderColumn, StringComparison.OrdinalIgnoreCase))
                : null;
            if (outer != null)
            {
                if (StripParentheses(outer.Expression) is not ColumnReferenceExpression { MultiPartIdentifier.Identifiers: { Count: > 0 } source })
                {
                    return SqlValidationResult.Success();
                }
                orderColumn = source[^1].Value;
            }

            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);
            foreach (var group in variables.CursorGroups)
            {
                var keyVariable = $"lastKey_{group}";
                var homes = specs.Specifications.Where(s => s.WhereClause?.SearchCondition != null
                    && References(s.WhereClause.SearchCondition).Variables.Contains(keyVariable));
                foreach (var home in homes)
                {
                    var orderExpr = specs.Projection(home, orderColumn);
                    var keyExpr = specs.Projection(home, $"__lastKey_{group}");
                    if (orderExpr != null && keyExpr != null
                        && NormalizeCursorExpression(StripParentheses(orderExpr)) != NormalizeCursorExpression(StripParentheses(keyExpr)))
                    {
                        return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalGroupOrderInvalid, [group, orderColumn]);
                    }
                }
            }

            return SqlValidationResult.Success();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm cột mốc khi lưu câu nối đuôi qua API (Worker không gọi; câu đã lưu thì Worker chỉ ghi rủi ro):
        ///              cột mốc chỉ là một cột ngày giờ (UpdateTime AS __lastKey) thì sót các dòng trùng giờ, phải ghép thêm '_' + ID;
        ///              công thức mốc dùng UpdateTime trơn thì dòng mới thêm (UpdateTime trống khi thêm mới) không được gửi, phải bọc
        ///              COALESCE(UpdateTime, CreateTime), trừ câu có nhánh riêng lọc UpdateTime IS NULL (mẫu hai nhóm upd / new);
        ///              ghép ngày giờ không sắp đúng thứ tự thời gian (FORMAT, CONVERT sai kiểu) thì có thể bỏ sót dòng;
        ///              cột mốc khác công thức CONVERT(varchar(8), &lt;thời gian có CreateTime&gt;, 112) + REPLACE(CONVERT(varchar(12), &lt;thời gian&gt;, 114), ':', '') + '_' + &lt;đuôi&gt;
        ///              thì bị từ chối: cột chuỗi trơn, ép số sang chuỗi, cột thời gian có thể trống, đuôi có thể NULL;
        ///              OFFSET bỏ qua dòng thì các dòng đó không bao giờ được gửi;
        ///              gộp nhiều nguồn bằng UNION mà dùng chung một mốc thì nguồn về trễ mất dòng, phải dùng nhóm mốc;
        ///              câu con có TOP sắp theo cột khác cột mốc thì dòng có mốc nhỏ hơn bị bỏ sót;
        ///              so đúng biểu thức cột mốc bằng >= thì dòng cuối bị gửi lặp mỗi lượt, phải dùng >.
        /// Created date: 04/11/2026
        /// Modified date: 16/11/2026
        /// </summary>
        public static SqlValidationResult ValidateCursorTime(string sql, IReadOnlyList<SqlColumnInfo> columnInfos, SqlVariableSet variables)
        {
            var fragment = new TSql160Parser(true).Parse(new StringReader(sql), out var errors);
            var statement = errors is { Count: > 0 } || fragment is not TSqlScript script
                ? null
                : script.Batches.SelectMany(b => b.Statements).OfType<SelectStatement>().FirstOrDefault();
            if (statement == null)
            {
                return SqlValidationResult.Success();
            }

            var timeOnly = CursorColumns(variables).Select(c => c.KeyColumn).FirstOrDefault(column =>
                columnInfos.FirstOrDefault(c => string.Equals(c.Name, column, StringComparison.OrdinalIgnoreCase))?.DbType is { } dbType
                && Regex.IsMatch(dbType, @"^(date|datetime|datetime2|smalldatetime|datetimeoffset|time)\b", RegexOptions.IgnoreCase));
            if (timeOnly != null)
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalKeyTimeOnly, [timeOnly]);
            }

            var bareUpdateTime = BareUpdateTimeCursor(statement, variables);
            if (bareUpdateTime != null)
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalUpdateTimeEmpty, [bareUpdateTime]);
            }

            var unsortableDate = UnsortableDateCursor(statement, variables);
            if (unsortableDate != null)
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalKeyDateUnsortable, [unsortableDate]);
            }

            var invalidFormula = InvalidFormulaCursor(statement, variables);
            if (invalidFormula != null)
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalKeyFormulaInvalid, [invalidFormula]);
            }

            if (HasRowSkippingOffset(statement))
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalOffsetNotAllowed);
            }

            if (SharesCursorAcrossSources(statement, variables))
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalUnionOneCursor);
            }

            if (HasInnerTopOffCursor(statement, variables))
            {
                return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalInnerTopOrderInvalid);
            }

            var inclusiveColumn = InclusiveCursorColumn(statement, variables);
            return inclusiveColumn == null
                ? SqlValidationResult.Success()
                : SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInclusive, [inclusiveColumn]);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm câu có cột mốc khi lưu qua API: có cột __lastKey hoặc __lastKey_&lt;nhóm&gt; mà không dùng biến mốc nào thì không phải câu nối đuôi,
        ///              Worker gửi như câu gửi toàn bộ, không ghi mốc nên lượt nào cũng gửi lại cùng dữ liệu. Câu đã lưu từ trước vẫn chạy.
        /// Created date: 05/11/2026
        /// </summary>
        public static SqlValidationResult ValidateCursorColumnUsed(string sql, IReadOnlyCollection<string> columns, SqlVariableSet variables)
        {
            var cursorColumn = columns.FirstOrDefault(c => Regex.IsMatch(c, @"^__lastKey(_[A-Za-z0-9_]+)?$", RegexOptions.IgnoreCase));
            return cursorColumn == null || IsIncrementalSql(sql, variables.CursorNames)
                ? SqlValidationResult.Success()
                : SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalCursorVariableMissing, [cursorColumn]);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cột mốc đầu tiên sai công thức PA1 ở SELECT lọc theo biến mốc của cột đó. Không dò được biểu thức của cột thì không xét. Không có thì trả null.
        /// Created date: 05/11/2026
        /// Modified date: 16/11/2026
        /// </summary>
        private static string? InvalidFormulaCursor(SelectStatement statement, SqlVariableSet variables)
        {
            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);

            foreach (var (keyColumn, keyVariables) in CursorColumns(variables))
            {
                var homes = specs.Specifications.Where(s => s.WhereClause?.SearchCondition != null
                    && References(s.WhereClause.SearchCondition).Variables.Overlaps(keyVariables));
                foreach (var home in homes)
                {
                    var keyExpr = specs.ResolveProjection(home, keyColumn);
                    if (keyExpr == null)
                    {
                        continue;
                    }

                    var time = CursorFormulaTime(keyExpr);
                    if (time == null || !IsCursorTimeCovered(time) || !HasNonNullKeyTail(specs, keyExpr))
                    {
                        return keyColumn;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đuôi sau '_' của công thức mốc không thể NULL: mỗi phần ghép là chuỗi hằng, cột ID của bảng không ở phía tùy chọn của OUTER JOIN / OUTER APPLY,
        ///              hoặc ISNULL / COALESCE có tham số cuối là chuỗi hằng; ít nhất một phần lấy từ cột. Không xét đuôi có phân biệt được mọi dòng hay không.
        ///              Cột khác, hoặc ID của bảng phía tùy chọn, có thể NULL làm cả chuỗi mốc thành NULL và dòng đó không bao giờ được gửi.
        /// Created date: 05/11/2026
        /// Modified date: 16/11/2026
        /// </summary>
        private static bool HasNonNullKeyTail(QuerySpecificationCollector specs, ScalarExpression keyExpr)
        {
            var parts = new List<ScalarExpression>();
            FlattenConcat(keyExpr, parts);
            var keyParts = parts.Skip(3).ToList();
            if (!keyParts.Any(part => References(part).HasColumn))
            {
                return false;
            }

            var owner = specs.OwnerOf(keyExpr);
            var optionalTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in owner?.FromClause?.TableReferences ?? [])
            {
                CollectOptionalTables(table, false, optionalTables);
            }

            return keyParts.All(part => part is StringLiteral
                || HasStringFallback(part)
                || IsIdColumn(part) && !IsFromTable(part, optionalTables));
        }

        /// <summary>ISNULL(..., '&lt;hằng&gt;') hoặc COALESCE(..., '&lt;hằng&gt;'): tham số cuối là chuỗi hằng nên phần ghép không thể NULL.</summary>
        private static bool HasStringFallback(ScalarExpression expression)
        {
            var fallback = expression switch
            {
                CoalesceExpression coalesce => coalesce.Expressions.LastOrDefault(),
                FunctionCall { Parameters.Count: 2 } isNull when StringComparer.OrdinalIgnoreCase.Equals(isNull.FunctionName?.Value, "ISNULL") => isNull.Parameters[1],
                _ => null
            };
            return fallback != null && StripParentheses(fallback) is StringLiteral;
        }

        /// <summary>Biểu thức là cột tên ID, có hoặc không có tiền tố bảng.</summary>
        private static bool IsIdColumn(ScalarExpression expression) =>
            expression is ColumnReferenceExpression { MultiPartIdentifier.Identifiers: { Count: > 0 } parts }
            && string.Equals(parts[^1].Value, "ID", StringComparison.OrdinalIgnoreCase);

        /// <summary>Cột có tiền tố là một trong các bảng đã cho.</summary>
        private static bool IsFromTable(ScalarExpression expression, HashSet<string> tables) =>
            expression is ColumnReferenceExpression { MultiPartIdentifier.Identifiers: { Count: > 1 } parts }
            && tables.Contains(parts[^2].Value);

        /// <summary>
        /// Author: DatHQ
        /// Description: Gom bí danh các bảng ở phía tùy chọn của OUTER JOIN, OUTER APPLY: bảng đó có thể không có dòng khớp nên mọi cột của nó có thể trống.
        /// Created date: 05/11/2026
        /// </summary>
        private static void CollectOptionalTables(TableReference table, bool optional, HashSet<string> tables)
        {
            switch (table)
            {
                case QualifiedJoin join:
                    CollectOptionalTables(join.FirstTableReference, optional || join.QualifiedJoinType is QualifiedJoinType.RightOuter or QualifiedJoinType.FullOuter, tables);
                    CollectOptionalTables(join.SecondTableReference, optional || join.QualifiedJoinType is QualifiedJoinType.LeftOuter or QualifiedJoinType.FullOuter, tables);
                    break;
                case UnqualifiedJoin join:
                    CollectOptionalTables(join.FirstTableReference, optional, tables);
                    CollectOptionalTables(join.SecondTableReference, optional || join.UnqualifiedJoinType == UnqualifiedJoinType.OuterApply, tables);
                    break;
                case JoinParenthesisTableReference parenthesis:
                    CollectOptionalTables(parenthesis.Join, optional, tables);
                    break;
                case TableReferenceWithAlias named when optional:
                    var alias = named.Alias?.Value ?? (named as NamedTableReference)?.SchemaObject?.BaseIdentifier?.Value;
                    if (alias != null)
                    {
                        tables.Add(alias);
                    }
                    break;
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu có OFFSET khác 0 ở bất kỳ cấp nào: mỗi trang bỏ qua vài dòng đầu mà mốc vẫn tiến qua, các dòng đó không bao giờ được gửi.
        /// Created date: 05/11/2026
        /// </summary>
        private static bool HasRowSkippingOffset(SelectStatement statement)
        {
            var visitor = new OffsetVisitor();
            statement.Accept(visitor);
            return visitor.Found;
        }

        /// <summary>Duyệt câu SQL: đánh dấu có OFFSET khác 0.</summary>
        private sealed class OffsetVisitor : TSqlFragmentVisitor
        {
            public bool Found { get; private set; }

            /// <summary>
            /// Author: DatHQ
            /// Description: OFFSET khác số 0 thì đánh dấu đã có.
            /// Created date: 05/11/2026
            /// </summary>
            public override void Visit(OffsetClause node)
            {
                if (StripParentheses(node.OffsetExpression) is not IntegerLiteral { Value: "0" })
                {
                    Found = true;
                }
                base.Visit(node);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Biến mốc của nhóm mặc định dùng cho câu gộp nhiều nguồn: nhánh UNION so với biến đó, hoặc câu so với biến đó đọc từ bảng dẫn xuất, CTE là UNION.
        ///              Một mốc chung thì nguồn về trễ có giá trị nhỏ hơn mốc sẽ mất dòng. Câu con UNION chỉ dùng để lọc (IN) không tính.
        /// Created date: 05/11/2026
        /// </summary>
        private static bool SharesCursorAcrossSources(SelectStatement statement, SqlVariableSet variables)
        {
            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);
            var unionCtes = statement.WithCtesAndXmlNamespaces?.CommonTableExpressions
                .Where(cte => IsUnionQuery(cte.QueryExpression))
                .Select(cte => cte.ExpressionName.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

            return specs.Specifications
                .Where(spec => spec.WhereClause?.SearchCondition != null
                    && References(spec.WhereClause.SearchCondition).Variables.Overlaps(variables.DefaultCursorNames))
                .Any(spec =>
                {
                    var source = new UnionSourceVisitor(unionCtes);
                    spec.FromClause?.Accept(source);
                    return specs.IsUnionBranch(spec) || source.Found;
                });
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu con trong nguồn dòng (bảng dẫn xuất, CTE, nhánh UNION) có TOP hoặc FETCH mà không sắp tăng dần theo cột mốc của chính nó
        ///              (hoặc theo phần giờ của công thức PA1 kèm TOP ... WITH TIES):
        ///              câu con chọn dòng theo thứ tự khác nên dòng có mốc nhỏ hơn mốc cuối trang bị bỏ lại rồi bị bỏ sót ở lượt sau.
        ///              Câu con ở cột SELECT, WHERE hoặc bên phải APPLY chỉ lấy giá trị phụ cho từng dòng nên không xét.
        /// Created date: 06/11/2026
        /// Modified date: 16/11/2026
        /// </summary>
        private static bool HasInnerTopOffCursor(SelectStatement statement, SqlVariableSet variables)
        {
            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);
            var ctes = new Dictionary<string, QueryExpression>(StringComparer.OrdinalIgnoreCase);
            foreach (var cte in statement.WithCtesAndXmlNamespaces?.CommonTableExpressions ?? [])
            {
                ctes.TryAdd(cte.ExpressionName.Value, cte.QueryExpression);
            }

            var sources = new List<QuerySpecification>();
            CollectRowSources(statement.QueryExpression, ctes, sources, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            var keyColumns = CursorColumns(variables).Select(c => c.KeyColumn).ToList();
            return sources
                .Where(spec => spec != statement.QueryExpression && (spec.TopRowFilter != null || spec.OffsetClause?.FetchExpression != null))
                .Any(spec => IsInnerTopOffCursorSpec(spec, specs, keyColumns));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu con có TOP / FETCH không sắp tăng dần theo cột mốc, hoặc sắp theo phần giờ của công thức PA1 nhưng thiếu WITH TIES.
        /// Created date: 16/11/2026
        /// </summary>
        private static bool IsInnerTopOffCursorSpec(QuerySpecification spec, QuerySpecificationCollector specs, IReadOnlyCollection<string> keyColumns)
        {
            var first = spec.OrderByClause?.OrderByElements.FirstOrDefault();
            if (first == null || first.SortOrder == SortOrder.Descending)
            {
                return true;
            }

            foreach (var column in keyColumns)
            {
                var keyExpr = specs.Projection(spec, column);
                if (keyExpr == null)
                {
                    continue;
                }

                if (IsOrderByColumn(first.Expression, column, spec))
                {
                    return false;
                }

                if (spec.TopRowFilter is { WithTies: true } && IsCursorTimeOrder(first.Expression, keyExpr, spec, specs))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Biểu thức sắp xếp khớp phần giờ (2 hạng tử đầu CONVERT 112 + REPLACE CONVERT 114) của công thức cột mốc PA1.
        /// Created date: 16/11/2026
        /// </summary>
        private static bool IsCursorTimeOrder(ScalarExpression orderExpr, ScalarExpression keyExpr, QuerySpecification spec, QuerySpecificationCollector specs)
        {
            if (CursorFormulaTime(keyExpr) == null)
            {
                return false;
            }

            var resolvedOrder = orderExpr;
            var strippedOrder = StripParentheses(orderExpr);
            if (strippedOrder is ColumnReferenceExpression { MultiPartIdentifier.Identifiers: [var aliasId] })
            {
                var projected = specs.Projection(spec, aliasId.Value);
                if (projected != null)
                {
                    resolvedOrder = projected;
                }
            }

            var orderParts = new List<ScalarExpression>();
            FlattenConcat(resolvedOrder, orderParts);
            var orderTimeParts = FormulaTimeParts(resolvedOrder);
            var keyTimeParts = FormulaTimeParts(keyExpr);
            if (orderTimeParts == null || keyTimeParts == null || orderParts.Count != 2)
            {
                return false;
            }

            var orderTarget = NormalizeCursorExpression(orderTimeParts.Value.Date.Parameter);
            var keyTarget = NormalizeCursorExpression(keyTimeParts.Value.Date.Parameter);
            var orderDateType = ((SqlDataTypeReference)orderTimeParts.Value.Date.DataType).SqlDataTypeOption;
            var keyDateType = ((SqlDataTypeReference)keyTimeParts.Value.Date.DataType).SqlDataTypeOption;
            var orderTimeType = ((SqlDataTypeReference)orderTimeParts.Value.Time.DataType).SqlDataTypeOption;
            var keyTimeType = ((SqlDataTypeReference)keyTimeParts.Value.Time.DataType).SqlDataTypeOption;
            return orderTarget == keyTarget && orderDateType == keyDateType && orderTimeType == keyTimeType;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Gom các SELECT là nguồn dòng của câu: chính câu, nhánh UNION, bảng dẫn xuất và CTE được đọc ở FROM, JOIN; bỏ bên phải APPLY.
        /// Created date: 06/11/2026
        /// </summary>
        private static void CollectRowSources(QueryExpression? query, Dictionary<string, QueryExpression> ctes, List<QuerySpecification> sources, HashSet<string> visitedCtes)
        {
            switch (query)
            {
                case QuerySpecification spec:
                    sources.Add(spec);
                    foreach (var table in spec.FromClause?.TableReferences ?? [])
                    {
                        CollectTableRowSources(table, ctes, sources, visitedCtes);
                    }
                    break;
                case BinaryQueryExpression binary:
                    CollectRowSources(binary.FirstQueryExpression, ctes, sources, visitedCtes);
                    CollectRowSources(binary.SecondQueryExpression, ctes, sources, visitedCtes);
                    break;
                case QueryParenthesisExpression parenthesis:
                    CollectRowSources(parenthesis.QueryExpression, ctes, sources, visitedCtes);
                    break;
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Nguồn dòng trong một bảng ở FROM: hai phía JOIN, bảng dẫn xuất, CTE (mỗi CTE một lần); APPLY chỉ lấy bên trái.
        /// Created date: 06/11/2026
        /// </summary>
        private static void CollectTableRowSources(TableReference table, Dictionary<string, QueryExpression> ctes, List<QuerySpecification> sources, HashSet<string> visitedCtes)
        {
            switch (table)
            {
                case QualifiedJoin join:
                    CollectTableRowSources(join.FirstTableReference, ctes, sources, visitedCtes);
                    CollectTableRowSources(join.SecondTableReference, ctes, sources, visitedCtes);
                    break;
                case UnqualifiedJoin join:
                    CollectTableRowSources(join.FirstTableReference, ctes, sources, visitedCtes);
                    if (join.UnqualifiedJoinType is not (UnqualifiedJoinType.CrossApply or UnqualifiedJoinType.OuterApply))
                    {
                        CollectTableRowSources(join.SecondTableReference, ctes, sources, visitedCtes);
                    }
                    break;
                case JoinParenthesisTableReference parenthesis:
                    CollectTableRowSources(parenthesis.Join, ctes, sources, visitedCtes);
                    break;
                case QueryDerivedTable derived:
                    CollectRowSources(derived.QueryExpression, ctes, sources, visitedCtes);
                    break;
                case NamedTableReference named when named.SchemaObject?.BaseIdentifier?.Value is { } name
                    && ctes.TryGetValue(name, out var cteQuery) && visitedCtes.Add(name):
                    CollectRowSources(cteQuery, ctes, sources, visitedCtes);
                    break;
            }
        }

        /// <summary>Câu là UNION, kể cả khi nằm trong ngoặc.</summary>
        private static bool IsUnionQuery(QueryExpression query) => query switch
        {
            BinaryQueryExpression { BinaryQueryExpressionType: BinaryQueryExpressionType.Union } => true,
            QueryParenthesisExpression parenthesis => IsUnionQuery(parenthesis.QueryExpression),
            _ => false
        };

        /// <summary>Duyệt mệnh đề FROM: đánh dấu có đọc từ bảng dẫn xuất là UNION hoặc CTE là UNION.</summary>
        private sealed class UnionSourceVisitor(HashSet<string> unionCtes) : TSqlFragmentVisitor
        {
            public bool Found { get; private set; }

            /// <summary>
            /// Author: DatHQ
            /// Description: Bảng dẫn xuất là UNION thì đánh dấu đã có.
            /// Created date: 05/11/2026
            /// </summary>
            public override void Visit(QueryDerivedTable node)
            {
                if (IsUnionQuery(node.QueryExpression))
                {
                    Found = true;
                }
                base.Visit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Tên bảng trùng CTE là UNION thì đánh dấu đã có.
            /// Created date: 05/11/2026
            /// </summary>
            public override void Visit(NamedTableReference node)
            {
                if (node.SchemaObject?.BaseIdentifier?.Value is { } name && unionCtes.Contains(name))
                {
                    Found = true;
                }
                base.Visit(node);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tên biến đặt số dòng của trang: TOP (@biến) ngoài cùng (không PERCENT, không WITH TIES),
        ///              và nếu câu con cũng đặt TOP (@biến) (như TOP (@pageSize) WITH TIES ở câu hai tầng) thì phải dùng cùng biến đó. Không có thì trả null.
        /// Created date: 05/11/2026
        /// Modified date: 16/11/2026
        /// </summary>
        public static string? PageSizeVariable(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return null;
            }

            var fragment = new TSql160Parser(true).Parse(new StringReader(sql), out var errors);
            var statement = errors is { Count: > 0 } || fragment is not TSqlScript script
                ? null
                : script.Batches.SelectMany(b => b.Statements).OfType<SelectStatement>().FirstOrDefault();
            if (statement == null)
            {
                return null;
            }

            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);
            var variableTops = specs.Specifications
                .Select(spec => spec.TopRowFilter)
                .Where(top => TopVariableName(top) != null)
                .ToList();
            if (variableTops.Count == 0 || variableTops.Any(top => top!.Percent))
            {
                return null;
            }

            if (variableTops is [{ WithTies: false } single])
            {
                return TopVariableName(single);
            }

            var outer = statement.QueryExpression as QuerySpecification;
            var outerName = outer?.TopRowFilter is { Percent: false, WithTies: false } outerTop
                ? TopVariableName(outerTop)
                : null;
            if (outerName == null)
            {
                return null;
            }

            var allMatchOuter = variableTops.All(top =>
            {
                var name = TopVariableName(top);
                return string.Equals(name, outerName, StringComparison.OrdinalIgnoreCase);
            });
            return allMatchOuter ? outerName : null;
        }

        /// <summary>Tên biến đặt ở mệnh đề TOP (bỏ @); TOP dùng hằng số hoặc biểu thức khác thì trả null.</summary>
        private static string? TopVariableName(TopRowFilter? top)
        {
            if (top == null)
            {
                return null;
            }

            var expr = StripParentheses(top.Expression);
            return expr is VariableReference variable ? variable.Name.TrimStart('@') : null;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Phần thời gian T của công thức PA1 CONVERT(varchar(8), T, 112) + REPLACE(CONVERT(varchar(12), T, 114), ':', '') + '_' + &lt;đuôi&gt;;
        ///              đuôi là phần còn lại, phải có cột dữ liệu. Không đúng dạng này thì trả null.
        /// Created date: 05/11/2026
        /// Modified date: 16/11/2026
        /// </summary>
        private static ScalarExpression? CursorFormulaTime(ScalarExpression keyExpr)
        {
            var parts = new List<ScalarExpression>();
            FlattenConcat(keyExpr, parts);
            var timeParts = FormulaTimeParts(keyExpr);
            if (timeParts == null
                || parts.Count < 4
                || parts[2] is not StringLiteral { Value: "_" }
                || !parts.Skip(3).Any(part => References(part).HasColumn))
            {
                return null;
            }

            return timeParts.Value.Date.Parameter;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Hai phần ngày giờ ở đầu công thức PA1: CONVERT(varchar(8), T, 112) rồi REPLACE(CONVERT(varchar(12), T, 114), ':', ''), nhận varchar hoặc nvarchar,
        ///              T ở hai CONVERT giống nhau sau chuẩn hóa. Ghép lại ra yyyyMMddHHmmssfff nên sắp đúng thứ tự thời gian. Không đúng dạng này thì trả null.
        /// Created date: 16/11/2026
        /// </summary>
        private static (ConvertCall Date, ConvertCall Time)? FormulaTimeParts(ScalarExpression keyExpr)
        {
            var parts = new List<ScalarExpression>();
            FlattenConcat(keyExpr, parts);
            if (parts.Count < 2
                || parts[0] is not ConvertCall
                {
                    Style: IntegerLiteral { Value: "112" },
                    DataType: SqlDataTypeReference { SqlDataTypeOption: SqlDataTypeOption.VarChar or SqlDataTypeOption.NVarChar, Parameters: [IntegerLiteral { Value: "8" }] }
                } datePart
                || parts[1] is not FunctionCall
                {
                    FunctionName.Value: var replaceName,
                    Parameters:
                    [
                        ConvertCall
                        {
                            Style: IntegerLiteral { Value: "114" },
                            DataType: SqlDataTypeReference { SqlDataTypeOption: SqlDataTypeOption.VarChar or SqlDataTypeOption.NVarChar, Parameters: [IntegerLiteral { Value: "12" }] }
                        } timePart,
                        StringLiteral { Value: ":" },
                        StringLiteral { Value: "" }
                    ]
                }
                || !StringComparer.OrdinalIgnoreCase.Equals(replaceName, "REPLACE")
                || NormalizeCursorExpression(datePart.Parameter) != NormalizeCursorExpression(timePart.Parameter))
            {
                return null;
            }

            return (datePart, timePart);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Trải phép ghép chuỗi a + b + c thành danh sách các phần theo đúng thứ tự, bỏ ngoặc.
        /// Created date: 05/11/2026
        /// </summary>
        private static void FlattenConcat(ScalarExpression expression, List<ScalarExpression> parts)
        {
            var stripped = StripParentheses(expression);
            if (stripped is BinaryExpression { BinaryExpressionType: BinaryExpressionType.Add } add)
            {
                FlattenConcat(add.FirstExpression, parts);
                FlattenConcat(add.SecondExpression, parts);
                return;
            }

            parts.Add(stripped);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Phần thời gian luôn có giá trị: có cột CreateTime (COALESCE, ISNULL hoặc chính cột đó). UpdateTime trơn chỉ tới đây khi câu có nhánh lọc riêng UpdateTime IS NULL.
        /// Created date: 05/11/2026
        /// </summary>
        private static bool IsCursorTimeCovered(ScalarExpression time)
        {
            if (StripParentheses(time) is ColumnReferenceExpression column && IsUpdateTimeColumn(column))
            {
                return true;
            }

            var visitor = new CreateTimeVisitor();
            time.Accept(visitor);
            return visitor.Found;
        }

        /// <summary>Duyệt phần thời gian của công thức mốc: đánh dấu có cột CreateTime, kể cả kèm tiền tố bảng.</summary>
        private sealed class CreateTimeVisitor : TSqlFragmentVisitor
        {
            public bool Found { get; private set; }

            /// <summary>
            /// Author: DatHQ
            /// Description: Cột tên CreateTime thì đánh dấu đã có.
            /// Created date: 05/11/2026
            /// </summary>
            public override void Visit(ColumnReferenceExpression node)
            {
                if (node.MultiPartIdentifier?.Identifiers is { Count: > 0 } parts
                    && string.Equals(parts[^1].Value, "CreateTime", StringComparison.OrdinalIgnoreCase))
                {
                    Found = true;
                }
                base.Visit(node);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cột mốc đầu tiên có phần ghép ngày giờ không sắp đúng thứ tự thời gian, xét ở mọi SELECT có cột đó. Không có thì trả null.
        /// Created date: 05/11/2026
        /// Modified date: 16/11/2026
        /// </summary>
        private static string? UnsortableDateCursor(SelectStatement statement, SqlVariableSet variables)
        {
            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);

            foreach (var (keyColumn, _) in CursorColumns(variables))
            {
                foreach (var spec in specs.Specifications)
                {
                    var keyExpr = specs.Projection(spec, keyColumn);
                    if (keyExpr == null)
                    {
                        continue;
                    }

                    var visitor = new LastKeyFormatVisitor(keyExpr);
                    keyExpr.Accept(visitor);
                    if (visitor.DateUnsortable)
                    {
                        return keyColumn;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cột mốc đầu tiên có WHERE so đúng biểu thức của cột đó với biến mốc bằng >=. Chặn dưới theo cột thời gian gốc
        ///              (COALESCE(UpdateTime, CreateTime) >= ...) khác biểu thức cột mốc nên không tính. Không có thì trả null.
        /// Created date: 05/11/2026
        /// </summary>
        private static string? InclusiveCursorColumn(SelectStatement statement, SqlVariableSet variables)
        {
            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);
            var filtered = specs.Specifications.Where(s => s.WhereClause?.SearchCondition != null).ToList();

            foreach (var (keyColumn, keyVariables) in CursorColumns(variables))
            {
                foreach (var spec in filtered)
                {
                    var keyExpr = specs.Projection(spec, keyColumn);
                    if (keyExpr == null)
                    {
                        continue;
                    }

                    var conjuncts = Flatten(spec.WhereClause.SearchCondition, BooleanBinaryExpressionType.And);
                    var terms = conjuncts.SelectMany(c => Flatten(c, BooleanBinaryExpressionType.Or));
                    if (terms.Any(t => keyVariables.Any(k => IsComparison(t, BooleanComparisonType.GreaterThanOrEqualTo, keyExpr, k))))
                    {
                        return keyColumn;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Các cột mốc của câu kèm biến mốc lọc theo cột đó: nhóm mặc định (__lastKey) và từng nhóm có tên (__lastKey_&lt;nhóm&gt;).
        /// Created date: 04/11/2026
        /// </summary>
        private static IEnumerable<(string KeyColumn, IReadOnlyCollection<string> KeyVariables)> CursorColumns(SqlVariableSet variables) =>
            variables.CursorGroups
                .Select(group => (KeyColumn: $"__lastKey_{group}", KeyVariables: (IReadOnlyCollection<string>)new[] { $"lastKey_{group}" }))
                .Prepend(("__lastKey", variables.LastKeyNames));

        /// <summary>
        /// Author: DatHQ
        /// Description: Cột mốc đầu tiên có công thức dùng UpdateTime trơn ở SELECT lọc theo biến mốc của cột đó; câu có nhánh lọc riêng
        ///              UpdateTime IS NULL đã gửi dòng mới thêm nên không tính. Không có thì trả null.
        /// Created date: 04/11/2026
        /// </summary>
        private static string? BareUpdateTimeCursor(SelectStatement statement, SqlVariableSet variables)
        {
            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);
            if (specs.Specifications.Any(s => s.WhereClause?.SearchCondition != null && HasEmptyUpdateTimeFilter(s.WhereClause.SearchCondition)))
            {
                return null;
            }

            foreach (var (keyColumn, keyVariables) in CursorColumns(variables))
            {
                var homes = specs.Specifications.Where(s => s.WhereClause?.SearchCondition != null
                    && References(s.WhereClause.SearchCondition).Variables.Overlaps(keyVariables));
                foreach (var home in homes)
                {
                    var visitor = new BareUpdateTimeVisitor();
                    specs.Projection(home, keyColumn)?.Accept(visitor);
                    if (visitor.Found)
                    {
                        return keyColumn;
                    }
                }
            }

            return null;
        }

        /// <summary>WHERE có vế AND lọc riêng dòng UpdateTime trống (UpdateTime IS NULL).</summary>
        private static bool HasEmptyUpdateTimeFilter(BooleanExpression where) =>
            Flatten(where, BooleanBinaryExpressionType.And).Any(conjunct =>
                Unwrap(conjunct) is BooleanIsNullExpression { IsNot: false, Expression: ColumnReferenceExpression column }
                && IsUpdateTimeColumn(column));

        /// <summary>Cột tham chiếu tên UpdateTime, kể cả kèm tiền tố bảng.</summary>
        private static bool IsUpdateTimeColumn(ColumnReferenceExpression column) =>
            column.MultiPartIdentifier?.Identifiers is { Count: > 0 } parts
            && string.Equals(parts[^1].Value, "UpdateTime", StringComparison.OrdinalIgnoreCase);

        /// <summary>Duyệt công thức cột mốc: đánh dấu cột UpdateTime dùng trơn, không nằm trong COALESCE / ISNULL.</summary>
        private sealed class BareUpdateTimeVisitor : TSqlFragmentVisitor
        {
            private int _fallbackDepth;

            public bool Found { get; private set; }

            /// <summary>
            /// Author: DatHQ
            /// Description: Cột trong COALESCE(...) đã có giá trị thay thế khi trống.
            /// Created date: 04/11/2026
            /// </summary>
            public override void ExplicitVisit(CoalesceExpression node)
            {
                _fallbackDepth++;
                base.ExplicitVisit(node);
                _fallbackDepth--;
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Cột trong ISNULL(...) đã có giá trị thay thế khi trống.
            /// Created date: 04/11/2026
            /// </summary>
            public override void ExplicitVisit(FunctionCall node)
            {
                var fallback = string.Equals(node.FunctionName?.Value, "ISNULL", StringComparison.OrdinalIgnoreCase);
                if (fallback) _fallbackDepth++;
                base.ExplicitVisit(node);
                if (fallback) _fallbackDepth--;
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Cột UpdateTime nằm ngoài COALESCE / ISNULL thì đánh dấu dùng trơn.
            /// Created date: 04/11/2026
            /// </summary>
            public override void Visit(ColumnReferenceExpression node)
            {
                if (_fallbackDepth == 0 && IsUpdateTimeColumn(node)) Found = true;
                base.Visit(node);
            }
        }

        /// <summary>
        /// Điều kiện WHERE lọc dòng sau mốc: K &gt; @k. K &gt;= @k qua được bước này để câu đã lưu vẫn chạy; lưu qua API thì ValidateCursorTime từ chối.
        /// K là biểu thức của cột keyAlias ở cùng SELECT (nhánh UNION sau lấy theo vị trí cột ở nhánh đầu); không dò được thì không so biểu thức.
        /// So cột khác cột mốc, so với hằng số thay cho biến mốc hay cột mốc không lấy từ cột dữ liệu (GETDATE()) đều lấy thiếu dòng nên không hợp lệ;
        /// không có WHERE nào dùng biến mốc cũng không hợp lệ (30/10/2026).
        /// Modified date: 05/11/2026
        /// </summary>
        private static SqlValidationResult ValidateCursorFilter(
            SelectStatement statement,
            IReadOnlyCollection<string> keyVariables,
            string keyAlias)
        {
            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);
            var filtered = specs.Specifications
                .Where(s => s.WhereClause?.SearchCondition != null && References(s.WhereClause.SearchCondition).Variables.Overlaps(keyVariables))
                .ToList();

            var valid = keyVariables.Count > 0 && filtered.Count > 0 && filtered.All(spec =>
            {
                var keyExpr = specs.Projection(spec, keyAlias);
                return keyVariables.Any(k => HasKeyFilter(spec.WhereClause.SearchCondition, keyExpr, k));
            });
            if (valid)
            {
                return SqlValidationResult.Success();
            }

            var keyVariable = keyVariables.FirstOrDefault() ?? ShareDataConst.SqlVariableSourceLastKey;
            return SqlValidationResult.Failure(BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInvalid, [keyVariable, keyAlias]);
        }

        /// <summary>Dạng chỉ khóa: có vế AND gồm K &gt; @k hoặc K &gt;= @k, các vế OR khác chỉ dùng biến hoặc chặn.</summary>
        private static bool HasKeyFilter(BooleanExpression where, ScalarExpression? keyExpr, string keyVariable) =>
            Flatten(where, BooleanBinaryExpressionType.And).Any(conjunct =>
            {
                var terms = Flatten(conjunct, BooleanBinaryExpressionType.Or);
                return terms.Any(t => IsKeyComparison(t, keyExpr, keyVariable))
                    && terms.All(t => IsKeyComparison(t, keyExpr, keyVariable) || !References(t).HasColumn);
            });

        /// <summary>
        /// Author: DatHQ
        /// Description: Phép so K lớn hơn, hoặc lớn hơn bằng biến mốc khóa.
        /// Created date: 30/10/2026
        /// </summary>
        private static bool IsKeyComparison(BooleanExpression t, ScalarExpression? keyExpr, string keyVariable) =>
            IsComparison(t, BooleanComparisonType.GreaterThan, keyExpr, keyVariable)
            || IsComparison(t, BooleanComparisonType.GreaterThanOrEqualTo, keyExpr, keyVariable);

        /// <summary>Phép so &lt;cột&gt; (type) &lt;biểu thức chỉ có biến&gt;, viết xuôi hoặc ngược; column có giá trị thì vế cột phải đúng biểu thức đó.</summary>
        private static bool IsComparison(BooleanExpression condition, BooleanComparisonType type, ScalarExpression? column, string variable)
        {
            if (Unwrap(condition) is not BooleanComparisonExpression compare)
            {
                return false;
            }

            var flipped = type switch
            {
                BooleanComparisonType.GreaterThan => BooleanComparisonType.LessThan,
                BooleanComparisonType.GreaterThanOrEqualTo => BooleanComparisonType.LessThanOrEqualTo,
                _ => type
            };
            return (compare.ComparisonType == type && IsVariableOnly(compare.SecondExpression, variable) && IsSameColumn(column, compare.FirstExpression))
                || (compare.ComparisonType == flipped && IsVariableOnly(compare.FirstExpression, variable) && IsSameColumn(column, compare.SecondExpression));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Biểu thức có dùng biến này và không tham chiếu cột nào.
        /// Created date: 30/10/2026
        /// </summary>
        private static bool IsVariableOnly(ScalarExpression expression, string variable)
        {
            var references = References(expression);
            return !references.HasColumn && references.Variables.Contains(variable);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Vế cột có tham chiếu cột và khớp biểu thức mong đợi; expected null thì không so biểu thức.
        /// Created date: 30/10/2026
        /// </summary>
        private static bool IsSameColumn(ScalarExpression? expected, ScalarExpression actual) =>
            References(actual).HasColumn
            && (expected == null || NormalizeCursorExpression(StripParentheses(expected)) == NormalizeCursorExpression(StripParentheses(actual)));

        /// <summary>
        /// Author: DatHQ
        /// Description: Bỏ các lớp ngoặc bao ngoài biểu thức giá trị.
        /// Created date: 30/10/2026
        /// </summary>
        private static ScalarExpression StripParentheses(ScalarExpression expression)
        {
            while (expression is ParenthesisExpression parenthesis)
            {
                expression = parenthesis.Expression;
            }
            return expression;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bỏ các lớp ngoặc bao ngoài biểu thức điều kiện.
        /// Created date: 30/10/2026
        /// </summary>
        private static BooleanExpression Unwrap(BooleanExpression expression)
        {
            while (expression is BooleanParenthesisExpression parenthesis)
            {
                expression = parenthesis.Expression;
            }
            return expression;
        }

        /// <summary>Tách biểu thức theo AND (hoặc OR) ở cấp ngoài, bỏ ngoặc bao.</summary>
        private static List<BooleanExpression> Flatten(BooleanExpression expression, BooleanBinaryExpressionType type)
        {
            var unwrapped = Unwrap(expression);
            return unwrapped is BooleanBinaryExpression binary && binary.BinaryExpressionType == type
                ? [.. Flatten(binary.FirstExpression, type), .. Flatten(binary.SecondExpression, type)]
                : [unwrapped];
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Duyệt một phần câu SQL để biết các biến và có tham chiếu cột hay không.
        /// Created date: 30/10/2026
        /// </summary>
        private static ReferenceVisitor References(TSqlFragment fragment)
        {
            var visitor = new ReferenceVisitor();
            fragment.Accept(visitor);
            return visitor;
        }

        /// <summary>Biến (không có @) và có tham chiếu cột trong một biểu thức.</summary>
        private sealed class ReferenceVisitor : TSqlFragmentVisitor
        {
            public HashSet<string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);
            public bool HasColumn { get; private set; }

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom tên biến, bỏ dấu @.
            /// Created date: 19/10/2026
            /// </summary>
            public override void Visit(VariableReference node)
            {
                if (node?.Name != null)
                {
                    Variables.Add(node.Name.TrimStart('@'));
                }
                base.Visit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Đánh dấu biểu thức có tham chiếu cột.
            /// Created date: 30/10/2026
            /// </summary>
            public override void Visit(ColumnReferenceExpression node)
            {
                HasColumn = true;
                base.Visit(node);
            }
        }

        /// <summary>Gom mọi SELECT (QuerySpecification) của câu kèm nhánh đầu của UNION / EXCEPT / INTERSECT chứa nó, để lấy cột theo vị trí.</summary>
        private sealed class QuerySpecificationCollector : TSqlFragmentVisitor
        {
            private readonly Dictionary<QuerySpecification, QuerySpecification> _firstBranch = [];
            private readonly HashSet<QuerySpecification> _unionBranches = [];

            public List<QuerySpecification> Specifications { get; } = [];

            /// <summary>SELECT là một nhánh của UNION.</summary>
            public bool IsUnionBranch(QuerySpecification spec) => _unionBranches.Contains(spec);

            /// <summary>SELECT có chứa đúng biểu thức cột đã cho; không có thì trả null.</summary>
            public QuerySpecification? OwnerOf(ScalarExpression expression) =>
                Specifications.FirstOrDefault(spec => spec.SelectElements.OfType<SelectScalarExpression>().Any(column => ReferenceEquals(column.Expression, expression)));

            /// <summary>
            /// Author: DatHQ
            /// Description: Ghi nhận nhánh đầu của UNION, EXCEPT, INTERSECT cho từng nhánh để lấy cột theo vị trí.
            /// Created date: 30/10/2026
            /// </summary>
            public override void ExplicitVisit(BinaryQueryExpression node)
            {
                var branches = new List<QuerySpecification>();
                CollectBranches(node, branches);
                foreach (var branch in branches)
                {
                    _firstBranch.TryAdd(branch, branches[0]);
                    if (node.BinaryQueryExpressionType == BinaryQueryExpressionType.Union)
                    {
                        _unionBranches.Add(branch);
                    }
                }
                base.ExplicitVisit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom mọi câu SELECT trong câu SQL.
            /// Created date: 30/10/2026
            /// </summary>
            public override void ExplicitVisit(QuerySpecification node)
            {
                Specifications.Add(node);
                base.ExplicitVisit(node);
            }

            /// <summary>Biểu thức của cột alias trong SELECT: theo bí danh; nhánh UNION sau không đặt bí danh thì lấy cột cùng vị trí với nhánh đầu.</summary>
            public ScalarExpression? Projection(QuerySpecification spec, string alias)
            {
                var index = AliasIndex(spec, alias);
                if (index < 0 && _firstBranch.TryGetValue(spec, out var first) && first != spec)
                {
                    index = AliasIndex(first, alias);
                }
                return index >= 0 && index < spec.SelectElements.Count && spec.SelectElements[index] is SelectScalarExpression column
                    ? column.Expression
                    : null;
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Biểu thức gốc của cột alias: SELECT chỉ lấy lại cột của bảng dẫn xuất (x.__lastKey) hoặc dùng SELECT * thì lấy biểu thức ở SELECT bên trong có cùng tên cột;
            ///              không có thì trả biểu thức đang có.
            /// Created date: 05/11/2026
            /// </summary>
            public ScalarExpression? ResolveProjection(QuerySpecification spec, string alias)
            {
                var expr = Projection(spec, alias);
                var sourceColumn = expr == null ? alias : (StripParentheses(expr) as ColumnReferenceExpression)?.MultiPartIdentifier?.Identifiers[^1].Value;
                if (sourceColumn == null)
                {
                    return expr;
                }

                var inner = Specifications
                    .Where(s => s != spec)
                    .Select(s => Projection(s, sourceColumn))
                    .OfType<ScalarExpression>()
                    .ToList();
                return inner.FirstOrDefault(e => StripParentheses(e) is not ColumnReferenceExpression) ?? expr ?? inner.FirstOrDefault();
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Vị trí cột có bí danh alias trong SELECT.
            /// Created date: 30/10/2026
            /// </summary>
            private static int AliasIndex(QuerySpecification spec, string alias)
            {
                for (var i = 0; i < spec.SelectElements.Count; i++)
                {
                    if (spec.SelectElements[i] is SelectScalarExpression column
                        && string.Equals(column.ColumnName?.Value, alias, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
                return -1;
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Trải câu gộp thành danh sách các nhánh SELECT theo đúng thứ tự.
            /// Created date: 30/10/2026
            /// </summary>
            private static void CollectBranches(QueryExpression query, List<QuerySpecification> branches)
            {
                switch (query)
                {
                    case BinaryQueryExpression binary:
                        CollectBranches(binary.FirstQueryExpression, branches);
                        CollectBranches(binary.SecondQueryExpression, branches);
                        break;
                    case QueryParenthesisExpression parenthesis:
                        CollectBranches(parenthesis.QueryExpression, branches);
                        break;
                    case QuerySpecification spec:
                        branches.Add(spec);
                        break;
                }
            }
        }

        /// <summary>
        /// Biểu thức ORDER BY có trỏ tới cột bí danh alias không: viết bằng chính tên bí danh (kể cả kèm tiền tố bảng),
        /// hoặc viết lại đúng biểu thức của cột đó trong SELECT (so không phân biệt hoa thường, bỏ khoảng trắng).
        /// </summary>
        private static bool IsOrderByColumn(ScalarExpression expression, string alias, QuerySpecification querySpec)
        {
            if (expression is ColumnReferenceExpression column
                && column.MultiPartIdentifier?.Identifiers is { Count: > 0 } parts
                && string.Equals(parts[^1].Value, alias, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var selected = querySpec.SelectElements
                .OfType<SelectScalarExpression>()
                .FirstOrDefault(e => string.Equals(e.ColumnName?.Value, alias, StringComparison.OrdinalIgnoreCase));
            return selected != null && string.Equals(FragmentText(selected.Expression), FragmentText(expression), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghép văn bản của một phần câu SQL để so sánh, bỏ khoảng trắng.
        /// Created date: 26/10/2026
        /// </summary>
        private static string FragmentText(TSqlFragment fragment)
        {
            var builder = new System.Text.StringBuilder();
            for (var i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
            {
                var token = fragment.ScriptTokenStream[i];
                if (token.TokenType != TSqlTokenType.WhiteSpace)
                {
                    builder.Append(token.Text);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Xác định dòng, cột (đếm từ 1) của lỗi trong câu SQL để giao diện đánh dấu trên trình soạn thảo.
        /// Lỗi cú pháp lấy vị trí từ ScriptDom; lỗi CSDL (sai tên cột, tên bảng) tìm tên được SQL Server đặt trong dấu nháy đơn.
        /// </summary>
        public static bool TryLocateError(string? sql, string? errorKey, object[]? errorArgs, out int line, out int column)
        {
            line = 0;
            column = 0;
            if (string.IsNullOrEmpty(sql) || errorArgs == null || errorArgs.Length == 0)
            {
                return false;
            }

            if (errorKey == BaseMsg.ShareData.Validation.SqlScriptSyntaxError && errorArgs.Length >= 2
                && int.TryParse(errorArgs[0]?.ToString(), out var syntaxLine) && int.TryParse(errorArgs[1]?.ToString(), out var syntaxColumn)
                && syntaxLine > 0 && syntaxColumn > 0)
            {
                line = syntaxLine;
                column = syntaxColumn;
                return true;
            }

            if (errorKey != BaseMsg.ShareData.Validation.SqlDatabaseExecutionError)
            {
                return false;
            }

            var quoted = Regex.Match(errorArgs[0]?.ToString() ?? string.Empty, "'([^']+)'");
            if (!quoted.Success)
            {
                return false;
            }

            var name = quoted.Groups[1].Value;
            var index = sql.IndexOf(name, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                var shortName = name[(name.LastIndexOf('.') + 1)..];
                index = shortName.Length > 0 ? sql.IndexOf(shortName, StringComparison.OrdinalIgnoreCase) : -1;
            }
            if (index < 0)
            {
                return false;
            }

            var before = sql[..index];
            line = before.Count(c => c == '\n') + 1;
            column = index - (before.LastIndexOf('\n') + 1) + 1;
            return true;
        }

        /// <summary>
        /// Bản dịch tiếng Việt chuẩn phục vụ hiển thị fallback và ghi log Worker khi không có IStringLocalizer.
        /// </summary>
        public static string FormatVietnamese(string? errorKey, object[]? args)
        {
            if (string.IsNullOrWhiteSpace(errorKey)) return string.Empty;
            var template = errorKey switch
            {
                BaseMsg.ShareData.Validation.SqlScriptEmpty => "Câu lệnh truy vấn không được để trống.",
                BaseMsg.ShareData.Validation.SqlScriptSyntaxError => "Cú pháp SQL không hợp lệ (Dòng {0}, Cột {1}): {2}",
                BaseMsg.ShareData.Validation.SqlScriptInvalid => "Cú pháp SQL không hợp lệ hoặc không chứa script thực thi.",
                BaseMsg.ShareData.Validation.SqlMultipleBatchesNotAllowed => "Chỉ chấp nhận duy nhất một lô lệnh (batch). Không được sử dụng lệnh phân đoạn 'GO'.",
                BaseMsg.ShareData.Validation.SqlMultipleStatementsNotAllowed => "Chỉ chấp nhận duy nhất một câu lệnh SELECT (không cho phép đa câu lệnh).",
                BaseMsg.ShareData.Validation.SqlOnlySelectAllowed => "Chỉ chấp nhận câu lệnh đọc dữ liệu bắt đầu bằng SELECT.",
                BaseMsg.ShareData.Validation.SqlSelectIntoNotAllowed => "Không cho phép sử dụng cú pháp SELECT ... INTO tạo bảng mới.",
                BaseMsg.ShareData.Validation.SqlFunctionCallBlocked => "Không cho phép gọi hàm hoặc thủ tục: [{0}].",
                BaseMsg.ShareData.Validation.SqlExternalDataSourcesBlocked => "Không cho phép sử dụng các nguồn dữ liệu ngoài (OPENROWSET/OPENQUERY/...).",
                BaseMsg.ShareData.Validation.SqlExecuteStatementBlocked => "Không cho phép thực thi câu lệnh EXEC/EXECUTE.",
                BaseMsg.ShareData.Validation.SqlCrossDatabaseQueryBlocked => "Không được phép truy vấn bảng vượt ra ngoài CSDL hiện tại (tên bảng 3-4 phần: [{0}]).",
                BaseMsg.ShareData.Validation.SqlSystemSchemaBlocked => "Không được phép truy vấn schema hệ thống: [{0}].",
                BaseMsg.ShareData.Validation.SqlSensitiveTableBlocked => "Không được phép truy vấn bảng hệ thống nhạy cảm: [{0}].",
                BaseMsg.ShareData.Validation.SqlUnsupportedParameter => "Biến [@{0}] chưa khai ở bảng Tham số câu truy vấn. Mọi biến dùng trong câu SQL phải được khai ở bảng này.",
                BaseMsg.ShareData.Validation.SqlDatabaseExecutionError => "Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: {0}",
                BaseMsg.ShareData.Validation.SqlIncrementalMissingOrderBy => "Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề ORDER BY ở cấp ngoài cùng.",
                BaseMsg.ShareData.Validation.SqlIncrementalOrderByDescNotAllowed => "Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc sắp xếp tăng dần (ASC); không cho phép sắp xếp giảm dần (DESC) ở mệnh đề ORDER BY ngoài cùng.",
                BaseMsg.ShareData.Validation.SqlIncrementalOrderByKeyMismatch => "Truy vấn đồng bộ nối đuôi theo khóa phải sắp xếp ORDER BY ngoài cùng theo cột [__lastKey], nếu không có thể bỏ sót dòng.",
                BaseMsg.ShareData.Validation.SqlIncrementalMissingLastKey => "Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có cột con trỏ khóa [__lastKey].",
                BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInvalid => "WHERE của truy vấn nối đuôi theo khóa phải có [{1}] > @{0}, viết đúng biểu thức của cột mốc; thiếu thì bỏ sót hoặc gửi lặp dòng.",
                BaseMsg.ShareData.Validation.SqlIncrementalGroupColumnMissing => "Nhóm mốc [{0}] còn thiếu {1}. Mỗi nhóm cần cột [__lastKey_{0}] và biến @lastKey_{0}.",
                BaseMsg.ShareData.Validation.SqlIncrementalGroupNotFound => "Câu truy vấn đối tác đang chạy không có nhóm mốc [{0}].",
                BaseMsg.ShareData.Validation.SqlIncrementalKeyTimeOnly => "Cột mốc chỉ có ngày giờ nên sót các dòng trùng giờ. Ghép thêm đuôi phân biệt dòng: CONVERT(varchar(8), <cột thời gian>, 112) + REPLACE(CONVERT(varchar(12), <cột thời gian>, 114), ':', '') + '_' + ID.",
                BaseMsg.ShareData.Validation.SqlIncrementalUpdateTimeEmpty => "Cột mốc [{0}] dùng UpdateTime trơn nên dòng mới thêm (chưa có UpdateTime) không được gửi. Dùng COALESCE(UpdateTime, CreateTime) ở cả cột mốc và WHERE.",
                BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInclusive => "Cột mốc [{0}]: so với biến mốc phải dùng dấu >; dùng >= thì dòng cuối bị gửi lặp mỗi lượt.",
                BaseMsg.ShareData.Validation.SqlIncrementalKeyDateUnsortable => "Cột mốc ghép ngày giờ sai định dạng nên sắp sai thứ tự, có thể bỏ sót dòng. Dùng CONVERT(varchar(8), <cột thời gian>, 112) + REPLACE(CONVERT(varchar(12), <cột thời gian>, 114), ':', '').",
                BaseMsg.ShareData.Validation.SqlIncrementalCursorVariableMissing => "Câu có cột mốc [{0}] nhưng không dùng biến mốc (nguồn Theo Mốc đang chạy) nên lượt nào cũng gửi lại cùng dữ liệu. So cột mốc với biến lastKey trong WHERE, hoặc bỏ cột mốc nếu muốn gửi toàn bộ.",
                BaseMsg.ShareData.Validation.SqlIncrementalKeyFormulaInvalid => "Cột mốc phải viết đúng công thức CONVERT(varchar(8), <thời gian>, 112) + REPLACE(CONVERT(varchar(12), <thời gian>, 114), ':', '') + '_' + <đuôi>; phần thời gian phải có CreateTime, đuôi không được NULL (cột có thể NULL thì bọc ISNULL(<cột>, '')); viết khác thì có thể bỏ sót dòng.",
                BaseMsg.ShareData.Validation.SqlIncrementalOffsetNotAllowed => "Câu nối đuôi không được dùng OFFSET: các dòng bị bỏ qua sẽ không bao giờ được gửi. Giới hạn số dòng bằng TOP.",
                BaseMsg.ShareData.Validation.SqlIncrementalUnionOneCursor => "Câu gộp nhiều nguồn không dùng chung một mốc: nguồn về trễ sẽ mất dòng. Mỗi nguồn cần cột __lastKey_nhóm và biến lastKey_nhóm riêng.",
                BaseMsg.ShareData.Validation.SqlIncrementalInnerTopOrderInvalid => "Câu con có TOP phải sắp tăng dần theo cột mốc: sắp theo cột khác thì dòng có mốc nhỏ hơn bị bỏ sót. Đổi ORDER BY trong câu con thành cột mốc.",
                BaseMsg.ShareData.Validation.SqlIncrementalGroupOrderInvalid => "Nhóm mốc [{0}]: ORDER BY ngoài cùng sắp theo [{1}], ở nhánh của nhóm này cột đó khác công thức cột [__lastKey_{0}] nên dòng ra sai thứ tự mốc. Hãy sắp theo cột có cùng công thức cột mốc ở mọi nhánh.",
                BaseMsg.ShareData.Validation.SqlTableNotAllowed => "Bảng [{0}] không nằm trong danh mục bảng nguồn được phép truy vấn của ShareData.",
                BaseMsg.ShareData.Validation.SqlColumnUnnamed => "Cột thứ {0} của câu SQL chưa có tên. Hãy đặt tên cột bằng AS, tên cột chính là khoá trường của gói tin.",
                BaseMsg.ShareData.Validation.SqlColumnDuplicate => "Câu SQL trả về trùng tên cột [{0}]. Hãy đặt tên khác bằng AS.",
                BaseMsg.ShareData.Validation.SqlFieldKeyNotFound => "Khoá trường [{0}] không có trong danh sách trường của câu SQL.",
                BaseMsg.ShareData.Validation.SqlFieldTypeInvalid => "Trường [{0}]: kiểu [{1}] không có trong danh sách kiểu dữ liệu.",
                BaseMsg.ShareData.Validation.SqlFieldValueTooLong => "Trường [{0}]: {1} vượt quá {2} ký tự.",
                BaseMsg.ShareData.Validation.SqlFieldSkipInUse => "Không thể bỏ qua trường [{0}] vì hồ sơ ánh xạ [{1}] đang dùng. Hãy bỏ chọn trường ở hồ sơ ánh xạ trước.",
                _ => errorKey
            };

            if (args != null && args.Length > 0)
            {
                if (errorKey == BaseMsg.ShareData.Validation.SqlDatabaseExecutionError)
                {
                    var detail = args[0]?.ToString() ?? string.Empty;
                    var colMatch = Regex.Match(detail, @"Invalid column name '([^']+)'", RegexOptions.IgnoreCase);
                    if (colMatch.Success)
                    {
                        return $"Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: Cột '{colMatch.Groups[1].Value}' không tồn tại trên cơ sở dữ liệu.";
                    }
                    var tblMatch = Regex.Match(detail, @"Invalid object name '([^']+)'", RegexOptions.IgnoreCase);
                    if (tblMatch.Success)
                    {
                        return $"Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: Bảng hoặc đối tượng '{tblMatch.Groups[1].Value}' không tồn tại trên cơ sở dữ liệu.";
                    }
                    var ambMatch = Regex.Match(detail, @"Ambiguous column name '([^']+)'", RegexOptions.IgnoreCase);
                    if (ambMatch.Success)
                    {
                        return $"Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: Cột '{ambMatch.Groups[1].Value}' bị trùng lặp (cần chỉ định rõ alias bảng).";
                    }
                }
                return string.Format(template, args);
            }
            return template;
        }

        /// <summary>
        /// Phân tích và lọc bỏ chú thích (line comment --, block comment /* */)
        /// đồng thời tạo chuỗi đã xóa chuỗi hằng số ('...') để kiểm tra từ khóa an toàn.
        /// </summary>
        public static (string CleanSql, string SqlWithoutStrings) SanitizeSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return (string.Empty, string.Empty);

            var cleanSb = new System.Text.StringBuilder(sql.Length);
            var noStrSb = new System.Text.StringBuilder(sql.Length);

            var i = 0;
            var len = sql.Length;

            while (i < len)
            {
                var c = sql[i];

                // Kiểm tra chuỗi ký tự '...'
                if (c == '\'')
                {
                    cleanSb.Append('\'');
                    noStrSb.Append('\'');
                    noStrSb.Append('\'');
                    i++;

                    while (i < len)
                    {
                        if (sql[i] == '\'')
                        {
                            cleanSb.Append('\'');
                            if (i + 1 < len && sql[i + 1] == '\'')
                            {
                                // Escaped quote ''
                                cleanSb.Append('\'');
                                i += 2;
                                continue;
                            }
                            i++;
                            break;
                        }

                        cleanSb.Append(sql[i]);
                        noStrSb.Append(' ');
                        i++;
                    }
                    continue;
                }

                // Kiểm tra line comment --
                if (c == '-' && i + 1 < len && sql[i + 1] == '-')
                {
                    i += 2;
                    while (i < len && sql[i] != '\r' && sql[i] != '\n')
                    {
                        i++;
                    }
                    cleanSb.Append(' ');
                    noStrSb.Append(' ');
                    continue;
                }

                // Kiểm tra block comment /* ... */
                if (c == '/' && i + 1 < len && sql[i + 1] == '*')
                {
                    i += 2;
                    while (i < len)
                    {
                        if (sql[i] == '*' && i + 1 < len && sql[i + 1] == '/')
                        {
                            i += 2;
                            break;
                        }
                        i++;
                    }
                    cleanSb.Append(' ');
                    noStrSb.Append(' ');
                    continue;
                }

                cleanSb.Append(c);
                noStrSb.Append(c);
                i++;
            }

            return (cleanSb.ToString(), noStrSb.ToString());
        }

        /// <summary>
        /// Phân tích danh sách cột từ mệnh đề SELECT ... FROM tĩnh. Câu không bắt đầu bằng SELECT (có WITH, chú thích ở đầu) thì đọc bằng bộ phân tích SQL.
        /// Modified date: 06/11/2026
        /// </summary>
        public static List<string> ExtractColumnsStatically(string sql)
        {
            var columns = new List<string>();
            var match = Regex.Match(sql, @"^\s*SELECT\s+(?:DISTINCT\s+)?(?:TOP\s*\([^)]+\)\s+)?(.*?)\s+FROM\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return ExtractColumnsByParser(sql);

            var selectBody = match.Groups[1].Value.Trim();
            var parts = SplitSelectColumns(selectBody);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Kiểm tra AS alias: e.g. "zs.AverageSpeed AS averageSpeed"
                var asMatch = Regex.Match(trimmed, @"\bAS\s+([A-Za-z0-9_#$]+|\[[^\]]+\]|""[^""]+"")\s*$", RegexOptions.IgnoreCase);
                if (asMatch.Success)
                {
                    var alias = asMatch.Groups[1].Value.Trim().Trim('[', ']', '"');
                    if (!columns.Contains(alias)) columns.Add(alias);
                    continue;
                }

                // Không có AS nhưng có alias cách khoảng trắng: e.g. "zs.ZoneId zoneId"
                var spaceAliasMatch = Regex.Match(trimmed, @"\s+([A-Za-z0-9_#$]+|\[[^\]]+\]|""[^""]+"")\s*$", RegexOptions.IgnoreCase);
                if (spaceAliasMatch.Success && !spaceAliasMatch.Groups[1].Value.Equals("ASC", StringComparison.OrdinalIgnoreCase) && !spaceAliasMatch.Groups[1].Value.Equals("DESC", StringComparison.OrdinalIgnoreCase))
                {
                    var alias = spaceAliasMatch.Groups[1].Value.Trim().Trim('[', ']', '"');
                    if (!columns.Contains(alias)) columns.Add(alias);
                    continue;
                }

                // Biểu thức cột thuần: e.g. "table.Column" -> "Column"
                var colMatch = Regex.Match(trimmed, @"(?:[A-Za-z0-9_#$]+\.)?([A-Za-z0-9_#$]+|\[[^\]]+\]|""[^""]+"")\s*$", RegexOptions.IgnoreCase);
                if (colMatch.Success)
                {
                    var colName = colMatch.Groups[1].Value.Trim().Trim('[', ']', '"');
                    if (!columns.Contains(colName)) columns.Add(colName);
                }
            }

            return columns;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tách danh sách cột của SELECT theo dấu phẩy nằm ngoài ngoặc.
        /// Created date: 19/10/2026
        /// </summary>
        /// <summary>
        /// Author: DatHQ
        /// Description: Tên cột của SELECT ngoài cùng (nhánh đầu nếu là UNION) đọc bằng bộ phân tích SQL: theo bí danh, không có bí danh thì lấy tên cột.
        ///              Lỗi cú pháp hoặc SELECT * thì không biết tên cột, trả danh sách rỗng như cách đọc tĩnh.
        /// Created date: 06/11/2026
        /// </summary>
        private static List<string> ExtractColumnsByParser(string sql)
        {
            var fragment = new TSql160Parser(true).Parse(new StringReader(sql), out var errors);
            var statement = errors is { Count: > 0 } || fragment is not TSqlScript script
                ? null
                : script.Batches.SelectMany(b => b.Statements).OfType<SelectStatement>().FirstOrDefault();
            var query = statement?.QueryExpression;
            while (query is QueryParenthesisExpression or BinaryQueryExpression)
            {
                query = query is QueryParenthesisExpression parenthesis ? parenthesis.QueryExpression : ((BinaryQueryExpression)query).FirstQueryExpression;
            }

            var columns = new List<string>();
            foreach (var element in (query as QuerySpecification)?.SelectElements.OfType<SelectScalarExpression>() ?? [])
            {
                var name = element.ColumnName?.Value ?? (StripParentheses(element.Expression) as ColumnReferenceExpression)?.MultiPartIdentifier?.Identifiers[^1].Value;
                if (name != null && !columns.Contains(name))
                {
                    columns.Add(name);
                }
            }

            return columns;
        }

        private static List<string> SplitSelectColumns(string selectBody)
        {
            var list = new List<string>();
            var parenDepth = 0;
            var current = new System.Text.StringBuilder();

            foreach (var ch in selectBody)
            {
                if (ch == '(') parenDepth++;
                else if (ch == ')') parenDepth = Math.Max(0, parenDepth - 1);

                if (ch == ',' && parenDepth == 0)
                {
                    list.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            if (current.Length > 0)
            {
                list.Add(current.ToString());
            }

            return list;
        }

        /// <summary>
        /// Description: Kiểm tra câu truy vấn có giới hạn số dòng trả về (TOP hoặc OFFSET-FETCH) ở SELECT ngoài cùng hay không.
        /// Created date: 26/10/2026
        /// </summary>
        public static bool HasRowLimit(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false;

            var parser = new TSql160Parser(true);
            using var reader = new StringReader(sql);
            var fragment = parser.Parse(reader, out var errors);

            if (errors != null && errors.Count > 0) return false;
            if (fragment is not TSqlScript { Batches: [{ Statements: [SelectStatement { QueryExpression: QuerySpecification spec }] }] })
            {
                return false;
            }

            if (spec.TopRowFilter != null || spec.OffsetClause?.FetchExpression != null)
            {
                return true;
            }

            return spec.FromClause?.TableReferences is [QueryDerivedTable { QueryExpression: QuerySpecification inner }]
                && (inner.TopRowFilter != null || inner.OffsetClause?.FetchExpression != null);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Rủi ro của câu nối đuôi lưu từ trước khi có các luật chặn lúc lưu, để Worker ghi vào nhật ký, không dừng gói. Khóa __lastKey được so như chuỗi
        ///              nên mọi phần ghép phải cố định độ dài: ngày giờ đi từ năm đến giây (FORMAT theo thứ tự yyyy, MM, dd hoặc CONVERT kiểu
        ///              20/21/23/25/102/111/112/120/121/126/127; kiểu 114 chỉ khi đi cặp với kiểu 112 ở công thức PA1), số ép sang chuỗi phải được đệm (RIGHT / REPLICATE / FORMAT),
        ///              trừ khi đứng sau phần thời gian của công thức PA1. Phần thời gian của công thức PA1 không có CreateTime thì dòng có thời gian trống không được gửi;
        ///              đuôi có phần có thể NULL thì dòng có đuôi NULL không được gửi. Câu chỉ khóa mà __lastKey là một cột trơn không phải số
        ///              thì khóa không tăng theo thứ tự ghi. WHERE so đúng biểu thức cột mốc &gt;= biến mốc thì gửi lặp dòng cuối; chặn dưới theo cột thời gian gốc không tính.
        ///              Công thức mốc dùng UpdateTime trơn thì dòng mới thêm không được gửi. Ba ca >=, UpdateTime trơn, ngày giờ sai định dạng khi lưu đã bị chặn, ở đây chỉ còn gặp ở câu lưu trước khi có luật.
        ///              Lỗi cú pháp thì trả rỗng (đã có bộ kiểm khác báo lỗi).
        /// Created date: 28/10/2026
        /// Modified date: 16/11/2026
        /// </summary>
        public static List<string> FindCursorRisks(string? sql, IReadOnlyList<SqlColumnInfo>? columnInfos = null, SqlVariableSet? variables = null)
        {
            var risks = new List<string>();
            if (string.IsNullOrWhiteSpace(sql)) return risks;

            var fragment = new TSql160Parser(true).Parse(new StringReader(sql), out var errors);
            if (errors is { Count: > 0 } || fragment is not TSqlScript script) return risks;
            var statement = script.Batches.SelectMany(b => b.Statements).OfType<SelectStatement>().FirstOrDefault();
            if (statement != null && variables != null && InclusiveCursorColumn(statement, variables) != null)
            {
                risks.Add("So khóa bằng >= với biến mốc: dòng cuối lượt trước bị gửi lại mỗi lượt.");
            }

            var bareUpdateTime = statement != null && variables != null ? BareUpdateTimeCursor(statement, variables) : null;
            if (bareUpdateTime != null)
            {
                risks.Add($"Cột mốc [{bareUpdateTime}] dùng UpdateTime trơn: dòng mới thêm chưa có UpdateTime nên không được gửi.");
            }

            if (statement != null && HasRowSkippingOffset(statement))
            {
                risks.Add("Câu dùng OFFSET: các dòng bị bỏ qua không được gửi.");
            }

            if (statement != null && variables != null && SharesCursorAcrossSources(statement, variables))
            {
                risks.Add("Câu gộp nhiều nguồn dùng chung một mốc: nguồn về trễ bị mất dòng.");
            }

            if (statement != null && variables != null && HasInnerTopOffCursor(statement, variables))
            {
                risks.Add("Câu con có TOP sắp theo cột khác cột mốc: dòng có mốc nhỏ hơn có thể bị bỏ sót.");
            }

            QuerySpecificationCollector? specs = null;
            if (statement != null)
            {
                specs = new QuerySpecificationCollector();
                statement.Accept(specs);
            }

            var query = statement?.QueryExpression as QuerySpecification;
            var keyExpr = query != null && specs != null
                ? specs.ResolveProjection(query, "__lastKey")
                : null;
            if (keyExpr == null) return risks;

            // __lastKey là một cột trơn không phải số: dòng mới có thể có khóa nhỏ hơn mốc và bị bỏ sót; cột số (số tự tăng) thì đúng
            var keyType = columnInfos?.FirstOrDefault(c => string.Equals(c.Name, "__lastKey", StringComparison.OrdinalIgnoreCase))?.DbType;
            var isNumericKey = keyType != null && Regex.IsMatch(keyType, @"^(tinyint|smallint|int|bigint|decimal|numeric)\b", RegexOptions.IgnoreCase);
            if (StripParentheses(keyExpr) is ColumnReferenceExpression && !isNumericKey)
            {
                risks.Add("__lastKey là một cột không tăng theo thứ tự ghi: dòng mới có thể bị bỏ sót.");
            }

            var visitor = new LastKeyFormatVisitor(keyExpr);
            keyExpr.Accept(visitor);
            var formulaTime = CursorFormulaTime(keyExpr);
            if (visitor.DateUnsortable) risks.Add("__lastKey ghép ngày giờ sai định dạng: sắp sai thứ tự, có thể bỏ sót dòng.");
            if (visitor.NotFixedWidth && formulaTime == null) risks.Add("__lastKey ép số sang chuỗi không đệm 0: sắp sai thứ tự, có thể bỏ sót dòng.");
            if (formulaTime != null && !IsCursorTimeCovered(formulaTime)) risks.Add("__lastKey không có CreateTime: dòng có cột thời gian trống không được gửi.");
            if (formulaTime != null && specs != null)
            {
                if (!HasNonNullKeyTail(specs, keyExpr)) risks.Add("Đuôi của __lastKey có thể NULL: dòng có đuôi NULL không được gửi.");
            }
            return risks;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chữ ký công thức cột mốc của câu nối đuôi: băm SHA-256 biểu thức SELECT ngoài cùng của __lastKey.
        ///              Bỏ khoảng trắng, chú thích, tiền tố bảng / bí danh (zs.UpdateTime như UpdateTime) và không phân biệt hoa thường
        ///              (trừ chuỗi trong nháy như mẫu 'HHmm'), để sửa câu cho gọn không bị coi là đổi công thức. Đổi cột mốc hay đổi chuỗi ghép
        ///              thì chữ ký đổi. Câu không có __lastKey hoặc lỗi cú pháp thì trả null.
        ///              Chuỗi băm giữ dạng "|&lt;khóa&gt;" để chữ ký của câu đang chạy không đổi.
        /// Created date: 29/10/2026
        /// Modified date: 16/11/2026
        /// </summary>
        public static string? GetCursorSignature(string? sql, string? group = null)
        {
            if (string.IsNullOrWhiteSpace(sql)) return null;

            var fragment = new TSql160Parser(true).Parse(new StringReader(sql), out var errors);
            if (errors is { Count: > 0 } || fragment is not TSqlScript script) return null;
            var statement = script.Batches.SelectMany(b => b.Statements).OfType<SelectStatement>().FirstOrDefault();
            var query = statement?.QueryExpression as QuerySpecification;
            var suffix = group == null ? string.Empty : "_" + group;
            ScalarExpression? keyExpr = null;
            QuerySpecificationCollector? specs = null;
            if (statement != null)
            {
                specs = new QuerySpecificationCollector();
                statement.Accept(specs);
            }

            // Nhóm có tên (câu gộp nhiều nguồn): công thức ở SELECT có WHERE dùng @lastKey_<nhóm> (nhánh UNION của nguồn đó)
            if (group != null && specs != null)
            {
                var home = specs.Specifications.FirstOrDefault(s => s.WhereClause?.SearchCondition != null
                    && References(s.WhereClause.SearchCondition).Variables.Contains(ShareDataConst.SqlVariableSourceLastKey + suffix));
                if (home != null)
                {
                    keyExpr = specs.ResolveProjection(home, "__lastKey" + suffix);
                }
            }

            keyExpr ??= query != null && specs != null
                ? specs.ResolveProjection(query, "__lastKey" + suffix)
                : null;
            if (keyExpr == null) return null;

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"|{NormalizeCursorExpression(keyExpr)}")));
        }

        /// <summary>Tên các nhóm mốc có tên của câu SQL: hậu tố của cột bí danh __lastKey_&lt;nhóm&gt; ở mọi SELECT (kể cả nhánh UNION).</summary>
        public static List<string> GetCursorGroups(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return [];

            var fragment = new TSql160Parser(true).Parse(new StringReader(sql), out var errors);
            if (errors is { Count: > 0 } || fragment == null) return [];
            var specs = new QuerySpecificationCollector();
            fragment.Accept(specs);
            return specs.Specifications
                .SelectMany(s => s.SelectElements.OfType<SelectScalarExpression>())
                .Select(e => Regex.Match(e.ColumnName?.Value ?? string.Empty, @"^__lastKey_([A-Za-z0-9_]+)$", RegexOptions.IgnoreCase))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Nhận diện cột mốc để so khi lưu câu: công thức gốc của __lastKey (lần qua SELECT *, bảng dẫn xuất, câu WITH tới SELECT viết công thức)
        ///              đã chuẩn hóa như chữ ký, kèm các bảng nguồn mà cột trong công thức thuộc về. Chữ ký chỉ đọc SELECT ngoài cùng và bỏ tiền tố bảng,
        ///              nên công thức viết trong câu con, đổi bảng nguồn mà giữ công thức, lấy cột mốc từ bảng JOIN khác đều không làm đổi chữ ký.
        ///              Chữ ký đã lưu kèm mốc nên giữ cách tính; phần so khi lưu dùng thêm hàm này. Câu không có __lastKey hoặc lỗi cú pháp thì trả null.
        /// Created date: 06/11/2026
        /// </summary>
        public static string? GetCursorIdentity(string? sql, string? group = null)
        {
            if (string.IsNullOrWhiteSpace(sql)) return null;

            using var reader = new StringReader(sql);
            var fragment = new TSql160Parser(true).Parse(reader, out var errors);
            if (errors is { Count: > 0 } || fragment is not TSqlScript script) return null;
            var statement = script.Batches.SelectMany(b => b.Statements).OfType<SelectStatement>().FirstOrDefault();
            if (statement == null) return null;

            var specs = new QuerySpecificationCollector();
            statement.Accept(specs);
            var suffix = group == null ? string.Empty : "_" + group;
            var outer = statement.QueryExpression as QuerySpecification;

            // Nhóm có tên: công thức ở SELECT có WHERE dùng @lastKey_<nhóm>; không thấy thì đọc từ SELECT ngoài cùng như nhóm mặc định
            var home = group == null
                ? outer
                : specs.Specifications.FirstOrDefault(s => s.WhereClause?.SearchCondition != null
                    && References(s.WhereClause.SearchCondition).Variables.Contains(ShareDataConst.SqlVariableSourceLastKey + suffix)) ?? outer;
            var keyExpr = home == null ? null : specs.ResolveProjection(home, "__lastKey" + suffix);
            if (keyExpr == null) return null;

            var owner = specs.OwnerOf(keyExpr);
            var tables = CursorSourceTables(owner, keyExpr, statement);
            var normalizedExpr = NormalizeCursorExpression(keyExpr);
            var tableList = string.Join(",", tables);
            return $"{normalizedExpr}|{tableList}";
        }

        /// <summary>Văn bản chuẩn hóa của biểu thức cột mốc: bỏ khoảng trắng, chú thích và tiền tố bảng, viết hoa trừ chuỗi trong nháy.</summary>
        private static string NormalizeCursorExpression(TSqlFragment fragment)
        {
            var builder = new StringBuilder();
            for (var i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
            {
                var token = fragment.ScriptTokenStream[i];
                if (token.TokenType is TSqlTokenType.WhiteSpace or TSqlTokenType.SingleLineComment or TSqlTokenType.MultilineComment)
                {
                    continue;
                }

                // Tiền tố bảng / bí danh (zs. trong zs.UpdateTime): đổi bí danh không làm đổi công thức
                if (token.TokenType is TSqlTokenType.Identifier or TSqlTokenType.QuotedIdentifier
                    && i < fragment.LastTokenIndex
                    && fragment.ScriptTokenStream[i + 1].TokenType == TSqlTokenType.Dot)
                {
                    i++;
                    continue;
                }

                builder.Append(token.TokenType is TSqlTokenType.AsciiStringLiteral or TSqlTokenType.UnicodeStringLiteral
                    ? token.Text
                    : token.Text.Trim('[', ']').ToUpperInvariant());
            }
            return builder.ToString();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tên bảng nguồn (viết hoa, xếp tăng) của các cột trong công thức cột mốc, tra theo bí danh ở FROM của SELECT chứa công thức.
        ///              Bảng dẫn xuất và câu WITH tính các bảng bên trong; cột không có tiền tố thì tính mọi bảng ở FROM.
        /// Created date: 06/11/2026
        /// </summary>
        private static SortedSet<string> CursorSourceTables(QuerySpecification? owner, ScalarExpression keyExpr, SelectStatement statement)
        {
            var ctes = (statement.WithCtesAndXmlNamespaces?.CommonTableExpressions ?? [])
                .Where(cte => !string.IsNullOrWhiteSpace(cte.ExpressionName?.Value))
                .GroupBy(cte => cte.ExpressionName.Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (TSqlFragment)g.First().QueryExpression, StringComparer.OrdinalIgnoreCase);
            var sources = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in owner?.FromClause?.TableReferences ?? [])
            {
                CollectCursorSources(table, ctes, sources);
            }

            var visitor = new CursorSourceVisitor();
            keyExpr.Accept(visitor);
            var result = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var column in visitor.Columns)
            {
                var parts = column.MultiPartIdentifier?.Identifiers;
                if (parts == null || parts.Count == 0)
                {
                    continue;
                }

                if (parts.Count == 1)
                {
                    // Cột không có tiền tố: thuộc một trong các bảng ở FROM
                    foreach (var fromTables in sources.Values)
                    {
                        result.UnionWith(fromTables);
                    }
                }
                else if (sources.TryGetValue(parts[^2].Value, out var named))
                {
                    result.UnionWith(named);
                }
                else
                {
                    result.Add(parts[^2].Value.ToUpperInvariant());
                }
            }
            return result;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Gom bí danh của từng nguồn ở FROM kèm các bảng thật phía sau: bảng thường là chính nó, bảng dẫn xuất và câu WITH là các bảng bên trong.
        /// Created date: 06/11/2026
        /// </summary>
        private static void CollectCursorSources(TableReference table, Dictionary<string, TSqlFragment> ctes, Dictionary<string, SortedSet<string>> sources)
        {
            switch (table)
            {
                case QualifiedJoin join:
                    CollectCursorSources(join.FirstTableReference, ctes, sources);
                    CollectCursorSources(join.SecondTableReference, ctes, sources);
                    break;
                case UnqualifiedJoin join:
                    CollectCursorSources(join.FirstTableReference, ctes, sources);
                    CollectCursorSources(join.SecondTableReference, ctes, sources);
                    break;
                case JoinParenthesisTableReference parenthesis:
                    CollectCursorSources(parenthesis.Join, ctes, sources);
                    break;
                case NamedTableReference named when named.SchemaObject?.BaseIdentifier?.Value is { } name:
                    sources[named.Alias?.Value ?? name] = ctes.TryGetValue(name, out var cte)
                        ? BaseTables(cte, ctes)
                        : new SortedSet<string>(StringComparer.Ordinal) { name.ToUpperInvariant() };
                    break;
                case QueryDerivedTable derived when derived.Alias?.Value is { } alias:
                    sources[alias] = BaseTables(derived.QueryExpression, ctes);
                    break;
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tên các bảng thật (viết hoa) trong một câu con; tên trùng câu WITH thì lấy tiếp các bảng của câu WITH đó.
        /// Created date: 06/11/2026
        /// </summary>
        private static SortedSet<string> BaseTables(TSqlFragment fragment, Dictionary<string, TSqlFragment> ctes, int depth = 0)
        {
            var visitor = new CursorSourceVisitor();
            fragment.Accept(visitor);
            var result = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var name in visitor.Tables)
            {
                if (depth < 5 && ctes.TryGetValue(name, out var cte) && !ReferenceEquals(cte, fragment))
                {
                    var cteTables = BaseTables(cte, ctes, depth + 1);
                    result.UnionWith(cteTables);
                }
                else
                {
                    result.Add(name.ToUpperInvariant());
                }
            }
            return result;
        }

        /// <summary>Duyệt biểu thức __lastKey: đánh dấu định dạng ngày giờ không sắp được và giá trị ép sang chuỗi không được đệm.</summary>
        private sealed class LastKeyFormatVisitor(ScalarExpression keyExpr) : TSqlFragmentVisitor
        {
            /// <summary>Kiểu CONVERT ngày giờ sang chuỗi sắp đúng thứ tự (năm → giây, cố định độ dài).</summary>
            private static readonly HashSet<int> SortableDateStyles = [20, 21, 23, 25, 102, 111, 112, 120, 121, 126, 127];
            private static readonly HashSet<string> PaddingFunctions = new(StringComparer.OrdinalIgnoreCase) { "RIGHT", "REPLICATE", "FORMAT" };
            private int _paddingDepth;

            /// <summary>CONVERT kiểu 114 (chỉ có giờ) ở công thức PA1: đi cặp sau kiểu 112 cùng thời gian nên sắp được.</summary>
            private readonly ConvertCall? _formulaTime = FormulaTimeParts(keyExpr)?.Time;

            public bool DateUnsortable { get; private set; }
            public bool NotFixedWidth { get; private set; }

            /// <summary>
            /// Author: DatHQ
            /// Description: FORMAT ngày giờ không đi từ năm đến giây thì đánh dấu không sắp được; các hàm đệm RIGHT, REPLICATE, FORMAT tăng độ sâu đệm.
            /// Created date: 28/10/2026
            /// Modified date: 05/11/2026
            /// </summary>
            public override void ExplicitVisit(FunctionCall node)
            {
                var name = node.FunctionName?.Value ?? string.Empty;
                if (string.Equals(name, "FORMAT", StringComparison.OrdinalIgnoreCase)
                    && node.Parameters.Count >= 2 && node.Parameters[1] is StringLiteral pattern
                    && IsDatePattern(pattern.Value) && !IsSortableDatePattern(pattern.Value))
                {
                    DateUnsortable = true;
                }

                var padding = PaddingFunctions.Contains(name);
                if (padding) _paddingDepth++;
                base.ExplicitVisit(node);
                if (padding) _paddingDepth--;
            }

            /// <summary>ScriptDom biểu diễn RIGHT(...) / LEFT(...) bằng lớp riêng, không qua FunctionCall.</summary>
            public override void ExplicitVisit(RightFunctionCall node)
            {
                _paddingDepth++;
                base.ExplicitVisit(node);
                _paddingDepth--;
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: CONVERT sang chuỗi: kiểu ngày giờ ngoài danh sách sắp được thì đánh dấu DateUnsortable; kiểu 114 của công thức PA1 không xét.
            /// Created date: 28/10/2026
            /// Modified date: 16/11/2026
            /// </summary>
            public override void ExplicitVisit(ConvertCall node)
            {
                if (IsCharType(node.DataType) && !ReferenceEquals(node, _formulaTime))
                {
                    if (node.Style is IntegerLiteral style)
                    {
                        if (int.TryParse(style.Value, out var code) && !SortableDateStyles.Contains(code)) DateUnsortable = true;
                    }
                    else if (_paddingDepth == 0)
                    {
                        NotFixedWidth = true;
                    }
                }

                base.ExplicitVisit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: CAST sang chuỗi nằm ngoài hàm đệm thì độ dài không cố định.
            /// Created date: 28/10/2026
            /// </summary>
            public override void ExplicitVisit(CastCall node)
            {
                if (IsCharType(node.DataType) && _paddingDepth == 0) NotFixedWidth = true;
                base.ExplicitVisit(node);
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Kiểu đích là kiểu chuỗi: VARCHAR, NVARCHAR, CHAR, NCHAR.
            /// Created date: 28/10/2026
            /// </summary>
            private static bool IsCharType(DataTypeReference? type) =>
                type is SqlDataTypeReference sql && sql.SqlDataTypeOption is SqlDataTypeOption.VarChar or SqlDataTypeOption.NVarChar
                    or SqlDataTypeOption.Char or SqlDataTypeOption.NChar;

            /// <summary>
            /// Author: DatHQ
            /// Description: Mẫu FORMAT là ngày giờ (có ký hiệu ngày/giờ); mẫu số như 'D10', 'd10', '0000000000' không xét ở đây.
            /// Created date: 28/10/2026
            /// Modified date: 05/11/2026
            /// </summary>
            private static bool IsDatePattern(string pattern) =>
                Regex.IsMatch(pattern, "[yMdHhms]") && !Regex.IsMatch(pattern, @"^[dD]\d+$");

            /// <summary>
            /// Author: DatHQ
            /// Description: Mẫu ngày giờ sắp đúng thứ tự thời gian: đủ yyyy, MM, dd, các phần đi từ lớn đến nhỏ và cố định độ dài (tháng dạng số, giờ 24 HH).
            ///              Mẫu chuẩn 's' có dạng yyyy-MM-ddTHH:mm:ss nên sắp được.
            /// Created date: 28/10/2026
            /// Modified date: 05/11/2026
            /// </summary>
            private static bool IsSortableDatePattern(string pattern)
            {
                if (pattern == "s") return true;

                var parts = Regex.Matches(pattern, "y+|M+|d+|H+|h+|m+|s+|f+|F+|t+").Select(m => m.Value);
                var partSequence = string.Join(' ', parts);
                return Regex.IsMatch(partSequence, @"^yyyy MM dd( HH( mm( ss( f+)?)?)?)?$");
            }
        }
    }
}
