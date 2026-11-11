using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Kiểm tải nhiều đối tác trên chung một CSDL như bộ seed kiểm tải: nửa đầu dùng câu mẫu chung, nửa sau mỗi đối tác một câu riêng
    ///              biến tấu từ câu mẫu (thêm cột phụ, đổi cỡ trang, đổi xuống dòng, chú thích dài ngắn) mà giữ công thức cột mốc;
    ///              4 bản Worker chỉ gửi chạy cùng lúc, không khai SelfPartnerCode nên tự chia nhau mọi đối tác.
    /// Created date: 11/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>Số bản Worker chỉ gửi chạy cùng lúc trong các bài test nhiều đối tác.</summary>
        private const int ManyPartnerSenderCount = 4;

        /// <summary>Đối tác của bài test nhiều đối tác: tên vai, đăng ký gửi và cỡ trang câu SQL đối tác đang dùng.</summary>
        private sealed record ManyPartner(string Name, ShareDataSubscription Subscription, int PageSize);

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu riêng của đối tác sinh từ câu mẫu chung theo kiểu biến tấu 0..4 như bộ seed kiểm tải:
        ///              0 chỉ thêm chú thích, 1 thêm một cột phụ, 2 giữ câu nhưng đổi cỡ trang ở bộ biến, 3 thêm bốn cột phụ (có cột chuỗi dài),
        ///              4 xuống dòng trước FROM / WHERE / ORDER BY; câu nào cũng thêm chú thích cuối dài ngắn theo kiểu.
        /// Created date: 11/11/2026
        /// </summary>
        private static string ManyPartnerOwnSql(string commonSql, int variant, string partnerCode)
        {
            var extraColumns = variant switch
            {
                1 => $", N'{partnerCode}' AS ktPartner",
                3 => $", N'{partnerCode}' AS ktPartner, {variant} AS ktNo, GETDATE() AS ktReadAt, REPLICATE(N'x', {100 + partnerCode.Length * 7}) AS ktPad",
                _ => string.Empty
            };
            var withColumns = commonSql.Replace("AS __lastKey", "AS __lastKey" + extraColumns);
            var formatted = variant == 4
                ? withColumns.Replace(" FROM ", "\nFROM ").Replace(" WHERE ", "\nWHERE ").Replace(" ORDER BY ", "\nORDER BY ")
                : withColumns;
            var padding = new string('-', variant * 40);
            return $"{formatted}\n/* {partnerCode} - câu riêng kiểm tải {padding} */";
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo partnerCount đối tác cho gói: đối tác số 1..partnerCount/2 dùng câu mẫu chung (cỡ trang của gói),
        ///              đối tác còn lại có câu riêng và bộ biến riêng theo kiểu biến tấu (số thứ tự % 5); kiểu 2 đổi cỡ trang thành 3.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task<List<ManyPartner>> SeedManyPartnersAsync(ISqlSugarClient db, ClonePacket packet, int partnerCount, int commonPageSize)
        {
            var partners = new List<ManyPartner>();
            for (var index = 1; index <= partnerCount; index++)
            {
                var name = $"M{index:000}";
                var (partner, sub) = await SeedClonePartnerAsync(db, packet, name);
                var pageSize = commonPageSize;
                if (index > partnerCount / 2)
                {
                    var variant = index % 5;
                    pageSize = variant == 2 ? commonPageSize + 1 : commonPageSize;
                    var commonSql = ZoneCursorSql();
                    var ownSql = ManyPartnerOwnSql(commonSql, variant, partner.Code!);
                    var variables = ZoneVariables(pageSize, packet.RowPrefix, null);
                    await ShareDataSqlSeed.SetPartnerAsync(db, packet.PacketId, partner.ID, ownSql);
                    await ShareDataSqlSeed.SetVariablesAsync(db, packet.PacketId, partner.ID, variables);
                }

                partners.Add(new ManyPartner(name, sub, pageSize));
            }

            return partners;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Một lượt vận hành của các bản Worker chỉ gửi chạy cùng lúc, xong lượt thì cho mọi đăng ký đến hạn lại.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task RunManyPartnerRoundAsync(ISqlSugarClient db, List<DataOutboundService> senders, ClonePacket packet, bool byEvent, List<ManyPartner> partners)
        {
            var runs = senders.Select(sender => RunCloneAsync(sender, packet, byEvent));
            await Task.WhenAll(runs);
            foreach (var partner in partners)
            {
                await MakeSubscriptionDue(db, partner.Subscription.ID);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Nhiều đối tác trộn câu mẫu chung và câu riêng biến tấu, 4 bản Worker chỉ gửi chạy cùng lúc theo chu kỳ lẫn theo tín hiệu:
        ///              mỗi đối tác nhận đủ mọi dòng đúng một lần, đúng thứ tự, đúng mã đối tác; số lần gửi khớp cỡ trang của câu đối tác đang dùng
        ///              (chứng tỏ câu riêng được dùng thật), cột phụ của câu riêng không lọt vào gói gửi; mốc tới dòng cuối, khóa được nhả, không có cảnh báo.
        /// Created date: 11/11/2026
        /// </summary>
        [Theory]
        [InlineData(10, false)]
        [InlineData(10, true)]
        [InlineData(50, false)]
        public async Task ProcessSubscriptions_WhenManyPartnersMixCommonAndOwnSql_EachPartnerGetsEveryRowOnce_Test(int partnerCount, bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 7, pageSize: 2);
            var partners = await SeedManyPartnersAsync(db, packet, partnerCount, commonPageSize: 2);
            var senders = Enumerable.Range(0, ManyPartnerSenderCount).Select(_ => CreateClone(scope, null)).ToList();
            var lastRowCursorEnd = CursorEnd(packet, 7);

            // Act: cỡ trang 2 cần 4 lượt, cỡ trang 3 cần 3 lượt; lượt thứ 5 không còn gì để gửi
            for (var round = 0; round < 5; round++)
            {
                await RunManyPartnerRoundAsync(db, senders, packet, byEvent, partners);
            }

            // Assert
            foreach (var partner in partners)
            {
                var partnerCode = ClonePartnerCode(packet, partner.Name);
                var clonePath = ClonePath(packet, partner.Name);
                var requests = _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains(clonePath)).ToList();
                var bodies = PartnerBodies(clonePath).ToList();
                var rows = ReceivedRows(packet, partner.Name);
                var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
                var savedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Subscription.ID);
                var expectedRequests = (packet.RowCodes.Count + partner.PageSize - 1) / partner.PageSize;

                Assert.Equal(packet.RowCodes, rows);
                Assert.Equal(expectedRequests, requests.Count);
                Assert.All(requests, r => Assert.Equal(partnerCode, r.Headers["PartnerCode"]));
                Assert.All(bodies, body => Assert.DoesNotContain("ktPad", body));
                Assert.EndsWith(lastRowCursorEnd, cursor.LastKey);
                Assert.Null(savedSub.ProcessingUntil);
            }

            var subIds = partners.Select(p => p.Subscription.ID).ToArray();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => subIds.Contains(a.SubscriptionId)).ToListAsync();
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đang gửi cho nhiều đối tác trộn câu mẫu chung và câu riêng thì có thêm dòng mới (dòng mới có giờ sau mọi dòng cũ):
        ///              sau khi các bản Worker chạy đủ lượt, mỗi đối tác nhận đủ cả dòng cũ lẫn dòng mới đúng một lần, không lủng, không trùng, mốc tới dòng cuối.
        /// Created date: 11/11/2026
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProcessSubscriptions_WhenRowsArriveWhileManyPartnersSending_EachPartnerGetsEveryRowOnce_Test(bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var partners = await SeedManyPartnersAsync(db, packet, partnerCount: 20, commonPageSize: 2);
            var senders = Enumerable.Range(0, ManyPartnerSenderCount).Select(_ => CreateClone(scope, null)).ToList();
            var newRowTime = new DateTime(2026, 10, 27, 10, 0, 0);

            // Act: 2 lượt đầu gửi một phần, rồi thêm 4 dòng mới, rồi chạy tiếp tới khi hết dòng (9 dòng, cỡ trang 2 cần 5 lượt)
            for (var round = 0; round < 2; round++)
            {
                await RunManyPartnerRoundAsync(db, senders, packet, byEvent, partners);
            }

            for (var index = 6; index <= 9; index++)
            {
                var name = $"R{index:00}";
                var rowTime = newRowTime.AddMinutes(index);
                await SeedZoneRowAsync(db, packet.RowPrefix, packet.Unique, name, rowTime);
                packet.RowCodes.Add($"{packet.RowPrefix}{name}");
            }

            for (var round = 0; round < 5; round++)
            {
                await RunManyPartnerRoundAsync(db, senders, packet, byEvent, partners);
            }

            // Assert
            var lastRowCursorEnd = CursorEnd(packet, 9);
            foreach (var partner in partners)
            {
                var rows = ReceivedRows(packet, partner.Name);
                var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
                var savedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(partner.Subscription.ID);

                Assert.Equal(packet.RowCodes, rows);
                Assert.EndsWith(lastRowCursorEnd, cursor.LastKey);
                Assert.Null(savedSub.ProcessingUntil);
            }

            var subIds = partners.Select(p => p.Subscription.ID).ToArray();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => subIds.Contains(a.SubscriptionId)).ToListAsync();
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Trong nhiều đối tác có một câu riêng lỗi lúc chạy (cột không tồn tại): đối tác đó không nhận gì, có cảnh báo, giữ mốc, nhả khóa;
        ///              mọi đối tác khác (mẫu chung lẫn câu riêng) vẫn nhận đủ mọi dòng đúng một lần, không có cảnh báo.
        /// Created date: 11/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenOneOwnSqlFailsAmongManyPartners_OthersGetEveryRowOnce_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 2);
            var partners = await SeedManyPartnersAsync(db, packet, partnerCount: 10, commonPageSize: 2);
            var broken = partners[^1];
            var brokenCode = ClonePartnerCode(packet, broken.Name);
            var brokenPartner = await db.Queryable<ShareDataPartner>().Where(p => p.Code == brokenCode).FirstAsync();
            var commonSql = ZoneCursorSql();
            var brokenSql = commonSql.Replace("AS __lastKey", "AS __lastKey, z.NoSuchColumn AS ktBroken");
            await ShareDataSqlSeed.SetPartnerAsync(db, packet.PacketId, brokenPartner.ID, brokenSql);
            var senders = Enumerable.Range(0, ManyPartnerSenderCount).Select(_ => CreateClone(scope, null)).ToList();

            // Act
            for (var round = 0; round < 4; round++)
            {
                await RunManyPartnerRoundAsync(db, senders, packet, false, partners);
            }

            // Assert
            var healthy = partners.Where(p => p.Name != broken.Name).ToList();
            var lastRowCursorEnd = CursorEnd(packet, 5);
            foreach (var partner in healthy)
            {
                var rows = ReceivedRows(packet, partner.Name);
                var cursor = await GetCloneCursorAsync(db, packet, partner.Name);
                Assert.Equal(packet.RowCodes, rows);
                Assert.EndsWith(lastRowCursorEnd, cursor.LastKey);
            }

            var healthyIds = healthy.Select(p => p.Subscription.ID).ToArray();
            var healthyAlerts = await db.Queryable<ShareDataAlertLog>().Where(a => healthyIds.Contains(a.SubscriptionId)).ToListAsync();
            var brokenAlerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == broken.Subscription.ID).ToListAsync();
            var brokenRows = ReceivedRows(packet, broken.Name);
            var brokenSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(broken.Subscription.ID);

            Assert.Empty(healthyAlerts);
            Assert.NotEmpty(brokenAlerts);
            Assert.Empty(brokenRows);
            Assert.Null(brokenSub.ProcessingUntil);
        }
    }
}
