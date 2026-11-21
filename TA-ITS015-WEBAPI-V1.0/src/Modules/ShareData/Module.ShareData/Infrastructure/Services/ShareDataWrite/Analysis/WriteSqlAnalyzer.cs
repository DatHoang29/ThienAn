using Microsoft.SqlServer.TransactSql.ScriptDom;
using Module.ShareData.Core.Constants;
using Msg = Module.ShareData.Core.Exceptions.BaseMsg.ShareData.Validation;

namespace Module.ShareData.Infrastructure.Services.ShareDataWrite.Analysis
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Phân tích tĩnh một câu ghi chiều nhận (ShareDataSql.WriteSql) bằng ScriptDom, không cần CSDL:
    ///              - Lỗi (chặn lưu): một câu lệnh MERGE / INSERT / UPDATE, chỉ ghi vào bảng được phép ghi, không DELETE / EXEC /
    ///                OUTPUT INTO / OPENROWSET…, không dùng bảng ở CSDL khác, chỉ dùng tham số hệ thống.
    ///              - Cảnh báo (cho lưu): không đọc OPENJSON(@records), cột trong WITH lệch trường của gói, không dò được khoá,
    ///                bảng nguồn chưa GROUP BY / lọc IS NOT NULL theo khoá, MERGE thiếu OUTPUT $action.
    ///              Khoá bản ghi dò từ điều kiện ON của MERGE (t.cột = s.khoá).
    /// Created date: 26/10/2026
    /// </summary>
    public static class WriteSqlAnalyzer
    {
        /// <summary>Một kết quả kiểm: khoá thông điệp BaseMsg kèm tham số và vị trí trong câu ghi.</summary>
        public sealed record Finding(string Level, string MessageKey, object[] Args, int? Line = null, int? Column = null);

        /// <summary>Kết quả phân tích khối câu ghi.</summary>
        public sealed record Result(List<Finding> Findings, List<string> DetectedTables, List<string> KeyFields)
        {
            public bool HasError => Findings.Any(f => f.Level == ShareDataConst.WriteSqlCheckError);
        }

        /// <summary>Các mục bị cấm và vị trí, gom khi duyệt cây cú pháp.</summary>
        private sealed class SafetyVisitor : TSqlFragmentVisitor
        {
            public List<(string What, TSqlFragment At)> Forbidden { get; } = [];
            public List<(string Name, TSqlFragment At)> CrossDatabase { get; } = [];
            public List<(string Name, TSqlFragment At)> Variables { get; } = [];
            public List<string> JsonColumns { get; } = [];
            public bool ReadsRecords { get; private set; }

            // Các lệnh và nguồn dữ liệu không được dùng trong câu ghi: gom vào Forbidden để báo lỗi
            public override void Visit(ExecuteStatement node) => Forbidden.Add(("EXEC", node));
            public override void Visit(DeleteSpecification node) => Forbidden.Add(("DELETE", node));
            public override void Visit(DeleteMergeAction node) => Forbidden.Add(("DELETE", node));
            public override void Visit(OpenRowsetTableReference node) => Forbidden.Add(("OPENROWSET", node));
            public override void Visit(BulkOpenRowset node) => Forbidden.Add(("OPENROWSET", node));
            public override void Visit(InternalOpenRowset node) => Forbidden.Add(("OPENROWSET", node));
            public override void Visit(OpenQueryTableReference node) => Forbidden.Add(("OPENQUERY", node));
            public override void Visit(AdHocTableReference node) => Forbidden.Add(("OPENDATASOURCE", node));
            public override void Visit(OutputIntoClause node) => Forbidden.Add(("OUTPUT INTO", node));

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom tên đối tượng có chỉ định máy chủ hoặc CSDL khác để báo lỗi truy cập chéo.
            /// Created date: 27/10/2026
            /// </summary>
            public override void Visit(SchemaObjectName node)
            {
                if (node.ServerIdentifier != null || node.DatabaseIdentifier != null)
                {
                    CrossDatabase.Add((FormatName(node), node));
                }
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom các biến câu ghi có dùng, bỏ dấu @.
            /// Created date: 27/10/2026
            /// </summary>
            public override void Visit(VariableReference node) => Variables.Add((node.Name.TrimStart('@'), node));

            /// <summary>
            /// Author: DatHQ
            /// Description: Nhận biết câu có đọc @records qua OPENJSON và đọc khai báo cột của nó.
            /// Created date: 27/10/2026
            /// </summary>
            public override void Visit(OpenJsonTableReference node)
            {
                if (node.Variable is VariableReference v && string.Equals(v.Name, "@records", StringComparison.OrdinalIgnoreCase))
                {
                    ReadsRecords = true;
                }

                foreach (var item in node.SchemaDeclarationItems)
                {
                    // Cột có đường dẫn riêng ('$.khoá') thì khoá JSON lấy theo đường dẫn, không thì theo tên cột
                    var path = (item.Mapping as StringLiteral)?.Value;
                    JsonColumns.Add(path != null && path.StartsWith("$.", StringComparison.Ordinal)
                        ? path[2..]
                        : item.ColumnDefinition.ColumnIdentifier.Value);
                }
            }
        }

        /// <summary>Gom các phép so sánh bằng và kiểm IS NOT NULL trong một biểu thức điều kiện.</summary>
        private sealed class ConditionVisitor : TSqlFragmentVisitor
        {
            public List<BooleanComparisonExpression> EqualComparisons { get; } = [];
            public List<string> NotNullColumns { get; } = [];

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom các phép so bằng.
            /// Created date: 27/10/2026
            /// </summary>
            public override void Visit(BooleanComparisonExpression node)
            {
                if (node.ComparisonType == BooleanComparisonType.Equals)
                {
                    EqualComparisons.Add(node);
                }
            }

            /// <summary>
            /// Author: DatHQ
            /// Description: Gom các cột được kiểm IS NOT NULL.
            /// Created date: 27/10/2026
            /// </summary>
            public override void Visit(BooleanIsNullExpression node)
            {
                if (node.IsNot && node.Expression is ColumnReferenceExpression column && LastPart(column) is { } name)
                {
                    NotNullColumns.Add(name);
                }
            }
        }

        /// <summary>
        /// Description: Phân tích khối câu ghi (cho phép 1 hoặc nhiều câu MERGE/INSERT/UPDATE trong một batch).
        ///              affectedTables là danh sách bảng bị ảnh hưởng đã khai; packetFieldKeys là khoá các trường của gói.
        /// </summary>
        public static Result Analyze(string? writeSql, IEnumerable<string>? affectedTables, IReadOnlyCollection<string>? packetFieldKeys)
        {
            var findings = new List<Finding>();
            var detectedTables = new List<string>();
            var keyFields = new List<string>();
            var declaredTables = (affectedTables ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .DistinctBy(NormalizeTable, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(writeSql))
            {
                if (declaredTables.Count == 0)
                {
                    findings.Add(Error(Msg.WriteTableRequired));
                }
                findings.Add(Error(Msg.WriteSqlEmpty));
                return new Result(findings, detectedTables, keyFields);
            }

            var parser = new TSql160Parser(true);
            using var reader = new StringReader(writeSql);
            var fragment = parser.Parse(reader, out var errors);
            if (errors != null && errors.Count > 0)
            {
                var e = errors[0];
                findings.Add(new Finding(ShareDataConst.WriteSqlCheckError, Msg.SqlScriptSyntaxError, [e.Line, e.Column, e.Message], e.Line, e.Column));
                return new Result(findings, detectedTables, keyFields);
            }

            if (fragment is not TSqlScript script || script.Batches.Count != 1)
            {
                findings.Add(Error(Msg.SqlMultipleBatchesNotAllowed));
                return new Result(findings, detectedTables, keyFields);
            }
            if (script.Batches[0].Statements.Count == 0)
            {
                findings.Add(Error(Msg.WriteSqlEmpty));
                return new Result(findings, detectedTables, keyFields);
            }

            var statementTargets = new List<(string TargetName, TSqlStatement Statement)>();
            foreach (var statement in script.Batches[0].Statements)
            {
                var targetName = statement switch
                {
                    MergeStatement m => TableName(m.MergeSpecification.Target, null),
                    InsertStatement i => TableName(i.InsertSpecification.Target, null),
                    UpdateStatement u => TableName(u.UpdateSpecification.Target, u.UpdateSpecification.FromClause),
                    _ => null
                };
                if (statement is not (MergeStatement or InsertStatement or UpdateStatement) || targetName == null)
                {
                    findings.Add(Error(Msg.WriteSqlStatementInvalid, at: statement));
                    return new Result(findings, detectedTables, keyFields);
                }

                statementTargets.Add((targetName, statement));
                if (!detectedTables.Any(t => NormalizeTable(t) == NormalizeTable(targetName)))
                {
                    detectedTables.Add(targetName);
                }
            }

            if (declaredTables.Count == 0)
            {
                findings.Add(Error(Msg.WriteTableRequired));
            }
            else
            {
                var allowedNormalized = declaredTables.Select(NormalizeTable).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var allowedLabel = string.Join(", ", declaredTables);
                foreach (var (targetName, statement) in statementTargets)
                {
                    if (!allowedNormalized.Contains(NormalizeTable(targetName)))
                    {
                        findings.Add(Error(Msg.WriteSqlTableNotAllowed, [targetName, allowedLabel], statement));
                    }
                }

                var detectedNormalized = detectedTables.Select(NormalizeTable).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var declared in declaredTables)
                {
                    if (!detectedNormalized.Contains(NormalizeTable(declared)))
                    {
                        findings.Add(Warning(Msg.WriteSqlTableUnused, [declared]));
                    }
                }
            }

            var safety = new SafetyVisitor();
            script.Accept(safety);
            foreach (var (what, at) in safety.Forbidden)
            {
                findings.Add(Error(Msg.WriteSqlForbidden, [what], at));
            }
            foreach (var (name, at) in safety.CrossDatabase)
            {
                findings.Add(Error(Msg.WriteSqlCrossDatabase, [name], at));
            }
            var allowedParameters = string.Join(", ", ShareDataConst.WriteSqlSystemParameters.Keys.Select(k => "@" + k));
            foreach (var (name, at) in safety.Variables.Where(v => !ShareDataConst.WriteSqlSystemParameters.ContainsKey(v.Name)).DistinctBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
            {
                findings.Add(Error(Msg.WriteSqlParameterInvalid, [name, allowedParameters], at));
            }

            if (!safety.ReadsRecords)
            {
                findings.Add(Warning(Msg.WriteSqlRecordsNotUsed, at: script.Batches[0].Statements[0]));
            }
            if (packetFieldKeys is { Count: > 0 })
            {
                foreach (var column in safety.JsonColumns.Distinct(StringComparer.Ordinal))
                {
                    if (packetFieldKeys.Contains(column, StringComparer.Ordinal))
                    {
                        continue;
                    }
                    var sameIgnoringCase = packetFieldKeys.FirstOrDefault(k => string.Equals(k, column, StringComparison.OrdinalIgnoreCase));
                    findings.Add(sameIgnoringCase != null
                        ? Warning(Msg.WriteSqlFieldCaseMismatch, [column, sameIgnoringCase])
                        : Warning(Msg.WriteSqlFieldNotInPacket, [column]));
                }
            }

            foreach (var statement in script.Batches[0].Statements)
            {
                if (statement is MergeStatement merge)
                {
                    CheckMerge(merge.MergeSpecification, findings, keyFields);
                }
            }

            return new Result(findings, detectedTables, keyFields);
        }

        /// <summary>Dò khoá từ điều kiện ON, kiểm bảng nguồn gộp/lọc theo khoá và OUTPUT $action của câu MERGE.</summary>
        private static void CheckMerge(MergeSpecification spec, List<Finding> findings, List<string> keyFields)
        {
            var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (spec.TableAlias?.Value is { } mergeAlias) targetNames.Add(mergeAlias);
            if (spec.Target is NamedTableReference target)
            {
                if (target.Alias?.Value is { } targetAlias) targetNames.Add(targetAlias);
                targetNames.Add(target.SchemaObject.BaseIdentifier.Value);
            }
            var sourceAlias = (spec.TableReference as TableReferenceWithAlias)?.Alias?.Value;

            var stmtKeys = new List<string>();
            var on = new ConditionVisitor();
            spec.SearchCondition?.Accept(on);
            foreach (var cmp in on.EqualComparisons)
            {
                if (cmp.FirstExpression is not ColumnReferenceExpression a || cmp.SecondExpression is not ColumnReferenceExpression b)
                {
                    continue;
                }
                var key = SourceKey(a, b, targetNames, sourceAlias) ?? SourceKey(b, a, targetNames, sourceAlias);
                if (key != null && !stmtKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    stmtKeys.Add(key);
                }
                if (key != null && !keyFields.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    keyFields.Add(key);
                }
            }

            if (stmtKeys.Count == 0)
            {
                findings.Add(Warning(Msg.WriteSqlKeyNotFound, at: spec.SearchCondition));
            }
            else
            {
                var source = (spec.TableReference as QueryDerivedTable)?.QueryExpression as QuerySpecification;
                var grouped = source?.GroupByClause?.GroupingSpecifications
                    .OfType<ExpressionGroupingSpecification>()
                    .Select(g => g.Expression is ColumnReferenceExpression c ? LastPart(c) : null)
                    .Where(n => n != null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
                var filter = new ConditionVisitor();
                source?.WhereClause?.Accept(filter);
                foreach (var key in stmtKeys)
                {
                    if (!grouped.Contains(key))
                    {
                        findings.Add(Warning(Msg.WriteSqlKeyNotGrouped, [key], spec.TableReference));
                    }
                    if (!filter.NotNullColumns.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        findings.Add(Warning(Msg.WriteSqlKeyNotFiltered, [key], spec.TableReference));
                    }
                }
            }

            var hasAction = spec.OutputClause?.SelectColumns
                .OfType<SelectScalarExpression>()
                .Any(s => s.Expression is ColumnReferenceExpression { ColumnType: ColumnType.PseudoColumnAction }) == true;
            if (!hasAction)
            {
                findings.Add(Warning(Msg.WriteSqlMissingOutputAction));
            }
        }

        /// <summary>a là cột bảng đích (t.X), b là cột bảng nguồn (s.y) thì trả y.</summary>
        private static string? SourceKey(ColumnReferenceExpression a, ColumnReferenceExpression b, HashSet<string> targetNames, string? sourceAlias)
        {
            var aParts = a.MultiPartIdentifier?.Identifiers;
            var bParts = b.MultiPartIdentifier?.Identifiers;
            if (aParts is not { Count: >= 2 } || bParts is not { Count: >= 2 } || sourceAlias == null)
            {
                return null;
            }
            return targetNames.Contains(aParts[^2].Value) && string.Equals(bParts[^2].Value, sourceAlias, StringComparison.OrdinalIgnoreCase)
                ? bParts[^1].Value
                : null;
        }

        /// <summary>Tên bảng đích của câu ghi; câu UPDATE viết theo bí danh trong FROM thì tra ra tên bảng thật.</summary>
        private static string? TableName(TableReference? target, FromClause? from)
        {
            if (target is not NamedTableReference named)
            {
                return null;
            }

            var name = named.SchemaObject;
            if (from != null && name.SchemaIdentifier == null)
            {
                var aliased = from.TableReferences
                    .SelectMany(Flatten)
                    .FirstOrDefault(t => string.Equals(t.Alias?.Value, name.BaseIdentifier.Value, StringComparison.OrdinalIgnoreCase));
                if (aliased != null)
                {
                    name = aliased.SchemaObject;
                }
            }
            return FormatName(name);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Trải các bảng trong mệnh đề JOIN thành danh sách phẳng; nguồn khác bảng có tên thì bỏ qua.
        /// Created date: 27/10/2026
        /// </summary>
        private static IEnumerable<NamedTableReference> Flatten(TableReference reference) => reference switch
        {
            NamedTableReference n => [n],
            QualifiedJoin j => Flatten(j.FirstTableReference).Concat(Flatten(j.SecondTableReference)),
            UnqualifiedJoin j => Flatten(j.FirstTableReference).Concat(Flatten(j.SecondTableReference)),
            _ => []
        };

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghép đầy đủ tên đối tượng, cách nhau bằng dấu chấm.
        /// Created date: 27/10/2026
        /// </summary>
        private static string FormatName(SchemaObjectName name)
            => string.Join(".", name.Identifiers.Select(i => i.Value));

        /// <summary>So bảng không phân biệt hoa thường, bỏ [], thiếu schema thì coi là dbo.</summary>
        public static string NormalizeTable(string name)
        {
            var parts = name.Replace("[", string.Empty).Replace("]", string.Empty).Trim().Split('.');
            return (parts.Length == 1 ? "dbo." + parts[0] : string.Join(".", parts)).ToLowerInvariant();
        }

        /// <summary>Tách chuỗi danh sách bảng (AffectedTables hoặc fallback TableName) thành danh sách không trùng.</summary>
        public static List<string> ParseTableList(string? affectedTablesCsv, string? fallbackTable = null)
        {
            var source = !string.IsNullOrWhiteSpace(affectedTablesCsv) ? affectedTablesCsv : fallbackTable;
            if (string.IsNullOrWhiteSpace(source))
            {
                return [];
            }

            return source
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .DistinctBy(NormalizeTable, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tên cột không kèm tiền tố bảng.
        /// Created date: 27/10/2026
        /// </summary>
        private static string? LastPart(ColumnReferenceExpression column)
            => column.MultiPartIdentifier?.Identifiers is { Count: > 0 } ids ? ids[^1].Value : null;

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo kết quả kiểm mức lỗi, kèm vị trí trong câu nếu có.
        /// Created date: 27/10/2026
        /// </summary>
        private static Finding Error(string key, object[]? args = null, TSqlFragment? at = null)
            => new(ShareDataConst.WriteSqlCheckError, key, args ?? [], at?.StartLine, at?.StartColumn);

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo kết quả kiểm mức cảnh báo, kèm vị trí trong câu nếu có.
        /// Created date: 27/10/2026
        /// </summary>
        private static Finding Warning(string key, object[]? args = null, TSqlFragment? at = null)
            => new(ShareDataConst.WriteSqlCheckWarning, key, args ?? [], at?.StartLine, at?.StartColumn);
    }
}
