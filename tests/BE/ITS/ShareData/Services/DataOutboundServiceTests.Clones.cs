using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Base.Enums;
using Modules.Integration.Delivery.Senders;
using ShareDataWorker.Extensions;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using Tests.ShareData.Common;
using Tests.ShareData.Mocks;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Ca biên khi chạy nhiều bản clone của Worker trên chung một CSDL, mỗi bản khai ShareData:SelfPartnerCode riêng:
    ///              chạy cùng lúc, cấu hình sai, một đối tác lỗi, bản clone chết khi giữ khóa, khởi động lại, dừng giữa lượt, thêm đối tác.
    /// Created date: 05/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>Gói nối đuôi của các bài test clone: mã gói, tiền tố dòng và mã các dòng theo đúng thứ tự gửi.</summary>
        private sealed record ClonePacket(string Unique, string PacketId, string PacketCode, string RowPrefix, List<string> RowCodes);

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo gói nối đuôi trên TmsZone với số dòng và @pageSize do test đặt, mỗi dòng cách nhau một phút, chưa có Mốc lần đầu.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task<ClonePacket> SeedClonePacketAsync(ISqlSugarClient db, int rowCount, int pageSize)
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-cln-{unique}";
            var packetCode = $"PKT_CLN_{unique}";
            var rowPrefix = $"ZC_CLN_{unique}_";
            var baseTime = new DateTime(2026, 10, 27, 9, 0, 0);
            var rowCodes = new List<string>();
            for (var index = 1; index <= rowCount; index++)
            {
                var name = $"R{index:00}";
                var rowTime = baseTime.AddMinutes(index);
                await SeedZoneRowAsync(db, rowPrefix, unique, name, rowTime);
                rowCodes.Add($"{rowPrefix}{name}");
            }

            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var variables = ZoneVariables(pageSize, rowPrefix, null);
            var sql = ZoneCursorSql();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);

            return new ClonePacket(unique, packetId, packetCode, rowPrefix, rowCodes);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo đối tác và đăng ký của gói, đến hạn chạy và bật gửi theo tín hiệu; mỗi đối tác có đường dẫn nhận riêng.
        /// Created date: 05/11/2026
        /// </summary>
        private static Task<(ShareDataPartner Partner, ShareDataSubscription Subscription)> SeedClonePartnerAsync(
            ISqlSugarClient db,
            ClonePacket packet,
            string name,
            Action<ShareDataPartner>? configurePartner = null,
            Action<ShareDataSubscription>? configureSub = null)
        {
            return SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = ClonePartnerCode(packet, name),
                SubCode = $"SUB_{name}_{packet.Unique}",
                DatatypeId = packet.PacketId,
                MappingShape = RowCodeShape,
                ConfigurePartner = p =>
                {
                    p.EndPointApiUrl = $"/api/sharedata/{ClonePath(packet, name)}";
                    configurePartner?.Invoke(p);
                },
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    configureSub?.Invoke(s);
                }
            });
        }

        /// <summary>Mã đối tác của bài test, cũng là giá trị ShareData:SelfPartnerCode của bản clone tương ứng.</summary>
        private static string ClonePartnerCode(ClonePacket packet, string name) => $"{name}_{packet.Unique}";

        /// <summary>Đường dẫn nhận riêng của đối tác trên máy chủ giả lập.</summary>
        private static string ClonePath(ClonePacket packet, string name) => $"cln-{name}-{packet.Unique}";

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo một bản clone của Worker: một tiến trình riêng đọc appsettings riêng, không khai selfPartnerCode thì chạy mọi đối tác.
        /// Created date: 05/11/2026
        /// </summary>
        private static DataOutboundService CreateClone(
            IServiceScope scope,
            string? selfPartnerCode,
            DataWorkerConfigService? workerConfig = null,
            string? storagePath = null)
        {
            var settings = new Dictionary<string, string?>();
            if (selfPartnerCode != null)
            {
                settings["ShareData:SelfPartnerCode"] = selfPartnerCode;
            }

            if (storagePath != null)
            {
                settings["NasStorage:BasePath"] = storagePath;
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            return CreateWorker(scope, configuration: configuration, workerConfig: workerConfig);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chạy một lượt của bản clone theo hai đường vào thật: quét theo chu kỳ hoặc tín hiệu NATS có dữ liệu mới.
        /// Created date: 05/11/2026
        /// </summary>
        private static Task RunCloneAsync(DataOutboundService clone, ClonePacket packet, bool byEvent) =>
            byEvent
                ? clone.ProcessSubscriptions(packet.PacketCode, CancellationToken.None)
                : clone.ProcessSubscriptions(CancellationToken.None);

        /// <summary>
        /// Author: DatHQ
        /// Description: Mã các dòng đối tác đã nhận, theo đúng thứ tự nhận; dòng nhận hai lần xuất hiện hai lần.
        /// Created date: 05/11/2026
        /// </summary>
        private List<string> ReceivedRows(ClonePacket packet, string name)
        {
            var escapedPrefix = Regex.Escape(packet.RowPrefix);
            var rowPattern = new Regex($"\"({escapedPrefix}R\\d+)\"");
            var clonePath = ClonePath(packet, name);
            return PartnerBodies(clonePath)
                .SelectMany(body => rowPattern.Matches(body).Select(match => match.Groups[1].Value))
                .ToList();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc dòng mốc của đối tác với gói, trả null khi đối tác chưa có mốc.
        /// Created date: 05/11/2026
        /// </summary>
        private static Task<ShareDataLastSend> GetCloneCursorAsync(ISqlSugarClient db, ClonePacket packet, string name)
        {
            var partnerCode = ClonePartnerCode(packet, name);
            var packetCode = packet.PacketCode;
            return db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode && c.CursorKey == null)
                .FirstAsync();
        }

        /// <summary>Phần cuối của mốc khi đối tác đã nhận tới dòng thứ rowIndex.</summary>
        private static string CursorEnd(ClonePacket packet, int rowIndex) => $"_R{rowIndex:00}_{packet.Unique}";

        /// <summary>
        /// Author: DatHQ
        /// Description: 4 bản clone chỉ gửi chạy cùng lúc trên một gói nhiều trang, theo chu kỳ lẫn theo tín hiệu: mỗi đối tác nhận đủ mọi dòng đúng một lần,
        ///              đúng thứ tự, đúng mã đối tác, có mốc riêng, tệp lưu không ghi đè nhau và không có cảnh báo;
        ///              đối tác không có bản clone không bị đụng tới.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProcessSubscriptions_WhenFourClonesRunTogether_EachPartnerGetsEveryRowOnce_Test(bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 7, pageSize: 2);
            var storagePath = Path.Combine(Path.GetTempPath(), $"sharedata-clones-{packet.Unique}");
            var names = new[] { "A101", "A102", "A103", "A104" };
            var subs = new Dictionary<string, ShareDataSubscription>();
            foreach (var name in names)
            {
                var (_, sub) = await SeedClonePartnerAsync(db, packet, name);
                subs[name] = sub;
            }

            var dueTime = new DateTime(2026, 1, 1, 8, 0, 0);
            var (_, noCloneSub) = await SeedClonePartnerAsync(db, packet, "A105", configureSub: s => s.NextTimeRun = dueTime);
            var clones = names.Select(name => CreateClone(scope, ClonePartnerCode(packet, name), storagePath: storagePath)).ToList();
            var evidenceFiles = new List<string>();

            try
            {
                // Act: 4 lượt chạy tuần tự (mỗi lượt gửi 1 page theo đúng cơ chế 1 page/lượt)
                for (var run = 0; run < 4; run++)
                {
                    await Task.WhenAll(clones.Select(clone => RunCloneAsync(clone, packet, byEvent)));
                    foreach (var sub in subs.Values)
                    {
                        await MakeSubscriptionDue(db, sub.ID);
                    }
                }

                // Assert
                foreach (var name in names)
                {
                    var clonePath = ClonePath(packet, name);
                    var partnerCode = ClonePartnerCode(packet, name);
                    var partnerRequests = _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains(clonePath)).ToList();
                    var cursor = await GetCloneCursorAsync(db, packet, name);
                    var savedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(subs[name].ID);
                    var logs = await GetLogs(db, subs[name].ID);
                    var partnerFiles = logs.Where(l => !string.IsNullOrEmpty(l.FilePath)).Select(l => l.FilePath!).Distinct().ToList();

                    Assert.Equal(packet.RowCodes, ReceivedRows(packet, name));
                    Assert.Equal(4, partnerRequests.Count);
                    Assert.All(partnerRequests, r => Assert.Equal(partnerCode, r.Headers["PartnerCode"]));
                    Assert.EndsWith(CursorEnd(packet, 7), cursor.LastKey);
                    Assert.Null(savedSub.ProcessingUntil);
                    Assert.Equal(4, partnerFiles.Count);
                    Assert.All(partnerFiles, f => Assert.True(File.Exists(Path.Combine(storagePath, f)), f));
                    evidenceFiles.AddRange(partnerFiles);
                }

                Assert.Equal(evidenceFiles.Count, evidenceFiles.Distinct().Count());

                var subIds = subs.Values.Select(s => s.ID).ToArray();
                var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => subIds.Contains(a.SubscriptionId)).ToListAsync();
                Assert.Empty(alerts);

                var noClonePartnerCursor = await GetCloneCursorAsync(db, packet, "A105");
                var savedNoCloneSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(noCloneSub.ID);
                Assert.Empty(ReceivedRows(packet, "A105"));
                Assert.Null(noClonePartnerCursor);
                Assert.Null(savedNoCloneSub.ProcessingUntil);
                Assert.Equal(dueTime, savedNoCloneSub.NextTimeRun);
                Assert.Empty(await GetLogs(db, noCloneSub.ID));
            }
            finally
            {
                if (Directory.Exists(storagePath))
                {
                    Directory.Delete(storagePath, recursive: true);
                }
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Một bản clone quên khai ShareData:SelfPartnerCode nên chạy mọi đối tác, cùng lúc với các bản đã khai: khóa đăng ký giữ cho mỗi đối tác
        ///              nhận mọi dòng đúng một lần, kể cả đối tác chỉ bản quên khai phục vụ.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProcessSubscriptions_WhenOneCloneHasNoSelfPartnerCode_NoRowIsSentTwice_Test(bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 7, pageSize: 2);
            var names = new[] { "A101", "A102", "A103" };
            foreach (var name in names)
            {
                await SeedClonePartnerAsync(db, packet, name);
            }

            var cloneA101 = CreateClone(scope, ClonePartnerCode(packet, "A101"));
            var cloneA102 = CreateClone(scope, ClonePartnerCode(packet, "A102"));
            var cloneWithoutCode = CreateClone(scope, null);

            // Act: 4 lượt chạy tuần tự (mỗi lượt gửi 1 page theo đúng cơ chế 1 page/lượt)
            for (var run = 0; run < 4; run++)
            {
                await Task.WhenAll(
                    RunCloneAsync(cloneWithoutCode, packet, byEvent),
                    RunCloneAsync(cloneA101, packet, byEvent),
                    RunCloneAsync(cloneA102, packet, byEvent));
                var subList = await db.Queryable<ShareDataSubscription>().Where(s => s.DatatypeId == packet.PacketId).ToListAsync();
                foreach (var s in subList)
                {
                    await MakeSubscriptionDue(db, s.ID);
                }
            }

            // Assert
            foreach (var name in names)
            {
                Assert.Equal(packet.RowCodes, ReceivedRows(packet, name));
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cùng một đối tác có hai lượt chạy song song: hai bản clone khai trùng mã, hoặc trong một bản clone thì lượt theo chu kỳ gặp lượt theo tín hiệu:
        ///              mọi dòng chỉ gửi một lần, mốc tới dòng cuối và khóa được nhả.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        public async Task ProcessSubscriptions_WhenTwoRunsServeSamePartner_NoRowIsSentTwice_Test(bool firstByEvent, bool secondByEvent, bool sameProcess)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 7, pageSize: 2);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");
            var selfCode = ClonePartnerCode(packet, "A101");
            var first = CreateClone(scope, selfCode);
            var second = sameProcess ? first : CreateClone(scope, selfCode);

            // Act: 4 lượt chạy tuần tự, mỗi lượt 2 tiến trình quét đồng thời
            for (var run = 0; run < 4; run++)
            {
                await Task.WhenAll(
                    RunCloneAsync(first, packet, firstByEvent),
                    RunCloneAsync(second, packet, secondByEvent));
                await MakeSubscriptionDue(db, sub.ID);
            }

            // Assert
            var cursor = await GetCloneCursorAsync(db, packet, "A101");
            var savedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(packet.RowCodes, ReceivedRows(packet, "A101"));
            Assert.EndsWith(CursorEnd(packet, 7), cursor.LastKey);
            Assert.Null(savedSub.ProcessingUntil);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác của một bản clone trả lỗi hoặc đóng cổng: các bản khác vẫn gửi đủ và tiến mốc; bản lỗi giữ mốc, có cảnh báo gửi lỗi,
        ///              khi đối tác hoạt động lại thì gửi đủ từ đầu mà không gửi thêm cho các đối tác khác.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProcessSubscriptions_WhenOnePartnerFails_OtherClonesAdvanceAndFailedOneRecovers_Test(bool closedPort)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 7, pageSize: 7);
            await SeedClonePartnerAsync(db, packet, "A101");
            var (failedPartner, failedSub) = await SeedClonePartnerAsync(
                db,
                packet,
                "A102",
                configurePartner: p =>
                {
                    if (closedPort)
                    {
                        p.Port = ShareDataPartnerServerMock.ClosedPort;
                    }
                });
            await SeedClonePartnerAsync(db, packet, "A103");

            var failedPath = ClonePath(packet, "A102");
            _host.PartnerServer.Responder = (_, request) => request.Path.Contains(failedPath)
                ? new ShareDataPartnerServerMock.PartnerResponsePlan(HttpStatusCode.InternalServerError)
                : null;
            var clones = new[] { "A101", "A102", "A103" }.Select(name => CreateClone(scope, ClonePartnerCode(packet, name))).ToList();

            // Act
            await Task.WhenAll(clones.Select(clone => RunCloneAsync(clone, packet, byEvent: false)));
            var rowsWhileFailing = ReceivedRows(packet, "A102");
            var cursorWhileFailing = await GetCloneCursorAsync(db, packet, "A102");
            var failedRunAlerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == failedSub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed).CountAsync();

            _host.PartnerServer.Responder = null;
            await db.Updateable<ShareDataPartner>().SetColumns(p => p.Port == ShareDataPartnerServerMock.DefaultPort).Where(p => p.ID == failedPartner.ID).ExecuteCommandAsync();
            await MakeSubscriptionDue(db, failedSub.ID);
            await RunCloneAsync(clones[1], packet, byEvent: false);

            // Assert
            Assert.True(cursorWhileFailing == null || string.IsNullOrEmpty(cursorWhileFailing.LastKey), cursorWhileFailing?.LastKey);
            Assert.True(failedRunAlerts > 0);

            var recoveredRows = ReceivedRows(packet, "A102").Skip(rowsWhileFailing.Count).ToList();
            var recoveredCursor = await GetCloneCursorAsync(db, packet, "A102");
            Assert.Equal(packet.RowCodes, recoveredRows);
            Assert.EndsWith(CursorEnd(packet, 7), recoveredCursor.LastKey);

            foreach (var healthyName in new[] { "A101", "A103" })
            {
                var healthyCursor = await GetCloneCursorAsync(db, packet, healthyName);
                Assert.Equal(packet.RowCodes, ReceivedRows(packet, healthyName));
                Assert.EndsWith(CursorEnd(packet, 7), healthyCursor.LastKey);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bản clone chết khi đang giữ khóa, đã gửi xong trang đầu. Khóa chưa hết hạn: bản chạy lại không gửi, mốc và khóa giữ nguyên.
        ///              Khóa hết hạn: bản chạy lại đi tiếp từ mốc, không gửi lại trang đầu, nhả khóa. Khóa của đối tác này không chặn đối tác khác.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ProcessSubscriptions_WhenCloneDiedHoldingLock_WaitsForExpiryThenContinuesFromCursor_Test(bool lockExpired, bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 7, pageSize: 2);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");
            var (_, sub2) = await SeedClonePartnerAsync(db, packet, "A102");
            var selfCode = ClonePartnerCode(packet, "A101");
            await RunCloneAsync(CreateClone(scope, selfCode), packet, byEvent: false);
            var cursorBefore = await GetCloneCursorAsync(db, packet, "A101");

            await MakeSubscriptionDue(db, sub.ID);
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var wholeHour = dbNow.Date.AddHours(dbNow.Hour);
            var heldUntil = lockExpired ? wholeHour.AddHours(-1) : wholeHour.AddHours(1);
            await db.Updateable<ShareDataSubscription>().SetColumns(s => s.ProcessingUntil == heldUntil).Where(s => s.ID == sub.ID).ExecuteCommandAsync();

            var restarted = CreateClone(scope, selfCode);
            var otherClone = CreateClone(scope, ClonePartnerCode(packet, "A102"));

            // Act
            for (var run = 0; run < 4; run++)
            {
                await Task.WhenAll(
                    RunCloneAsync(restarted, packet, byEvent),
                    RunCloneAsync(otherClone, packet, byEvent));
                await MakeSubscriptionDue(db, sub.ID);
                await MakeSubscriptionDue(db, sub2.ID);
            }

            // Assert
            var firstPage = packet.RowCodes.Take(2).ToList();
            var cursorAfter = await GetCloneCursorAsync(db, packet, "A101");
            var savedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.EndsWith(CursorEnd(packet, 2), cursorBefore.LastKey);
            Assert.Equal(packet.RowCodes, ReceivedRows(packet, "A102"));

            if (lockExpired)
            {
                Assert.Equal(packet.RowCodes, ReceivedRows(packet, "A101"));
                Assert.EndsWith(CursorEnd(packet, 7), cursorAfter.LastKey);
                Assert.Null(savedSub.ProcessingUntil);
            }
            else
            {
                Assert.Equal(firstPage, ReceivedRows(packet, "A101"));
                Assert.Equal(cursorBefore.LastKey, cursorAfter.LastKey);
                Assert.Equal(heldUntil, savedSub.ProcessingUntil);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Khởi động lại bản clone sau khi lượt trước dừng ở trần số trang: tiến trình mới đi tiếp từ mốc, không gửi lại và không bỏ sót dòng nào.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProcessSubscriptions_WhenCloneRestarts_ContinuesFromCursorWithoutResending_Test(bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 7, pageSize: 2);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");
            var selfCode = ClonePartnerCode(packet, "A101");
            // Act
            await RunCloneAsync(CreateClone(scope, selfCode), packet, byEvent);
            var rowsBeforeRestart = ReceivedRows(packet, "A101");
            for (var i = 0; i < 3; i++)
            {
                await MakeSubscriptionDue(db, sub.ID);
                await RunCloneAsync(CreateClone(scope, selfCode), packet, byEvent);
            }

            // Assert
            var firstPage = packet.RowCodes.Take(2).ToList();
            Assert.Equal(firstPage, rowsBeforeRestart);
            Assert.Equal(packet.RowCodes, ReceivedRows(packet, "A101"));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dừng dịch vụ đúng lúc đang gửi trang đầu: lượt dừng ngay, khóa được nhả nên chạy lại được luôn;
        ///              chạy lại không mất dòng, nhiều nhất gửi lại đúng trang đang gửi dở.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenCloneStopsMidRun_ReleasesLockAndRestartLosesNoRow_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 7, pageSize: 2);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");
            var selfCode = ClonePartnerCode(packet, "A101");
            var clonePath = ClonePath(packet, "A101");
            var clone = CreateClone(scope, selfCode);
            using var serviceStop = new CancellationTokenSource();
            _host.PartnerServer.Responder = (_, request) =>
            {
                if (request.Path.Contains(clonePath))
                {
                    serviceStop.Cancel();
                }
                return null;
            };

            // Act
            var stopError = await Record.ExceptionAsync(() => clone.ProcessSubscriptions(serviceStop.Token));
            var requestsWhenStopped = PartnerBodies(clonePath).Count;
            var subAfterStop = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);

            _host.PartnerServer.Responder = null;
            var restartClone = CreateClone(scope, selfCode);
            for (var run = 0; run < 4; run++)
            {
                await MakeSubscriptionDue(db, sub.ID);
                await RunCloneAsync(restartClone, packet, byEvent: false);
            }

            // Assert
            Assert.True(stopError is null or OperationCanceledException, stopError?.ToString());
            Assert.Equal(1, requestsWhenStopped);
            Assert.Null(subAfterStop.ProcessingUntil);

            var received = ReceivedRows(packet, "A101");
            var cursor = await GetCloneCursorAsync(db, packet, "A101");
            Assert.Equal(packet.RowCodes, received.Distinct().ToList());
            Assert.InRange(received.Count - packet.RowCodes.Count, 0, 2);
            Assert.EndsWith(CursorEnd(packet, 7), cursor.LastKey);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: ShareData:SelfPartnerCode trỏ tới đối tác tắt, mất kết nối, đã xóa, đăng ký tạm dừng hoặc mã không tồn tại:
        ///              bản clone không gửi, không báo lỗi, không đụng tới đăng ký của mình và của đối tác khác.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("Disabled", false)]
        [InlineData("Disabled", true)]
        [InlineData("Disconnected", false)]
        [InlineData("Disconnected", true)]
        [InlineData("Deleted", false)]
        [InlineData("Deleted", true)]
        [InlineData("Paused", false)]
        [InlineData("Paused", true)]
        [InlineData("Unknown", false)]
        [InlineData("Unknown", true)]
        public async Task ProcessSubscriptions_WhenSelfPartnerCodeCannotSend_SendsNothingAndTouchesNothing_Test(string scenario, bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var dueTime = new DateTime(2026, 1, 1, 8, 0, 0);
            var (_, targetSub) = await SeedClonePartnerAsync(
                db,
                packet,
                "A101",
                configurePartner: p =>
                {
                    if (scenario == "Disabled")
                    {
                        p.Status = BaseEnums.StatusEnum.Disable;
                    }
                    else if (scenario == "Disconnected")
                    {
                        p.SessionState = BaseEnums.SessionState.Disconnected;
                    }
                    else if (scenario == "Deleted")
                    {
                        p.IsDelete = dueTime;
                    }
                },
                configureSub: s =>
                {
                    s.NextTimeRun = dueTime;
                    if (scenario == "Paused")
                    {
                        s.State = BaseEnums.SubSubscriptionState.Paused;
                    }
                });
            var (_, otherSub) = await SeedClonePartnerAsync(db, packet, "A102", configureSub: s => s.NextTimeRun = dueTime);
            var selfCode = scenario == "Unknown" ? $"KHONGCO_{packet.Unique}" : ClonePartnerCode(packet, "A101");
            var clone = CreateClone(scope, selfCode);

            // Act
            var error = await Record.ExceptionAsync(() => RunCloneAsync(clone, packet, byEvent));

            // Assert
            Assert.Null(error);
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);
            foreach (var subId in new[] { targetSub.ID, otherSub.ID })
            {
                var savedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(subId);
                Assert.Equal(dueTime, savedSub.NextTimeRun);
                Assert.Null(savedSub.ProcessingUntil);
                Assert.Empty(await GetLogs(db, subId));
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: ShareData:SelfPartnerCode khác hoa thường và có khoảng trắng so với mã đối tác: vẫn nhận đúng đối tác, mốc ghi theo mã đối tác trên CSDL
        ///              nên đổi cách viết trong cấu hình không làm gửi lại từ đầu.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSelfPartnerCodeDiffersInCase_KeepsPartnerCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");
            await SeedClonePartnerAsync(db, packet, "A102");
            var partnerCode = ClonePartnerCode(packet, "A101");
            var lowerCode = $"  {partnerCode.ToLowerInvariant()}  ";
            var upperCode = partnerCode.ToUpperInvariant();

            // Act
            await RunCloneAsync(CreateClone(scope, lowerCode), packet, byEvent: false);
            await MakeSubscriptionDue(db, sub.ID);
            await RunCloneAsync(CreateClone(scope, upperCode), packet, byEvent: false);

            // Assert
            var cursor = await GetCloneCursorAsync(db, packet, "A101");
            Assert.Equal(packet.RowCodes, ReceivedRows(packet, "A101"));
            Assert.Empty(ReceivedRows(packet, "A102"));
            Assert.Equal(partnerCode, cursor.PartnerCode);
            Assert.EndsWith(CursorEnd(packet, 3), cursor.LastKey);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Thêm đối tác và bản clone sau khi các bản cũ đã gửi xong: đối tác mới nhận từ đầu theo mốc riêng, đối tác cũ không nhận lại dòng nào.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerAddedLater_NewCloneStartsFromOwnCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 5);
            var (_, oldSub) = await SeedClonePartnerAsync(db, packet, "A101");
            var oldClone = CreateClone(scope, ClonePartnerCode(packet, "A101"));
            await RunCloneAsync(oldClone, packet, byEvent: false);
            var cursorBefore = await GetCloneCursorAsync(db, packet, "A101");

            await SeedClonePartnerAsync(db, packet, "A102");
            await MakeSubscriptionDue(db, oldSub.ID);
            var newClone = CreateClone(scope, ClonePartnerCode(packet, "A102"));

            // Act
            await Task.WhenAll(
                RunCloneAsync(oldClone, packet, byEvent: false),
                RunCloneAsync(newClone, packet, byEvent: false));

            // Assert
            var cursorAfter = await GetCloneCursorAsync(db, packet, "A101");
            var newCursor = await GetCloneCursorAsync(db, packet, "A102");
            Assert.Equal(packet.RowCodes, ReceivedRows(packet, "A101"));
            Assert.Equal(packet.RowCodes, ReceivedRows(packet, "A102"));
            Assert.Equal(cursorBefore.LastKey, cursorAfter.LastKey);
            Assert.EndsWith(CursorEnd(packet, 5), newCursor.LastKey);
        }

        /// <summary>Bộ khung bọc dòng trong khóa data, dùng chung cho ánh xạ chiều gửi của bản clone và chiều nhận của hệ thống mình.</summary>
        private const string ReceiverShapeJson = """{"data":[{"rowCode":{"$field":"rowCode"}}]}""";

        /// <summary>Đường dẫn API nhận gói tin của WebAPI.</summary>
        private const string ReceiveEndpoint = "/api/sharedata/shareDataInbound/receive";

        /// <summary>Cấp HttpClient gọi thẳng WebAPI chạy trong tiến trình test, thay cho đường mạng từ bản clone tới máy nhận.</summary>
        private sealed class InProcessApiClientFactory(Host host) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => host.CreateApiClient();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác dùng chung CSDL như khi clone: đăng ký gửi kênh API trỏ về API nhận của hệ thống mình,
        ///              kèm đăng ký nhận và ánh xạ chiều nhận của cùng gói.
        /// Created date: 10/11/2026
        /// </summary>
        private static async Task<(ShareDataPartner Partner, ShareDataSubscription Subscription)> SeedReceiverPartnerAsync(
            ISqlSugarClient db,
            ClonePacket packet,
            string name)
        {
            var partnerCode = ClonePartnerCode(packet, name);
            var shape = JsonNode.Parse(ReceiverShapeJson);
            var seed = new OutboundSubSeed
            {
                PartnerCode = partnerCode,
                SubCode = $"SUB_{name}_{packet.Unique}",
                DatatypeId = packet.PacketId,
                MappingShape = shape,
                ConfigurePartner = p =>
                {
                    p.ConnectionType = ConnectionType.Api;
                    p.Address = "http://localhost";
                    p.Port = 80;
                    p.EndPointApiUrl = ReceiveEndpoint;
                }
            };
            var (partner, sub) = await SeedOutboundSubscription(db, seed);

            var inboundSub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"SUB_IN_{name}_{packet.Unique}",
                PartnerId = partner.ID,
                DatatypeId = packet.PacketId,
                Direction = BaseEnums.Direction.Inbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active
            };
            await db.Insertable(inboundSub).ExecuteCommandAsync();

            var inboundMapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = packet.PacketId,
                Direction = BaseEnums.Direction.Inbound,
                IsActive = true,
                TargetShapeJson = ReceiverShapeJson
            };
            await db.Insertable(inboundMapping).ExecuteCommandAsync();

            return (partner, sub);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bản clone chỉ gửi khai ShareData:SelfPartnerCode riêng, gửi kênh API qua WebAPI trong tiến trình test nên gói vào đúng controller nhận thật.
        /// Created date: 10/11/2026
        /// </summary>
        private DataOutboundService CreateReceiverClone(IServiceScope scope, string selfPartnerCode)
        {
            var settings = new Dictionary<string, string?> { ["ShareData:SelfPartnerCode"] = selfPartnerCode };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var services = scope.ServiceProvider;
            var apiClientFactory = new InProcessApiClientFactory(_host);
            var fileLogger = services.GetRequiredService<ILogger<FileDeliverySender>>();
            var httpSender = new HttpDeliverySender(apiClientFactory);
            var fileSender = new FileDeliverySender(configuration, fileLogger);
            var ftpSender = new FtpDeliverySender();
            var kafkaSender = new KafkaDeliverySender();
            var dispatcher = new DeliveryDispatcher(httpSender, fileSender, ftpSender, kafkaSender);
            var transport = new DataOutboundTransport(dispatcher, configuration);
            var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
            var logger = services.GetRequiredService<ILogger<DataOutboundService>>();
            var extraction = services.GetRequiredService<DataOutboundExtractionProcess>();
            var workerConfig = services.GetRequiredService<DataWorkerConfigService>();
            return new DataOutboundService(scopeFactory, logger, extraction, transport, workerConfig, configuration);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: 3 đến 5 bản clone chỉ gửi, mỗi bản khai ShareData:SelfPartnerCode riêng, cùng gửi một gói về API nhận của hệ thống mình,
        ///              một bản chỉ nhận ghi vào CSDL nhận: mỗi đối tác được ghi đủ mọi dòng đúng một lần dưới mã của chính nó,
        ///              gói nhận đều Done, hết dữ liệu thì không gửi thêm và không có cảnh báo.
        /// Created date: 10/11/2026
        /// </summary>
        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        public async Task ProcessSubscriptions_WhenSendOnlyClonesSendToOwnReceiver_ReceiverWritesEachPartnerRowsOnce_Test(int senderCount)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = db.AsTenant().GetConnectionScope(ShareDataWorkerExtensions.InboundConnectionKey);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var markerCode = $"MARK_RCV_{packet.Unique}";
            var doneState = (int)BaseEnums.InboundProcessState.Done;
            var writeSql = $"""
                INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode, ProcessState, ErrorMessage)
                OUTPUT 'INSERT'
                SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), @partnerCode, '{markerCode}', {doneState}, r.rowCode
                FROM OPENJSON(@records) WITH (rowCode NVARCHAR(128) '$.rowCode') r
                """;
            await ShareDataSqlSeed.SetWriteAsync(db, packet.PacketId, writeSql);

            var names = Enumerable.Range(1, senderCount).Select(index => $"A{100 + index}").ToList();
            var partners = new List<ShareDataPartner>();
            var subs = new List<ShareDataSubscription>();
            foreach (var name in names)
            {
                var (partner, sub) = await SeedReceiverPartnerAsync(db, packet, name);
                partners.Add(partner);
                subs.Add(sub);
            }

            var clones = partners.Select(partner => CreateReceiverClone(scope, partner.Code!)).ToList();
            var receiver = scope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act: 5 dòng, mỗi lượt 1 trang 2 dòng nên 3 lượt gửi hết, lượt thứ 4 không còn gì để gửi
            for (var run = 0; run < 4; run++)
            {
                var sends = clones.Select(clone => clone.ProcessSubscriptions(CancellationToken.None));
                await Task.WhenAll(sends);
                await receiver.ProcessPendingPackets(CancellationToken.None);
                foreach (var sub in subs)
                {
                    await MakeSubscriptionDue(db, sub.ID);
                }
            }

            // Assert
            var written = await inboundDb.Queryable<ShareDataInboundPacket>().Where(p => p.PacketCode == markerCode).ToListAsync();
            var received = await inboundDb.Queryable<ShareDataInboundPacket>().Where(p => p.PacketCode == packet.PacketCode).ToListAsync();
            var partnerIds = partners.Select(p => p.ID).ToList();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => partnerIds.Contains(a.PartnerId)).ToListAsync();
            foreach (var partner in partners)
            {
                var partnerRows = written.Where(w => w.PartnerCode == partner.Code).Select(w => w.ErrorMessage!).Order().ToList();
                var partnerPackets = received.Where(p => p.PartnerCode == partner.Code).ToList();
                Assert.Equal(packet.RowCodes, partnerRows);
                Assert.Equal(3, partnerPackets.Count);
                Assert.All(partnerPackets, p => Assert.Equal(BaseEnums.InboundProcessState.Done, p.ProcessState));
            }

            Assert.Equal(senderCount * packet.RowCodes.Count, written.Count);
            Assert.Empty(alerts);
        }
    }
}
