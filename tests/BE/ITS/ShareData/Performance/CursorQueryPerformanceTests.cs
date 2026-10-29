using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Xunit.Abstractions;

namespace Tests.ShareData.Performance
{
    /// <summary>
    /// Description: Đo hiệu năng câu truy vấn nối đuôi trên bảng lớn, so 3 phương án khóa nối đuôi bàn ngày 28/10/2026
    ///              (PA1 ghép chuỗi ngay trong SQL, PA2 chỉ tính công thức cho dòng cuối, PA3 ghép chuỗi ở Worker C#)
    ///              với cách đang chạy (2 cột __lastTime + __lastKey: gói 101 so cột thô, gói 103–111 so COALESCE)
    ///              và một cách viết lại câu 2 cột có điều kiện mở đầu dùng được chỉ mục.
    ///              Dữ liệu nằm trong bảng tạm trên một kết nối riêng tới CSDL test local, đóng kết nối là bảng tự mất.
    /// Created date: 29/10/2026
    /// </summary>
    [Collection("api")]
    public class CursorQueryPerformanceTests
    {
        private const int PageSize = 100;
        private const int DefaultRowCount = 1_000_000;
        private const int TimingRuns = 3;
        private const string RowCountVariable = "SHAREDATA_PERF_ROWS";
        private static readonly XNamespace ShowPlan = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";
        private static readonly Regex IoStatistics = new(@"Table '(?<table>[^']+)'\. Scan count (?<scans>\d+), logical reads (?<reads>\d+)", RegexOptions.Compiled);
        private static readonly HashSet<string> RangeOperators = new(StringComparer.Ordinal) { "Index Seek", "Index Scan", "Clustered Index Seek", "Clustered Index Scan", "Table Scan" };

        private const string ConcatKeyExpression = "CONVERT(varchar(23), s.UpdateTime, 121) + '_' + s.ID";

        /// <summary>Các cách lấy một trang nối đuôi. {tag} là chú thích tách kế hoạch thực thi theo từng cách × vị trí mốc.</summary>
        private static readonly QueryOption[] Options =
        [
            new("PA1", "PA1 · Ghép chuỗi trong SQL", CursorShape.ConcatKey, $"""
                {"{tag}"}SELECT TOP (@pageSize) s.ID, s.Code, s.Speed, s.UpdateTime, {ConcatKeyExpression} AS __lastKey
                FROM #CursorSource s
                WHERE {ConcatKeyExpression} > @lastKey
                ORDER BY __lastKey ASC
                """),
            new("PA2", "PA2 · Công thức chỉ tính dòng cuối", CursorShape.ConcatKey, $"""
                {"{tag}"}SELECT TOP (@pageSize) s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE {ConcatKeyExpression} > @lastKey
                ORDER BY s.UpdateTime ASC, s.ID ASC
                """,
                LastRowKeySql: $"""
                {"{tag}"}SELECT {ConcatKeyExpression} AS __lastKey
                FROM #CursorSource s
                WHERE s.ID = @lastRowId
                """),
            new("PA3", "PA3 · Ghép chuỗi ở Worker C#", CursorShape.ConcatKey, $"""
                {"{tag}"}SELECT TOP (@pageSize) s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE {ConcatKeyExpression} > @lastKey
                ORDER BY s.UpdateTime ASC, s.ID ASC
                """, KeyBuiltInWorker: true),
            new("HT-101", "Hiện tại · gói 101 (so cột thô)", CursorShape.TimeAndKey, """
                {tag}SELECT TOP (@pageSize) s.UpdateTime AS __lastTime, s.ID AS __lastKey, s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE @lastTime IS NULL
                   OR s.UpdateTime > @lastTime
                   OR (s.UpdateTime = @lastTime AND s.ID > @lastKey)
                ORDER BY s.UpdateTime ASC, s.ID ASC
                """),
            new("HT-101S", "Hiện tại · gói 101, dùng lại kế hoạch dựng lúc chưa có mốc", CursorShape.TimeAndKey, """
                {tag}SELECT TOP (@pageSize) s.UpdateTime AS __lastTime, s.ID AS __lastKey, s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE @lastTime IS NULL
                   OR s.UpdateTime > @lastTime
                   OR (s.UpdateTime = @lastTime AND s.ID > @lastKey)
                ORDER BY s.UpdateTime ASC, s.ID ASC
                """, SharePlanAcrossPositions: true),
            new("HT-101R", "Hiện tại · gói 101 + OPTION (RECOMPILE)", CursorShape.TimeAndKey, """
                {tag}SELECT TOP (@pageSize) s.UpdateTime AS __lastTime, s.ID AS __lastKey, s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE @lastTime IS NULL
                   OR s.UpdateTime > @lastTime
                   OR (s.UpdateTime = @lastTime AND s.ID > @lastKey)
                ORDER BY s.UpdateTime ASC, s.ID ASC
                OPTION (RECOMPILE)
                """),
            new("HT-103", "Hiện tại · gói 103–111 (so COALESCE)", CursorShape.TimeAndKey, """
                {tag}SELECT TOP (@pageSize) COALESCE(s.UpdateTime, s.CreateTime, s.DetectTime) AS __lastTime, s.ID AS __lastKey, s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE @lastTime IS NULL
                   OR COALESCE(s.UpdateTime, s.CreateTime, s.DetectTime) > @lastTime
                   OR (COALESCE(s.UpdateTime, s.CreateTime, s.DetectTime) = @lastTime AND s.ID > @lastKey)
                ORDER BY COALESCE(s.UpdateTime, s.CreateTime, s.DetectTime) ASC, s.ID ASC
                OPTION (RECOMPILE)
                """),
            new("DX", "Đề xuất · 2 cột, điều kiện mở đầu >=", CursorShape.TimeAndKey, """
                {tag}SELECT TOP (@pageSize) s.UpdateTime AS __lastTime, s.ID AS __lastKey, s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE s.UpdateTime >= ISNULL(@lastTime, '17530101')
                  AND (@lastTime IS NULL OR s.UpdateTime > @lastTime OR s.ID > @lastKey)
                ORDER BY s.UpdateTime ASC, s.ID ASC
                """),
            new("DX-S", "Đề xuất · dùng lại kế hoạch dựng lúc chưa có mốc", CursorShape.TimeAndKey, """
                {tag}SELECT TOP (@pageSize) s.UpdateTime AS __lastTime, s.ID AS __lastKey, s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE s.UpdateTime >= ISNULL(@lastTime, '17530101')
                  AND (@lastTime IS NULL OR s.UpdateTime > @lastTime OR s.ID > @lastKey)
                ORDER BY s.UpdateTime ASC, s.ID ASC
                """, SharePlanAcrossPositions: true),
            new("DX-103", "Viết lại kiểu >= cho gói 103–111 (vẫn so COALESCE)", CursorShape.TimeAndKey, """
                {tag}SELECT TOP (@pageSize) COALESCE(s.UpdateTime, s.CreateTime, s.DetectTime) AS __lastTime, s.ID AS __lastKey, s.ID, s.Code, s.Speed, s.UpdateTime
                FROM #CursorSource s
                WHERE COALESCE(s.UpdateTime, s.CreateTime, s.DetectTime) >= ISNULL(@lastTime, '17530101')
                  AND (@lastTime IS NULL OR COALESCE(s.UpdateTime, s.CreateTime, s.DetectTime) > @lastTime OR s.ID > @lastKey)
                ORDER BY COALESCE(s.UpdateTime, s.CreateTime, s.DetectTime) ASC, s.ID ASC
                """),
        ];

        private readonly Host _host;
        private readonly ITestOutputHelper _output;

        public CursorQueryPerformanceTests(Host host, ITestOutputHelper output)
        {
            _host = host;
            _output = output;
        }

        /// <summary>
        /// Description: Trên bảng 1 triệu dòng có chỉ mục (UpdateTime, ID), lấy trang 100 dòng bằng từng cách ở 3 vị trí mốc
        ///              (chưa có mốc, đã gửi một nửa, chỉ còn 500 dòng mới) rồi so:
        ///              - mọi cách trả cùng một trang và cùng mốc kế tiếp (khóa ghép "thời gian_mã" tương đương cặp UpdateTime, ID);
        ///              - mốc ở cuối: cách 2 cột có điều kiện mở đầu >= dùng Index Seek và đọc ít trang (kể cả khi dùng lại
        ///                kế hoạch dựng lúc chưa có mốc), các cách so biểu thức
        ///                (ghép chuỗi, COALESCE) phải đọc gấp nhiều lần vì không dùng được chỉ mục.
        ///              Số liệu đầy đủ ghi ra kết quả test và tệp perf-results/cursor-query.md ở thư mục chạy test.
        /// Created date: 29/10/2026
        /// </summary>
        [Fact(Skip = "Đo hiệu năng trên bảng tạm 1 triệu dòng, chạy khoảng 2–3 phút — bỏ Skip khi cần đo lại; đổi số dòng bằng biến môi trường SHAREDATA_PERF_ROWS.")]
        [Trait("Feature", "Performance")]
        public async Task CursorQuery_OnLargeTable_ExpressionCursorScansWhileRawColumnCursorSeeks_Test()
        {
            var rowCount = int.TryParse(Environment.GetEnvironmentVariable(RowCountVariable), NumberStyles.Integer, CultureInfo.InvariantCulture, out var configured) && configured >= 10_000
                ? configured
                : DefaultRowCount;
            var connectionString = _host.Services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection trong appsettings.Test.json.");

            await using var conn = new SqlConnection(connectionString);
            var messages = new List<string>();
            conn.InfoMessage += (_, e) =>
            {
                lock (messages)
                {
                    foreach (SqlError error in e.Errors)
                    {
                        messages.Add(error.Message);
                    }
                }
            };
            await conn.OpenAsync();

            var server = await ScalarAsync(conn, "SELECT CONCAT(CAST(SERVERPROPERTY('Edition') AS nvarchar(128)), N' ', CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)))");
            var seedWatch = Stopwatch.StartNew();
            await SeedSourceAsync(conn, rowCount);
            seedWatch.Stop();

            CursorPosition[] positions =
            [
                new("start", "Chưa có mốc (lần gửi đầu / chạy lại từ đầu)", null, string.Empty, string.Empty),
                await ReadPositionAsync(conn, "mid", "Đã gửi 50%", rowCount / 2),
                await ReadPositionAsync(conn, "end", "Còn 500 dòng mới (chạy định kỳ)", rowCount - 500),
            ];

            var results = new List<Measurement>();
            foreach (var position in positions)
            {
                foreach (var option in Options)
                {
                    results.Add(await MeasureAsync(conn, messages, option, position));
                }
            }

            WriteReport(server, rowCount, seedWatch.Elapsed, positions, results);

            foreach (var position in positions)
            {
                var atPosition = results.Where(r => r.Position == position).ToList();
                var reference = atPosition.Single(r => r.Option.Code == "HT-101");
                Assert.Equal(PageSize, reference.PageIds.Count);
                foreach (var r in atPosition)
                {
                    Assert.True(reference.PageIds.SequenceEqual(r.PageIds), $"{r.Option.Code} ở mốc {position.Code} trả trang khác HT-101.");
                }

                var lastRow = reference.LastRow ?? throw new InvalidOperationException("Trang HT-101 rỗng.");
                var expectedConcatKey = BuildKeyInWorker(lastRow);
                foreach (var r in atPosition.Where(r => r.Option.Shape == CursorShape.ConcatKey))
                {
                    Assert.Equal(expectedConcatKey, r.NextCursor);
                }
            }

            var end = results.Where(r => r.Position.Code == "end").ToDictionary(r => r.Option.Code);
            foreach (var code in new[] { "DX", "DX-S" })
            {
                Assert.Contains("Index Seek", end[code].Plan, StringComparison.Ordinal);
                Assert.True(end[code].LogicalReads <= 2_000, $"{code} đọc {end[code].LogicalReads} trang.");
            }
            foreach (var code in new[] { "PA1", "PA2", "PA3", "HT-103", "DX-103" })
            {
                Assert.True(end[code].LogicalReads >= 10 * end["DX"].LogicalReads,
                    $"{code} đọc {end[code].LogicalReads} trang, chưa gấp 10 lần DX ({end["DX"].LogicalReads}).");
            }
        }

        private enum CursorShape
        {
            /// <summary>Một chuỗi ghép thời gian + mã (PA1, PA2, PA3).</summary>
            ConcatKey,

            /// <summary>Hai cột mốc __lastTime + __lastKey (cách đang chạy).</summary>
            TimeAndKey
        }

        private sealed record QueryOption(string Code, string Name, CursorShape Shape, string Sql, string? LastRowKeySql = null, bool KeyBuiltInWorker = false, bool SharePlanAcrossPositions = false);

        private sealed record CursorPosition(string Code, string Name, DateTime? LastTime, string LastId, string ConcatKey);

        private sealed record PageRow(string Id, DateTime? UpdateTime, string? LastKey);

        private sealed record QueryRun(List<PageRow> Rows, string? PlanXml);

        private sealed record Measurement(
            QueryOption Option,
            CursorPosition Position,
            int RoundTrips,
            int RowsReturned,
            long LogicalReads,
            long RowsRead,
            string Plan,
            double MedianMs,
            IReadOnlyList<string> PageIds,
            string NextCursor,
            PageRow? LastRow);

        private static async Task SeedSourceAsync(SqlConnection conn, int rowCount)
        {
            // Câu không tham số chạy thẳng trên phiên (không qua sp_executesql) nên bảng tạm còn sống cho các câu đo phía sau.
            // Mỗi mốc thời gian có 3 dòng để luôn có dòng trùng giờ; cột thời gian cho phép NULL như cột UpdateTime thật.
            var sql = $"""
                CREATE TABLE #CursorSource (
                    ID NVARCHAR(64) NOT NULL PRIMARY KEY CLUSTERED,
                    Code NVARCHAR(64) NOT NULL,
                    UpdateTime DATETIME NULL,
                    CreateTime DATETIME NULL,
                    DetectTime DATETIME NULL,
                    Speed DECIMAL(18, 2) NULL,
                    Payload NVARCHAR(100) NULL
                );
                WITH n AS (
                    SELECT TOP ({rowCount}) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
                    FROM sys.all_columns a CROSS JOIN sys.all_columns b
                )
                INSERT INTO #CursorSource (ID, Code, UpdateTime, CreateTime, DetectTime, Speed, Payload)
                SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())),
                       CONCAT(N'Z', i % 500),
                       DATEADD(MILLISECOND, (i / 3) * 10, CAST('20260101' AS DATETIME)),
                       DATEADD(MILLISECOND, (i / 3) * 10 - 1000, CAST('20260101' AS DATETIME)),
                       DATEADD(MILLISECOND, (i / 3) * 10 - 2000, CAST('20260101' AS DATETIME)),
                       CAST(i % 120 AS DECIMAL(18, 2)),
                       REPLICATE(N'x', 60)
                FROM n;
                CREATE NONCLUSTERED INDEX IX_CursorSource_UpdateTime_ID ON #CursorSource (UpdateTime, ID);
                UPDATE STATISTICS #CursorSource WITH FULLSCAN;
                """;
            await ExecuteAsync(conn, sql, 1800);
        }

        private static async Task<CursorPosition> ReadPositionAsync(SqlConnection conn, string code, string name, int rowNumber)
        {
            const string sql = """
                SELECT x.UpdateTime, x.ID, CONVERT(varchar(23), x.UpdateTime, 121) + '_' + x.ID AS ConcatKey
                FROM (SELECT s.UpdateTime, s.ID, ROW_NUMBER() OVER (ORDER BY s.UpdateTime, s.ID) AS rn FROM #CursorSource s) x
                WHERE x.rn = @rowNumber
                """;
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 600 };
            cmd.Parameters.Add(new SqlParameter("@rowNumber", SqlDbType.BigInt) { Value = (long)rowNumber });
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), $"Không đọc được dòng thứ {rowNumber} làm mốc.");
            return new CursorPosition(code, name, reader.GetDateTime(0), reader.GetString(1), reader.GetString(2));
        }

        private static async Task<Measurement> MeasureAsync(SqlConnection conn, List<string> messages, QueryOption option, CursorPosition position)
        {
            var tag = option.SharePlanAcrossPositions ? $"/* perf {option.Code} */ " : $"/* perf {option.Code} {position.Code} */ ";
            var mainSql = option.Sql.Replace("{tag}", tag, StringComparison.Ordinal);
            var lastRowSql = option.LastRowKeySql?.Replace("{tag}", tag, StringComparison.Ordinal);

            // Lượt đo đầu bật STATISTICS IO + XML: số trang đọc, số dòng phải đọc và toán tử của kế hoạch thực thi
            await ExecuteAsync(conn, "SET STATISTICS IO ON; SET STATISTICS XML ON;");
            lock (messages)
            {
                messages.Clear();
            }
            var main = await QueryAsync(conn, mainSql, BuildParameters(option, position));
            var plans = new List<string?> { main.PlanXml };
            var lastRow = main.Rows.LastOrDefault();
            var nextCursor = string.Empty;
            if (lastRow != null)
            {
                nextCursor = option.Shape == CursorShape.TimeAndKey
                    ? $"{lastRow.UpdateTime:yyyy-MM-dd HH:mm:ss.fff}|{lastRow.Id}"
                    : lastRow.LastKey ?? string.Empty;
                if (lastRowSql != null)
                {
                    var keyRun = await QueryAsync(conn, lastRowSql, [LastRowIdParameter(lastRow.Id)]);
                    plans.Add(keyRun.PlanXml);
                    nextCursor = keyRun.Rows.Single().LastKey ?? string.Empty;
                }
                if (option.KeyBuiltInWorker)
                {
                    nextCursor = BuildKeyInWorker(lastRow);
                }
            }
            await ExecuteAsync(conn, "SET STATISTICS XML OFF; SET STATISTICS IO OFF;");

            long logicalReads;
            lock (messages)
            {
                logicalReads = messages.SelectMany(m => IoStatistics.Matches(m)).Sum(m => long.Parse(m.Groups["reads"].Value, CultureInfo.InvariantCulture));
            }
            var (rowsRead, plan) = SummarizePlans(plans);

            var timings = new List<double>();
            for (var i = 0; i < TimingRuns; i++)
            {
                var watch = Stopwatch.StartNew();
                var run = await QueryAsync(conn, mainSql, BuildParameters(option, position));
                if (lastRowSql != null && run.Rows.Count > 0)
                {
                    await QueryAsync(conn, lastRowSql, [LastRowIdParameter(run.Rows[^1].Id)]);
                }
                watch.Stop();
                timings.Add(watch.Elapsed.TotalMilliseconds);
            }
            timings.Sort();

            return new Measurement(option, position, lastRowSql == null ? 1 : 2, main.Rows.Count, logicalReads, rowsRead, plan,
                timings[timings.Count / 2], [.. main.Rows.Select(r => r.Id)], nextCursor, lastRow);
        }

        private static SqlParameter[] BuildParameters(QueryOption option, CursorPosition position)
        {
            var pageSize = new SqlParameter("@pageSize", SqlDbType.Int) { Value = PageSize };
            if (option.Shape == CursorShape.ConcatKey)
            {
                return [pageSize, new SqlParameter("@lastKey", SqlDbType.NVarChar, 256) { Value = position.ConcatKey }];
            }

            return
            [
                pageSize,
                new SqlParameter("@lastTime", SqlDbType.DateTime) { Value = (object?)position.LastTime ?? DBNull.Value },
                new SqlParameter("@lastKey", SqlDbType.NVarChar, 256) { Value = position.LastId },
            ];
        }

        private static SqlParameter LastRowIdParameter(string id) => new("@lastRowId", SqlDbType.NVarChar, 64) { Value = id };

        /// <summary>Công thức PA3 viết bằng C#: cùng định dạng CONVERT(varchar(23), …, 121) + '_' + ID của SQL.</summary>
        private static string BuildKeyInWorker(PageRow row) =>
            (row.UpdateTime ?? DateTime.MinValue).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "_" + row.Id;

        private static async Task<QueryRun> QueryAsync(SqlConnection conn, string sql, SqlParameter[] parameters)
        {
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 600 };
            cmd.Parameters.AddRange(parameters);
            await using var reader = await cmd.ExecuteReaderAsync();
            var idOrdinal = FindOrdinal(reader, "ID");
            var timeOrdinal = FindOrdinal(reader, "UpdateTime");
            var keyOrdinal = FindOrdinal(reader, "__lastKey");
            var rows = new List<PageRow>();
            while (await reader.ReadAsync())
            {
                rows.Add(new PageRow(
                    idOrdinal >= 0 ? reader.GetString(idOrdinal) : string.Empty,
                    timeOrdinal >= 0 && !reader.IsDBNull(timeOrdinal) ? reader.GetDateTime(timeOrdinal) : null,
                    keyOrdinal >= 0 && !reader.IsDBNull(keyOrdinal) ? reader.GetString(keyOrdinal) : null));
            }

            string? planXml = null;
            while (await reader.NextResultAsync())
            {
                if (reader.FieldCount == 1 && reader.GetName(0).Contains("Showplan", StringComparison.OrdinalIgnoreCase) && await reader.ReadAsync())
                {
                    planXml = reader.GetString(0);
                }
            }
            return new QueryRun(rows, planXml);
        }

        private static int FindOrdinal(SqlDataReader reader, string name)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>Tóm tắt kế hoạch thực thi thật: các toán tử đọc bảng / chỉ mục, sắp xếp, tra khóa và tổng số dòng các toán tử đọc phải duyệt.</summary>
        private static (long RowsRead, string Plan) SummarizePlans(IEnumerable<string?> plans)
        {
            long rowsRead = 0;
            var steps = new List<string>();
            foreach (var xml in plans)
            {
                if (string.IsNullOrWhiteSpace(xml))
                    continue;

                foreach (var relOp in XDocument.Parse(xml).Descendants(ShowPlan + "RelOp"))
                {
                    var physical = (string?)relOp.Attribute("PhysicalOp") ?? string.Empty;
                    if (RangeOperators.Contains(physical))
                    {
                        var index = relOp.Elements().SelectMany(e => e.Elements(ShowPlan + "Object")).Select(o => (string?)o.Attribute("Index")).FirstOrDefault(i => i != null)?.Trim('[', ']') ?? string.Empty;
                        var indexLabel = index.StartsWith("PK__", StringComparison.Ordinal) ? "khóa chính" : index;
                        steps.Add(indexLabel.Length > 0 ? $"{physical} ({indexLabel})" : physical);
                        rowsRead += relOp.Element(ShowPlan + "RunTimeInformation")?.Elements(ShowPlan + "RunTimeCountersPerThread")
                            .Sum(c => (long?)c.Attribute("ActualRowsRead") ?? (long?)c.Attribute("ActualRows") ?? 0) ?? 0;
                    }
                    else if (physical is "Key Lookup" or "RID Lookup")
                    {
                        steps.Add(physical);
                    }
                    else if (physical == "Sort")
                    {
                        steps.Add((string?)relOp.Attribute("LogicalOp") == "TopN Sort" ? "Top N Sort" : "Sort");
                    }
                }
            }
            return (rowsRead, string.Join(" + ", steps.Distinct()));
        }

        private static async Task ExecuteAsync(SqlConnection conn, string sql, int timeoutSeconds = 60)
        {
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = timeoutSeconds };
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<string> ScalarAsync(SqlConnection conn, string sql)
        {
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            return Convert.ToString(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private void WriteReport(string server, int rowCount, TimeSpan seedTime, IReadOnlyList<CursorPosition> positions, IReadOnlyList<Measurement> results)
        {
            var report = new StringBuilder();
            report.AppendLine(CultureInfo.InvariantCulture, $"# Đo câu truy vấn nối đuôi — {rowCount:N0} dòng, trang {PageSize} dòng");
            report.AppendLine(CultureInfo.InvariantCulture, $"- Máy chủ: {server}; nạp dữ liệu + tạo chỉ mục: {seedTime.TotalSeconds:F1} s; thời gian = trung vị {TimingRuns} lần chạy, dữ liệu đã nằm trong bộ nhớ đệm.");
            report.AppendLine("- Chỉ mục: khóa chính ID (clustered) + IX_CursorSource_UpdateTime_ID (UpdateTime, ID).");
            report.AppendLine();
            foreach (var position in positions)
            {
                report.AppendLine(CultureInfo.InvariantCulture, $"## Mốc: {position.Name}");
                report.AppendLine();
                report.AppendLine("| Cách | Lượt gọi DB | Dòng trả | Trang đọc (logical reads) | Dòng phải đọc | Kế hoạch thực thi | Thời gian (ms) |");
                report.AppendLine("|---|---:|---:|---:|---:|---|---:|");
                foreach (var r in results.Where(x => x.Position == position))
                {
                    report.AppendLine(CultureInfo.InvariantCulture, $"| {r.Option.Name} | {r.RoundTrips} | {r.RowsReturned} | {r.LogicalReads:N0} | {r.RowsRead:N0} | {r.Plan} | {r.MedianMs:F1} |");
                }
                report.AppendLine();
            }

            var text = report.ToString();
            _output.WriteLine(text);
            var dir = Path.Combine(AppContext.BaseDirectory, "perf-results");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "cursor-query.md"), text);
        }
    }
}
