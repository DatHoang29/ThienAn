using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Dto.AlertLog;
using Module.ShareData.Core.Dto.CodeSet;
using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Dto.Metadata;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Dto.Session;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Shared.Core.Utilities.Constants;
using Tests.ShareData.Common;
using Xunit.Abstractions;

namespace Tests.ShareData.Performance
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Đo thời gian từng API đọc của ShareData qua luồng API thật (validator + IMessageBus) trên dữ liệu cỡ như CSDL dev
    ///              (nhật ký hoạt động mặc định 300.000 dòng, cảnh báo 30.000 dòng, 14 đối tác, 20 gói tin có câu SQL).
    ///              Mỗi API gọi khởi động 1 lần rồi đo 5 lần, lấy trung vị; đếm số câu SQL mỗi lần gọi và chạy lại từng câu SELECT
    ///              trên kết nối riêng với SET STATISTICS IO để đếm số trang SQL Server phải đọc (không phụ thuộc máy nhanh hay chậm).
    ///              Kết quả ghi ra kết quả test và tệp perf-results/sharedata-api-latency.md ở thư mục chạy test.
    ///              Đổi số dòng nhật ký bằng SHAREDATA_API_PERF_ROWS, ngưỡng thời gian bằng SHAREDATA_API_BUDGET_MS, ngưỡng số trang đọc bằng SHAREDATA_API_READS_BUDGET.
    /// Created date: 08/11/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataApiLatencyTests(Host host, ShareDataApiLatencyTests.SeedFixture seed, ITestOutputHelper output)
        : IClassFixture<ShareDataApiLatencyTests.SeedFixture>
    {
        private const int WarmupRuns = 1;
        private const int TimedRuns = 5;
        private static readonly ConcurrentDictionary<string, string> ReportRows = new(StringComparer.Ordinal);

        /// <summary>
        /// Author: DatHQ
        /// Description: Mở từng màn ShareData gọi API đọc như FE: mỗi API phải trả về trong ngưỡng thời gian (mặc định 300 ms, trung vị 5 lần)
        ///              và mỗi lần gọi không được đọc quá ngưỡng số trang (mặc định 5.000 trang 8 KB), tức là không quét cả bảng lớn.
        /// Created date: 08/11/2026
        /// </summary>
        [Theory(Skip = "Đo thời gian API đọc trên dữ liệu cỡ dev (dựng 300.000 dòng nhật ký, chạy khoảng 5 phút) — bỏ Skip khi cần đo lại; đổi số dòng bằng SHAREDATA_API_PERF_ROWS, ngưỡng bằng SHAREDATA_API_BUDGET_MS, SHAREDATA_API_READS_BUDGET.")]
        [Trait("Feature", "Performance")]
        [InlineData("ActivityLog.GetById")]
        [InlineData("ActivityLog.GetSteps")]
        [InlineData("AlertLog.Page")]
        [InlineData("AlertLog.Summary")]
        [InlineData("AlertLog.CountUnacked")]
        [InlineData("AlertLog.GetList")]
        [InlineData("CodeSet.Page")]
        [InlineData("CodeSet.GetList")]
        [InlineData("Mapping.Page")]
        [InlineData("Mapping.GetList")]
        [InlineData("Partner.Page")]
        [InlineData("Partner.GetList")]
        [InlineData("Partner.GetById")]
        [InlineData("Session.Page")]
        [InlineData("Session.GetList")]
        [InlineData("Session.Summary")]
        [InlineData("Subscription.Page")]
        [InlineData("Subscription.GetList")]
        [InlineData("Packet.Page")]
        [InlineData("Packet.Page.ByPartner")]
        [InlineData("Packet.GetList")]
        [InlineData("Packet.GetById")]
        [InlineData("Packet.GetExtractSql")]
        [InlineData("Packet.GetSqlVariables")]
        [InlineData("Packet.GetSqlVariables.Partner")]
        [InlineData("Packet.GetSqlDetail")]
        [InlineData("Packet.GetPartnerSqlList")]
        [InlineData("Packet.GetPartnerExtractSql")]
        [InlineData("Packet.GetWriteSqls")]
        [InlineData("Packet.GetPartnerWriteSqlList")]
        [InlineData("Packet.GetMetadataTables")]
        [InlineData("Packet.GetMetadataFields")]
        [InlineData("Packet.GetFields")]
        [InlineData("Packet.SampleData")]
        [InlineData("Packet.ParseExtractColumns")]
        [InlineData("Packet.DescribeColumns")]
        [InlineData("Packet.ExecuteExtractSqlSample")]
        public async Task ReadApi_OnDevSizedData_RespondsWithinBudgetWithoutTableScan_Test(string api)
        {
            // Arrange
            var data = await seed.EnsureSeededAsync(host.Services);
            SetSuperAdminContext();
            var budgetMs = ReadIntSetting("SHAREDATA_API_BUDGET_MS", 300);
            var readsBudget = ReadIntSetting("SHAREDATA_API_READS_BUDGET", 5_000);

            // Act
            for (var i = 0; i < WarmupRuns; i++)
            {
                await CallAsync(api, data);
            }

            var timings = new List<double>();
            IReadOnlyList<CapturedCommand> lastCommands = [];
            for (var i = 0; i < TimedRuns; i++)
            {
                using var capture = SqlCommandCapture.Start();
                var watch = Stopwatch.StartNew();
                await CallAsync(api, data);
                watch.Stop();
                timings.Add(watch.Elapsed.TotalMilliseconds);
                lastCommands = capture.Commands;
            }

            var median = timings.Order().ElementAt(timings.Count / 2);
            var reads = await CountLogicalReadsAsync(data.ConnectionString, lastCommands);
            var sqlMs = lastCommands.Sum(c => c.ElapsedMs);
            var heaviest = reads.Tables.OrderByDescending(t => t.Value).Select(t => $"{t.Key} {t.Value:N0}").FirstOrDefault() ?? "-";
            WriteReport(api, median, timings.Max(), lastCommands.Count, sqlMs, reads.Total, heaviest, data.ActivityRows);

            // Assert
            Assert.True(reads.Total <= readsBudget,
                $"{api}: mỗi lần gọi đọc {reads.Total:N0} trang (ngưỡng {readsBudget:N0}), nặng nhất {heaviest} — câu SQL đang quét cả bảng, thiếu chỉ mục cho điều kiện lọc.");
            Assert.True(median <= budgetMs,
                $"{api}: trung vị {median:N0} ms (ngưỡng {budgetMs} ms), {lastCommands.Count} câu SQL mất {sqlMs:N0} ms, đọc {reads.Total:N0} trang.");
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Gọi một API đọc qua luồng API thật với dữ liệu đã dựng; FE truyền gì thì truyền đúng như vậy (khoảng ngày 7 ngày, sắp theo occurredAt giảm dần).
        /// Created date: 08/11/2026
        /// </summary>
        private async Task CallAsync(string api, SeedData d)
        {
            using var scope = host.Services.CreateScope();
            var sp = scope.ServiceProvider;
            // FE gửi giờ UTC, API đổi về giờ máy chủ.
            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow;

            switch (api)
            {
                case "ActivityLog.GetById":
                    await sp.CallApiAsync<ShareDataActivityLogOutput>(new ShareDataIdActivityLogInput { ID = d.ActivityRootId });
                    break;
                case "ActivityLog.GetSteps":
                    await sp.CallApiAsync<List<ShareDataActivityLogOutput>>(new ShareDataStepActivityLogInput { ID = d.ActivityRootId });
                    break;
                case "AlertLog.Page":
                    await sp.CallApiAsync<SqlSugarPagedList<ShareDataPageAlertLogOutput>>(new ShareDataPageAlertLogInput { FromDate = from, ToDate = to, Page = 1, PageSize = 50, Field = "occurredAt", Order = "desc", DescStr = "desc" });
                    break;
                case "AlertLog.Summary":
                    await sp.CallApiAsync<ShareDataSummaryAlertLogOutput>(new ShareDataSummaryAlertLogInput { FromDate = from, ToDate = to });
                    break;
                case "AlertLog.CountUnacked":
                    await sp.CallApiAsync<int>(new ShareDataCountUnackedAlertLogInput());
                    break;
                case "AlertLog.GetList":
                    await sp.CallApiAsync<List<ShareDataAlertLogOutput>>(new ShareDataAlertLogInput { FromDate = from, ToDate = to });
                    break;
                case "CodeSet.Page":
                    await sp.CallApiAsync<SqlSugarPagedList<ShareDataPageCodeSetOutput>>(new ShareDataPageCodeSetInput { Page = 1, PageSize = 20 });
                    break;
                case "CodeSet.GetList":
                    await sp.CallApiAsync<List<ShareDataCodeSetOutput>>(new ShareDataCodeSetInput());
                    break;
                case "Mapping.Page":
                    await sp.CallApiAsync<SqlSugarPagedList<ShareDataPageMappingOutput>>(new ShareDataPageMappingInput { Page = 1, PageSize = 20 });
                    break;
                case "Mapping.GetList":
                    await sp.CallApiAsync<List<ShareDataMappingOutput>>(new ShareDataMappingInput());
                    break;
                case "Partner.Page":
                    await sp.CallApiAsync<SqlSugarPagedList<ShareDataPagePartnerOutput>>(new ShareDataPagePartnerInput { Page = 1, PageSize = 20 });
                    break;
                case "Partner.GetList":
                    await sp.CallApiAsync<List<ShareDataPartnerOutput>>(new ShareDataPartnerInput());
                    break;
                case "Partner.GetById":
                    await sp.CallApiAsync<ShareDataPartnerOutput>(new ShareDataIdPartnerInput { ID = d.PartnerId });
                    break;
                case "Session.Page":
                    await sp.CallApiAsync<SqlSugarPagedList<ShareDataPageSessionOutput>>(new ShareDataPageSessionInput { Page = 1, PageSize = 20 });
                    break;
                case "Session.GetList":
                    await sp.CallApiAsync<List<ShareDataSessionOutput>>(new ShareDataSessionInput());
                    break;
                case "Session.Summary":
                    await sp.CallApiAsync<ShareDataSummarySessionOutput>(new ShareDataSummarySessionInput());
                    break;
                case "Subscription.Page":
                    await sp.CallApiAsync<SqlSugarPagedList<ShareDataPageSubscriptionOutput>>(new ShareDataPageSubscriptionInput { Page = 1, PageSize = 20 });
                    break;
                case "Subscription.GetList":
                    await sp.CallApiAsync<List<ShareDataSubscriptionOutput>>(new ShareDataSubscriptionInput());
                    break;
                case "Packet.Page":
                    await sp.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(new ShareDataPagePacketInput { Page = 1, PageSize = 20, Field = "orderNo", Order = "asc", DescStr = "desc" });
                    break;
                case "Packet.Page.ByPartner":
                    await sp.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(new ShareDataPagePacketInput { Page = 1, PageSize = 20, Field = "code", Order = "asc", PartnerId = d.PartnerId });
                    break;
                case "Packet.GetList":
                    await sp.CallApiAsync<List<ShareDataPagePacketOutput>>(new ShareDataPacketInput());
                    break;
                case "Packet.GetById":
                    await sp.CallApiAsync<ShareDataPacketOutput>(new ShareDataIdPacketInput { ID = d.PacketId });
                    break;
                case "Packet.GetExtractSql":
                    await sp.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput { PacketCode = d.PacketCode });
                    break;
                case "Packet.GetSqlVariables":
                    await sp.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = d.PacketCode });
                    break;
                case "Packet.GetSqlVariables.Partner":
                    await sp.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = d.PacketCode, PartnerId = d.PartnerId });
                    break;
                case "Packet.GetSqlDetail":
                    await sp.CallApiAsync<PacketSqlDetailOutput>(new GetPacketSqlDetailInput { PacketCode = d.PacketCode });
                    break;
                case "Packet.GetPartnerSqlList":
                    await sp.CallApiAsync<List<PartnerSqlListItemOutput>>(new GetPartnerSqlListInput { PacketCode = d.PacketCode });
                    break;
                case "Packet.GetPartnerExtractSql":
                    await sp.CallApiAsync<PartnerExtractSqlOutput>(new GetPartnerExtractSqlInput { PacketCode = d.PacketCode, PartnerId = d.PartnerId });
                    break;
                case "Packet.GetWriteSqls":
                    await sp.CallApiAsync<PacketWriteSqlsOutput>(new GetPacketWriteSqlsInput { PacketCode = d.PacketCode });
                    break;
                case "Packet.GetPartnerWriteSqlList":
                    await sp.CallApiAsync<List<PartnerSqlListItemOutput>>(new GetPartnerWriteSqlListInput { PacketCode = d.PacketCode });
                    break;
                case "Packet.GetMetadataTables":
                    await sp.CallApiAsync<List<MetadataTableDto>>(new GetMetadataTablesInput());
                    break;
                case "Packet.GetMetadataFields":
                    await sp.CallApiAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput { Id = d.SourceTableId });
                    break;
                case "Packet.GetFields":
                    await sp.CallApiAsync<List<SqlFieldItem>>(new GetPacketFieldsInput { DatatypeId = d.PacketId });
                    break;
                case "Packet.SampleData":
                    await sp.CallApiAsync<ShareDataPacketSampleDataOutput>(new ShareDataPacketSampleDataInput { DatatypeId = d.PacketId, Top = 5 });
                    break;
                case "Packet.ParseExtractColumns":
                    await sp.CallApiAsync<ParsePacketColumnsOutput>(new ParsePacketColumnsInput { PacketCode = d.PacketCode, ExtractSql = d.ExtractSql });
                    break;
                case "Packet.DescribeColumns":
                    await sp.CallApiAsync<DescribeColumnsOutput>(new DescribeColumnsInput { PacketCode = d.PacketCode, ExtractSql = d.ExtractSql });
                    break;
                case "Packet.ExecuteExtractSqlSample":
                    await sp.CallApiAsync<ExecutePacketSqlSampleOutput>(new ExecutePacketSqlSampleInput { PacketCode = d.PacketCode, ExtractSql = d.ExtractSql, Top = 10 });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(api), api, "Chưa khai cách gọi API này.");
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đặt người dùng quản trị cao nhất cho HttpContext như khi đăng nhập trên web (một số API đọc chỉ mở cho quản trị).
        /// Created date: 08/11/2026
        /// </summary>
        private void SetSuperAdminContext()
        {
            var claims = new List<Claim>
            {
                new(ClaimConst.AccountType, ((int)BaseEnums.AccountTypeEnum.SuperAdmin).ToString(CultureInfo.InvariantCulture)),
                new(ClaimConst.Account, "perf_operator"),
                new(ClaimConst.UserId, Guid.NewGuid().ToString("N")),
                new(ClaimTypes.Name, "perf_operator")
            };
            host.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            };
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chạy lại các câu SELECT của lần gọi cuối trên kết nối riêng với SET STATISTICS IO ON, cộng số trang đọc theo từng bảng.
        ///              Câu ghi (INSERT/UPDATE/DELETE/MERGE) không chạy lại.
        /// Created date: 08/11/2026
        /// </summary>
        private static async Task<(long Total, Dictionary<string, long> Tables)> CountLogicalReadsAsync(string connectionString, IReadOnlyList<CapturedCommand> commands)
        {
            var tables = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var ioPattern = new Regex(@"Table '(?<table>[^']+)'\. Scan count \d+, logical reads (?<reads>\d+)", RegexOptions.Compiled);
            await using var conn = new SqlConnection(connectionString);
            conn.InfoMessage += (_, e) =>
            {
                foreach (Match m in ioPattern.Matches(e.Message))
                {
                    var table = m.Groups["table"].Value;
                    tables[table] = tables.GetValueOrDefault(table) + long.Parse(m.Groups["reads"].Value, CultureInfo.InvariantCulture);
                }
            };
            await conn.OpenAsync();
            await using (var on = new SqlCommand("SET STATISTICS IO ON", conn))
            {
                await on.ExecuteNonQueryAsync();
            }

            foreach (var command in commands.Where(c => c.IsReadOnly))
            {
                await using var cmd = new SqlCommand(command.Text, conn) { CommandType = command.Type, CommandTimeout = 120 };
                foreach (var p in command.Parameters)
                {
                    cmd.Parameters.Add(new SqlParameter(p.Name, p.DbType) { Size = p.Size, Value = p.Value ?? DBNull.Value, Precision = p.Precision, Scale = p.Scale });
                }
                await using var reader = await cmd.ExecuteReaderAsync();
                do
                {
                    while (await reader.ReadAsync())
                    {
                    }
                } while (await reader.NextResultAsync());
            }

            return (tables.Values.Sum(), tables);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghi một dòng kết quả đo ra kết quả test và gộp vào tệp perf-results/sharedata-api-latency.md.
        /// Created date: 08/11/2026
        /// </summary>
        private void WriteReport(string api, double median, double max, int sqlCount, double sqlMs, long reads, string heaviest, int activityRows)
        {
            var row = $"| {api} | {median:N0} | {max:N0} | {sqlCount} | {sqlMs:N0} | {reads:N0} | {heaviest} |";
            output.WriteLine("| API | Trung vị ms | Lâu nhất ms | Số câu SQL | Thời gian SQL ms | Trang đọc | Bảng đọc nhiều nhất |");
            output.WriteLine(row);
            ReportRows[api] = row;

            var sb = new StringBuilder()
                .AppendLine("# Thời gian API đọc ShareData")
                .AppendLine()
                .AppendLine(CultureInfo.InvariantCulture, $"Đo lúc {DateTime.Now:dd/MM/yyyy HH:mm}, nhật ký hoạt động {activityRows:N0} dòng, gọi qua validator + IMessageBus (không gồm HTTP, xác thực, ghi log request).")
                .AppendLine()
                .AppendLine("| API | Trung vị ms | Lâu nhất ms | Số câu SQL | Thời gian SQL ms | Trang đọc | Bảng đọc nhiều nhất |")
                .AppendLine("|---|---:|---:|---:|---:|---:|---|");
            foreach (var line in ReportRows.OrderBy(r => r.Key, StringComparer.Ordinal))
            {
                sb.AppendLine(line.Value);
            }
            var dir = Path.Combine(AppContext.BaseDirectory, "perf-results");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "sharedata-api-latency.md"), sb.ToString());
        }

        private static int ReadIntSetting(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : fallback;

        /// <summary>Định danh dữ liệu đã dựng để các API đọc tra cứu.</summary>
        public sealed record SeedData(
            string ConnectionString,
            int ActivityRows,
            string ActivityRootId,
            string PartnerId,
            string PacketId,
            string PacketCode,
            string ExtractSql,
            string SourceTableId);

        /// <summary>Một câu SQL đã chạy trong lần gọi API được đo.</summary>
        public sealed record CapturedCommand(string Text, CommandType Type, IReadOnlyList<CapturedParameter> Parameters, double ElapsedMs)
        {
            public bool IsReadOnly => Regex.IsMatch(Text.TrimStart(), @"^(SELECT|WITH)\b", RegexOptions.IgnoreCase)
                && !Regex.IsMatch(Text, @"\b(INSERT|UPDATE|DELETE|MERGE)\b", RegexOptions.IgnoreCase);
        }

        /// <summary>Tham số của câu SQL đã chạy, chép lại để chạy lại câu.</summary>
        public sealed record CapturedParameter(string Name, SqlDbType DbType, int Size, byte Precision, byte Scale, object? Value);

        /// <summary>
        /// Author: DatHQ
        /// Description: Nghe sự kiện chẩn đoán của Microsoft.Data.SqlClient để chép mọi câu SQL chạy trong cùng luồng async với lần gọi đang đo,
        ///              kèm thời gian chạy từng câu. Câu do luồng khác chạy (Worker nền) không bị đếm.
        /// Created date: 08/11/2026
        /// </summary>
        private sealed class SqlCommandCapture : IDisposable, IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
        {
            private static readonly AsyncLocal<SqlCommandCapture?> Current = new();
            private readonly List<IDisposable> _subscriptions = [];
            private readonly ConcurrentDictionary<Guid, (long Started, CapturedCommand Command)> _running = new();
            private readonly List<CapturedCommand> _commands = [];

            public IReadOnlyList<CapturedCommand> Commands
            {
                get
                {
                    lock (_commands)
                    {
                        return _commands.ToList();
                    }
                }
            }

            public static SqlCommandCapture Start()
            {
                var capture = new SqlCommandCapture();
                Current.Value = capture;
                capture._subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(capture));
                return capture;
            }

            public void OnNext(DiagnosticListener listener)
            {
                if (listener.Name == "SqlClientDiagnosticListener")
                {
                    _subscriptions.Add(listener.Subscribe(this));
                }
            }

            public void OnNext(KeyValuePair<string, object?> evt)
            {
                if (!ReferenceEquals(Current.Value, this) || evt.Value is null)
                {
                    return;
                }
                var payload = evt.Value;
                var type = payload.GetType();
                if (type.GetProperty("OperationId")?.GetValue(payload) is not Guid operationId)
                {
                    return;
                }
                var timestamp = type.GetProperty("Timestamp")?.GetValue(payload) is long ts ? ts : Stopwatch.GetTimestamp();

                if (evt.Key.EndsWith("WriteCommandBefore", StringComparison.Ordinal) && type.GetProperty("Command")?.GetValue(payload) is SqlCommand command)
                {
                    var parameters = command.Parameters.Cast<SqlParameter>()
                        .Select(p => new CapturedParameter(p.ParameterName, p.SqlDbType, p.Size, p.Precision, p.Scale, p.Value))
                        .ToList();
                    _running[operationId] = (timestamp, new CapturedCommand(command.CommandText, command.CommandType, parameters, 0));
                }
                else if ((evt.Key.EndsWith("WriteCommandAfter", StringComparison.Ordinal) || evt.Key.EndsWith("WriteCommandError", StringComparison.Ordinal))
                    && _running.TryRemove(operationId, out var started))
                {
                    var elapsed = Stopwatch.GetElapsedTime(started.Started, timestamp).TotalMilliseconds;
                    lock (_commands)
                    {
                        _commands.Add(started.Command with { ElapsedMs = elapsed });
                    }
                }
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void Dispose()
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
                Current.Value = null;
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dựng dữ liệu cỡ như CSDL dev một lần cho cả lớp test và xóa sạch khi lớp chạy xong để không ảnh hưởng test khác.
        ///              Nhật ký hoạt động: 40% dòng cha, 60% dòng bước, phần lớn nằm ngoài khoảng 7 ngày gần nhất (như dev: 650.000 dòng mà 10 ngày gần nhất chỉ vài chục dòng),
        ///              mỗi dòng khoảng 1,3 KB như dev (864 MB / 651.882 dòng).
        /// Created date: 08/11/2026
        /// </summary>
        public sealed class SeedFixture : IAsyncLifetime
        {
            private const string SeedUser = "SHAREDATA-API-PERF";
            private readonly SemaphoreSlim _lock = new(1, 1);
            private SeedData? _data;
            private IServiceProvider? _services;
            private readonly List<string> _partnerIds = [];
            private readonly List<string> _packetIds = [];

            public Task InitializeAsync() => Task.CompletedTask;

            public async Task<SeedData> EnsureSeededAsync(IServiceProvider services)
            {
                await _lock.WaitAsync();
                try
                {
                    if (_data is not null)
                    {
                        return _data;
                    }
                    _services = services;
                    using var scope = services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
                    var rows = ReadIntSetting("SHAREDATA_API_PERF_ROWS", 300_000);

                    await RemoveSeedAsync(db);
                    await SeedActivityLogAsync(db, rows);
                    await SeedAlertLogAsync(db, 30_000);
                    var sourceTableId = await EnsureSourceTablesAsync(db);
                    var (partnerId, packetId, packetCode, extractSql) = await SeedPartnersAndPacketsAsync(db);
                    var rootId = await db.Ado.GetStringAsync($"SELECT TOP 1 ID FROM ShareDataActivityLog WHERE CreateUId = '{SeedUser}' AND ParentId IS NULL ORDER BY ID");

                    _data = new SeedData(db.CurrentConnectionConfig.ConnectionString, rows, rootId, partnerId, packetId, packetCode, extractSql, sourceTableId);
                    return _data;
                }
                finally
                {
                    _lock.Release();
                }
            }

            public async Task DisposeAsync()
            {
                if (_services is null)
                {
                    return;
                }
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
                await RemoveSeedAsync(db);
                if (_packetIds.Count > 0)
                {
                    await db.Deleteable<ShareDataSql>().Where(x => _packetIds.Contains(x.PacketId!)).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataPacket>().Where(x => _packetIds.Contains(x.ID)).ExecuteCommandAsync();
                }
                if (_partnerIds.Count > 0)
                {
                    await db.Deleteable<ShareDataPartner>().Where(x => _partnerIds.Contains(x.ID)).ExecuteCommandAsync();
                }
            }

            private static async Task RemoveSeedAsync(ISqlSugarClient db)
            {
                db.Ado.CommandTimeOut = 600;
                await db.Ado.ExecuteCommandAsync($"DELETE FROM ShareDataActivityLog WHERE CreateUId = '{SeedUser}'; DELETE FROM ShareDataAlertLog WHERE CreateUId = '{SeedUser}';");
            }

            private static async Task SeedActivityLogAsync(ISqlSugarClient db, int rows)
            {
                // i % 5 ∈ {0, 1}: dòng cha; còn lại là bước của dòng cha gần nhất (i - i % 5). 300 dòng đầu rơi vào 7 ngày gần nhất.
                var sql = $"""
                    ;WITH n AS (
                        SELECT TOP ({rows}) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
                        FROM sys.all_objects a CROSS JOIN sys.all_objects b CROSS JOIN sys.all_objects c
                    )
                    INSERT INTO ShareDataActivityLog
                        (ID, LogType, Action, OccurredAt, Success, Description, TargetType, TargetId, TargetName, PartnerId, PartnerName,
                         SubscriptionId, SessionId, TransferDirection, DatatypeId, PacketVersion, SerialNbr, PacketNbr, Format, ByteSize,
                         RecordCount, FilePath, Hash, OperatorName, ParentId, StepNbr, CreateTime, CreateUId)
                    SELECT
                        CONCAT('perf', RIGHT(CONCAT('000000000000', i), 12)),
                        1,
                        10 + i % 2,
                        CASE WHEN i <= 300 THEN DATEADD(minute, -i * 20, GETDATE()) ELSE DATEADD(second, -i * 8, '20260901') END,
                        CASE WHEN i % 7 = 0 THEN 0 ELSE 1 END,
                        CONCAT(N'Gửi gói tin dữ liệu chung cho đối tác nhận, lượt ', i, N'. ', REPLICATE(N'Nội dung mô tả bước truyền nhận. ', 7)),
                        1,
                        CONCAT('sub', RIGHT(CONCAT('000000000000', i % 29), 12), 'aaaaaaaaaaaaaaaa'),
                        CONCAT(N'Đăng ký gửi gói tin ', i % 29, N' của đối tác'),
                        CONCAT('par', RIGHT(CONCAT('000000000000', i % 14), 12), 'bbbbbbbbbbbbbbbbb'),
                        CONCAT(N'Đối tác nhận dữ liệu số ', i % 14),
                        CONCAT('sub', RIGHT(CONCAT('000000000000', i % 29), 12), 'aaaaaaaaaaaaaaaa'),
                        CONCAT('ses', RIGHT(CONCAT('000000000000', i % 19), 12), 'ccccccccccccccccc'),
                        i % 2,
                        CONCAT('pkt', RIGHT(CONCAT('000000000000', i % 20), 12), 'ddddddddddddddddd'),
                        '1.0', i % 100000, i % 1000, 0, 2048 + i % 5000, 100,
                        CONCAT('Out/2026/09/01/partner_', i % 14, '/packet_', i % 20, '/data_20260901_', RIGHT(CONCAT('000000000000', i), 12), '_payload_export.json'),
                        CONVERT(varchar(64), HASHBYTES('SHA2_256', CAST(i AS varchar(12))), 2),
                        'worker',
                        CASE WHEN i % 5 IN (0, 1) THEN NULL ELSE CONCAT('perf', RIGHT(CONCAT('000000000000', i - i % 5), 12)) END,
                        CASE WHEN i % 5 IN (0, 1) THEN NULL ELSE i % 5 END,
                        GETDATE(),
                        '{SeedUser}'
                    FROM n;
                    """;
                db.Ado.CommandTimeOut = 600;
                await db.Ado.ExecuteCommandAsync(sql);
            }

            private static async Task SeedAlertLogAsync(ISqlSugarClient db, int rows)
            {
                var sql = $"""
                    ;WITH n AS (
                        SELECT TOP ({rows}) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
                        FROM sys.all_objects a CROSS JOIN sys.all_objects b
                    )
                    INSERT INTO ShareDataAlertLog (ID, OccurredAt, Severity, AlertSource, AlertCode, PartnerId, Message, Acknowledged, CreateTime, CreateUId)
                    SELECT
                        CONCAT('perfalert', RIGHT(CONCAT('000000000000', i), 12)),
                        CASE WHEN i <= 100 THEN DATEADD(minute, -i * 30, GETDATE()) ELSE DATEADD(minute, -i * 3, '20261001') END,
                        i % 3, 3, CONCAT('ESH-', 1000 + i % 50),
                        CONCAT('par', RIGHT(CONCAT('000000000000', i % 14), 12), 'bbbbbbbbbbbbbbbbb'),
                        CONCAT(N'Gửi gói tin thất bại do đối tác không phản hồi, lượt ', i),
                        CASE WHEN i % 4 = 0 THEN 0 ELSE 1 END,
                        GETDATE(),
                        '{SeedUser}'
                    FROM n;
                    """;
                await db.Ado.ExecuteCommandAsync(sql);
            }

            private static async Task<string> EnsureSourceTablesAsync(ISqlSugarClient db)
            {
                var existing = (await db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null).ToListAsync())
                    .Select(x => x.TableName?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missing = SeedScriptReader.ReadSourceTables()
                    .Where(s => !string.IsNullOrEmpty(s.TableName) && !existing.Contains(s.TableName.Trim()))
                    .ToList();
                if (missing.Count > 0)
                {
                    await db.Insertable(missing).ExecuteCommandAsync();
                }
                return (await db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null && x.IsQueryable == true).OrderBy(x => x.OrderNo).FirstAsync()).ID;
            }

            private async Task<(string PartnerId, string PacketId, string PacketCode, string ExtractSql)> SeedPartnersAndPacketsAsync(ISqlSugarClient db)
            {
                Assert.True(PacketTemplateSeed.TryGet("101", out var template));
                var unique = Guid.NewGuid().ToString("N")[..8];

                var partners = Enumerable.Range(1, 14).Select(i => new ShareDataPartner
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"PERF_P{i:00}_{unique}",
                    Name = $"Đối tác đo hiệu năng {i:00}",
                    Address = "127.0.0.1",
                    Port = 8080,
                    ConnectionType = ConnectionType.Api,
                    Status = BaseEnums.StatusEnum.Enable,
                    SessionState = BaseEnums.SessionState.Disconnected
                }).ToList();
                await db.Insertable(partners).ExecuteCommandAsync();
                _partnerIds.AddRange(partners.Select(p => p.ID));

                var packets = Enumerable.Range(1, 20).Select(i => new ShareDataPacket
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"perf{i:00}_{unique}",
                    Name = $"Gói đo hiệu năng {i:00}",
                    Status = BaseEnums.StatusEnum.Enable
                }).ToList();
                await db.Insertable(packets).ExecuteCommandAsync();
                _packetIds.AddRange(packets.Select(p => p.ID));

                foreach (var packet in packets)
                {
                    await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, template.ExtractSql);
                    await ShareDataSqlSeed.DeclareStandardVariablesAsync(db, packet.ID, null, template.ExtractSql);
                }
                var target = packets[0];
                await ShareDataSqlSeed.SetPartnerAsync(db, target.ID, partners[0].ID, template.ExtractSql);
                await ShareDataSqlSeed.DeclareStandardVariablesAsync(db, target.ID, partners[0].ID, template.ExtractSql);

                return (partners[0].ID, target.ID, target.Code!, template.ExtractSql);
            }
        }
    }
}
