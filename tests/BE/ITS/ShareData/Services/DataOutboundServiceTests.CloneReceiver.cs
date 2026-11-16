using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Base.Enums;
using Modules.Integration.Delivery.Senders;
using Shared.Utility.Threading;
using ShareDataWorker.Extensions;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Kịch bản clone thực tế dùng chung một CSDL: các bản Worker chỉ gửi (chỉ khai ShareData:Role, hoặc khai thêm ShareData:SelfPartnerCode)
    ///              gửi kênh API vào đúng controller nhận của WebAPI chạy trong tiến trình test; một bản chỉ nhận ghi CSDL nhận.
    ///              Gồm ca chạy thường và các ca biên khi vận hành: bản nhận dừng rồi chạy lại, một đối tác sai đường dẫn nhận, quên tắt Worker cũ,
    ///              bản gửi khởi động lại, có dữ liệu mới sau khi gửi hết, mỗi cặp đối tác gửi một gói khác nhau, WebAPI khởi động lại đúng lúc vừa lưu gói.
    /// Created date: 10/11/2026
    /// Modified date: 13/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>Bộ khung bọc dòng trong khóa data, dùng chung cho ánh xạ chiều gửi của bản clone và chiều nhận của hệ thống mình.</summary>
        private const string ReceiverShapeJson = """{"data":[{"rowCode":{"$field":"rowCode"}}]}""";

        /// <summary>Đường dẫn API nhận gói tin của WebAPI.</summary>
        private const string ReceiveEndpoint = "/api/sharedata/shareDataInbound/receive";

        /// <summary>Bốn vai đối tác của kịch bản 4 bản chỉ gửi.</summary>
        private static readonly string[] FourNames = ["A101", "A102", "A103", "A104"];

        /// <summary>Đối tác của kịch bản clone: tên vai, đăng ký gửi và đăng ký nhận của cùng gói.</summary>
        private sealed record ReceiverPartner(string Name, ShareDataPartner Partner, ShareDataSubscription Outbound, ShareDataSubscription Inbound);

        /// <summary>Cấp HttpClient gọi thẳng WebAPI chạy trong tiến trình test, thay cho đường mạng từ bản clone tới máy nhận.</summary>
        private sealed class InProcessApiClientFactory(Host host) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => host.CreateApiClient();
        }

        /// <summary>
        /// Cấp HttpClient gọi thẳng WebAPI trong tiến trình test nhưng làm mất phản hồi của vài lần gửi đầu, như khi WebAPI khởi động lại
        /// đúng lúc vừa lưu gói: yêu cầu vẫn vào controller nhận thật và gói đã lưu, chỉ có phản hồi không về tới bản gửi.
        /// </summary>
        private sealed class LostResponseApiClientFactory(Host host, int lostCount) : IHttpClientFactory
        {
            private int _sentCount;

            /// <summary>Chuyển yêu cầu vào WebAPI, tới lượt mất phản hồi thì bỏ phản hồi và báo máy chủ đóng kết nối ngang.</summary>
            private sealed class LostResponseHandler(LostResponseApiClientFactory owner) : DelegatingHandler
            {
                protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    if (!owner.TakeLostTurn())
                    {
                        return response;
                    }

                    response.Dispose();
                    var connectionClosed = new IOException("An existing connection was forcibly closed by the remote host.");
                    throw new HttpRequestException("An error occurred while sending the request.", connectionClosed);
                }
            }

            public HttpClient CreateClient(string name)
            {
                var handler = new LostResponseHandler(this);
                return host.CreateApiClient(handler);
            }

            /// <summary>Đếm lần gửi; trả true khi lần này còn nằm trong số lần bị mất phản hồi.</summary>
            private bool TakeLostTurn() => Interlocked.Increment(ref _sentCount) <= lostCount;
        }

        /// <summary>
        /// Cấp HttpClient gọi thẳng WebAPI trong tiến trình test; lần gửi đầu tới nơi và nhận phản hồi thành công xong thì dừng dịch vụ gửi
        /// (hủy token của lượt chạy), như khi tắt dịch vụ ngay sau lúc đối tác đã lưu gói, trước khi Worker kịp lưu số thứ tự và mốc.
        /// </summary>
        private sealed class StopAfterDeliveryApiClientFactory(Host host, CancellationTokenSource stopping) : IHttpClientFactory
        {
            private int _deliveredCount;

            /// <summary>Chuyển yêu cầu vào WebAPI, đọc hết phản hồi rồi mới dừng dịch vụ để phản hồi thành công vẫn về tới bản gửi.</summary>
            private sealed class StopAfterDeliveryHandler(StopAfterDeliveryApiClientFactory owner) : DelegatingHandler
            {
                protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    await response.Content.LoadIntoBufferAsync();
                    owner.StopAfterFirstDelivery();
                    return response;
                }
            }

            public HttpClient CreateClient(string name)
            {
                var handler = new StopAfterDeliveryHandler(this);
                return host.CreateApiClient(handler);
            }

            /// <summary>Lần tới nơi đầu tiên thì dừng dịch vụ gửi; các lần sau gửi bình thường.</summary>
            private void StopAfterFirstDelivery()
            {
                if (Interlocked.Increment(ref _deliveredCount) == 1)
                {
                    stopping.Cancel();
                }
            }
        }

        /// <summary>
        /// Cấp HttpClient gọi thẳng WebAPI trong tiến trình test; lần gửi đầu thì dừng dịch vụ gửi lúc Worker đang chờ đối tác trả lời:
        /// đối tác đã lưu gói (partnerSaved) hoặc yêu cầu chưa tới nơi, lệnh gửi bị hủy theo token dừng nên phản hồi không về tới bản gửi.
        /// </summary>
        private sealed class StopWhileAwaitingResponseApiClientFactory(Host host, CancellationTokenSource stopping, bool partnerSaved) : IHttpClientFactory
        {
            private int _sentCount;

            /// <summary>Lần gửi đầu: (tùy ca) chuyển yêu cầu vào WebAPI, dừng dịch vụ rồi chờ tới khi lệnh gửi bị hủy; các lần sau gửi bình thường.</summary>
            private sealed class StopWhileAwaitingResponseHandler(StopWhileAwaitingResponseApiClientFactory owner) : DelegatingHandler
            {
                protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    if (!owner.TakeFirstTurn())
                    {
                        return await base.SendAsync(request, cancellationToken);
                    }

                    if (owner.PartnerSaved)
                    {
                        using var delivered = await base.SendAsync(request, cancellationToken);
                        await delivered.Content.LoadIntoBufferAsync();
                    }

                    // Dừng dịch vụ lúc đang chờ phản hồi: lệnh gửi phải bị hủy theo token dừng, không hủy thì hết 30 giây báo lỗi để test không treo
                    owner.Stop();
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                    throw new TimeoutException("Lệnh gửi không bị hủy khi dịch vụ dừng.");
                }
            }

            /// <summary>Đối tác đã lưu gói trước khi dịch vụ dừng (true) hay yêu cầu chưa tới nơi (false).</summary>
            private bool PartnerSaved => partnerSaved;

            public HttpClient CreateClient(string name)
            {
                var handler = new StopWhileAwaitingResponseHandler(this);
                return host.CreateApiClient(handler);
            }

            /// <summary>Đếm lần gửi; trả true ở lần gửi đầu tiên.</summary>
            private bool TakeFirstTurn() => Interlocked.Increment(ref _sentCount) == 1;

            /// <summary>Dừng dịch vụ gửi (hủy token của lượt chạy).</summary>
            private void Stop() => stopping.Cancel();
        }

        /// <summary>
        /// Cấp HttpClient gọi thẳng WebAPI trong tiến trình test; lần gửi đầu tới nơi và nhận phản hồi thành công xong thì chạy việc chen vào
        /// trước khi Worker lưu số thứ tự và mốc (phiên khác khóa dòng mốc, lượt khác lấy khóa, tiến trình bị tắt cứng).
        /// </summary>
        private sealed class AfterDeliveryApiClientFactory(Host host, Func<Task> afterFirstDelivery) : IHttpClientFactory
        {
            private int _deliveredCount;

            /// <summary>Chuyển yêu cầu vào WebAPI, đọc hết phản hồi rồi mới chạy việc chen vào để phản hồi thành công vẫn về tới bản gửi.</summary>
            private sealed class AfterDeliveryHandler(AfterDeliveryApiClientFactory owner) : DelegatingHandler
            {
                protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    await response.Content.LoadIntoBufferAsync();
                    await owner.RunAfterFirstDelivery();
                    return response;
                }
            }

            public HttpClient CreateClient(string name)
            {
                var handler = new AfterDeliveryHandler(this);
                return host.CreateApiClient(handler);
            }

            /// <summary>Lần tới nơi đầu tiên thì chạy việc chen vào; các lần sau gửi bình thường.</summary>
            private Task RunAfterFirstDelivery() => Interlocked.Increment(ref _deliveredCount) == 1 ? afterFirstDelivery() : Task.CompletedTask;
        }

        /// <summary>Chờ câu lệnh đầu tiên bị phiên giữ khóa chặn rồi cắt kết nối của câu đó (KILL), như mất kết nối CSDL giữa lúc chờ khóa.</summary>
        private static async Task CutBlockedConnectionAsync(ISqlSugarClient adminSession, int holderSession)
        {
            var blockedSession = 0;
            for (var attempt = 0; blockedSession == 0 && attempt < 200; attempt++)
            {
                await Task.Delay(50);
                blockedSession = await adminSession.Ado.GetIntAsync(
                    "SELECT ISNULL(MAX(session_id), 0) FROM sys.dm_exec_requests WHERE blocking_session_id = @holderSession",
                    new { holderSession });
            }

            await adminSession.Ado.ExecuteCommandAsync($"KILL {blockedSession}");
        }

        /// <summary>Kết nối CSDL nhận, nơi WebAPI lưu gói và bản chỉ nhận ghi dữ liệu.</summary>
        private static ISqlSugarClient InboundDb(ISqlSugarClient db) => db.AsTenant().GetConnectionScope(ShareDataWorkerExtensions.InboundConnectionKey);

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác dùng chung CSDL như khi clone: đăng ký gửi kênh API trỏ về API nhận của hệ thống mình, bật gửi theo tín hiệu,
        ///              kèm đăng ký nhận và ánh xạ chiều nhận của cùng gói.
        /// Created date: 10/11/2026
        /// </summary>
        private static async Task<ReceiverPartner> SeedReceiverPartnerAsync(
            ISqlSugarClient db,
            ClonePacket packet,
            string name,
            string endpoint = ReceiveEndpoint,
            string? shapeJson = null)
        {
            var partnerCode = ClonePartnerCode(packet, name);
            var shapeText = shapeJson ?? ReceiverShapeJson;
            var shape = JsonNode.Parse(shapeText);
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
                    p.EndPointApiUrl = endpoint;
                },
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    s.SerialNbr = 1;
                }
            };
            var (partner, outbound) = await SeedOutboundSubscription(db, seed);

            var inbound = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"SUB_IN_{name}_{packet.Unique}",
                PartnerId = partner.ID,
                DatatypeId = packet.PacketId,
                Direction = BaseEnums.Direction.Inbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active
            };
            await db.Insertable(inbound).ExecuteCommandAsync();

            var inboundMapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = packet.PacketId,
                Direction = BaseEnums.Direction.Inbound,
                IsActive = true,
                TargetShapeJson = shapeText
            };
            await db.Insertable(inboundMapping).ExecuteCommandAsync();

            return new ReceiverPartner(name, partner, outbound, inbound);
        }

        /// <summary>Tạo các đối tác theo tên vai cho gói, mỗi đối tác ứng với một bản chỉ gửi.</summary>
        private static async Task<List<ReceiverPartner>> SeedReceiverPartnersAsync(ISqlSugarClient db, ClonePacket packet, params string[] names)
        {
            var partners = new List<ReceiverPartner>();
            foreach (var name in names)
            {
                var partner = await SeedReceiverPartnerAsync(db, packet, name);
                partners.Add(partner);
            }

            return partners;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu ghi chiều nhận của gói: mỗi dòng nhận được thành một dòng đánh dấu mang mã đối tác gửi, đặt Done để bản nhận không coi là gói chờ.
        ///              Câu cố ý dùng INSERT để bên nhận ghi trùng là thấy ngay dòng thừa.
        /// Created date: 10/11/2026
        /// </summary>
        private static async Task<string> SetMarkerWriteSqlAsync(ISqlSugarClient db, ClonePacket packet)
        {
            var markerCode = $"MARK_RCV_{packet.Unique}";
            var doneState = (int)BaseEnums.InboundProcessState.Done;
            var writeSql = $"""
                INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode, ProcessState, ErrorMessage)
                OUTPUT 'INSERT'
                SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), @partnerCode, '{markerCode}', {doneState}, r.rowCode
                FROM OPENJSON(@records) WITH (rowCode NVARCHAR(128) '$.rowCode') r
                """;
            await ShareDataSqlSeed.SetWriteAsync(db, packet.PacketId, writeSql);
            return markerCode;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Một bản Worker chỉ gửi: khai ShareData:SelfPartnerCode thì chỉ gửi đối tác đó, để null là Worker cũ chạy mọi đối tác;
        ///              gửi kênh API qua WebAPI trong tiến trình test nên gói vào đúng controller nhận thật.
        /// Created date: 10/11/2026
        /// </summary>
        private DataOutboundService CreateReceiverClone(IServiceScope scope, string? selfPartnerCode, IHttpClientFactory? apiClientFactory = null)
        {
            var settings = new Dictionary<string, string?>();
            if (selfPartnerCode != null)
            {
                settings["ShareData:SelfPartnerCode"] = selfPartnerCode;
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var services = scope.ServiceProvider;
            var clientFactory = apiClientFactory ?? new InProcessApiClientFactory(_host);
            var fileLogger = services.GetRequiredService<ILogger<FileDeliverySender>>();
            var httpSender = new HttpDeliverySender(clientFactory);
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
        /// Description: Chiều nhận của một dịch vụ khác chạy cùng lúc: bản DataInboundService riêng như một tiến trình riêng, không dùng chung với dịch vụ chỉ nhận.
        /// Created date: 10/11/2026
        /// </summary>
        private static DataInboundService CreateInboundService(IServiceScope scope)
        {
            var services = scope.ServiceProvider;
            var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
            var logger = services.GetRequiredService<ILogger<DataInboundService>>();
            var workerConfig = services.GetRequiredService<DataWorkerConfigService>();
            return new DataInboundService(scopeFactory, logger, workerConfig);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Một lượt vận hành: các bản gửi và bản nhận (khi đang chạy) chạy cùng lúc như các dịch vụ thật, xong lượt thì cho các đăng ký gửi đến hạn lại.
        /// Created date: 10/11/2026
        /// </summary>
        private static async Task RunRoundAsync(
            ISqlSugarClient db,
            IEnumerable<DataOutboundService> senders,
            DataInboundService? receiver,
            ClonePacket packet,
            bool byEvent,
            IEnumerable<ReceiverPartner> partners)
        {
            var runs = senders.Select(sender => RunCloneAsync(sender, packet, byEvent)).ToList();
            if (receiver != null)
            {
                var receive = receiver.ProcessPendingPackets(CancellationToken.None);
                runs.Add(receive);
            }

            await Task.WhenAll(runs);
            foreach (var partner in partners)
            {
                await MakeSubscriptionDue(db, partner.Outbound.ID);
            }
        }

        /// <summary>Mã các dòng bên nhận đã ghi cho đối tác, xếp tăng; dòng ghi hai lần xuất hiện hai lần.</summary>
        private static async Task<List<string>> WrittenRowsAsync(ISqlSugarClient inboundDb, string markerCode, ReceiverPartner partner)
        {
            var partnerCode = partner.Partner.Code;
            var rows = await inboundDb.Queryable<ShareDataInboundPacket>()
                .Where(p => p.PacketCode == markerCode && p.PartnerCode == partnerCode)
                .Select(p => p.ErrorMessage)
                .ToListAsync();
            return rows.Select(row => row ?? string.Empty).Order().ToList();
        }

        /// <summary>Các gói WebAPI đã nhận của đối tác với gói.</summary>
        private static Task<List<ShareDataInboundPacket>> ReceivedPacketsAsync(ISqlSugarClient inboundDb, ClonePacket packet, ReceiverPartner partner)
        {
            var partnerCode = partner.Partner.Code;
            return inboundDb.Queryable<ShareDataInboundPacket>()
                .Where(p => p.PacketCode == packet.PacketCode && p.PartnerCode == partnerCode)
                .ToListAsync();
        }

        /// <summary>Tổng số dòng bên nhận đã ghi theo câu ghi của gói.</summary>
        private static Task<int> CountWrittenAsync(ISqlSugarClient inboundDb, string markerCode) =>
            inboundDb.Queryable<ShareDataInboundPacket>().Where(p => p.PacketCode == markerCode).CountAsync();

        /// <summary>
        /// Author: DatHQ
        /// Description: Ca thực tế: 4 bản chỉ gửi và 1 bản chỉ nhận chạy cùng lúc. Cấu hình triển khai chỉ khai Role nên các bản gửi chia nhau mọi đối tác;
        ///              thêm biến thể mỗi bản khai SelfPartnerCode riêng, 3 và 5 bản gửi, đường gửi theo tín hiệu có dữ liệu mới:
        ///              mỗi đối tác được ghi đủ mọi dòng đúng một lần dưới mã của chính nó, mỗi trang là một gói nhận Done,
        ///              đăng ký nhận ghi tới số thứ tự gói lớn nhất, mốc gửi tới dòng cuối, khóa được nhả và không có cảnh báo.
        /// Created date: 10/11/2026
        /// </summary>
        [Theory]
        [InlineData(4, false, false)]
        [InlineData(4, true, false)]
        [InlineData(4, false, true)]
        [InlineData(4, true, true)]
        [InlineData(3, false, true)]
        [InlineData(5, false, true)]
        public async Task ProcessSubscriptions_WhenSendOnlyClonesAndOneReceiverRunTogether_EachPartnerRowsWrittenOnce_Test(int senderCount, bool byEvent, bool withSelfPartnerCode)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var names = Enumerable.Range(1, senderCount).Select(index => $"A{100 + index}").ToArray();
            var partners = await SeedReceiverPartnersAsync(db, packet, names);
            var senders = partners.Select(p => CreateReceiverClone(scope, withSelfPartnerCode ? p.Partner.Code : null)).ToList();
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act: 5 dòng, mỗi lượt 1 trang 2 dòng nên 3 lượt gửi hết, lượt thứ 4 không còn gì để gửi; bản nhận chạy thêm một lượt cuối
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, byEvent, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var partnerIds = partners.Select(p => p.Partner.ID).ToList();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => partnerIds.Contains(a.PartnerId)).ToListAsync();
            var totalWritten = await CountWrittenAsync(inboundDb, markerCode);
            foreach (var partner in partners)
            {
                var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
                var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
                var maxSerial = received.Max(r => r.SerialNbr);
                var inbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Inbound.ID);
                var outbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Outbound.ID);
                var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
                Assert.Equal(packet.RowCodes, rows);
                Assert.Equal(3, received.Count);
                Assert.All(received, r => Assert.Equal(BaseEnums.InboundProcessState.Done, r.ProcessState));
                Assert.Equal(maxSerial, inbound.SerialNbr);
                Assert.EndsWith(CursorEnd(packet, 5), cursor.LastKey);
                Assert.Null(outbound.ProcessingUntil);
            }

            Assert.Equal(senderCount * packet.RowCodes.Count, totalWritten);
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bản giao 6 dịch vụ chạy cùng lúc trên chung CSDL: 1 dịch vụ chạy cả hai chiều, 1 dịch vụ chỉ nhận và 4 dịch vụ chỉ gửi,
        ///              theo chu kỳ lẫn theo tín hiệu có dữ liệu mới. Năm dịch vụ cùng gửi, hai dịch vụ cùng nhận nhưng khóa đăng ký và khóa gói giữ cho
        ///              mỗi đối tác được ghi đủ mọi dòng đúng một lần, mỗi trang là một gói nhận Done, mốc gửi tới dòng cuối, khóa được nhả và không có cảnh báo.
        /// Created date: 10/11/2026
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProcessSubscriptions_WhenBothServiceRunsWithInboundAndOutboundClones_EachPartnerRowsWrittenOnce_Test(bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, FourNames);
            var senders = Enumerable.Range(0, 5).Select(_ => CreateReceiverClone(scope, null)).ToList();
            var inboundOnly = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var bothService = CreateInboundService(scope);

            // Act: 4 dịch vụ chỉ gửi và chiều gửi của dịch vụ hai chiều là 5 bản gửi; chiều nhận của dịch vụ hai chiều chạy cùng lúc với dịch vụ chỉ nhận
            for (var round = 0; round < 4; round++)
            {
                var bothReceive = bothService.ProcessPendingPackets(CancellationToken.None);
                await RunRoundAsync(db, senders, inboundOnly, packet, byEvent, partners);
                await bothReceive;
            }

            await inboundOnly.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var partnerIds = partners.Select(p => p.Partner.ID).ToList();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => partnerIds.Contains(a.PartnerId)).ToListAsync();
            var totalWritten = await CountWrittenAsync(inboundDb, markerCode);
            foreach (var partner in partners)
            {
                var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
                var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
                var outbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Outbound.ID);
                var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
                var serials = received.Select(r => r.SerialNbr).Distinct().Count();
                Assert.Equal(packet.RowCodes, rows);
                Assert.Equal(3, received.Count);
                Assert.Equal(3, serials);
                Assert.All(received, r => Assert.Equal(BaseEnums.InboundProcessState.Done, r.ProcessState));
                Assert.EndsWith(CursorEnd(packet, 5), cursor.LastKey);
                Assert.Null(outbound.ProcessingUntil);
            }

            Assert.Equal(FourNames.Length * packet.RowCodes.Count, totalWritten);
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bản chỉ nhận dừng trong lúc 4 bản gửi chạy: WebAPI vẫn nhận nên bản gửi vẫn tiến mốc, gói nằm chờ Pending, chưa ghi dòng nào;
        ///              bật lại bản nhận thì một lượt ghi đủ dữ liệu của cả 4 đối tác, mỗi dòng đúng một lần.
        /// Created date: 10/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessPendingPackets_WhenReceiverStartsAfterSendersFinished_WritesBacklogOnce_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, FourNames);
            var senders = partners.Select(p => CreateReceiverClone(scope, p.Partner.Code)).ToList();
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, null, packet, false, partners);
            }

            var pendingBeforeReceiver = await inboundDb.Queryable<ShareDataInboundPacket>()
                .Where(p => p.PacketCode == packet.PacketCode && p.ProcessState == BaseEnums.InboundProcessState.Pending)
                .CountAsync();
            var writtenBeforeReceiver = await CountWrittenAsync(inboundDb, markerCode);
            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            Assert.Equal(FourNames.Length * 3, pendingBeforeReceiver);
            Assert.Equal(0, writtenBeforeReceiver);
            foreach (var partner in partners)
            {
                var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
                var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
                var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
                Assert.Equal(packet.RowCodes, rows);
                Assert.All(received, r => Assert.Equal(BaseEnums.InboundProcessState.Done, r.ProcessState));
                Assert.EndsWith(CursorEnd(packet, 5), cursor.LastKey);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Một đối tác khai sai đường dẫn nhận (WebAPI trả 404): 3 đối tác còn lại vẫn được ghi đủ; đối tác sai có cảnh báo gửi lỗi và chưa có mốc;
        ///              sửa lại đường dẫn thì đối tác đó gửi bù đủ từ đầu, không đối tác nào bị ghi trùng, cảnh báo chỉ thuộc đối tác sai.
        /// Created date: 10/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenOnePartnerEndpointIsWrong_OthersUnaffectedAndItCatchesUpAfterFix_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, "A101", "A103", "A104");
            var wrongPartner = await SeedReceiverPartnerAsync(db, packet, "A102", $"{ReceiveEndpoint}-sai");
            partners.Add(wrongPartner);
            var wrongPartnerId = wrongPartner.Partner.ID;
            var wrongSubId = wrongPartner.Outbound.ID;
            var senders = partners.Select(p => CreateReceiverClone(scope, p.Partner.Code)).ToList();
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            var cursorWhileWrong = await GetCloneCursorAsync(db, packet, wrongPartner.Name);
            var cursorAdvancedWhileWrong = !string.IsNullOrEmpty(cursorWhileWrong?.LastKey);
            var alertsWhileWrong = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == wrongSubId).CountAsync();
            await db.Updateable<ShareDataPartner>()
                .SetColumns(p => p.EndPointApiUrl == ReceiveEndpoint)
                .Where(p => p.ID == wrongPartnerId)
                .ExecuteCommandAsync();
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var otherSubIds = partners.Where(p => p.Outbound.ID != wrongSubId).Select(p => p.Outbound.ID).ToList();
            var otherAlerts = await db.Queryable<ShareDataAlertLog>().Where(a => otherSubIds.Contains(a.SubscriptionId)).CountAsync();
            Assert.False(cursorAdvancedWhileWrong);
            Assert.NotEqual(0, alertsWhileWrong);
            Assert.Equal(0, otherAlerts);
            foreach (var partner in partners)
            {
                var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
                Assert.Equal(packet.RowCodes, rows);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Quên tắt Worker cũ chạy cả hai chiều (không khai SelfPartnerCode) khi đã bật 4 bản chỉ gửi: Worker cũ tranh gửi cùng lúc
        ///              nhưng khóa đăng ký giữ cho bên nhận chỉ ghi mỗi dòng đúng một lần, không đối tác nào thiếu dòng.
        /// Created date: 10/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenOldWorkerWithoutSelfPartnerCodeStillRuns_ReceiverWritesEachRowOnce_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, FourNames);
            var senders = partners.Select(p => CreateReceiverClone(scope, p.Partner.Code)).ToList();
            var oldWorker = CreateReceiverClone(scope, null);
            senders.Add(oldWorker);
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var totalWritten = await CountWrittenAsync(inboundDb, markerCode);
            foreach (var partner in partners)
            {
                var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
                var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
                Assert.Equal(packet.RowCodes, rows);
                Assert.Equal(3, received.Count);
            }

            Assert.Equal(FourNames.Length * packet.RowCodes.Count, totalWritten);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Một bản chỉ gửi khởi động lại sau lượt đầu (tiến trình mới, cấu hình đọc lại): gửi tiếp từ mốc,
        ///              bên nhận không có dòng nào bị ghi trùng hay thiếu.
        /// Created date: 10/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSendOnlyCloneRestarts_ReceiverHasNoDuplicateOrMissingRow_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            using var restartScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, FourNames);
            var senders = partners.Select(p => CreateReceiverClone(scope, p.Partner.Code)).ToList();
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act
            await RunRoundAsync(db, senders, receiver, packet, false, partners);
            senders[0] = CreateReceiverClone(restartScope, partners[0].Partner.Code);
            for (var round = 1; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            foreach (var partner in partners)
            {
                var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
                var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
                Assert.Equal(packet.RowCodes, rows);
                Assert.Equal(3, received.Count);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Gửi hết rồi nguồn có thêm dòng mới (dịch vụ chạy liên tục): lượt sau chỉ gửi đúng các dòng mới tới cả 4 đối tác,
        ///              không gửi lại dòng cũ; mỗi đối tác có thêm đúng một gói nhận và mốc tiến tới dòng mới cuối cùng.
        /// Created date: 10/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenNewRowsArriveAfterSync_SendsOnlyNewRowsToEveryPartner_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, FourNames);
            var senders = partners.Select(p => CreateReceiverClone(scope, p.Partner.Code)).ToList();
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var baseTime = new DateTime(2026, 10, 27, 9, 0, 0);

            // Act: gửi hết 5 dòng, rồi nguồn có thêm dòng R06, R07 sau mốc
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            foreach (var index in new[] { 6, 7 })
            {
                var name = $"R{index:00}";
                var rowTime = baseTime.AddMinutes(index);
                await SeedZoneRowAsync(db, packet.RowPrefix, packet.Unique, name, rowTime);
                packet.RowCodes.Add($"{packet.RowPrefix}{name}");
            }

            for (var round = 0; round < 2; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            foreach (var partner in partners)
            {
                var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
                var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
                var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
                Assert.Equal(packet.RowCodes, rows);
                Assert.Equal(4, received.Count);
                Assert.EndsWith(CursorEnd(packet, 7), cursor.LastKey);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Mỗi cặp đối tác gửi một gói khác nhau (A101, A102 gửi gói thứ nhất; A103, A104 gửi gói thứ hai), bên nhận chạy câu ghi riêng của từng gói:
        ///              dữ liệu gói nào ghi đúng chỗ gói đó, đúng mã đối tác, không lẫn sang gói kia.
        /// Created date: 10/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnersSendDifferentPackets_ReceiverWritesEachPacketSeparately_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packetOne = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var packetTwo = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var markerOne = await SetMarkerWriteSqlAsync(db, packetOne);
            var markerTwo = await SetMarkerWriteSqlAsync(db, packetTwo);
            var partnersOne = await SeedReceiverPartnersAsync(db, packetOne, "A101", "A102");
            var partnersTwo = await SeedReceiverPartnersAsync(db, packetTwo, "A103", "A104");
            var partners = partnersOne.Concat(partnersTwo).ToList();
            var senders = partners.Select(p => CreateReceiverClone(scope, p.Partner.Code)).ToList();
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packetOne, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            foreach (var partner in partnersOne)
            {
                var rowsOne = await WrittenRowsAsync(inboundDb, markerOne, partner);
                var rowsTwo = await WrittenRowsAsync(inboundDb, markerTwo, partner);
                Assert.Equal(packetOne.RowCodes, rowsOne);
                Assert.Empty(rowsTwo);
            }

            foreach (var partner in partnersTwo)
            {
                var rowsOne = await WrittenRowsAsync(inboundDb, markerOne, partner);
                var rowsTwo = await WrittenRowsAsync(inboundDb, markerTwo, partner);
                Assert.Equal(packetTwo.RowCodes, rowsTwo);
                Assert.Empty(rowsOne);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: WebAPI khởi động lại đúng lúc vừa lưu gói: gói số 1 đã lưu nhưng phản hồi không về tới bản gửi, trong lúc đó nguồn có thêm dòng mới.
        ///              Bản gửi không biết gói đã tới hay chưa nên coi số đó đã dùng, giữ nguyên mốc và lượt sau gửi lại trang với số kế tiếp, không gửi lại mãi một số bên nhận đã có:
        ///              mọi dòng đều được ghi, chỉ trang bị mất phản hồi ghi hai lần, mỗi số thứ tự chỉ có một gói nhận, mốc tới dòng cuối, khóa được nhả.
        /// Created date: 13/11/2026
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProcessSubscriptions_WhenReceiverSavedPacketButResponseWasLost_ResendsWithNextSerialAndWritesEveryRow_Test(bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, "A101");
            var partner = partners[0];
            var lostResponses = new LostResponseApiClientFactory(_host, lostCount: 1);
            var senders = new[] { CreateReceiverClone(scope, null, lostResponses) };
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var baseTime = new DateTime(2026, 10, 27, 9, 0, 0);
            var lostPage = packet.RowCodes.Take(2).ToList();

            // Act: lượt đầu WebAPI lưu gói số 1 (R01, R02) nhưng phản hồi bị mất; nguồn có thêm R04; chạy tiếp 4 lượt
            await RunRoundAsync(db, senders, receiver, packet, byEvent, partners);
            var newRowName = "R04";
            var newRowTime = baseTime.AddMinutes(4);
            await SeedZoneRowAsync(db, packet.RowPrefix, packet.Unique, newRowName, newRowTime);
            packet.RowCodes.Add($"{packet.RowPrefix}{newRowName}");
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, byEvent, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
            var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
            var serials = received.Select(r => r.SerialNbr).Order().ToList();
            var outbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Outbound.ID);
            var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
            var expectedRows = packet.RowCodes.Concat(lostPage).Order().ToList();
            Assert.Equal(expectedRows, rows);
            Assert.Equal(new int?[] { 1, 2, 3 }, serials);
            Assert.Equal(4, outbound.SerialNbr);
            Assert.EndsWith(CursorEnd(packet, 4), cursor.LastKey);
            Assert.Null(outbound.ProcessingUntil);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dịch vụ gửi bị dừng ngay sau khi đối tác đã lưu gói (hủy token trước khi Worker lưu số thứ tự và mốc): lượt đó vẫn lưu
        ///              xong số thứ tự và mốc, nên khi bật lại gửi tiếp trang sau với số kế tiếp, không gửi lại số cũ rồi bị từ chối mãi;
        ///              mọi dòng được ghi đúng một lần, khóa được nhả.
        /// Created date: 14/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenServiceStopsRightAfterPartnerSavedPacket_KeepsSerialAndCursorThenSendsNextPage_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, "A101");
            var partner = partners[0];
            using var stopping = new CancellationTokenSource();
            var stopAfterDelivery = new StopAfterDeliveryApiClientFactory(_host, stopping);
            var stoppedSender = CreateReceiverClone(scope, null, stopAfterDelivery);
            var senders = new[] { CreateReceiverClone(scope, null) };
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act: lượt đầu đối tác lưu gói số 1 (R01, R02) rồi dịch vụ gửi bị dừng; bật lại (bản mới) chạy tiếp 3 lượt.
            // Bản bị dừng chạy trong luồng riêng như một tiến trình riêng: token dừng không bám vào kết nối mà test dùng tiếp
            var stoppedRun = IsolatedFlow.Run(() => stoppedSender.ProcessSubscriptions(stopping.Token));
            var stopError = await Record.ExceptionAsync(() => stoppedRun);
            await MakeSubscriptionDue(db, partner.Outbound.ID);
            for (var round = 0; round < 3; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
            var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
            var serials = received.Select(r => r.SerialNbr).Order().ToList();
            var outbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Outbound.ID);
            Assert.True(stopError is null or OperationCanceledException, $"Dừng dịch vụ chỉ được hủy lượt chạy, nhận được: {stopError}");
            Assert.True(stopping.IsCancellationRequested);
            Assert.Equal(packet.RowCodes.Order().ToList(), rows);
            Assert.Equal(new int?[] { 1, 2 }, serials);
            Assert.Equal(3, outbound.SerialNbr);
            Assert.Null(outbound.ProcessingUntil);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tắt dịch vụ gửi đúng lúc đang chờ đối tác trả lời (lệnh gửi bị hủy theo token dừng), đối tác đã lưu gói hoặc yêu cầu chưa tới nơi:
        ///              Worker không biết nên coi như đối tác có thể đã nhận, vẫn tiêu số thứ tự, giữ nguyên mốc, nhả khóa dù dịch vụ đang dừng.
        ///              Bật lại thì gửi lại các dòng đó với số kế tiếp: không kẹt, không sót dòng; đối tác đã lưu gói thì nhận lại hai dòng đầu một lần nữa.
        /// Created date: 16/11/2026
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ProcessSubscriptions_WhenServiceStopsWhileAwaitingPartnerResponse_ConsumesSerialKeepsCursorThenResends_Test(bool partnerSaved)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, "A101");
            var partner = partners[0];
            using var stopping = new CancellationTokenSource();
            var stopWhileAwaiting = new StopWhileAwaitingResponseApiClientFactory(_host, stopping, partnerSaved);
            var stoppedSender = CreateReceiverClone(scope, null, stopWhileAwaiting);
            var senders = new[] { CreateReceiverClone(scope, null) };
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var firstPageRows = packet.RowCodes.Order().Take(2).ToList();
            var expectedRows = partnerSaved
                ? packet.RowCodes.Concat(firstPageRows).Order().ToList()
                : packet.RowCodes.Order().ToList();
            var expectedSerials = partnerSaved ? new int?[] { 1, 2, 3 } : new int?[] { 2, 3 };

            // Act: lượt đầu dừng dịch vụ lúc đang chờ đối tác trả lời gói số 1 (R01, R02); bật lại (bản mới) chạy tiếp 3 lượt
            var stoppedRun = IsolatedFlow.Run(() => stoppedSender.ProcessSubscriptions(stopping.Token));
            var stopError = await Record.ExceptionAsync(() => stoppedRun);
            var afterStop = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Outbound.ID);
            await MakeSubscriptionDue(db, partner.Outbound.ID);
            for (var round = 0; round < 3; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
            var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
            var serials = received.Select(r => r.SerialNbr).Order().ToList();
            var outbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Outbound.ID);
            Assert.True(stopError is null or OperationCanceledException, $"Dừng dịch vụ chỉ được hủy lượt chạy, nhận được: {stopError}");
            Assert.True(stopping.IsCancellationRequested);
            Assert.Equal(2, afterStop.SerialNbr);
            Assert.Null(afterStop.ProcessingUntil);
            Assert.Equal(expectedRows, rows);
            Assert.Equal(expectedSerials, serials);
            Assert.Equal(4, outbound.SerialNbr);
            Assert.Null(outbound.ProcessingUntil);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác đã lưu gói nhưng lượt gửi không lưu được số thứ tự và mốc: câu lưu đang chờ khóa dòng mốc thì mất kết nối CSDL,
        ///              hoặc lượt khác đã lấy khóa của đăng ký. Lượt đó vẫn tăng số thứ tự nên lượt sau gửi lại trang với số kế tiếp,
        ///              không gửi lại số đối tác đã lưu rồi bị từ chối mãi: mọi dòng đều được ghi, chỉ trang đó ghi hai lần, mốc tới dòng cuối, khóa được nhả.
        /// Created date: 15/11/2026
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProcessSubscriptions_WhenSavingSerialFailsAfterPartnerSavedPacket_ResendsWithNextSerialAndWritesEveryRow_Test(bool lockTakenOver)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, "A101");
            var partner = partners[0];
            var subId = partner.Outbound.ID;
            var partnerCode = partner.Partner.Code;
            var packetCode = packet.PacketCode;
            using var otherSession = db.CopyNew();
            using var adminSession = db.CopyNew();
            var connectionCut = Task.CompletedTask;
            var interfere = new AfterDeliveryApiClientFactory(_host, async () =>
            {
                if (lockTakenOver)
                {
                    // Lượt khác lấy khóa của đăng ký rồi chạy xong nhả khóa, trước khi lượt này lưu số thứ tự và mốc
                    await otherSession.Updateable<ShareDataSubscription>()
                        .SetColumns(s => s.ProcessingUntil == null)
                        .Where(s => s.ID == subId)
                        .ExecuteCommandAsync();
                    return;
                }

                // Phiên khác giữ khóa ghi dòng mốc; câu lưu số thứ tự và mốc của lượt này phải chờ thì bị cắt kết nối
                await otherSession.Ado.BeginTranAsync();
                await otherSession.Updateable<ShareDataLastSend>()
                    .SetColumns(c => c.UpdateTime == c.UpdateTime)
                    .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode && c.CursorKey == null)
                    .ExecuteCommandAsync();
                var holderSession = await otherSession.Ado.GetIntAsync("SELECT @@SPID");
                connectionCut = CutBlockedConnectionAsync(adminSession, holderSession);
            });
            var senders = new[] { CreateReceiverClone(scope, partnerCode, interfere) };
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var resentPage = packet.RowCodes.Take(2).ToList();

            // Act: lượt đầu đối tác lưu gói số 1 (R01, R02) nhưng lượt đó không lưu được số thứ tự và mốc; phiên khác xong việc rồi chạy tiếp 3 lượt
            await RunRoundAsync(db, senders, receiver, packet, false, partners);
            await connectionCut;
            await otherSession.Ado.RollbackTranAsync();
            for (var round = 0; round < 3; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
            var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
            var serials = received.Select(r => r.SerialNbr).Order().ToList();
            var outbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(subId);
            var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
            var expectedRows = packet.RowCodes.Concat(resentPage).Order().ToList();
            var expectedCursorEnd = CursorEnd(packet, 3);
            Assert.Equal(expectedRows, rows);
            Assert.Equal(new int?[] { 1, 2, 3 }, serials);
            Assert.Equal(4, outbound.SerialNbr);
            Assert.EndsWith(expectedCursorEnd, cursor.LastKey);
            Assert.Null(outbound.ProcessingUntil);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tiến trình gửi bị tắt cứng (End task, mất điện) ngay sau khi đối tác đã lưu gói: lượt đó không lưu số thứ tự, mốc và không nhả khóa.
        ///              Khóa hết hạn thì lượt chạy sau nhận khóa bị bỏ lại với số thứ tự kế tiếp, không gửi lại số đối tác đã lưu rồi bị từ chối mãi:
        ///              mọi dòng đều được ghi, chỉ trang đó ghi hai lần, mốc tới dòng cuối, khóa được nhả.
        /// Created date: 15/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSenderKilledRightAfterPartnerSavedPacket_NextRunUsesNextSerialAndWritesEveryRow_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var markerCode = await SetMarkerWriteSqlAsync(db, packet);
            var partners = await SeedReceiverPartnersAsync(db, packet, "A101");
            var partner = partners[0];
            var subId = partner.Outbound.ID;
            var partnerCode = partner.Partner.Code;
            var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var killed = new TaskCompletionSource();
            var killAfterDelivery = new AfterDeliveryApiClientFactory(_host, () =>
            {
                // Tiến trình bị tắt cứng: không chạy thêm lệnh nào, không lưu số thứ tự, mốc và không nhả khóa
                delivered.SetResult();
                return killed.Task;
            });
            var killedSender = CreateReceiverClone(scope, partnerCode, killAfterDelivery);
            var senders = new[] { CreateReceiverClone(scope, partnerCode) };
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var resentPage = packet.RowCodes.Take(2).ToList();

            // Act: lượt đầu đối tác lưu gói số 1 (R01, R02) rồi tiến trình gửi bị tắt cứng; khóa của lượt đó hết hạn thì bản chạy lại chạy 3 lượt.
            // Bản bị tắt chạy trong luồng riêng như một tiến trình riêng và không bao giờ chạy tiếp
            _ = IsolatedFlow.Run(() => killedSender.ProcessSubscriptions(CancellationToken.None));
            await delivered.Task;
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var expiredLock = dbNow.AddMinutes(-1);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.ProcessingUntil == expiredLock)
                .Where(s => s.ID == subId)
                .ExecuteCommandAsync();
            for (var round = 0; round < 3; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, partners);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var rows = await WrittenRowsAsync(inboundDb, markerCode, partner);
            var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
            var serials = received.Select(r => r.SerialNbr).Order().ToList();
            var outbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(subId);
            var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
            var expectedRows = packet.RowCodes.Concat(resentPage).Order().ToList();
            var expectedCursorEnd = CursorEnd(packet, 3);
            Assert.Equal(expectedRows, rows);
            Assert.Equal(new int?[] { 1, 2, 3 }, serials);
            Assert.Equal(4, outbound.SerialNbr);
            Assert.EndsWith(expectedCursorEnd, cursor.LastKey);
            Assert.Null(outbound.ProcessingUntil);
        }
    }
}
