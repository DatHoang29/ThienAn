using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Tests.ShareData.Common;
using Xunit.Abstractions;

namespace Tests.ShareData.Performance
{
    /// <summary>
    /// Description: Đo hiệu năng câu truy vấn nối đuôi trên bảng lớn, so 3 phương án khóa nối đuôi bàn ngày 28/10/2026
    ///              (PA1 ghép chuỗi ngay trong SQL, PA2 chỉ tính công thức cho dòng cuối, PA3 ghép chuỗi ở Worker C#)
    ///              với cách đang chạy (2 cột __lastTime + __lastKey: gói 101 so cột thô, gói 103–111 so COALESCE)
    ///              và một cách viết lại câu 2 cột có điều kiện mở đầu dùng được chỉ mục.
    ///              Dữ liệu nằm trong bảng tạm trên một kết nối riêng tới CSDL test local, đóng kết nối là bảng tự mất.
    ///              Biến thể PA1, PA2: dùng lại kế hoạch (-S), ORDER BY cột gốc (-G), chặn dưới lấy thời gian từ @lastKey (-CD), để chọn cách giữ một chuỗi.
    /// Created date: 29/10/2026
    /// Modified date: 12/11/2026
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

        /// <summary>Chặn dưới lấy phần thời gian từ chính @lastKey: câu vẫn một cột, một biến mà SQL Server nhảy được tới gần mốc trên chỉ mục.</summary>
        private const string KeyLowerBound = "s.UpdateTime >= ISNULL(TRY_CONVERT(datetime, LEFT(@lastKey, 23), 121), '17530101')";

        /// <summary>Câu phụ của PA2: tính công thức trên đúng dòng cuối trang.</summary>
        private const string LastRowKeyQuery = $"{{tag}}SELECT {ConcatKeyExpression} AS __lastKey FROM #CursorSource s WHERE s.ID = @lastRowId";

        /// <summary>Câu lấy trang của các biến thể PA1, PA2: selectKey là PA1 (tính chuỗi trong câu chính), lowerBound thêm chặn dưới, orderByRaw sắp theo cột gốc thay vì __lastKey.</summary>
        private static string ConcatPageSql(bool selectKey, bool lowerBound, bool orderByRaw) =>
            $"{{tag}}SELECT TOP (@pageSize) s.ID, s.Code, s.Speed, s.UpdateTime{(selectKey ? $", {ConcatKeyExpression} AS __lastKey" : string.Empty)}\n"
            + "FROM #CursorSource s\n"
            + $"WHERE {(lowerBound ? KeyLowerBound + " AND " : string.Empty)}{ConcatKeyExpression} > @lastKey\n"
            + (orderByRaw ? "ORDER BY s.UpdateTime ASC, s.ID ASC" : "ORDER BY __lastKey ASC");

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
            new("PA1-S", "PA1 · dùng lại kế hoạch dựng lúc chưa có mốc", CursorShape.ConcatKey, ConcatPageSql(selectKey: true, lowerBound: false, orderByRaw: false), SharePlanAcrossPositions: true),
            new("PA1-G", "PA1 · ORDER BY cột gốc (phải nới luật kiểm)", CursorShape.ConcatKey, ConcatPageSql(selectKey: true, lowerBound: false, orderByRaw: true)),
            new("PA1-G-S", "PA1 · ORDER BY cột gốc, dùng lại kế hoạch", CursorShape.ConcatKey, ConcatPageSql(selectKey: true, lowerBound: false, orderByRaw: true), SharePlanAcrossPositions: true),
            new("PA1-CD", "PA1 · thêm chặn dưới (luật kiểm hiện tại cho qua)", CursorShape.ConcatKey, ConcatPageSql(selectKey: true, lowerBound: true, orderByRaw: false)),
            new("PA1-CD-S", "PA1 · thêm chặn dưới, dùng lại kế hoạch", CursorShape.ConcatKey, ConcatPageSql(selectKey: true, lowerBound: true, orderByRaw: false), SharePlanAcrossPositions: true),
            new("PA1-CDG", "PA1 · chặn dưới + ORDER BY cột gốc (phải nới luật kiểm)", CursorShape.ConcatKey, ConcatPageSql(selectKey: true, lowerBound: true, orderByRaw: true)),
            new("PA1-CDG-S", "PA1 · chặn dưới + ORDER BY cột gốc, dùng lại kế hoạch", CursorShape.ConcatKey, ConcatPageSql(selectKey: true, lowerBound: true, orderByRaw: true), SharePlanAcrossPositions: true),
            new("PA2-S", "PA2 · dùng lại kế hoạch dựng lúc chưa có mốc", CursorShape.ConcatKey, ConcatPageSql(selectKey: false, lowerBound: false, orderByRaw: true), LastRowKeySql: LastRowKeyQuery, SharePlanAcrossPositions: true),
            new("PA2-CD", "PA2 · thêm chặn dưới", CursorShape.ConcatKey, ConcatPageSql(selectKey: false, lowerBound: true, orderByRaw: true), LastRowKeySql: LastRowKeyQuery),
            new("PA2-CD-S", "PA2 · thêm chặn dưới, dùng lại kế hoạch", CursorShape.ConcatKey, ConcatPageSql(selectKey: false, lowerBound: true, orderByRaw: true), LastRowKeySql: LastRowKeyQuery, SharePlanAcrossPositions: true),
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
        ///              (chưa có mốc, đã gửi một nửa, chỉ còn 500 dòng mới, không có dòng mới) rồi so:
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
                await ReadPositionAsync(conn, "idle", "Không có dòng mới (mốc ở dòng cuối bảng)", rowCount),
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
                Assert.Equal(position.Code == "idle" ? 0 : PageSize, reference.PageIds.Count);
                foreach (var r in atPosition)
                {
                    Assert.True(reference.PageIds.SequenceEqual(r.PageIds), $"{r.Option.Code} ở mốc {position.Code} trả trang khác HT-101.");
                }

                var lastRow = reference.LastRow;
                if (lastRow == null)
                    continue;

                var expectedConcatKey = BuildKeyInWorker(lastRow);
                foreach (var r in atPosition.Where(r => r.Option.Shape == CursorShape.ConcatKey))
                {
                    Assert.Equal(expectedConcatKey, r.NextCursor);
                }
            }

            var end = results.Where(r => r.Position.Code == "end").ToDictionary(r => r.Option.Code);
            foreach (var code in new[] { "DX", "DX-S", "PA1-CDG", "PA1-CDG-S", "PA2-CD", "PA2-CD-S" })
            {
                Assert.Contains("Index Seek", end[code].Plan, StringComparison.Ordinal);
                Assert.True(end[code].LogicalReads <= 2_000, $"{code} đọc {end[code].LogicalReads} trang.");
            }
            if (rowCount < DefaultRowCount)
                return;

            foreach (var code in new[] { "PA1", "PA2", "PA3", "HT-103", "DX-103" })
            {
                Assert.True(end[code].LogicalReads >= 10 * end["DX"].LogicalReads,
                    $"{code} đọc {end[code].LogicalReads} trang, chưa gấp 10 lần DX ({end["DX"].LogicalReads}).");
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: So PA1 với PA2 khi dòng cuối trang bị sửa hoặc bị xóa sau lúc đọc trang, trước lúc lấy mốc. PA1 lấy mốc cùng lần đọc trang nên không sót dòng.
        ///              PA2 tính lại công thức trên dòng đã đổi: dòng bị sửa làm mốc nhảy quá các dòng chưa gửi, dòng bị xóa thì không có mốc nên gửi lại trang.
        ///              PA2 chưa có trong Worker nên đi đúng các bước của biên bản 28/10/2026 trên bảng tạm, không qua luồng thật.
        /// Created date: 02/11/2026
        /// </summary>
        [Theory(Skip = "So sánh thiết kế PA1 và PA2 trên bảng tạm, PA2 chưa có trong Worker; bỏ Skip khi cần đo lại.")]
        [Trait("Feature", "Performance")]
        [InlineData("PA1", "update", 0, 1)]
        [InlineData("PA1", "delete", 0, 0)]
        [InlineData("PA2", "update", 4, 0)]
        [InlineData("PA2", "delete", 0, 2)]
        public async Task CursorKey_WhenLastRowChangesBeforeCursorIsRead_Test(string option, string change, int expectedMissing, int expectedDuplicates)
        {
            // Arrange: 10 dòng R01..R10 cách nhau 1 phút, trang 3 dòng
            var connectionString = _host.Services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection trong appsettings.Test.json.");
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await ExecuteAsync(conn, """
                CREATE TABLE #CursorChange (ID NVARCHAR(64) NOT NULL PRIMARY KEY, UpdateTime DATETIME NOT NULL);
                INSERT INTO #CursorChange (ID, UpdateTime)
                SELECT CONCAT(N'R', RIGHT(CONCAT('0', v.n), 2)), DATEADD(MINUTE, v.n, CAST('20260101' AS DATETIME))
                FROM (VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10)) v(n);
                """);
            const string key = "CONVERT(varchar(23), c.UpdateTime, 121) + '_' + c.ID";
            var isPa1 = option == "PA1";
            var pageSql = isPa1
                ? $"SELECT TOP (3) c.ID, {key} AS __lastKey FROM #CursorChange c WHERE {key} > @lastKey ORDER BY __lastKey ASC"
                : $"SELECT TOP (3) c.ID FROM #CursorChange c WHERE {key} > @lastKey ORDER BY c.UpdateTime ASC, c.ID ASC";
            var lastRowSql = $"SELECT {key} AS __lastKey FROM #CursorChange c WHERE c.ID = @lastRowId";

            // Act: gửi hết các trang; dòng cuối trang đầu (R03) đổi trước khi lấy mốc: sửa thành 00:07:30 (sau R07) hoặc bị xóa
            var sent = new List<string>();
            var cursor = string.Empty;
            for (var round = 0; round < 20; round++)
            {
                var page = (await QueryAsync(conn, pageSql, [new SqlParameter("@lastKey", SqlDbType.NVarChar, 256) { Value = cursor }])).Rows;
                if (page.Count == 0)
                    break;

                sent.AddRange(page.Select(r => r.Id));
                var lastRow = page[^1];
                if (round == 0)
                {
                    await ExecuteAsync(conn, change == "update"
                        ? $"UPDATE #CursorChange SET UpdateTime = '20260101 00:07:30' WHERE ID = N'{lastRow.Id}'"
                        : $"DELETE FROM #CursorChange WHERE ID = N'{lastRow.Id}'");
                }

                if (isPa1)
                {
                    cursor = lastRow.LastKey!;
                    continue;
                }

                // Không còn dòng để tính công thức thì không có mốc mới, lượt sau đọc lại từ mốc cũ
                var keyRows = (await QueryAsync(conn, lastRowSql, [LastRowIdParameter(lastRow.Id)])).Rows;
                if (keyRows.Count > 0)
                {
                    cursor = keyRows[0].LastKey!;
                }
            }

            // Assert
            var allRows = Enumerable.Range(1, 10).Select(n => $"R{n:00}");
            Assert.Equal(expectedMissing, allRows.Except(sent).Count());
            Assert.Equal(expectedDuplicates, sent.Count - sent.Distinct().Count());
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bảng nguồn có cột tính toán chỉ chứa phần giờ của mốc (yyyyMMddHHmmssfff) và chỉ mục trên cột đó. Câu nối đuôi hai tầng:
        ///              tầng trong lấy TOP (@pageSize) WITH TIES sắp theo phần giờ, chặn dưới bằng CONVERT(varchar(17), LEFT(@lastKey, 17)); tầng ngoài sắp theo cả khóa.
        ///              Với đuôi bất kỳ (mã dòng, mã có thể trống), có hay không nối bảng danh mục, mốc ở đầu (quét có thứ tự IX_CursorSource_CursorTime 102–204 dòng),
        ///              giữa, mốc chỉ có ngày giờ, gần cuối và không có dòng mới: câu phải dùng chỉ mục phần giờ (Index Seek ở giữa/cuối), chỉ đọc quanh một trang
        ///              và trả đúng trang như câu một tầng khi bảng chưa có chỉ mục. Nếu bỏ CONVERT(varchar(17), ...) ở vế chặn dưới của cột varchar,
        ///              SQL Server đổi kiểu cả cột theo @lastKey (nvarchar) và quét 30.102–60.000 dòng.
        /// Created date: 16/11/2026
        /// </summary>
        [Theory]
        [Trait("Feature", "Performance")]
        [InlineData("varchar-cast", "s.ID", false, "start")]
        [InlineData("varchar-cast", "s.ID", false, "middle")]
        [InlineData("varchar-cast", "s.ID", false, "idle")]
        [InlineData("varchar-cast", "s.ID", true, "middle")]
        [InlineData("varchar-cast", "s.ID", true, "prefix")]
        [InlineData("varchar-cast", "s.ID", true, "end")]
        [InlineData("varchar-cast", "ISNULL(s.Code, '')", false, "middle")]
        [InlineData("varchar-cast", "ISNULL(s.Code, '')", true, "start")]
        [InlineData("varchar-cast", "ISNULL(s.Code, '')", true, "middle")]
        [InlineData("varchar-cast", "ISNULL(s.Code, '')", true, "idle")]
        [InlineData("nvarchar", "s.ID", false, "middle")]
        [InlineData("nvarchar", "s.ID", false, "idle")]
        [InlineData("nvarchar-cast", "s.ID", false, "middle")]
        [InlineData("nvarchar-cast", "s.ID", false, "idle")]
        [InlineData("nvarchar-cast", "ISNULL(s.Code, '')", true, "middle")]
        public async Task CursorQuery_WhenSourceHasCursorTimeIndex_SeeksAndReadsAboutOnePage_Test(string timeType, string tail, bool joinCatalog, string position)
        {
            // Arrange: bảng nguồn 60.000 dòng (3 dòng mỗi mốc giờ), bảng danh mục 500 dòng; trang mong đợi đọc bằng câu một tầng khi chưa có chỉ mục phần giờ
            const int rowCount = 60_000;
            var castType = timeType.StartsWith("nvarchar", StringComparison.Ordinal) ? "nvarchar" : "varchar";
            var timePart = TimePart(castType, "s.");
            var columnTimePart = TimePart(castType, string.Empty);
            var lowerBound = timeType.EndsWith("-cast", StringComparison.Ordinal) ? "CONVERT(varchar(17), LEFT(@lastKey, 17))" : "LEFT(@lastKey, 17)";
            var cursorKey = $"{timePart} + '_' + {tail}";
            var join = joinCatalog ? "LEFT JOIN #CursorCatalog c ON s.Code = c.Code" : string.Empty;
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
            await SeedSourceAsync(conn, rowCount);
            await ExecuteAsync(conn, """
                ALTER TABLE #CursorSource ADD IsDelete DATETIME NULL;
                CREATE TABLE #CursorCatalog (Code NVARCHAR(64) NOT NULL PRIMARY KEY, KmNumber INT NULL);
                INSERT INTO #CursorCatalog (Code, KmNumber)
                SELECT DISTINCT s.Code, 1 FROM #CursorSource s;
                """);
            var rowNumber = position switch { "end" => rowCount - 50, "idle" => rowCount, _ => rowCount / 2 };
            var positionKey = await ReadCursorKeyAsync(conn, cursorKey, join, rowNumber);
            var lastKey = position switch { "start" => string.Empty, "prefix" => positionKey[..12], _ => positionKey };
            var expectedCount = position switch { "end" => 50, "idle" => 0, _ => PageSize };
            var flatSql = $"""
                SELECT TOP (@pageSize) s.ID, {cursorKey} AS __lastKey
                FROM #CursorSource s
                {join}
                WHERE s.IsDelete IS NULL
                  AND {cursorKey} > @lastKey
                ORDER BY __lastKey ASC
                """;
            var pageSql = $"""
                SELECT TOP (@pageSize) p.ID, p.__lastKey
                FROM (
                    SELECT TOP (@pageSize) WITH TIES s.ID, {cursorKey} AS __lastKey
                    FROM #CursorSource s
                    {join}
                    WHERE s.IsDelete IS NULL
                      AND {timePart} >= {lowerBound}
                      AND {cursorKey} > @lastKey
                    ORDER BY {timePart} ASC
                ) p
                ORDER BY p.__lastKey ASC
                """;
            var expectedParameters = BuildCursorKeyParameters(lastKey);
            var expected = await QueryAsync(conn, $"/* trước chỉ mục */ {flatSql}", expectedParameters);
            await ExecuteAsync(conn, $"""
                ALTER TABLE #CursorSource ADD CursorTime AS ({columnTimePart});
                CREATE NONCLUSTERED INDEX IX_CursorSource_CursorTime ON #CursorSource (CursorTime) INCLUDE (IsDelete);
                SET STATISTICS IO ON;
                SET STATISTICS XML ON;
                """, 600);
            lock (messages)
            {
                _output.WriteLine($"Thông báo khi tạo chỉ mục: {string.Join(" | ", messages)}");
                messages.Clear();
            }

            // Act
            var actualParameters = BuildCursorKeyParameters(lastKey);
            var actual = await QueryAsync(conn, $"/* sau chỉ mục */ {pageSql}", actualParameters);

            // Assert
            var planXml = new[] { actual.PlanXml };
            var (rowsRead, plan) = SummarizePlans(planXml);
            long logicalReads;
            lock (messages)
            {
                logicalReads = messages
                    .SelectMany(m => IoStatistics.Matches(m))
                    .Sum(m => long.Parse(m.Groups["reads"].Value, CultureInfo.InvariantCulture));
            }
            _output.WriteLine($"{timeType} | đuôi {tail} | nối danh mục {joinCatalog} | mốc {position}: trả {actual.Rows.Count} dòng, đọc {rowsRead:N0} dòng, {logicalReads:N0} trang; {plan}");
            var expectedPage = expected.Rows.Select(r => $"{r.Id}|{r.LastKey}").ToList();
            var actualPage = actual.Rows.Select(r => $"{r.Id}|{r.LastKey}").ToList();
            Assert.Equal(expectedCount, expectedPage.Count);
            Assert.Equal(expectedPage, actualPage);
            var expectedOperator = position == "start" ? "IX_CursorSource_CursorTime" : "Index Seek (IX_CursorSource_CursorTime)";
            Assert.Contains(expectedOperator, plan);
            Assert.True(rowsRead <= 1_000, $"Câu hai tầng đọc {rowsRead:N0} dòng.");
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đo tải đọc Trước vs Sau (Mức 1: 1 tầng có chặn dưới và Mức 2: 2 tầng WITH TIES) trên bảng tạm cùng cấu trúc TmsTrafficData
        ///              (342.036 dòng như staging 17/11/2026, có LEFT JOIN TmsEquipment), kiểm tra cả đuôi chuẩn (td.ID) và đuôi động (ISNULL(td.Lane, '') + '_' + td.ID)
        ///              tại 4 vị trí mốc (0%, 50%, 99,97% còn 100 dòng, 100% không có dòng mới).
        /// Created date: 17/11/2026
        /// Modified date: 18/11/2026
        /// </summary>
        [Fact(Skip = "Đo tải trên bảng tạm 342 nghìn dòng, chạy khoảng 1 phút — bỏ Skip khi cần đo lại; đổi số dòng bằng biến môi trường SHAREDATA_PERF_ROWS.")]
        [Trait("Feature", "Performance")]
        public async Task LoadProfile_WhenTrafficDataTemplatesRunOneTier_ScanWholeTableEveryRun_Test()
        {
            // Arrange: bảng tạm cùng cột TmsTrafficData, TmsEquipment; đối chiếu câu mẫu 103 từ tệp seed
            var configuredRows = Environment.GetEnvironmentVariable(RowCountVariable);
            var rowCount = int.TryParse(configuredRows, NumberStyles.Integer, CultureInfo.InvariantCulture, out var configured) && configured >= 10_000
                ? configured
                : StagingTrafficDataRows;
            var seed103 = SeedScriptReader.GetExtractSql("103");
            var tempSeed103 = ToTempTables(seed103);
            Assert.Contains("FROM #TmsTrafficData td", tempSeed103, StringComparison.Ordinal);

            var configuration = _host.Services.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
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
            await SeedTrafficDataAsync(conn, rowCount);

            var tails = new (string Name, string Expr)[]
            {
                ("Đuôi chuẩn (ID)", "td.ID"),
                ("Đuôi động (Lane + ID)", "ISNULL(td.Lane, '') + '_' + td.ID")
            };
            var cursorPositionsByTail = new Dictionary<string, (string Name, string Key, int Expected)[]>(StringComparer.Ordinal);
            foreach (var (tailName, tailExpr) in tails)
            {
                var middleKey = await ReadTrafficKeyAsync(conn, rowCount / 2, tailExpr);
                var endKey = await ReadTrafficKeyAsync(conn, rowCount - PageSize, tailExpr);
                var lastKey = await ReadTrafficKeyAsync(conn, rowCount, tailExpr);
                cursorPositionsByTail[tailName] =
                [
                    ("0% · Chưa có mốc", string.Empty, PageSize),
                    ("50% · Đã gửi một nửa", middleKey, PageSize),
                    ("99,97% · Chạy định kỳ (còn 100 dòng)", endKey, PageSize),
                    ("100% · Không có dòng mới", lastKey, 0)
                ];
            }

            const string beforeMode = "Trước (1 tầng gốc)";
            const string level1Mode = "Sau · Mức 1 (1 tầng + chặn dưới)";
            const string level2Mode = "Sau · Mức 2 (2 tầng WITH TIES)";
            var results = new List<TrafficBenchmarkResult>();

            // Act 1: Trước — câu 1 tầng gốc, chưa có cột CursorTime và chưa có chỉ mục IX_TmsTrafficData_CursorTime
            foreach (var (tailName, tailExpr) in tails)
            {
                var beforeSql = BuildTraffic103Sql(tailExpr, "baseline");
                var positions = cursorPositionsByTail[tailName];
                foreach (var pos in positions)
                {
                    var measured = await MeasureTrafficScenarioAsync(
                        conn, messages, beforeMode, tailName, pos.Name, pos.Expected, pos.Key, beforeSql);
                    results.Add(measured);
                }
            }

            // Bổ sung cột tính toán chỉ chứa phần thời gian 17 ký tự và chỉ mục (đuôi vẫn động 100%)
            var columnTimePart = TimePart("varchar", string.Empty);
            var indexSql = $"""
                ALTER TABLE #TmsTrafficData ADD CursorTime AS ({columnTimePart});
                CREATE NONCLUSTERED INDEX IX_TmsTrafficData_CursorTime ON #TmsTrafficData (CursorTime) INCLUDE (IsDelete);
                """;
            await ExecuteAsync(conn, indexSql, 600);

            // Act 2 & 3: Sau Mức 1 (1 tầng + chặn dưới) và Sau Mức 2 (2 tầng WITH TIES)
            foreach (var (tailName, tailExpr) in tails)
            {
                var level1Sql = BuildTraffic103Sql(tailExpr, "lower-bound");
                var level2Sql = BuildTraffic103Sql(tailExpr, "two-tier");
                var positions = cursorPositionsByTail[tailName];
                foreach (var pos in positions)
                {
                    var m1 = await MeasureTrafficScenarioAsync(
                        conn, messages, level1Mode, tailName, pos.Name, pos.Expected, pos.Key, level1Sql);
                    var m2 = await MeasureTrafficScenarioAsync(
                        conn, messages, level2Mode, tailName, pos.Name, pos.Expected, pos.Key, level2Sql);
                    results.Add(m1);
                    results.Add(m2);
                }
            }

            // Báo cáo bảng Markdown tổng hợp Trước vs Sau (Mức 1 & Mức 2)
            var report = new StringBuilder();
            report.AppendLine(CultureInfo.InvariantCulture, $"Bảng nguồn #TmsTrafficData: {rowCount:N0} dòng, cỡ trang {PageSize} dòng, nối #TmsEquipment.");
            report.AppendLine("| Đuôi khóa | Phương án | Vị trí mốc | Dòng trả | Trang đọc (8 KB) | Dòng duyệt | Tràn tempdb | Trung vị (ms) | Kế hoạch thực thi |");
            report.AppendLine("|---|---|---|---:|---:|---:|:---:|---:|---|");
            foreach (var r in results)
            {
                var spillText = r.Spill ? "Có" : "Không";
                report.AppendLine(CultureInfo.InvariantCulture, $"| {r.TailName} | {r.Mode} | {r.PositionName} | {r.RowKeys.Count} | {r.LogicalReads:N0} | {r.RowsRead:N0} | {spillText} | {r.MedianMs:F2} | {r.Plan} |");
            }

            var newRunsPerMinute = TrafficRunsPerMinute(rootPartners: 2, loadPartners: 8, LoadTestCyclesNow);
            var oldRunsPerMinute = TrafficRunsPerMinute(rootPartners: 2, loadPartners: 50, LoadTestCyclesBefore);
            report.AppendLine();
            foreach (var mode in new[] { beforeMode, level1Mode, level2Mode })
            {
                var steadyRows = results
                    .Where(r => r.Mode == mode && (r.PositionName.StartsWith("99,97%", StringComparison.Ordinal) || r.PositionName.StartsWith("100%", StringComparison.Ordinal)))
                    .ToList();
                var steadyReads = steadyRows.Average(r => r.LogicalReads);
                var newPagesPerMin = newRunsPerMinute * steadyReads;
                var newMbPerMin = newPagesPerMin * 8 / 1024;
                var oldPagesPerMin = oldRunsPerMinute * steadyReads;
                var oldMbPerMin = oldPagesPerMin * 8 / 1024;
                report.AppendLine(CultureInfo.InvariantCulture, $"- {mode} (chạy định kỳ/rỗi, TB {steadyReads:N1} trang/lượt): 10 ĐT ({newRunsPerMinute:F1} lượt/phút) = {newPagesPerMin:N0} trang/phút (~{newMbPerMin:N1} MB/phút); 52 ĐT ({oldRunsPerMinute:F1} lượt/phút) = {oldPagesPerMin:N0} trang/phút (~{oldMbPerMin:N1} MB/phút).");
            }
            var reportText = report.ToString();
            _output.WriteLine(reportText);

            // Assert: đúng dữ liệu & NextKey ở cả 3 cách; Mức 1 Seek khi định kỳ/rỗi; Mức 2 Seek/đọc <= 1.000 dòng ở cả 4 vị trí
            foreach (var r in results)
            {
                Assert.Equal(r.ExpectedCount, r.RowKeys.Count);
                if (r.RowKeys.Count > 0)
                {
                    var keyComparison = string.CompareOrdinal(r.NextKey, r.Cursor);
                    Assert.True(keyComparison > 0, $"[{r.Mode} | {r.TailName} | {r.PositionName}] NextKey không tiến qua mốc.");
                }
            }

            foreach (var (tailName, _) in tails)
            {
                var positions = cursorPositionsByTail[tailName];
                foreach (var pos in positions)
                {
                    var atSlot = results.Where(r => r.TailName == tailName && r.PositionName == pos.Name).ToList();
                    var baseline = atSlot.Single(r => r.Mode == beforeMode);
                    var level1 = atSlot.Single(r => r.Mode == level1Mode);
                    var level2 = atSlot.Single(r => r.Mode == level2Mode);

                    var level1SameRows = baseline.RowKeys.SequenceEqual(level1.RowKeys);
                    var level2SameRows = baseline.RowKeys.SequenceEqual(level2.RowKeys);
                    Assert.True(level1SameRows, $"Mức 1 khác danh sách dòng tại {tailName} | {pos.Name}.");
                    Assert.True(level2SameRows, $"Mức 2 khác danh sách dòng tại {tailName} | {pos.Name}.");
                    Assert.Equal(baseline.NextKey, level1.NextKey);
                    Assert.Equal(baseline.NextKey, level2.NextKey);

                    Assert.True(baseline.RowsRead >= rowCount, $"Trước phải duyệt cả bảng tại {tailName} | {pos.Name}, thực tế {baseline.RowsRead:N0}.");

                    var isSteady = pos.Name.StartsWith("99,97%", StringComparison.Ordinal);
                    var isIdle = pos.Name.StartsWith("100%", StringComparison.Ordinal);
                    if (isSteady || isIdle)
                    {
                        Assert.Contains("Index Seek (IX_TmsTrafficData_CursorTime)", level1.Plan, StringComparison.Ordinal);
                        Assert.True(level1.RowsRead <= 1_000, $"Mức 1 duyệt {level1.RowsRead:N0} dòng tại {tailName} | {pos.Name}.");
                    }

                    var isStart = pos.Name.StartsWith("0%", StringComparison.Ordinal);
                    var expectedL2Op = isStart ? "IX_TmsTrafficData_CursorTime" : "Index Seek (IX_TmsTrafficData_CursorTime)";
                    Assert.Contains(expectedL2Op, level2.Plan, StringComparison.Ordinal);
                    Assert.True(level2.RowsRead <= 1_000, $"Mức 2 duyệt {level2.RowsRead:N0} dòng tại {tailName} | {pos.Name}.");
                }
            }
        }

        /// <summary>Số dòng TmsTrafficData trên staging ngày 17/11/2026.</summary>
        private const int StagingTrafficDataRows = 342_036;

        /// <summary>Chu kỳ gửi (giây) chia cho đối tác kiểm tải theo K3 hiện tại và bản trước 17/11/2026; PC08, TTDHT luôn 30 giây.</summary>
        private static readonly int[] LoadTestCyclesNow = [30, 40, 50, 60, 120];

        private static readonly int[] LoadTestCyclesBefore = [15, 20, 25, 30, 60];

        /// <summary>Lượt đọc TmsTrafficData mỗi phút của 2 gói 103, 106: mỗi đối tác một đăng ký gửi mỗi gói, đối tác kiểm tải chia đều các chu kỳ trong danh sách.</summary>
        private static double TrafficRunsPerMinute(int rootPartners, int loadPartners, int[] cycles)
        {
            var perLoadPartner = cycles.Average(c => 60.0 / c);
            return 2 * (rootPartners * 60.0 / 30 + loadPartners * perLoadPartner);
        }

        /// <summary>Đổi bảng thật trong câu mẫu sang bảng tạm cùng cấu trúc để đo trên dữ liệu riêng của phiên.</summary>
        private static string ToTempTables(string sql) => sql
            .Replace("FROM TmsTrafficData td", "FROM #TmsTrafficData td", StringComparison.Ordinal)
            .Replace("JOIN TmsEquipment e", "JOIN #TmsEquipment e", StringComparison.Ordinal);

        private static async Task SeedTrafficDataAsync(SqlConnection conn, int rowCount)
        {
            // Cột giống TmsTrafficData thật: khóa chính cụm trên ID chuỗi, chỉ mục DetectTime; mỗi mốc giờ 3 dòng để có dòng trùng giờ
            var sql = $"""
                CREATE TABLE #TmsEquipment (ID NVARCHAR(64) NOT NULL PRIMARY KEY CLUSTERED, KmNumber INT NULL, MetNumber INT NULL);
                INSERT INTO #TmsEquipment (ID, KmNumber, MetNumber)
                SELECT TOP (200) CONCAT(N'EQ', ROW_NUMBER() OVER (ORDER BY (SELECT NULL))), 10, 500 FROM sys.all_columns;
                CREATE TABLE #TmsTrafficData (
                    ID NVARCHAR(64) NOT NULL PRIMARY KEY CLUSTERED,
                    DetectTime DATETIME NULL, Lane NVARCHAR(64) NULL, Location NVARCHAR(256) NULL, Speed DECIMAL(18, 2) NULL,
                    Height DECIMAL(18, 2) NULL, Width DECIMAL(18, 2) NULL, Length DECIMAL(18, 2) NULL, Type NVARCHAR(64) NULL,
                    LicensePlate NVARCHAR(64) NULL, Direction NVARCHAR(64) NULL, EquipmentId NVARCHAR(64) NULL,
                    CreateTime DATETIME NULL, UpdateTime DATETIME NULL, IsDelete DATETIME NULL
                );
                WITH n AS (
                    SELECT TOP ({rowCount}) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
                    FROM sys.all_columns a CROSS JOIN sys.all_columns b
                )
                INSERT INTO #TmsTrafficData (ID, DetectTime, Lane, Location, Speed, Height, Width, Length, Type, LicensePlate, Direction, EquipmentId, CreateTime, UpdateTime)
                SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())),
                       DATEADD(MILLISECOND, (i / 3) * 10 - 500, CAST('20260101' AS DATETIME)),
                       CONCAT(N'L', i % 4), N'Km10+500', CAST(i % 120 AS DECIMAL(18, 2)), 3.5, 2.5, 12, N'Car',
                       CONCAT(N'29A-', i % 99999), N'IN', CONCAT(N'EQ', i % 200 + 1),
                       DATEADD(MILLISECOND, (i / 3) * 10, CAST('20260101' AS DATETIME)),
                       CASE WHEN i % 5 = 0 THEN NULL ELSE DATEADD(MILLISECOND, (i / 3) * 10, CAST('20260101' AS DATETIME)) END
                FROM n;
                CREATE NONCLUSTERED INDEX IX_TmsTrafficData_DetectTime ON #TmsTrafficData (DetectTime);
                UPDATE STATISTICS #TmsTrafficData WITH FULLSCAN;
                """;
            await ExecuteAsync(conn, sql, 1800);
        }

        /// <summary>Mốc PA1 của dòng thứ rowNumber theo thứ tự mốc ứng với biểu thức đuôi truyền vào.</summary>
        private static async Task<string> ReadTrafficKeyAsync(SqlConnection conn, int rowNumber, string tail = "td.ID")
        {
            var timePart = TimePart("varchar", "td.");
            var cursorKey = $"{timePart} + '_' + {tail}";
            var sql = $"""
                SELECT x.__lastKey
                FROM (
                    SELECT {cursorKey} AS __lastKey, ROW_NUMBER() OVER (ORDER BY {cursorKey}) AS rn
                    FROM #TmsTrafficData td
                    WHERE td.IsDelete IS NULL
                ) x
                WHERE x.rn = @rowNumber
                """;
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 600 };
            var rowParameter = new SqlParameter("@rowNumber", SqlDbType.BigInt) { Value = (long)rowNumber };
            cmd.Parameters.Add(rowParameter);
            var key = await cmd.ExecuteScalarAsync();
            return Assert.IsType<string>(key);
        }

        private sealed record TrafficBenchmarkResult(
            string Mode,
            string TailName,
            string PositionName,
            int ExpectedCount,
            string Cursor,
            IReadOnlyList<string> RowKeys,
            string? NextKey,
            long LogicalReads,
            long RowsRead,
            bool Spill,
            double MedianMs,
            string Plan);

        /// <summary>Tạo biến thể câu gói 103 trên #TmsTrafficData: baseline (1 tầng gốc), lower-bound (Mức 1), two-tier (Mức 2).</summary>
        private static string BuildTraffic103Sql(string tail, string tierMode)
        {
            var timePart = TimePart("varchar", "td.");
            var cursorKey = $"{timePart} + '_' + {tail}";
            const string columns = """
                    td.ID AS detectionId,
                    td.DetectTime AS detectTime,
                    td.Type AS vehicleType,
                    td.LicensePlate AS licensePlate,
                    td.Speed AS speed,
                    td.Lane AS lane,
                    td.Direction AS direction,
                    td.Location AS locationRoute,
                    td.EquipmentId AS equipmentId,
                    e.KmNumber AS locationKm,
                    e.MetNumber AS locationMet,
                """;
            const string auditColumns = """
                    td.UpdateTime AS __auditUpdate,
                    td.CreateTime AS __auditCreate
                """;
            if (tierMode == "two-tier")
            {
                return $"""
                    SELECT TOP (@pageSize)
                        p.detectionId,
                        p.detectTime,
                        p.vehicleType,
                        p.licensePlate,
                        p.speed,
                        p.lane,
                        p.direction,
                        p.locationRoute,
                        p.equipmentId,
                        p.locationKm,
                        p.locationMet,
                        p.__lastKey,
                        p.__auditUpdate,
                        p.__auditCreate
                    FROM (
                        SELECT TOP (@pageSize) WITH TIES
                    {columns}
                            {cursorKey} AS __lastKey,
                    {auditColumns}
                        FROM #TmsTrafficData td
                        LEFT JOIN #TmsEquipment e ON td.EquipmentId = e.ID
                        WHERE td.IsDelete IS NULL
                          AND {timePart} >= CONVERT(varchar(17), LEFT(@lastKey, 17))
                          AND {cursorKey} > @lastKey
                        ORDER BY {timePart} ASC
                    ) p
                    ORDER BY p.__lastKey ASC
                    """;
            }

            var lowerBound = tierMode == "lower-bound"
                ? $"\n  AND {timePart} >= CONVERT(varchar(17), LEFT(@lastKey, 17))"
                : string.Empty;
            var recompile = tierMode == "lower-bound" ? "\nOPTION (RECOMPILE)" : string.Empty;
            return $"""
                SELECT TOP (@pageSize)
                {columns}
                    {cursorKey} AS __lastKey,
                {auditColumns}
                FROM #TmsTrafficData td
                LEFT JOIN #TmsEquipment e ON td.EquipmentId = e.ID
                WHERE td.IsDelete IS NULL{lowerBound}
                  AND {cursorKey} > @lastKey
                ORDER BY __lastKey ASC{recompile}
                """;
        }

        /// <summary>Đo 1 lượt STATISTICS IO + XML (warmup) và TimingRuns lần đo thời gian thuần cho một kịch bản kiểm tải.</summary>
        private static async Task<TrafficBenchmarkResult> MeasureTrafficScenarioAsync(
            SqlConnection conn,
            List<string> messages,
            string mode,
            string tailName,
            string positionName,
            int expectedCount,
            string lastKey,
            string sql)
        {
            var taggedSql = $"/* traffic {mode} | {tailName} | {positionName} */ {sql}";
            await ExecuteAsync(conn, "SET STATISTICS IO ON; SET STATISTICS XML ON;");
            lock (messages)
            {
                messages.Clear();
            }

            var statsParams = BuildCursorKeyParameters(lastKey);
            var statsRun = await QueryAsync(conn, taggedSql, statsParams);
            await ExecuteAsync(conn, "SET STATISTICS XML OFF; SET STATISTICS IO OFF;");

            long logicalReads = 0;
            lock (messages)
            {
                foreach (var message in messages)
                {
                    var matches = IoStatistics.Matches(message);
                    foreach (Match match in matches)
                    {
                        logicalReads += long.Parse(match.Groups["reads"].Value, CultureInfo.InvariantCulture);
                    }
                }
            }

            var planXmlList = new[] { statsRun.PlanXml };
            var (rowsRead, plan) = SummarizePlans(planXmlList);
            var spill = statsRun.PlanXml != null && statsRun.PlanXml.Contains("SpillToTempDb", StringComparison.Ordinal);
            var rowKeys = statsRun.Rows.Select(r => r.LastKey ?? string.Empty).ToList();
            var nextKey = rowKeys.LastOrDefault();

            var timings = new List<double>();
            for (var i = 0; i < TimingRuns; i++)
            {
                var timedParams = BuildCursorKeyParameters(lastKey);
                var watch = Stopwatch.StartNew();
                await QueryAsync(conn, taggedSql, timedParams);
                watch.Stop();
                timings.Add(watch.Elapsed.TotalMilliseconds);
            }
            timings.Sort();
            var medianMs = timings[timings.Count / 2];

            return new TrafficBenchmarkResult(
                mode,
                tailName,
                positionName,
                expectedCount,
                lastKey,
                rowKeys,
                nextKey,
                logicalReads,
                rowsRead,
                spill,
                medianMs,
                plan);
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

        private static SqlParameter[] BuildCursorKeyParameters(string lastKey) =>
        [
            new SqlParameter("@lastKey", SqlDbType.NVarChar, 4000) { Value = lastKey },
            new SqlParameter("@pageSize", SqlDbType.Int) { Value = PageSize }
        ];

        /// <summary>Phần giờ của mốc: yyyyMMddHHmmssfff (17 chữ số) từ COALESCE(UpdateTime, CreateTime, DetectTime); alias rỗng là dạng định nghĩa cột tính toán.</summary>
        private static string TimePart(string type, string alias) =>
            $"CONVERT({type}(8), COALESCE({alias}UpdateTime, {alias}CreateTime, {alias}DetectTime), 112) + REPLACE(CONVERT({type}(12), COALESCE({alias}UpdateTime, {alias}CreateTime, {alias}DetectTime), 114), ':', '')";

        private static async Task<string> ReadCursorKeyAsync(SqlConnection conn, string cursorKey, string join, int rowNumber)
        {
            var sql = $"""
                SELECT x.__lastKey
                FROM (
                    SELECT {cursorKey} AS __lastKey, ROW_NUMBER() OVER (ORDER BY {cursorKey}) AS rn
                    FROM #CursorSource s
                    {join}
                    WHERE s.IsDelete IS NULL
                ) x
                WHERE x.rn = @rowNumber
                """;
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 600 };
            var rowParameter = new SqlParameter("@rowNumber", SqlDbType.BigInt) { Value = (long)rowNumber };
            cmd.Parameters.Add(rowParameter);
            var key = await cmd.ExecuteScalarAsync();
            return Assert.IsType<string>(key);
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
