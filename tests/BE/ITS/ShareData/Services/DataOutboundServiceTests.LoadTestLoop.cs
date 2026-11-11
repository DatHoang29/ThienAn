using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery.Base.Enums;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Vòng kiểm tải như bộ seed kiểm tải trên staging: đối tác gửi kênh WebAPI vào API nhận của chính hệ thống
    ///              (https, cổng 12204, đường dẫn chữ thường /api/sharedata/sharedatainbound/receive), hồ sơ gửi và nhận cùng một bộ khung
    ///              có header ($meta) và mảng data, nửa đối tác dùng câu mẫu chung, nửa có câu riêng biến tấu; 4 bản chỉ gửi và 1 bản chỉ nhận
    ///              chạy cùng lúc. Kiểm Worker gọi đúng URL, WebAPI lưu gói thô vào CSDL nhận, bản nhận ghi lại đúng giá trị từng dòng nguồn.
    ///              Yêu cầu đi qua WebAPI trong tiến trình test, không ra mạng.
    /// Created date: 11/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>Địa chỉ nhận dạng https:// kèm cổng riêng như seed kiểm tải (K1), trỏ về máy chạy test: đăng ký còn sót bị test khác quét bằng kết nối mạng thật không được gọi ra máy chủ thật.</summary>
        private const string LoadTestReceiverAddress = "https://127.0.0.1";

        /// <summary>Cổng WebAPI nhận của seed kiểm tải (K1); màn hình web là cổng 12205.</summary>
        private const int LoadTestReceiverPort = 12204;

        /// <summary>Đường dẫn API nhận viết chữ thường như seed kiểm tải (K1).</summary>
        private const string LoadTestReceiverPath = "/api/sharedata/sharedatainbound/receive";

        /// <summary>Đối tác của vòng kiểm tải: đối tác dùng chung kiểu với kịch bản clone, kèm cỡ trang câu SQL đối tác đang dùng.</summary>
        private sealed record LoadTestPartner(ReceiverPartner Receiver, int PageSize);

        /// <summary>Cấp HttpClient gọi WebAPI trong tiến trình test, ghi lại URL của từng yêu cầu Worker gửi đi.</summary>
        private sealed class RecordingApiClientFactory(Host host, ConcurrentQueue<Uri> requestUris) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                var recorder = new RequestUriRecorder(requestUris);
                return host.CreateApiClient(recorder);
            }
        }

        /// <summary>Ghi URL của yêu cầu rồi chuyển tiếp nguyên vẹn vào WebAPI.</summary>
        private sealed class RequestUriRecorder(ConcurrentQueue<Uri> requestUris) : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                requestUris.Enqueue(request.RequestUri!);
                return base.SendAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu nối đuôi trên TmsZone trả thêm tên và giờ cập nhật của dòng để bên nhận ghi lại được giá trị.
        /// Created date: 11/11/2026
        /// </summary>
        private static string LoadTestZoneSql()
        {
            var key = "CONVERT(varchar(23), COALESCE(z.UpdateTime, z.CreateTime), 121) + '_' + z.ID";
            return $"SELECT TOP (@pageSize) z.Code AS rowCode, z.Name AS rowName, z.UpdateTime AS rowTime, {key} AS __lastKey FROM TmsZone z WHERE z.Code LIKE @prefix AND {key} > @lastKey ORDER BY __lastKey ASC";
        }

        /// <summary>Bộ khung hai chiều như hồ sơ seed kiểm tải (K2): header là giá trị hệ thống, mảng data chứa các trường của câu SQL.</summary>
        private static string LoadTestShapeJson(string partnerCode, string packetCode) => $$"""
            {
              "header": {
                "dataTime": { "$meta": "Now" },
                "partnerCode": { "$meta": "PartnerCode", "$value": "{{partnerCode}}" },
                "packetCode": { "$meta": "PacketCode", "$value": "{{packetCode}}" },
                "serial": { "$meta": "Serial" }
              },
              "data": [
                {
                  "rowCode": { "$field": "rowCode" },
                  "rowName": { "$field": "rowName" },
                  "rowTime": { "$field": "rowTime" }
                }
              ]
            }
            """;

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu ghi chiều nhận của gói: mỗi dòng nhận được thành một dòng đánh dấu mang mã đối tác gửi và chuỗi
        ///              "mã|tên|giờ" đọc lại từ gói, đặt Done để bản nhận không coi là gói chờ. Dùng INSERT để ghi trùng là thấy dòng thừa.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task<string> SetValueMarkerWriteSqlAsync(ISqlSugarClient db, ClonePacket packet)
        {
            var markerCode = $"MARK_VAL_{packet.Unique}";
            var doneState = (int)BaseEnums.InboundProcessState.Done;
            var writeSql = $"""
                INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode, ProcessState, ErrorMessage)
                OUTPUT 'INSERT'
                SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), @partnerCode, '{markerCode}', {doneState},
                       CONCAT(r.rowCode, '|', r.rowName, '|', CONVERT(varchar(19), r.rowTime, 120))
                FROM OPENJSON(@records) WITH (rowCode NVARCHAR(128) '$.rowCode', rowName NVARCHAR(128) '$.rowName', rowTime DATETIME2 '$.rowTime') r
                """;
            await ShareDataSqlSeed.SetWriteAsync(db, packet.PacketId, writeSql);
            return markerCode;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác như seed kiểm tải: kênh WebAPI trỏ vào API nhận qua https và cổng riêng, đăng ký gửi bật gửi theo tín hiệu,
        ///              đăng ký nhận và hồ sơ hai chiều cùng bộ khung; số thứ tự từ partnerCount/2 + 1 có câu riêng và bộ biến riêng
        ///              theo kiểu biến tấu (số thứ tự % 5), kiểu 2 đổi cỡ trang thành commonPageSize + 1. configurePartner chỉnh thêm đối tác sau cấu hình mặc định.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task<List<LoadTestPartner>> SeedLoadTestPartnersAsync(
            ISqlSugarClient db,
            ClonePacket packet,
            int partnerCount,
            int commonPageSize,
            Action<ShareDataPartner>? configurePartner = null)
        {
            var partners = new List<LoadTestPartner>();
            for (var index = 1; index <= partnerCount; index++)
            {
                var name = $"L{index:000}";
                var partnerCode = ClonePartnerCode(packet, name);
                var shapeJson = LoadTestShapeJson(partnerCode, packet.PacketCode);
                var seed = new OutboundSubSeed
                {
                    PartnerCode = partnerCode,
                    SubCode = $"SUB_{name}_{packet.Unique}",
                    DatatypeId = packet.PacketId,
                    MappingShape = JsonNode.Parse(shapeJson),
                    ConfigurePartner = p =>
                    {
                        p.ConnectionType = ConnectionType.Api;
                        p.Address = LoadTestReceiverAddress;
                        p.Port = LoadTestReceiverPort;
                        p.EndPointApiUrl = LoadTestReceiverPath;
                        p.ConnectionConfig = """{"Format":"Json"}""";
                        configurePartner?.Invoke(p);
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
                    TargetShapeJson = shapeJson
                };
                await db.Insertable(inboundMapping).ExecuteCommandAsync();

                var pageSize = commonPageSize;
                if (index > partnerCount / 2)
                {
                    var variant = index % 5;
                    pageSize = variant == 2 ? commonPageSize + 1 : commonPageSize;
                    var commonSql = LoadTestZoneSql();
                    var ownSql = ManyPartnerOwnSql(commonSql, variant, partnerCode);
                    var variables = ZoneVariables(pageSize, packet.RowPrefix, null);
                    await ShareDataSqlSeed.SetPartnerAsync(db, packet.PacketId, partner.ID, ownSql);
                    await ShareDataSqlSeed.SetVariablesAsync(db, packet.PacketId, partner.ID, variables);
                }

                var receiver = new ReceiverPartner(name, partner, outbound, inbound);
                partners.Add(new LoadTestPartner(receiver, pageSize));
            }

            return partners;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Vòng kiểm tải theo seed: nhiều đối tác trộn câu mẫu chung và câu riêng gửi kênh WebAPI vào API nhận qua https cổng 12204,
        ///              4 bản chỉ gửi và 1 bản chỉ nhận chạy cùng lúc theo chu kỳ lẫn theo tín hiệu. Mọi yêu cầu đi đúng
        ///              https://10.10.12.80:12204/api/sharedata/sharedatainbound/receive; WebAPI lưu mỗi trang thành một gói thô của đúng đối tác,
        ///              số thứ tự không trùng, gói nào cũng Done; bản nhận ghi lại đủ mọi dòng nguồn đúng một lần, đúng mã, tên và giờ;
        ///              đăng ký nhận ghi tới số thứ tự gói lớn nhất, mốc gửi tới dòng cuối, khóa được nhả, không có cảnh báo.
        /// Created date: 11/11/2026
        /// </summary>
        [Theory]
        [InlineData(10, false)]
        [InlineData(10, true)]
        [InlineData(50, false)]
        public async Task ProcessSubscriptions_WhenLoadTestPartnersSendToReceiveApi_ReceiverWritesEveryRowOnceWithSameValues_Test(int partnerCount, bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var commonSql = LoadTestZoneSql();
            await ShareDataSqlSeed.SetCommonAsync(db, packet.PacketId, commonSql);
            var markerCode = await SetValueMarkerWriteSqlAsync(db, packet);
            var partners = await SeedLoadTestPartnersAsync(db, packet, partnerCount, commonPageSize: 2);
            var receivers = partners.Select(p => p.Receiver).ToList();
            var requestUris = new ConcurrentQueue<Uri>();
            var clientFactory = new RecordingApiClientFactory(_host, requestUris);
            var senders = Enumerable.Range(0, ManyPartnerSenderCount).Select(_ => CreateReceiverClone(scope, null, clientFactory)).ToList();
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var rowPattern = $"{packet.RowPrefix}%";
            var expectedValues = await db.Ado.SqlQueryAsync<string>(
                "SELECT CONCAT(Code, '|', Name, '|', CONVERT(varchar(19), UpdateTime, 120)) FROM TmsZone WHERE Code LIKE @rowPattern ORDER BY Code",
                new { rowPattern });
            var expectedUri = new Uri($"{LoadTestReceiverAddress}:{LoadTestReceiverPort}{LoadTestReceiverPath}");
            var lastRowCursorEnd = CursorEnd(packet, 5);

            // Act: cỡ trang 2 cần 3 lượt, cỡ trang 3 cần 2 lượt; lượt thứ 4 không còn gì để gửi, bản nhận chạy thêm một lượt cuối
            for (var round = 0; round < 4; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, byEvent, receivers);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var expectedRequests = 0;
            foreach (var partner in partners)
            {
                var pages = (packet.RowCodes.Count + partner.PageSize - 1) / partner.PageSize;
                var writtenValues = await WrittenRowsAsync(inboundDb, markerCode, partner.Receiver);
                var received = await ReceivedPacketsAsync(inboundDb, packet, partner.Receiver);
                var serials = received.Select(r => r.SerialNbr).Distinct().ToList();
                var maxSerial = received.Max(r => r.SerialNbr);
                var inbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Receiver.Inbound.ID);
                var outbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Receiver.Outbound.ID);
                var cursor = await GetCloneCursorAsync(db, packet, partner.Receiver.Name);
                expectedRequests += pages;

                Assert.Equal(expectedValues, writtenValues);
                Assert.Equal(pages, received.Count);
                Assert.Equal(received.Count, serials.Count);
                Assert.All(received, r => Assert.Equal(BaseEnums.InboundProcessState.Done, r.ProcessState));
                Assert.Equal(maxSerial, inbound.SerialNbr);
                Assert.EndsWith(lastRowCursorEnd, cursor.LastKey);
                Assert.Null(outbound.ProcessingUntil);
            }

            var partnerIds = partners.Select(p => p.Receiver.Partner.ID).ToList();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => partnerIds.Contains(a.PartnerId)).ToListAsync();
            Assert.Equal(expectedRequests, requestUris.Count);
            Assert.All(requestUris, uri => Assert.Equal(expectedUri, uri));
            Assert.Empty(alerts);
        }
    }
}
