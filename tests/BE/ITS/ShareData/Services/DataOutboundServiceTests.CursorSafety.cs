using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Dto.Session;
using Module.ShareData.Core.Entities;
using Modules.TMS.Core.Entities;
using ShareDataWorker.Infrastructure.Logging;
using Tests.ShareData.Common;
using Tests.ShareData.Mocks;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Các ca làm mất dòng hoặc gửi lại khi vận hành: xóa rồi tạo lại đối tác, gói tin cùng mã; JOIN nhân dòng làm trùng giá trị cột mốc.
    /// Created date: 05/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>Cột mốc tính theo bảng cha: mọi dòng con của một cha cùng một giá trị.</summary>
        private const string ParentCursorKey = "CONVERT(varchar(23), COALESCE(z.UpdateTime, z.CreateTime), 121) + '_' + z.ID";

        /// <summary>Cột mốc có thêm khóa bảng con: mỗi dòng con một giá trị.</summary>
        private const string ParentChildCursorKey = ParentCursorKey + " + '_' + zs.ID";

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu JOIN cha (TmsZone) với con (TmsZoneStatus), giới hạn số dòng và công thức cột mốc do test đặt.
        /// Created date: 05/11/2026
        /// </summary>
        private static string JoinCursorSql(string key, string top = "(@pageSize)") =>
            $"SELECT TOP {top} zs.ID AS rowCode, {key} AS __lastKey FROM TmsZone z INNER JOIN TmsZoneStatus zs ON zs.ZoneId = z.ID "
            + $"WHERE z.Code LIKE @prefix AND {key} > @lastKey ORDER BY __lastKey ASC";

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo gói nối đuôi với câu JOIN cha con: mỗi phần tử childrenPerParent là số dòng con của một cha, cha sau có giờ muộn hơn cha trước.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task<ClonePacket> SeedJoinPacketAsync(ISqlSugarClient db, int pageSize, string sql, params int[] childrenPerParent)
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-joi-{unique}";
            var packetCode = $"PKT_JOI_{unique}";
            var rowPrefix = $"ZC_JOI_{unique}_";
            var baseTime = new DateTime(2026, 10, 27, 9, 0, 0);
            var rowCodes = new List<string>();
            for (var parentIndex = 0; parentIndex < childrenPerParent.Length; parentIndex++)
            {
                var parentName = $"P{parentIndex}";
                var parentTime = baseTime.AddMinutes(parentIndex);
                await SeedZoneRowAsync(db, rowPrefix, unique, parentName, parentTime);
                for (var childIndex = 0; childIndex < childrenPerParent[parentIndex]; childIndex++)
                {
                    var childCode = $"{rowPrefix}R{rowCodes.Count + 1:00}";
                    await db.Insertable(new TmsZoneStatus { ID = childCode, ZoneId = $"{parentName}_{unique}", AverageSpeed = "50", Condition = "NORMAL" }).ExecuteCommandAsync();
                    rowCodes.Add(childCode);
                }
            }

            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var variables = ZoneVariables(pageSize, rowPrefix, null);
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);

            return new ClonePacket(unique, packetId, packetCode, rowPrefix, rowCodes);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Thêm đối tác qua API như thao tác thật trên màn hình rồi mở kết nối, đối tác nhận ở đường dẫn path.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task<ShareDataPartner> AddConnectedPartnerViaApiAsync(IServiceProvider services, ISqlSugarClient db, string code, string path)
        {
            await services.CallApiAsync(new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {code}",
                Address = ShareDataPartnerServerMock.DefaultHost,
                Port = ShareDataPartnerServerMock.DefaultPort,
                EndPointApiUrl = $"/api/sharedata/{path}"
            });
            var partner = await db.Queryable<ShareDataPartner>().Where(p => p.IsDelete == null && p.Code == code).FirstAsync();
            await services.CallApiAsync<ShareDataSessionOutput>(new ShareDataConnectPartnerInput { ID = partner.ID });
            return partner;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo đăng ký gửi và hồ sơ ánh xạ đang bật cho đối tác đã có, đến hạn chạy ngay.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task AddOutboundSubscriptionAsync(ISqlSugarClient db, string partnerId, string packetId)
        {
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Insertable(new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"SUB_{Guid.NewGuid():N}",
                PartnerId = partnerId,
                DatatypeId = packetId,
                Direction = BaseEnums.Direction.Outbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = dbNow.AddSeconds(-10),
                IntervalSeconds = 30
            }).ExecuteCommandAsync();
            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partnerId,
                DatatypeId = packetId,
                Direction = BaseEnums.Direction.Outbound,
                IsActive = true,
                TargetShapeJson = System.Text.Json.JsonSerializer.Serialize(RowCodeShape)
            }).ExecuteCommandAsync();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Xóa đối tác đã có mốc (một dòng, nhiều dòng, hoặc dữ liệu xóa từ trước còn sót mốc) rồi tạo lại cùng mã: đối tác mới là đối tác khác nên nhận đủ từ đầu,
        ///              không đi tiếp từ mốc của đối tác cũ.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("single")]
        [InlineData("batch")]
        [InlineData("legacy")]
        public async Task ProcessSubscriptions_WhenPartnerDeletedAndRecreatedWithSameCode_NewPartnerGetsRowsFromStart_Test(string deleteBy)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 5);
            UseSuperAdmin(packet.Unique);
            var (partner, sub) = await SeedClonePartnerAsync(db, packet, "A101");
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);
            var cursorBeforeDelete = await GetCloneCursorAsync(db, packet, "A101");
            await SetSubscriptionStateAsync(db, BaseEnums.SubSubscriptionState.Paused, sub.ID);

            if (deleteBy == "single")
            {
                await services.CallApiAsync(new ShareDataDeletePartnerInput { ID = partner.ID });
            }
            else if (deleteBy == "batch")
            {
                await services.CallApiAsync(new List<ShareDataDeletePartnerInput> { new() { ID = partner.ID } });
            }
            else
            {
                var deletedAt = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
                await db.Updateable<ShareDataPartner>().SetColumns(p => p.IsDelete == deletedAt).Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }

            var newPartner = await AddConnectedPartnerViaApiAsync(services, db, partner.Code!, ClonePath(packet, "A101b"));
            await AddOutboundSubscriptionAsync(db, newPartner.ID!, packet.PacketId);

            // Act
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);

            // Assert
            Assert.EndsWith(CursorEnd(packet, 3), cursorBeforeDelete.LastKey);
            Assert.Equal(packet.RowCodes, ReceivedRows(packet, "A101b"));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Xóa gói tin đã có mốc (một dòng, nhiều dòng, hoặc dữ liệu xóa từ trước còn sót mốc) rồi tạo lại cùng mã: gói mới là gói khác nên đối tác nhận đủ từ đầu.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("single")]
        [InlineData("batch")]
        [InlineData("legacy")]
        public async Task ProcessSubscriptions_WhenPacketDeletedAndRecreatedWithSameCode_PartnerGetsRowsFromStart_Test(string deleteBy)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 5);
            UseSuperAdmin(packet.Unique);
            var (partner, sub) = await SeedClonePartnerAsync(db, packet, "A101");
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);
            var rowsBeforeDelete = ReceivedRows(packet, "A101");
            var deletedAt = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await SetSubscriptionStateAsync(db, BaseEnums.SubSubscriptionState.Cancelled, sub.ID);
            await db.Updateable<ShareDataMapping>().SetColumns(m => m.IsDelete == deletedAt).Where(m => m.PartnerId == partner.ID).ExecuteCommandAsync();

            if (deleteBy == "single")
            {
                await services.CallApiAsync(new ShareDataDeletePacketInput { ID = packet.PacketId });
            }
            else if (deleteBy == "batch")
            {
                await services.CallApiAsync(new List<ShareDataDeletePacketInput> { new() { ID = packet.PacketId } });
            }
            else
            {
                await db.Updateable<ShareDataPacket>().SetColumns(p => p.IsDelete == deletedAt).Where(p => p.ID == packet.PacketId).ExecuteCommandAsync();
            }

            await services.CallApiAsync(new ShareDataAddPacketInput { Code = packet.PacketCode, Name = $"Packet {packet.PacketCode}", PacketVersion = "1.0", OrderNo = 110 });
            var newPacket = await db.Queryable<ShareDataPacket>().Where(p => p.IsDelete == null && p.Code == packet.PacketCode).FirstAsync();
            var variables = ZoneVariables(5, packet.RowPrefix, null);
            var sql = ZoneCursorSql();
            await ShareDataSqlSeed.SetVariablesAsync(db, newPacket.ID, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, newPacket.ID, sql);
            await AddOutboundSubscriptionAsync(db, partner.ID!, newPacket.ID!);

            // Act
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);

            // Assert
            var rowsAfterRecreate = ReceivedRows(packet, "A101").Skip(rowsBeforeDelete.Count).ToList();
            Assert.Equal(packet.RowCodes, rowsBeforeDelete);
            Assert.Equal(packet.RowCodes, rowsAfterRecreate);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: JOIN nhân dòng làm nhiều dòng cùng một giá trị cột mốc: trùng ngay trong trang, hoặc dòng đầu của nhóm rơi vào cuối trang còn các dòng sau nằm ở trang kế.
        ///              Trang kế lọc lớn hơn mốc nên sẽ bỏ sót các dòng trùng: Worker không gửi trang đó, giữ mốc và báo ESH-1309, lượt sau vẫn như vậy cho tới khi sửa câu.
        ///              Đặt TOP bằng số cố định thì vẫn bắt được khi trùng trong trang.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(2, "(@pageSize)", new[] { 3 })]
        [InlineData(2, "(@pageSize)", new[] { 1, 3 })]
        [InlineData(3, "(@pageSize)", new[] { 1, 2 })]
        [InlineData(1, "(@pageSize)", new[] { 2 })]
        [InlineData(2, "2", new[] { 3 })]
        public async Task ProcessSubscriptions_WhenJoinRepeatsCursorKey_StopsKeepsCursorAndAlerts_Test(int pageSize, string top, int[] childrenPerParent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var sql = JoinCursorSql(ParentCursorKey, top);
            var packet = await SeedJoinPacketAsync(db, pageSize, sql, childrenPerParent);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");

            // Act
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);
            await MakeSubscriptionDue(db, sub.ID);
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);

            // Assert
            var cursor = await GetCloneCursorAsync(db, packet, "A101");
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.CursorKeyDuplicated).ToListAsync();
            Assert.Empty(ReceivedRows(packet, "A101"));
            Assert.True(cursor == null || string.IsNullOrEmpty(cursor.LastKey), cursor?.LastKey);
            Assert.Equal(2, alerts.Count);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Sửa câu bị trùng bằng cách thêm khóa bảng con vào công thức, hoặc JOIN không nhân dòng: mọi dòng được gửi đúng một lần, không có cảnh báo;
        ///              số bản ghi mỗi lần gửi đúng bằng @pageSize, dòng lấy dư để kiểm ranh giới trang không bị gửi đi, kể cả khi số dòng vừa tròn trang.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(ParentChildCursorKey, new[] { 1, 3, 2 }, 3)]
        [InlineData(ParentCursorKey, new[] { 1, 1, 1, 1 }, 2)]
        public async Task ProcessSubscriptions_WhenCursorKeysAreUnique_SendsEveryRowOnceAtPageSize_Test(string key, int[] childrenPerParent, int expectedRequests)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var sql = JoinCursorSql(key);
            var packet = await SeedJoinPacketAsync(db, 2, sql, childrenPerParent);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");

            // Act: mỗi lượt gửi 1 page theo đúng cơ chế 1 page/lượt
            var clone = CreateClone(scope, null);
            for (var run = 0; run < expectedRequests; run++)
            {
                await RunCloneAsync(clone, packet, byEvent: false);
                await MakeSubscriptionDue(db, sub.ID);
            }

            // Assert
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            var bodies = PartnerBodies(ClonePath(packet, "A101"));
            Assert.Equal(packet.RowCodes.OrderBy(c => c), ReceivedRows(packet, "A101").OrderBy(c => c));
            Assert.Equal(expectedRequests, bodies.Count);
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Viết công thức cột mốc một lần trong câu con (bảng dẫn xuất hoặc CTE), câu ngoài lọc và sắp theo tên cột x.__lastKey:
        ///              lưu qua API được; Worker gửi đủ mọi dòng đúng một lần qua nhiều trang, theo chu kỳ lẫn theo tín hiệu, mốc tới dòng cuối;
        ///              lượt sau chỉ gửi dòng mới thêm.
        /// Created date: 06/11/2026
        /// </summary>
        [Theory]
        [InlineData("SELECT TOP (@pageSize) x.rowCode, x.__lastKey FROM (SELECT z.Code AS rowCode, {K} AS __lastKey FROM TmsZone z WHERE z.Code LIKE @prefix) x WHERE x.__lastKey > @lastKey ORDER BY x.__lastKey ASC", false)]
        [InlineData("SELECT TOP (@pageSize) x.rowCode, x.__lastKey FROM (SELECT z.Code AS rowCode, {K} AS __lastKey FROM TmsZone z WHERE z.Code LIKE @prefix) x WHERE x.__lastKey > @lastKey ORDER BY x.__lastKey ASC", true)]
        [InlineData("WITH x AS (SELECT z.Code AS rowCode, {K} AS __lastKey FROM TmsZone z WHERE z.Code LIKE @prefix) SELECT TOP (@pageSize) x.rowCode, x.__lastKey FROM x WHERE x.__lastKey > @lastKey ORDER BY x.__lastKey ASC", false)]
        public async Task ProcessSubscriptions_WhenCursorFormulaWrittenOnceInSubquery_SendsEveryRowOnce_Test(string template, bool byEvent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ISqlSugarClient>();
            var packet = await SeedClonePacketAsync(db, rowCount: 5, pageSize: 5);
            UseSuperAdmin(packet.Unique);
            var sql = template.Replace("{K}", "CONVERT(varchar(23), COALESCE(z.UpdateTime, z.CreateTime), 121) + '_' + z.ID");
            await SaveCommonSqlAsync(services, db, packet.PacketCode, sql);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");
            var newRowCode = $"{packet.RowPrefix}R06";
            var newRowTime = new DateTime(2026, 10, 27, 10, 0, 0);

            // Act
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent);
            var rowsFirstRun = ReceivedRows(packet, "A101");
            await SeedZoneRowAsync(db, packet.RowPrefix, packet.Unique, "R06", newRowTime);
            await MakeSubscriptionDue(db, sub.ID);
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent);

            // Assert
            var savedSql = await ShareDataSqlSeed.GetCommonAsync(db, packet.PacketId);
            var cursor = await GetCloneCursorAsync(db, packet, "A101");
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            List<string> allRows = [.. packet.RowCodes, newRowCode];
            Assert.Equal(sql, savedSql?.ExtractSql);
            Assert.Equal(packet.RowCodes, rowsFirstRun);
            Assert.Equal(allRows, ReceivedRows(packet, "A101"));
            Assert.EndsWith(CursorEnd(packet, 6), cursor.LastKey);
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu lưu từ trước khi có luật, dùng OFFSET, gộp hai nguồn một mốc hoặc câu con có TOP sắp theo cột khác cột mốc: Worker vẫn chạy và gửi,
        ///              ghi rủi ro một lần vào nhật ký cấu hình, không ghi cảnh báo.
        /// Created date: 05/11/2026
        /// Modified date: 06/11/2026
        /// </summary>
        [Theory]
        [InlineData("SELECT T.rowCode, T.__lastKey FROM (SELECT TOP (@pageSize) z.Code AS rowCode, {K} AS __lastKey FROM TmsZone z WHERE z.Code LIKE @prefix AND {K} > @lastKey ORDER BY z.Name DESC) T ORDER BY T.__lastKey ASC", "Câu con có TOP")]
        [InlineData("SELECT z.Code AS rowCode, {K} AS __lastKey FROM TmsZone z WHERE z.Code LIKE @prefix AND {K} > @lastKey ORDER BY __lastKey ASC OFFSET 1 ROWS FETCH NEXT @pageSize ROWS ONLY", "OFFSET")]
        [InlineData("SELECT TOP (@pageSize) u.rowCode, u.__lastKey FROM (SELECT z.Code AS rowCode, {K} AS __lastKey FROM TmsZone z WHERE z.Code LIKE @prefix AND {K} > @lastKey UNION ALL SELECT z.Name, {K} FROM TmsZone z WHERE z.Name LIKE @prefix AND {K} > @lastKey) u ORDER BY u.__lastKey ASC", "một mốc")]
        public async Task ProcessSubscriptions_WhenSavedSqlSkipsRowsOrSharesCursor_LogsRiskAndKeepsRunning_Test(string template, string expectedRisk)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var sql = template.Replace("{K}", ParentCursorKey);
            var packet = await SeedClonePacketAsync(db, rowCount: 3, pageSize: 2);
            await ShareDataSqlSeed.SetCommonAsync(db, packet.PacketId, sql);
            var (_, sub) = await SeedClonePartnerAsync(db, packet, "A101");

            // Act
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);
            await MakeSubscriptionDue(db, sub.ID);
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);

            // Assert
            var riskLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.TargetId == sub.ID && l.Description == ShareDataEnum.ExportMessage.CursorSqlRisk)
                .ToListAsync();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            var riskLog = Assert.Single(riskLogs);
            Assert.NotEmpty(ReceivedRows(packet, "A101"));
            Assert.Contains(expectedRisk, riskLog.ErrorMessage);
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu gộp nhiều nguồn: một nhóm mốc có hai dòng cùng giá trị trong trang thì không gửi, giữ mốc của cả hai nhóm và báo ESH-1309.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenCursorGroupRepeatsKey_StopsKeepsCursorAndAlerts_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var groupSql = $"""
                SELECT TOP (@pageSize) u.rowCode, u.__lastKey_a, u.__lastKey_b
                FROM (
                    SELECT zs.ID AS rowCode, {ParentCursorKey} AS sortKey, {ParentCursorKey} AS __lastKey_a, CAST(NULL AS nvarchar(100)) AS __lastKey_b
                    FROM TmsZone z INNER JOIN TmsZoneStatus zs ON zs.ZoneId = z.ID
                    WHERE z.Code LIKE @prefix AND {ParentCursorKey} > @lastKey_a
                    UNION ALL
                    SELECT w.RefId, CONVERT(varchar(23), COALESCE(w.UpdateTime, w.CreateTime), 121) + '_' + w.ID, NULL, CONVERT(varchar(23), COALESCE(w.UpdateTime, w.CreateTime), 121) + '_' + w.ID
                    FROM TmsWeather w
                    WHERE w.RefId LIKE @prefix AND CONVERT(varchar(23), COALESCE(w.UpdateTime, w.CreateTime), 121) + '_' + w.ID > @lastKey_b
                ) u
                ORDER BY u.sortKey ASC
                """;
            var packet = await SeedJoinPacketAsync(db, 5, groupSql, 2);
            var variables = ZoneVariables(5, packet.RowPrefix, null);
            var lastKeyVariable = variables.Single(v => v.Code == "lastKey");
            variables.Remove(lastKeyVariable);
            variables.Add(new() { Code = "lastKey_a", DataType = lastKeyVariable.DataType, ValueKind = lastKeyVariable.ValueKind, Value = lastKeyVariable.Value, OrderNo = 3 });
            variables.Add(new() { Code = "lastKey_b", DataType = lastKeyVariable.DataType, ValueKind = lastKeyVariable.ValueKind, Value = lastKeyVariable.Value, OrderNo = 4 });
            await ShareDataSqlSeed.SetVariablesAsync(db, packet.PacketId, null, variables);
            var (partner, sub) = await SeedClonePartnerAsync(db, packet, "A101");

            // Act
            await RunCloneAsync(CreateClone(scope, null), packet, byEvent: false);

            // Assert
            var groupCursors = await db.Queryable<ShareDataLastSend>().Where(c => c.PartnerCode == partner.Code && c.LastKey != null).CountAsync();
            var alertCount = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.CursorKeyDuplicated).CountAsync();
            Assert.Empty(ReceivedRows(packet, "A101"));
            Assert.Equal(0, groupCursors);
            Assert.Equal(1, alertCount);
        }
    }
}
