using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery.Base.Enums;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using Tests.ShareData.Common;
using Tests.ShareData.Mocks;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Cấu hình kiểm tải như trên staging (nhiều đối tác, câu mẫu chung trộn câu riêng, kênh WebAPI qua https kèm cổng, BypassSsl)
    ///              tự seed trong bài test và chạy qua HTTPS thật: cổng nhận dùng chứng chỉ cấp cho tên miền, Worker gọi bằng IP như staging;
    ///              4 bản chỉ gửi, 1 bản hai chiều và 1 bản chỉ nhận chạy cùng lúc. Kiểm cả dữ liệu lẫn nhật ký để truy được khi có lỗi.
    /// Created date: 11/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>Đường dẫn API nhận của hệ thống mà đối tác kiểm tải gọi vào.</summary>
        private const string LoadTestEndpoint = "/api/sharedata/sharedatainbound/receive";

        /// <summary>Cấu hình kết nối của đối tác kiểm tải: gửi JSON, bỏ kiểm chứng chỉ vì staging gọi bằng IP.</summary>
        private const string LoadTestConnectionConfig = """{"Format":"Json","BypassSsl":"true"}""";

        /// <summary>Chu kỳ chạy (giây) của đăng ký kiểm tải.</summary>
        private const int LoadTestIntervalSeconds = 30;

        /// <summary>Lịch chạy liên tục theo chu kỳ, không giới hạn giờ, ngày.</summary>
        private const string LoadTestScheduleJson =
            """{"Kind":"continuous","IntervalSeconds":30,"StartTime":null,"EndTime":null,"DaysOfWeek":null,"StartDate":null,"EndDate":null,"DurationMinutes":null}""";

        /// <summary>Đánh dấu câu riêng của đối tác kiểm tải để phân biệt với câu mẫu chung.</summary>
        private const string LoadTestOwnSqlMarker = "câu riêng kiểm tải";

        /// <summary>Số lượt vận hành của bài test bộ seed: đủ cho gói nhiều trang nhất (5 dòng, mỗi lượt 2 dòng) gửi hết và còn một lượt trống.</summary>
        private const int LoadTestSeedRounds = 4;

        /// <summary>Số gói nối đuôi TOP 100 viết thẳng như trên staging (103, 104, 106, 107, 109).</summary>
        private const int LoadTestLiteralTopPacketCount = 5;

        /// <summary>Số gói gửi toàn bộ như trên staging (102, 108).</summary>
        private const int LoadTestSnapshotPacketCount = 2;

        /// <summary>Công thức cột mốc của các câu kiểu catalog trên TmsZone.</summary>
        private const string LoadTestCursorKey = "CONVERT(varchar(23), COALESCE(z.UpdateTime, z.CreateTime), 121) + '_' + z.ID";

        /// <summary>Đối tác của bộ seed: mỗi gói (theo PacketId) có một đăng ký gửi và một đăng ký nhận, kèm ID mọi hồ sơ ánh xạ.</summary>
        private sealed record LoadTestSeedPartner(
            ShareDataPartner Partner,
            Dictionary<string, ShareDataSubscription> Outbound,
            Dictionary<string, ShareDataSubscription> Inbound,
            List<string> MappingIds);

        /// <summary>Gói của bộ seed: gói nối đuôi hay gửi toàn bộ, kèm mã dòng đánh dấu của câu ghi chiều nhận.</summary>
        private sealed record LoadTestSeedPacket(ClonePacket Packet, bool IsSnapshot, string MarkerCode);

        /// <summary>Kết nối mạng thật cho kênh WebAPI khi không bật BypassSsl: kiểm chứng chỉ máy chủ theo mặc định của hệ điều hành.</summary>
        private sealed class NetworkHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new();
        }

        /// <summary>Câu nối đuôi kiểu gói 101: TOP (@pageSize), cột mốc đứng đầu, xuống dòng CRLF như câu mẫu chung trên staging.</summary>
        private static string LoadTestPageSizeSql() => string.Join("\r\n",
            "SELECT TOP (@pageSize)",
            $"    {LoadTestCursorKey} AS __lastKey,",
            "    z.Code AS rowCode,",
            "    z.Name AS rowName,",
            "    z.UpdateTime AS rowTime",
            "FROM TmsZone z",
            "WHERE z.Code LIKE @prefix",
            $"  AND {LoadTestCursorKey} > @lastKey",
            "ORDER BY __lastKey ASC");

        /// <summary>Câu nối đuôi kiểu gói 103/104/106/107/109: TOP 100 viết thẳng, cột mốc sau các trường, thêm hai cột __audit.</summary>
        private static string LoadTestLiteralTopSql() => string.Join("\r\n",
            "SELECT TOP 100",
            "    z.Code AS rowCode,",
            "    z.Name AS rowName,",
            "    z.UpdateTime AS rowTime,",
            $"    {LoadTestCursorKey} AS __lastKey,",
            "    z.UpdateTime AS __auditUpdate,",
            "    z.CreateTime AS __auditCreate",
            "FROM TmsZone z",
            "WHERE z.Code LIKE @prefix",
            $"  AND {LoadTestCursorKey} > @lastKey",
            "ORDER BY __lastKey ASC");

        /// <summary>Câu gửi toàn bộ kiểu gói 102/108: không cột mốc, không ORDER BY, mỗi lượt gửi lại mọi dòng.</summary>
        private static string LoadTestSnapshotSql() => string.Join("\r\n",
            "SELECT",
            "    z.Code AS rowCode,",
            "    z.Name AS rowName,",
            "    z.UpdateTime AS rowTime",
            "FROM TmsZone z",
            "WHERE z.Code LIKE @prefix");

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu riêng của đối tác thứ index, biến tấu từ câu mẫu chung theo index % 5 để câu dài ngắn khác nhau:
        ///              kiểu 1 và 3 thêm cột phụ ngay sau dòng SELECT (kiểu 3 thêm cả cột đệm dài), kiểu 2 đổi TOP 100 thành TOP 20 (gửi nhiều trang),
        ///              kiểu 4 đổi thành TOP 500; kiểu 2 và 4 xuống dòng LF; câu nào cũng có chú thích cuối dài theo kiểu.
        /// Created date: 11/11/2026
        /// </summary>
        private static string LoadTestOwnSql(string commonSql, int index, string partnerCode)
        {
            var variant = index % 5;
            var afterSelectLine = commonSql.IndexOf("\r\n", StringComparison.Ordinal) + 2;
            var extraColumns = variant switch
            {
                1 => $"    N'{partnerCode}' AS ktPartner,\r\n",
                3 => $"    N'{partnerCode}' AS ktPartner, {index} AS ktNo, GETDATE() AS ktReadAt, REPLICATE(N'x', {100 + index * 7}) AS ktPad,\r\n",
                _ => string.Empty
            };
            var withColumns = commonSql.Insert(afterSelectLine, extraColumns);
            var resized = variant switch
            {
                2 => withColumns.Replace("SELECT TOP 100", "SELECT TOP 20"),
                4 => withColumns.Replace("SELECT TOP 100", "SELECT TOP 500"),
                _ => withColumns
            };
            var formatted = variant is 2 or 4 ? resized.Replace("\r\n", "\n") : resized;
            var padding = new string('-', variant * 40);
            return $"{formatted}\n/* {partnerCode} - {LoadTestOwnSqlMarker} {padding} */";
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo gói kiểu catalog trên các dòng TmsZone riêng của gói: đặt câu mẫu chung (CRLF như staging), bộ biến như câu mẫu
        ///              trên staging (gói TOP 100 vẫn khai @pageSize không dùng), câu ghi chiều nhận ghi dòng đánh dấu.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task<LoadTestSeedPacket> SeedLoadTestCatalogPacketAsync(ISqlSugarClient db, int rowCount, int pageSize, string commonSql, bool isSnapshot)
        {
            var packet = await SeedClonePacketAsync(db, rowCount, pageSize);
            await ShareDataSqlSeed.SetCommonAsync(db, packet.PacketId, commonSql);
            if (isSnapshot)
            {
                var prefixOnly = new List<SqlVariableItem>
                {
                    new() { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{packet.RowPrefix}%", OrderNo = 1 }
                };
                await ShareDataSqlSeed.SetVariablesAsync(db, packet.PacketId, null, prefixOnly);
            }

            var markerCode = await SetValueMarkerWriteSqlAsync(db, packet);
            return new LoadTestSeedPacket(packet, isSnapshot, markerCode);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác kiểm tải mã {prefix}001.., kênh WebAPI trỏ vào cổng HTTPS với cấu hình BypassSsl; với mỗi gói một hồ sơ gửi, một hồ sơ nhận
        ///              cùng bộ khung, một đăng ký gửi và một đăng ký nhận đang chạy theo chu kỳ 30 giây; SerialNbr gán như màn hình: số lớn nhất của
        ///              đối tác cộng thứ tự, đăng ký gửi trước, đăng ký nhận sau, theo mã gói. Đối tác từ số ownSqlFrom có câu riêng biến tấu
        ///              từ câu mẫu chung, bộ biến chép từ câu mẫu chung.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task<List<LoadTestSeedPartner>> SeedLoadTestSeedPartnersAsync(
            ISqlSugarClient db,
            string prefix,
            int partnerCount,
            IReadOnlyList<LoadTestSeedPacket> packets,
            ShareDataHttpsReceiverMock receiverPort,
            int ownSqlFrom)
        {
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var dueTime = dbNow.AddSeconds(-10);
            var orderedPackets = packets.OrderBy(p => p.Packet.PacketCode, StringComparer.Ordinal).ToList();
            var partners = new List<LoadTestSeedPartner>();

            for (var index = 1; index <= partnerCount; index++)
            {
                var code = $"{prefix}{index:000}";
                var partner = new ShareDataPartner
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = code,
                    Name = $"Kiểm tải {index:000}",
                    Address = ShareDataHttpsReceiverMock.Address,
                    Port = receiverPort.Port,
                    EndPointApiUrl = LoadTestEndpoint,
                    ConnectionType = ConnectionType.Api,
                    ConnectionConfig = LoadTestConnectionConfig,
                    Status = BaseEnums.StatusEnum.Enable,
                    SessionState = BaseEnums.SessionState.Connected
                };
                await db.Insertable(partner).ExecuteCommandAsync();

                var serial = 0;
                var outbound = new Dictionary<string, ShareDataSubscription>();
                var inbound = new Dictionary<string, ShareDataSubscription>();
                var mappingIds = new List<string>();
                foreach (var direction in new[] { BaseEnums.Direction.Outbound, BaseEnums.Direction.Inbound })
                {
                    foreach (var seedPacket in orderedPackets)
                    {
                        serial++;
                        var packetId = seedPacket.Packet.PacketId;
                        var subscription = new ShareDataSubscription
                        {
                            ID = Guid.NewGuid().ToString("N"),
                            PartnerId = partner.ID,
                            Direction = direction,
                            DatatypeId = packetId,
                            Mode = BaseEnums.SubMode.Periodic,
                            Format = BaseEnums.PublishFormat.Data,
                            ScheduleJson = LoadTestScheduleJson,
                            Priority = 5,
                            State = BaseEnums.SubSubscriptionState.Active,
                            RequestedAt = dbNow,
                            ResolvedAt = dbNow,
                            SendOnNewData = direction == BaseEnums.Direction.Outbound ? false : null,
                            NextTimeRun = direction == BaseEnums.Direction.Outbound ? dueTime : null,
                            IntervalSeconds = LoadTestIntervalSeconds,
                            SerialNbr = serial
                        };
                        await db.Insertable(subscription).ExecuteCommandAsync();

                        var shapeJson = LoadTestShapeJson(code, seedPacket.Packet.PacketCode);
                        var mapping = new ShareDataMapping
                        {
                            ID = Guid.NewGuid().ToString("N"),
                            PartnerId = partner.ID,
                            DatatypeId = packetId,
                            Direction = direction,
                            IsActive = true,
                            TargetShapeJson = shapeJson
                        };
                        await db.Insertable(mapping).ExecuteCommandAsync();
                        mappingIds.Add(mapping.ID);

                        var target = direction == BaseEnums.Direction.Outbound ? outbound : inbound;
                        target[packetId] = subscription;
                    }
                }

                if (index >= ownSqlFrom)
                {
                    foreach (var seedPacket in orderedPackets)
                    {
                        var packetId = seedPacket.Packet.PacketId;
                        var common = await ShareDataSqlSeed.GetCommonAsync(db, packetId);
                        var ownSql = LoadTestOwnSql(common!.ExtractSql!, index, code);
                        var variables = await ShareDataSqlSeed.GetVariablesAsync(db, packetId, null);
                        await ShareDataSqlSeed.SetPartnerAsync(db, packetId, partner.ID, ownSql);
                        await ShareDataSqlSeed.SetVariablesAsync(db, packetId, partner.ID, variables);
                    }
                }

                partners.Add(new LoadTestSeedPartner(partner, outbound, inbound, mappingIds));
            }

            return partners;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Xóa đối tác kiểm tải như người vận hành thao tác trên màn hình: tạm dừng rồi xóa từng đăng ký, xóa hồ sơ ánh xạ, cuối cùng xóa đối tác.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task DeleteLoadTestSeedPartnersViaApiAsync(IServiceProvider services, IEnumerable<LoadTestSeedPartner> partners)
        {
            foreach (var seedPartner in partners)
            {
                var subscriptions = seedPartner.Outbound.Values.Concat(seedPartner.Inbound.Values).ToList();
                foreach (var subscription in subscriptions)
                {
                    await services.CallApiAsync(new ShareDataPauseSubscriptionInput { ID = subscription.ID });
                    await services.CallApiAsync(new ShareDataDeleteSubscriptionInput { ID = subscription.ID });
                }

                foreach (var mappingId in seedPartner.MappingIds)
                {
                    await services.CallApiAsync(new ShareDataDeleteMappingInput { ID = mappingId });
                }

                await services.CallApiAsync(new ShareDataDeletePartnerInput { ID = seedPartner.Partner.ID });
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Một lượt vận hành: mọi bản gửi và mọi bản nhận chạy cùng lúc như các dịch vụ thật.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task RunLoadTestSeedRoundAsync(IEnumerable<DataOutboundService> senders, IEnumerable<DataInboundService> receivers)
        {
            var sending = senders.Select(sender => sender.ProcessSubscriptions(CancellationToken.None));
            var receiving = receivers.Select(receiver => receiver.ProcessPendingPackets(CancellationToken.None));
            var runs = sending.Concat(receiving).ToList();
            await Task.WhenAll(runs);
        }

        /// <summary>Giá trị "mã|tên|giờ" của mọi dòng nguồn của gói, xếp theo mã, đúng định dạng câu ghi dòng đánh dấu.</summary>
        private static Task<List<string>> ReadLoadTestSourceValuesAsync(ISqlSugarClient db, ClonePacket packet)
        {
            var rowPattern = $"{packet.RowPrefix}%";
            return db.Ado.SqlQueryAsync<string>(
                "SELECT CONCAT(Code, '|', Name, '|', CONVERT(varchar(19), UpdateTime, 120)) FROM TmsZone WHERE Code LIKE @rowPattern ORDER BY Code",
                new { rowPattern });
        }

        /// <summary>Giá trị bên nhận đã ghi cho đối tác theo câu ghi của gói, xếp tăng; dòng ghi nhiều lần xuất hiện nhiều lần.</summary>
        private static async Task<List<string>> ReadLoadTestWrittenValuesAsync(ISqlSugarClient inboundDb, string markerCode, string partnerCode)
        {
            var rows = await inboundDb.Queryable<ShareDataInboundPacket>()
                .Where(p => p.PacketCode == markerCode && p.PartnerCode == partnerCode)
                .Select(p => p.ErrorMessage)
                .ToListAsync();
            return rows.Select(row => row ?? string.Empty).Order(StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cổng nhận dùng chứng chỉ cấp cho tên miền, Worker gọi bằng IP như staging nhưng đối tác không bật BypassSsl:
        ///              không gói nào tới được cổng nhận; mỗi lượt để lại một cảnh báo HttpSendFailed nêu lỗi chứng chỉ SSL/TLS và đúng URL
        ///              (cả ở câu cảnh báo lẫn DetailJson), nhật ký lượt gửi có dòng cha và bước 3 thất bại cùng câu lỗi đó, mốc không tiến,
        ///              số thứ tự gói giữ nguyên, khóa được nhả — người vận hành truy được nguyên nhân chỉ từ nhật ký.
        /// Created date: 11/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenReceiverCertificateDoesNotMatchAddressAndBypassSslOff_SendsNothingAndLogsCertificateErrorWithUrl_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            await using var receiverPort = await ShareDataHttpsReceiverMock.StartAsync(() => _host.CreateApiClient());
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            var commonSql = LoadTestZoneSql();
            await ShareDataSqlSeed.SetCommonAsync(db, packet.PacketId, commonSql);
            await SetValueMarkerWriteSqlAsync(db, packet);
            var partners = await SeedLoadTestPartnersAsync(db, packet, partnerCount: 1, commonPageSize: 2, configurePartner: p =>
            {
                p.Address = ShareDataHttpsReceiverMock.Address;
                p.Port = receiverPort.Port;
                p.EndPointApiUrl = LoadTestEndpoint;
            });
            var receivers = partners.Select(p => p.Receiver).ToList();
            var partner = partners[0].Receiver;
            var networkFactory = new NetworkHttpClientFactory();
            var senders = Enumerable.Range(0, ManyPartnerSenderCount).Select(_ => CreateReceiverClone(scope, null, networkFactory)).ToList();
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var expectedUrl = $"{ShareDataHttpsReceiverMock.Address}:{receiverPort.Port}{LoadTestEndpoint}";
            const int rounds = 2;

            // Act
            for (var round = 0; round < rounds; round++)
            {
                await RunRoundAsync(db, senders, receiver, packet, false, receivers);
            }

            // Assert
            var outboundId = partner.Outbound.ID;
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == outboundId).ToListAsync();
            var logs = await GetLogs(db, outboundId);
            var parents = logs.Where(l => l.ParentId == null).ToList();
            var sendSteps = logs.Where(l => l.StepNbr == 3).ToList();
            var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
            var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
            var lastKey = cursor?.LastKey ?? string.Empty;
            var savedOutbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(outboundId);

            Assert.Empty(receiverPort.Requests);
            Assert.Empty(received);
            Assert.Equal(rounds, alerts.Count);
            Assert.All(alerts, a => Assert.Equal(ShareDataAlertCode.Outbound.HttpSendFailed, a.AlertCode));
            Assert.All(alerts, a => Assert.Contains("chứng chỉ SSL/TLS", a.Message));
            Assert.All(alerts, a => Assert.Contains(expectedUrl, a.Message));
            Assert.All(alerts, a => Assert.Contains(expectedUrl, a.DetailJson));
            Assert.Equal(rounds, parents.Count);
            Assert.All(parents, l => Assert.Equal(BaseEnums.SuccessEnums.Fail, l.Success));
            Assert.All(parents, l => Assert.Contains("chứng chỉ SSL/TLS", l.ErrorMessage));
            Assert.Equal(rounds, sendSteps.Count);
            Assert.All(sendSteps, l => Assert.Equal(BaseEnums.SuccessEnums.Fail, l.Success));
            Assert.All(sendSteps, l => Assert.Contains(expectedUrl, l.ErrorMessage));
            Assert.DoesNotContain(packet.Unique, lastKey);
            Assert.Equal(1, savedOutbound.SerialNbr);
            Assert.Null(savedOutbound.ProcessingUntil);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cấu hình kiểm tải như trên staging chạy trên CSDL test: đối tác kênh WebAPI (https kèm cổng, BypassSsl) gửi vào cổng HTTPS có chứng chỉ
        ///              khác tên rồi tới controller nhận thật; mỗi đối tác giữ 8 gói kiểu catalog như 8 gói đang chạy trên staging (101–104, 106–109):
        ///              1 gói nối đuôi @pageSize (101), 5 gói nối đuôi TOP 100 viết thẳng (103, 104, 106, 107, 109), 2 gói gửi toàn bộ (102, 108);
        ///              lịch chu kỳ 30 giây, SerialNbr gán như màn hình; nửa sau số đối tác có câu riêng biến tấu dài ngắn khác nhau.
        ///              Ca 52 đối tác đúng quy mô seed kiểm tải: 52 đối tác × 8 gói × 2 chiều = 832 đăng ký.
        ///              4 bản chỉ gửi, 1 bản hai chiều, 1 bản chỉ nhận chạy cùng lúc. Bên nhận ghi đủ mọi dòng đúng một lần (gói gửi toàn bộ: mỗi lượt một lần),
        ///              mỗi gói nhận Done, gói đầu mang đúng SerialNbr đã gán, đăng ký nhận chỉ tăng số (giữ số lúc tạo nếu lớn hơn); mốc tới dòng cuối, khóa được nhả,
        ///              lần chạy kế tiếp hẹn sau đúng chu kỳ; không cảnh báo. Nhật ký nối được hai đầu: mỗi trang gửi có dòng cha Success mang
        ///              SerialNbr trùng gói bên nhận, mỗi gói nhận có dòng cha (ReceiveLogId) và bước 2 Success.
        /// Created date: 11/11/2026
        /// Modified date: 12/11/2026
        /// </summary>
        [Theory]
        [InlineData(10)]
        [InlineData(52)]
        public async Task ProcessSubscriptions_WhenLoadTestSeedRunsOverHttpsWithBypassSsl_EveryPartnerPacketReceivedOnceAndTraceable_Test(int partnerCount)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            await using var receiverPort = await ShareDataHttpsReceiverMock.StartAsync(() => _host.CreateApiClient());
            var pageSizeSql = LoadTestPageSizeSql();
            var literalTopSql = LoadTestLiteralTopSql();
            var snapshotSql = LoadTestSnapshotSql();
            var pagePacket = await SeedLoadTestCatalogPacketAsync(db, rowCount: 5, pageSize: 2, pageSizeSql, isSnapshot: false);
            var packets = new List<LoadTestSeedPacket> { pagePacket };
            for (var i = 0; i < LoadTestLiteralTopPacketCount; i++)
            {
                var topPacket = await SeedLoadTestCatalogPacketAsync(db, rowCount: 25, pageSize: 100, literalTopSql, isSnapshot: false);
                packets.Add(topPacket);
            }

            for (var i = 0; i < LoadTestSnapshotPacketCount; i++)
            {
                var snapshotPacket = await SeedLoadTestCatalogPacketAsync(db, rowCount: 3, pageSize: 1, snapshotSql, isSnapshot: true);
                packets.Add(snapshotPacket);
            }

            var prefix = $"Q{Guid.NewGuid():N}"[..7];
            var firstOwnSqlPartner = partnerCount / 2 + 1;
            var partners = await SeedLoadTestSeedPartnersAsync(db, prefix, partnerCount, packets, receiverPort, firstOwnSqlPartner);
            var networkFactory = new NetworkHttpClientFactory();
            var senders = Enumerable.Range(0, ManyPartnerSenderCount + 1).Select(_ => CreateReceiverClone(scope, null, networkFactory)).ToList();
            var receiveOnly = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var bothServiceReceiver = CreateInboundService(scope);
            var receivers = new List<DataInboundService> { receiveOnly, bothServiceReceiver };
            var outboundIds = partners.SelectMany(p => p.Outbound.Values).Select(s => s.ID).ToList();
            var packetIds = packets.Select(p => p.Packet.PacketId).ToList();
            var partnerIds = partners.Select(p => p.Partner.ID).ToList();
            var partnerCodes = partners.Select(p => p.Partner.Code).ToHashSet();

            // Act: các lượt đầu xong lượt thì cho đăng ký gửi đến hạn lại; lượt cuối giữ nguyên giờ hẹn do Worker tính; bản nhận chạy thêm một lượt cuối
            for (var round = 1; round < LoadTestSeedRounds; round++)
            {
                await RunLoadTestSeedRoundAsync(senders, receivers);
                foreach (var outboundId in outboundIds)
                {
                    await MakeSubscriptionDue(db, outboundId);
                }
            }

            var lastRoundStart = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await RunLoadTestSeedRoundAsync(senders, receivers);
            var lastRoundEnd = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await receiveOnly.ProcessPendingPackets(CancellationToken.None);

            // Assert: câu riêng có đúng ở nửa sau số đối tác, mỗi gói một câu, câu nào cũng khác câu mẫu chung
            var partnerSqlRows = await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PartnerId != null && packetIds.Contains(s.PacketId) && partnerIds.Contains(s.PartnerId))
                .ToListAsync();
            var commonRows = await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PartnerId == null && packetIds.Contains(s.PacketId))
                .ToListAsync();
            var commonSqlByPacket = commonRows.ToDictionary(s => s.PacketId!, s => s.ExtractSql);
            var ownSqlPartnerIds = partners.Skip(partnerCount / 2).Select(p => p.Partner.ID).ToHashSet();
            Assert.Equal(ownSqlPartnerIds.Count * packets.Count, partnerSqlRows.Count);
            Assert.All(partnerSqlRows, s => Assert.Contains(s.PartnerId!, ownSqlPartnerIds));
            Assert.All(partnerSqlRows, s => Assert.NotEqual(commonSqlByPacket[s.PacketId!], s.ExtractSql));
            Assert.All(partnerSqlRows, s => Assert.Contains(LoadTestOwnSqlMarker, s.ExtractSql));

            // Assert: dữ liệu, số thứ tự, mốc, khóa, lịch và nhật ký của từng đối tác × gói
            var totalReceived = 0;
            foreach (var seedPartner in partners)
            {
                var code = seedPartner.Partner.Code!;
                foreach (var seedPacket in packets)
                {
                    var packet = seedPacket.Packet;
                    var packetCode = packet.PacketCode;
                    var outbound = seedPartner.Outbound[packet.PacketId];
                    var seededInbound = seedPartner.Inbound[packet.PacketId];
                    var inboundId = seededInbound.ID;
                    var source = await ReadLoadTestSourceValuesAsync(db, packet);
                    var expected = seedPacket.IsSnapshot
                        ? Enumerable.Repeat(source, LoadTestSeedRounds).SelectMany(values => values).Order(StringComparer.Ordinal).ToList()
                        : source.Order(StringComparer.Ordinal).ToList();
                    var written = await ReadLoadTestWrittenValuesAsync(inboundDb, seedPacket.MarkerCode, code);
                    var received = await inboundDb.Queryable<ShareDataInboundPacket>()
                        .Where(p => p.PartnerCode == code && p.PacketCode == packetCode)
                        .ToListAsync();
                    var receivedSerials = received.Select(p => p.SerialNbr).ToList();
                    var orderedReceivedSerials = receivedSerials.Order().ToList();
                    var distinctSerialCount = receivedSerials.Distinct().Count();
                    var firstSerial = receivedSerials.Min();
                    var lastSerial = receivedSerials.Max();
                    // Đăng ký nhận chỉ tăng số thứ tự, không lùi: giữ số gán lúc tạo nếu số đó lớn hơn gói lớn nhất đã nhận.
                    var expectedInboundSerial = Math.Max(seededInbound.SerialNbr ?? 0, lastSerial ?? 0);
                    var savedOutbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(outbound.ID);
                    var savedInbound = await db.Queryable<ShareDataSubscription>().InSingleAsync(inboundId);
                    var sendLogs = await GetLogs(db, outbound.ID);
                    var sendParents = sendLogs.Where(l => l.ParentId == null).ToList();
                    var sentParents = sendParents.Where(l => l.RecordCount > 0).ToList();
                    var sentSerials = sentParents.Select(l => l.SerialNbr).Order().ToList();
                    var sentRecords = sendParents.Sum(l => l.RecordCount ?? 0);
                    var receiveLogIds = received.Select(p => p.ReceiveLogId).ToList();
                    var receiveParents = await db.Queryable<ShareDataActivityLog>().Where(l => receiveLogIds.Contains(l.ID)).ToListAsync();
                    var processSteps = await db.Queryable<ShareDataActivityLog>().Where(l => receiveLogIds.Contains(l.ParentId) && l.StepNbr == 2).ToListAsync();
                    var nextRunEarliest = lastRoundStart.AddSeconds(LoadTestIntervalSeconds - 1);
                    var nextRunLatest = lastRoundEnd.AddSeconds(LoadTestIntervalSeconds + 1);
                    totalReceived += received.Count;

                    Assert.Equal(expected, written);
                    Assert.All(received, p => Assert.Equal(BaseEnums.InboundProcessState.Done, p.ProcessState));
                    Assert.DoesNotContain(null, receivedSerials);
                    Assert.Equal(received.Count, distinctSerialCount);
                    Assert.Equal(outbound.SerialNbr, firstSerial);
                    Assert.Equal(expectedInboundSerial, savedInbound.SerialNbr);
                    Assert.Null(savedOutbound.ProcessingUntil);
                    Assert.InRange(savedOutbound.NextTimeRun!.Value, nextRunEarliest, nextRunLatest);
                    Assert.All(sendParents, l => Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success));
                    Assert.Equal(expected.Count, sentRecords);
                    Assert.Equal(orderedReceivedSerials, sentSerials);
                    Assert.Equal(received.Count, receiveParents.Count);
                    Assert.All(receiveParents, l => Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success));
                    Assert.Equal(received.Count, processSteps.Count);
                    Assert.All(processSteps, l => Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success));
                }

                foreach (var seedPacket in packets.Where(p => !p.IsSnapshot))
                {
                    var packetCode = seedPacket.Packet.PacketCode;
                    var cursor = await db.Queryable<ShareDataLastSend>()
                        .Where(c => c.PartnerCode == code && c.PacketCode == packetCode && c.CursorKey == null)
                        .FirstAsync();
                    var lastRow = CursorEnd(seedPacket.Packet, seedPacket.Packet.RowCodes.Count);
                    Assert.EndsWith(lastRow, cursor.LastKey);
                }
            }

            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => partnerIds.Contains(a.PartnerId)).ToListAsync();
            Assert.Empty(alerts);
            Assert.Equal(totalReceived, receiverPort.Requests.Count);
            Assert.All(receiverPort.Requests, r => Assert.Equal(LoadTestEndpoint, r.PathAndQuery));
            Assert.All(receiverPort.Requests, r => Assert.Contains(r.PartnerCode, partnerCodes));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Người vận hành xóa các đối tác kiểm tải qua API (tạm dừng, xóa đăng ký, xóa hồ sơ ánh xạ, xóa đối tác) đúng lúc Worker đang gửi dở
        ///              trang đầu của một đối tác kiểm tải, trong khi một đối tác thường cùng gửi gói đó. Lượt chạy không ném lỗi; đối tác, đăng ký, hồ sơ
        ///              của đối tác kiểm tải đều đã xóa, mỗi đối tác có dòng nhật ký xóa trong Lịch sử chia sẻ; trang đang gửi dở bị WebAPI từ chối và để lại
        ///              đúng một cảnh báo HttpSendFailed nêu mã đối tác cùng dòng nhật ký thất bại, không gói nào của đối tác kiểm tải vào CSDL nhận;
        ///              đối tác thường vẫn nhận đủ mọi dòng đúng một lần, mốc tới dòng cuối, không cảnh báo.
        /// Created date: 11/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSeedPartnersDeletedViaApiWhilePageInFlight_LogsInFlightPageAndKeepsOtherPartners_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            using var apiScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            await using var receiverPort = await ShareDataHttpsReceiverMock.StartAsync(() => _host.CreateApiClient());
            var pageSizeSql = LoadTestPageSizeSql();
            var seedPacket = await SeedLoadTestCatalogPacketAsync(db, rowCount: 4, pageSize: 2, pageSizeSql, isSnapshot: false);
            var packet = seedPacket.Packet;
            var prefix = $"Q{Guid.NewGuid():N}"[..7];
            const int seedPartnerCount = 3;
            var seedPartners = await SeedLoadTestSeedPartnersAsync(db, prefix, seedPartnerCount, [seedPacket], receiverPort, seedPartnerCount + 1);
            var others = await SeedLoadTestPartnersAsync(db, packet, partnerCount: 1, commonPageSize: 2, configurePartner: p =>
            {
                p.Address = ShareDataHttpsReceiverMock.Address;
                p.Port = receiverPort.Port;
                p.EndPointApiUrl = LoadTestEndpoint;
                p.ConnectionConfig = LoadTestConnectionConfig;
            });
            var other = others[0].Receiver;
            var networkFactory = new NetworkHttpClientFactory();
            var sender = CreateReceiverClone(scope, null, networkFactory);
            var receiver = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var seedPartnerIds = seedPartners.Select(p => p.Partner.ID).ToList();
            var deleteStarted = 0;
            string? triggeringPartnerCode = null;
            receiverPort.BeforeForward = async request =>
            {
                var isSeedPartner = request.PartnerCode != null && request.PartnerCode.StartsWith(prefix, StringComparison.Ordinal);
                if (!isSeedPartner || Interlocked.Exchange(ref deleteStarted, 1) == 1)
                {
                    return;
                }

                triggeringPartnerCode = request.PartnerCode;
                await DeleteLoadTestSeedPartnersViaApiAsync(apiScope.ServiceProvider, seedPartners);
            };

            // Act: lượt 1 xóa đối tác kiểm tải ngay khi trang đầu của một đối tác kiểm tải đang gửi; các lượt sau chỉ còn đối tác thường (4 dòng, mỗi lượt 2 dòng)
            for (var round = 0; round < 3; round++)
            {
                var sending = sender.ProcessSubscriptions(CancellationToken.None);
                var receiving = receiver.ProcessPendingPackets(CancellationToken.None);
                await Task.WhenAll(sending, receiving);
                await MakeSubscriptionDue(db, other.Outbound.ID);
            }

            await receiver.ProcessPendingPackets(CancellationToken.None);

            // Assert
            var alivePartners = await db.Queryable<ShareDataPartner>().Where(p => seedPartnerIds.Contains(p.ID) && p.IsDelete == null).CountAsync();
            var aliveSubscriptions = await db.Queryable<ShareDataSubscription>().Where(s => seedPartnerIds.Contains(s.PartnerId) && s.IsDelete == null).CountAsync();
            var aliveMappings = await db.Queryable<ShareDataMapping>().Where(m => seedPartnerIds.Contains(m.PartnerId) && m.IsDelete == null).CountAsync();
            var partnerDeleteLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.Action == BaseEnums.ActivityAction.Delete && seedPartnerIds.Contains(l.TargetId))
                .CountAsync();
            var seedReceived = await inboundDb.Queryable<ShareDataInboundPacket>().Where(p => SqlFunc.StartsWith(p.PartnerCode, prefix)).CountAsync();
            var triggeringPartner = seedPartners.Single(p => p.Partner.Code == triggeringPartnerCode);
            var triggeringSubscriptionId = triggeringPartner.Outbound[packet.PacketId].ID;
            var inFlightAlerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == triggeringSubscriptionId).ToListAsync();
            var inFlightLogs = await GetLogs(db, triggeringSubscriptionId);
            var inFlightParents = inFlightLogs.Where(l => l.ParentId == null && l.Action == BaseEnums.ActivityAction.Send).ToList();
            var inFlightConfigActions = inFlightLogs.Where(l => l.LogType == BaseEnums.LogTypeEnum.CONFIG).Select(l => l.Action).ToList();
            var sourceValues = await ReadLoadTestSourceValuesAsync(db, packet);
            var expectedValues = sourceValues.Order(StringComparer.Ordinal).ToList();
            var otherCode = other.Partner.Code!;
            var otherWritten = await ReadLoadTestWrittenValuesAsync(inboundDb, seedPacket.MarkerCode, otherCode);
            var otherCursor = await GetCloneCursorAsync(db, packet, other.Name);
            var otherSaved = await db.Queryable<ShareDataSubscription>().InSingleAsync(other.Outbound.ID);
            var otherPartnerId = other.Partner.ID;
            var otherAlerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.PartnerId == otherPartnerId).ToListAsync();
            var lastRow = CursorEnd(packet, 4);

            Assert.Equal(1, deleteStarted);
            Assert.Equal(0, alivePartners);
            Assert.Equal(0, aliveSubscriptions);
            Assert.Equal(0, aliveMappings);
            Assert.Equal(seedPartnerCount, partnerDeleteLogs);
            Assert.Equal(0, seedReceived);
            Assert.Single(inFlightAlerts);
            Assert.Equal(ShareDataAlertCode.Outbound.HttpSendFailed, inFlightAlerts[0].AlertCode);
            Assert.Contains(triggeringPartnerCode!, inFlightAlerts[0].Message);
            Assert.Single(inFlightParents);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, inFlightParents[0].Success);
            Assert.Contains(BaseEnums.ActivityAction.Pause, inFlightConfigActions);
            Assert.Contains(BaseEnums.ActivityAction.Delete, inFlightConfigActions);
            Assert.Equal(expectedValues, otherWritten);
            Assert.EndsWith(lastRow, otherCursor.LastKey);
            Assert.Null(otherSaved.ProcessingUntil);
            Assert.Empty(otherAlerts);
        }
    }
}
