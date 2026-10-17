using System.Net;
using System.Text.Json;
using Module.ShareData.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Infrastructure.Logging;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    public partial class DataOutboundServiceTests
    {
        #region 9. Mốc nối đuôi (cursor) khi gửi thất bại, retry và không có dữ liệu mới

        /// <summary>
        /// Description: T01 - Gói 103 nối đuôi, đối tác trả HTTP 500: log cha Fail, cảnh báo ESH-1402,
        ///              mốc KHÔNG tiến (ShareDataLastSend.LastTime/LastKey, Subscription.LastTimeRun/SerialNbr giữ nguyên).
        ///              Nguồn: DataOutboundService.cs ExportPage (nhánh !isSuccess trả về trước CommitSuccess, dòng 516-535);
        ///              mốc lưu ở ShareDataLastSend (GetLastSend dòng 339) và Subscription.LastTimeRun/SerialNbr (CommitSuccess dòng 634-640).
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenIncrementalHttpFails500_LogsEsh1402AndKeepsCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
            partnerServer.DefaultResponseBody = "Mock 500 Internal Error";

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T01_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T01_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T01_{unique}",
                SubCode = $"SUB_T01_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            await SeedTrafficRows(db, eqId, $"T1{unique}-", baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var logs = await GetLogs(db, sub.ID);
            var parent = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                .ToListAsync();
            Assert.NotEmpty(alerts);

            await AssertCursorUnchanged(db, partner.Code, sub.ID, baseTime);
        }

        /// <summary>
        /// Description: T02 - Gói 103 nối đuôi, cổng đối tác đóng (HTTP exception): không có request nào tới đối tác,
        ///              log cha Fail, cảnh báo ESH-1402, mốc không tiến.
        ///              Nguồn: DataOutboundService.cs ExportPage dòng 511-535; DataOutboundRestSender trả Success=false khi HttpRequestException.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenIncrementalPartnerPortClosed_LogsEsh1402AndKeepsCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T02_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T02_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T02_{unique}",
                SubCode = $"SUB_T02_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime,
                ConfigurePartner = p => p.Port = ShareDataPartnerServerMock.ClosedPort
            });
            await SeedTrafficRows(db, eqId, $"T2{unique}-", baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);

            var parent = (await GetLogs(db, sub.ID)).Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                .ToListAsync();
            Assert.NotEmpty(alerts);

            await AssertCursorUnchanged(db, partner.Code, sub.ID, baseTime);
        }

        /// <summary>
        /// Description: T03 - Retry sau khi đối tác phục hồi: lượt 1 trả 500; lượt 2 (200) gửi lại ĐÚNG các bản ghi của lượt 1;
        ///              lượt 3 không gửi lại gì (không gọi HTTP, log NoNewData).
        ///              Nguồn: ExportPage không commit khi gửi lỗi (dòng 516-535) nên GetLastSend vẫn trả mốc cũ ở lượt kế.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerRecoversAfter500_ResendsSameRecordsOnceAndThenStops_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T03_{unique}";
            var platePrefix = $"T3{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T03_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T03_{unique}",
                SubCode = $"SUB_T03_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            await SeedTrafficRows(db, eqId, platePrefix, baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun1 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);

            partnerServer.ResetDefaults();
            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun2 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);
            var requestsAfterRun2 = partnerServer.TotalReceivedRequests;

            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(5, platesRun1.Count);
            Assert.Equal(platesRun1, platesRun2);
            Assert.Equal(requestsAfterRun2, partnerServer.TotalReceivedRequests);

            var latest = (await GetLogs(db, sub.ID)).Where(l => l.ParentId == null).OrderByDescending(l => l.OccurredAt).First();
            Assert.Equal(BaseEnums.SuccessEnums.Success, latest.Success);
            Assert.Equal(0, latest.RecordCount);
            Assert.Equal(ShareDataEnum.ExportMessage.NoNewData, latest.ErrorMessage);
        }

        /// <summary>
        /// Description: T04 - Gói 103 nối đuôi không có bản ghi mới: không gọi HTTP, đúng 1 log phẳng Success/NoNewData,
        ///              mốc (LastTimeRun, SerialNbr, ShareDataLastSend) không đổi.
        ///              Nguồn: ExportPage dòng 432-447 (RawRows.Count == 0 ở trang 0 chỉ ghi 1 log, không commit).
        ///              Đã có test tương tự cho gói 107 (…WhenNoNewData_LogsSuccess_SingleLogOnly_Test); test này bổ sung gói nối đuôi và mốc.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenIncrementalHasNoNewRows_SendsNothingAndKeepsCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T04_{unique}",
                SubCode = $"SUB_T04_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = dbNow
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);

            var logs = await GetLogs(db, sub.ID);
            var log = Assert.Single(logs);
            Assert.Null(log.ParentId);
            Assert.Equal(BaseEnums.SuccessEnums.Success, log.Success);
            Assert.Equal(0, log.RecordCount);
            Assert.Equal(ShareDataEnum.ExportMessage.NoNewData, log.ErrorMessage);

            await AssertCursorUnchanged(db, partner.Code, sub.ID, dbNow);
        }

        /// <summary>
        /// Description: T05 - Nối đuôi gói 103 khi 150 bản ghi có CÙNG mốc thời gian (chỉ khác ID): trải qua 2 trang
        ///              (100 + 50), mỗi bản ghi xuất đúng 1 lần, không sót ở biên trang.
        ///              Nguồn: QueryPacket103 (DataOutboundExtractionProcess.cs) ORDER BY mốc ASC, ID ASC với điều kiện
        ///              mốc = @lastTime AND ID > @lastKey; DefaultPageSize = 100 là hằng số nên dùng 150 dòng thay cho "page size nhỏ".
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenRowsShareSameWatermark_ExportsEachRowExactlyOnceAcrossPages_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T05_{unique}";
            var platePrefix = $"T5{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T05_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T05_{unique}",
                SubCode = $"SUB_T05_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            await SeedTrafficRows(db, eqId, platePrefix, baseTime, 150);
            var sameTime = baseTime.AddMinutes(1);
            await db.Ado.ExecuteCommandAsync(
                "UPDATE TmsTrafficData SET DetectTime = @t, CreateTime = @t, UpdateTime = @t WHERE EquipmentId = @eqId",
                new { t = sameTime, eqId });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var plates = partnerServer.ReceivedRequests.SelectMany(r => ExtractPlates(r.Body, platePrefix)).ToList();
            Assert.Equal(150, plates.Count);
            Assert.Equal(150, plates.Distinct().Count());

            var pages = (await GetLogs(db, sub.ID)).Where(l => l.ParentId == null && l.RecordCount > 0).ToList();
            Assert.Equal(2, pages.Count);
        }

        /// <summary>
        /// Description: T06 - Nối đuôi gói 103: bản ghi mới hơn về thời gian nhưng ID NHỎ hơn khoá của mốc cũ
        ///              vẫn phải được xuất (điều kiện là OR giữa "mốc lớn hơn" và "mốc bằng AND ID lớn hơn";
        ///              nếu ai đổi OR thành AND thì test này đỏ).
        ///              Nguồn: QueryPacket103 (DataOutboundExtractionProcess.cs).
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenNewerRowHasSmallerIdThanCursorKey_StillExportsIt_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T06_{unique}";
            var platePrefix = $"T6{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T06_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T06_{unique}",
                SubCode = $"SUB_T06_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            var olderId = $"ZZZZ{unique}";
            var newerId = $"0000{unique}";
            var olderRows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 1);
            await db.Ado.ExecuteCommandAsync("UPDATE TmsTrafficData SET ID = @id WHERE ID = @old", new { id = olderId, old = olderRows[0].ID });

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun1 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);

            var eq2Id = $"EQ_T06B_{unique}";
            await db.Insertable(new TmsEquipment { ID = eq2Id, Code = $"VDS_T06B_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var newerRows = await SeedTrafficRows(db, eq2Id, $"{platePrefix}NEWER-", baseTime.AddMinutes(10), 1);
            await db.Ado.ExecuteCommandAsync("UPDATE TmsTrafficData SET ID = @id WHERE ID = @old", new { id = newerId, old = newerRows[0].ID });
            await MakeSubscriptionDue(db, sub.ID);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Single(platesRun1);
            var platesRun2 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);
            Assert.Equal([newerRows[0].LicensePlate!], platesRun2);
        }

        /// <summary>
        /// Description: T07 - Bản ghi bị UPDATE (UpdateTime tăng) sau khi đã xuất được xuất lại đúng 1 lần
        ///              (lượt 2), lượt 3 không gửi lại.
        ///              Nguồn: mốc dùng COALESCE(UpdateTime, CreateTime, DetectTime) trong QueryPacket103.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenExportedRowIsUpdated_ReExportsItExactlyOnce_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T07_{unique}";
            var platePrefix = $"T7{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T07_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T07_{unique}",
                SubCode = $"SUB_T07_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            var rows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 3);

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun1 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);

            await db.Ado.ExecuteCommandAsync(
                "UPDATE TmsTrafficData SET UpdateTime = @t WHERE ID = @id",
                new { t = baseTime.AddMinutes(30), id = rows[0].ID });
            await MakeSubscriptionDue(db, sub.ID);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun2 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);
            var requestsAfterRun2 = partnerServer.TotalReceivedRequests;

            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(3, platesRun1.Count);
            Assert.Equal([rows[0].LicensePlate!], platesRun2);
            Assert.Equal(requestsAfterRun2, partnerServer.TotalReceivedRequests);
        }

        /// <summary>
        /// Description: T08 - Snapshot gói 101 với 150 dòng TmsZoneStatus: payload chứa ĐỦ 150 (QueryPacket101 không có TOP);
        ///              lượt kế tiếp dữ liệu không đổi vẫn gửi lại đủ 150 (snapshot không nối đuôi).
        ///              Nguồn: DataOutboundExtractionProcess.cs QueryPacket101 (không giới hạn), isSnapshot bỏ qua cursor (dòng 113).
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSnapshot101Has150Rows_SendsAllAndResendsNextRun_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zonePrefix = $"ZONE_T08_{unique}_";
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var zones = Enumerable.Range(1, 150).Select(i => new TmsZone
            {
                ID = $"{zonePrefix}{i:D3}",
                Name = $"Tuyến T08 {i}",
                FromKmNumber = 10,
                FromMetNumber = 500,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ToList();
            await db.Insertable(zones).PageSize(100).ExecuteCommandAsync();
            var statuses = zones.Select(z => new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = z.ID,
                AverageSpeed = "65.5",
                Condition = "NORMAL",
                UpdateTime = dbNow
            }).ToList();
            await db.Insertable(statuses).PageSize(100).ExecuteCommandAsync();
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T08_{unique}",
                SubCode = $"SUB_T08_{unique}",
                DatatypeId = "101"
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var zonesRun1 = ExtractZones(partnerServer.LastRequest!.Body, zonePrefix);

            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var zonesRun2 = ExtractZones(partnerServer.LastRequest!.Body, zonePrefix);

            // Assert
            Assert.Equal(150, zonesRun1.Count);
            Assert.Equal(150, zonesRun2.Count);
        }

        /// <summary>
        /// Description: T10 - Gói 110 (policy NotReady): không gọi HTTP, không ném lỗi, kể cả khi có 1 TmsIncident State='1'.
        ///              Hành vi thực tế (DataOutboundService.cs GetExportConfig dòng 292-305): chỉ ghi logger Information,
        ///              KHÔNG ghi ShareDataActivityLog/ShareDataAlertLog nào — khác kỳ vọng "có cảnh báo NotReady" trong prompt
        ///              (cảnh báo NotReady chỉ có ở luồng Tracking, ShareDataAlertCode.Tracking.NotReady). Cần BA chốt.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPacket110IsNotReady_SendsNothingAndDoesNotThrow_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "110");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T10_{unique}",
                SubCode = $"SUB_T10_{unique}",
                DatatypeId = "110"
            });
            await db.Insertable(new TmsIncident { ID = Guid.NewGuid().ToString("N"), State = "1" }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Description: T11 - Không có mapping chiều gửi đang bật: ghi ESH-1304, log cha Fail, không gọi HTTP.
        ///              Nguồn: DataOutboundService.cs GetExportConfig dòng 321-329 (ném ShareDataException MappingNotFound).
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenNoActiveMapping_LogsEsh1304AndSendsNothing_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T11_{unique}",
                SubCode = $"SUB_T11_{unique}",
                DatatypeId = "103"
            });
            await db.Updateable<ShareDataMapping>()
                .SetColumns(m => m.IsActive == false)
                .Where(m => m.PartnerId == partner.ID)
                .ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.MappingNotFound)
                .ToListAsync();
            Assert.NotEmpty(alerts);

            var parent = (await GetLogs(db, sub.ID)).Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);
        }

        /// <summary>
        /// Description: T14 - Header HTTP khi chạy qua pipeline: PartnerCode, PacketCode, SerialNbr và Content-Type
        ///              "application/json; charset=utf-8" đúng giá trị; SerialNbr giữ nguyên sau khi gửi thất bại
        ///              (lượt retry gửi lại cùng giá trị) và tăng thêm 1 sau khi gửi thành công.
        ///              Nguồn: DataOutboundRestSender.AddIdentityHeaders (dòng 93-105); ExportPage tăng SerialNbr sau CommitSuccess (dòng 583).
        ///              Test HttpSender_Send_WithIdentityHeaders… đã phủ riêng bộ gửi; test này phủ toàn pipeline.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_SerialNbrHeaderStaysOnFailureAndAdvancesOnSuccess_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T14_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T14_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T14_{unique}",
                SubCode = $"SUB_T14_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => { s.LastTimeRun = baseTime; s.SerialNbr = 5; }
            });
            await SeedTrafficRows(db, eqId, $"T14{unique}-", baseTime, 2);
            var expectedPacketCode = PacketMetadataCatalogTest.Get("103").Packet.Code;

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var failedRequest = partnerServer.LastRequest!;
            var serialAfterFailure = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).SerialNbr;

            partnerServer.ResetDefaults();
            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var retryRequest = partnerServer.LastRequest!;
            var serialAfterSuccess = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).SerialNbr;

            // Assert
            Assert.Equal(partner.Code, failedRequest.Headers["PartnerCode"]);
            Assert.Equal(expectedPacketCode, failedRequest.Headers["PacketCode"]);
            Assert.Equal("5", failedRequest.Headers["SerialNbr"]);
            Assert.Equal("application/json; charset=utf-8", failedRequest.ContentType);
            Assert.Equal(5, serialAfterFailure);

            Assert.Equal("5", retryRequest.Headers["SerialNbr"]);
            Assert.Equal(6, serialAfterSuccess);
        }

        /// <summary>
        /// Description: T09 - Snapshot gói 105 với 120 dòng TollTransactionIn: payload chứa đủ 120 dòng
        ///              (theo quyết định 14/10/2026: các gói Snapshot gửi full, bỏ trần TOP cứng).
        ///              Nguồn: DataOutboundExtractionProcess.cs QueryPacket105 (không giới hạn TOP).
        /// Created date: 12/10/2026
        /// Modified date: 14/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSnapshot105Has120Rows_SendsAllWithoutTopCeiling_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "105");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var txnPrefix = $"TXN_T09_{unique}_";
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var futureBase = dbNow.AddDays(30);
            var rows = Enumerable.Range(1, 120).Select(i => new TollTransactionIn
            {
                ID = Guid.NewGuid().ToString("N"),
                TransactionId = $"{txnPrefix}{i:D3}",
                TagId = $"TAG_T09_{unique}_{i}",
                PlateEdit = $"T09-{i:D3}",
                VehicleTypeId = "1",
                TransactionDateTime = futureBase.AddSeconds(i),
                LaneId = "LANE_01",
                StationId = "STA_01"
            }).ToList();
            await db.Insertable(rows).PageSize(100).ExecuteCommandAsync();
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T09_{unique}",
                SubCode = $"SUB_T09_{unique}",
                DatatypeId = "105"
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            using var doc = JsonDocument.Parse(partnerServer.LastRequest!.Body);
            var sentIds = doc.RootElement.EnumerateArray()
                .Select(r => r.TryGetProperty("transactionId", out var t) ? t.GetString() : null)
                .Where(t => t != null && t.StartsWith(txnPrefix, StringComparison.Ordinal))
                .ToList();
            Assert.Equal(120, sentIds.Count);
            Assert.Equal(120, sentIds.Distinct().Count());
        }

        /// <summary>
        /// Description: T15 - Đối tác Disable hoặc Disconnected thì đăng ký không được chọn: không HTTP, không log, mốc không đổi.
        ///              Nguồn: DataOutboundService.cs chọn sub theo Status == Enable và SessionState == Connected.
        /// Created date: 12/10/2026
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ProcessSubscriptions_WhenPartnerDisabledOrDisconnected_SkipsSubscription_Test(bool disabled)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T15_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T15_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T15_{unique}",
                SubCode = $"SUB_T15_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime,
                ConfigurePartner = p =>
                {
                    if (disabled)
                        p.Status = BaseEnums.StatusEnum.Disable;
                    else
                        p.SessionState = BaseEnums.SessionState.Disconnected;
                }
            });
            await SeedTrafficRows(db, eqId, $"T15{unique}-", baseTime, 3);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);
            Assert.Empty(await GetLogs(db, sub.ID));
            var subAfter = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(baseTime, subAfter.LastTimeRun!.Value, TimeSpan.FromSeconds(1));
            Assert.Equal(0, subAfter.SerialNbr ?? 0);
            Assert.False(await db.Queryable<ShareDataLastSend>().AnyAsync(c => c.PartnerCode == partner.Code));
        }

        private static List<string> ExtractZones(string body, string zonePrefix)
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.EnumerateArray()
                .Select(r => r.TryGetProperty("zoneId", out var z) ? z.GetString() : null)
                .Where(z => z != null && z.StartsWith(zonePrefix, StringComparison.Ordinal))
                .Select(z => z!)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Description: Khẳng định mốc nối đuôi chưa tiến: Subscription.LastTimeRun và ShareDataLastSend.LastTime
        ///              bằng mốc khởi tạo, LastKey rỗng, SerialNbr chưa tăng.
        /// Created date: 12/10/2026
        /// </summary>
        private static async Task AssertCursorUnchanged(ISqlSugarClient db, string partnerCode, string subId, DateTime expectedTime)
        {
            var sub = await db.Queryable<ShareDataSubscription>().InSingleAsync(subId);
            Assert.NotNull(sub.LastTimeRun);
            Assert.Equal(expectedTime, sub.LastTimeRun!.Value, TimeSpan.FromSeconds(1));
            Assert.True((sub.SerialNbr ?? 0) == 0, $"SerialNbr={sub.SerialNbr}");

            var lastSend = await db.Queryable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).FirstAsync();
            Assert.NotNull(lastSend);
            Assert.Equal(expectedTime, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));
            Assert.Null(lastSend.LastKey);
        }

        /// <summary>
        /// Description: Đưa Subscription về trạng thái Active và đến hạn chạy để worker nhận ở lượt kế tiếp.
        /// Created date: 12/10/2026
        /// </summary>
        private static async Task MakeSubscriptionDue(ISqlSugarClient db, string subId)
        {
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .SetColumns(s => s.NextTimeRun == dbNow.AddSeconds(-10))
                .Where(s => s.ID == subId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Lấy danh sách biển số mang đúng tiền tố từ body JSON gửi tới đối tác.
        /// Created date: 12/10/2026
        /// </summary>
        private static List<string> ExtractPlates(string body, string platePrefix)
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.EnumerateArray()
                .Select(r => r.TryGetProperty("licensePlate", out var lp) ? lp.GetString() : null)
                .Where(p => p != null && p.StartsWith(platePrefix, StringComparison.Ordinal))
                .Select(p => p!)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Description: F3 - Gói Single khi gửi thất bại (HTTP 500) không được bị chuyển sang Expired
        ///              mà phải giữ Active và có NextTimeRun để thử lại ở chu kỳ tiếp theo.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSingleModeHttpFails500_ShouldNotBecomeExpired_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
            partnerServer.DefaultResponseBody = "Mock 500 Error";

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_F3_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_F3_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_F3_{unique}",
                SubCode = $"SUB_F3_{unique}",
                DatatypeId = "103",
                ConfigureSub = s =>
                {
                    s.Mode = BaseEnums.SubMode.Single;
                    s.LastTimeRun = baseTime;
                }
            });
            await SeedTrafficRows(db, eqId, $"F3{unique}-", baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var reloadedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(reloadedSub);
            // KỲ VỌNG: Gửi lỗi thì KHÔNG ĐƯỢC Expired! Phải giữ Active để retry!
            Assert.Equal(BaseEnums.SubSubscriptionState.Active, reloadedSub.State);
            Assert.NotNull(reloadedSub.NextTimeRun);
        }

        /// <summary>
        /// Description: F4 - Kịch bản At-least-once: Khi gửi REST thành công nhưng mất lock lúc CommitSuccess
        ///              (do timeout/OCC trượt), dòng log cha báo Fail, cursor KHÔNG tiến, SerialNbr KHÔNG tăng.
        ///              Ở lượt quét tiếp theo, Worker tự động gửi lại đúng trang đó với cùng SerialNbr để đối tác nhận diện idempotent.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenLockLostAtCommit_KeepsCursorAndResendsWithSameSerialNbr_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_F4_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_F4_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_F4_{unique}",
                SubCode = $"SUB_F4_{unique}",
                DatatypeId = "103",
                ConfigureSub = s =>
                {
                    s.LastTimeRun = baseTime;
                    s.SerialNbr = 10;
                }
            });
            await SeedTrafficRows(db, eqId, $"F4{unique}-", baseTime, 5);

            using var stealDb = db.CopyNew();
            var callIndex = 0;
            partnerServer.Responder = (call, req) =>
            {
                callIndex++;
                if (callIndex == 1)
                {
                    // Lượt 1: Phá ProcessingUntil trong DB để CommitSuccess bị trượt OCC
                    stealDb.Updateable<ShareDataSubscription>()
                        .SetColumns(s => s.ProcessingUntil == DateTime.Now.AddDays(-1))
                        .Where(s => s.ID == sub.ID)
                        .ExecuteCommand();
                }
                return new PartnerResponsePlan(HttpStatusCode.OK, "{\"success\":true}");
            };

            // Act 1: Lượt đầu tiên chạy -> Gửi REST OK nhưng CommitSuccess thất bại do mất lock
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 1:
            // 1. Đối tác đã nhận 1 request
            Assert.Equal(1, partnerServer.TotalReceivedRequests);
            var req1 = partnerServer.ReceivedRequests[0];

            // 2. Log cha ghi nhận Fail (mất lock khi commit)
            var logs = await GetLogs(db, sub.ID);
            var parent = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);
            Assert.Contains("Mất lock", parent.ErrorMessage ?? string.Empty);

            // 3. Mốc cursor KHÔNG tiến, SerialNbr KHÔNG tăng trong DB
            var subAfterFail = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(10, subAfterFail.SerialNbr);
            Assert.Equal(baseTime, subAfterFail.LastTimeRun!.Value, TimeSpan.FromSeconds(1));

            // Act 2: Cho Subscription đến hạn chạy lại ở lượt quét kế tiếp
            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 2:
            // 4. Đối tác nhận thêm request thứ 2 (resend)
            Assert.Equal(2, partnerServer.TotalReceivedRequests);
            var req2 = partnerServer.ReceivedRequests[1];

            // 5. Cả 2 request gửi sang đều có CÙNG SerialNbr header = 10 (chứng minh tính Idempotent)
            var serial1 = req1.Headers.TryGetValue("SerialNbr", out var s1) ? s1 : null;
            var serial2 = req2.Headers.TryGetValue("SerialNbr", out var s2) ? s2 : null;
            Assert.NotNull(serial1);
            Assert.Equal("10", serial1);
            Assert.Equal(serial1, serial2);

            // 6. Dữ liệu gửi đi ở 2 lượt giống hệt nhau
            var plates1 = ExtractPlates(req1.Body, $"F4{unique}-");
            var plates2 = ExtractPlates(req2.Body, $"F4{unique}-");
            Assert.Equal(5, plates1.Count);
            Assert.Equal(plates1, plates2);

            // 7. Ở lượt 2 thành công, Cursor đã tiến lên và SerialNbr trong DB đã tăng từ 10 lên 11
            var subAfterSuccess = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(11, subAfterSuccess.SerialNbr);
            Assert.True(subAfterSuccess.LastTimeRun > baseTime);
        }

        /// <summary>
        /// Description: F5 - Kiểm tra hành vi hiện tại: Khi gửi HTTP fail 500, fileSender vẫn chạy và ghi tệp ra đĩa
        ///              nhưng toàn bộ lượt gửi bị coi là thất bại, cursor không tiến.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenHttpFails500_FileSenderStillWritesFile_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
            partnerServer.DefaultResponseBody = "Mock 500 Error";

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_F5_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_F5_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_F5_{unique}",
                SubCode = $"SUB_F5_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            await SeedTrafficRows(db, eqId, $"F5{unique}-", baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            // 1. HTTP gửi thất bại -> Log cha là Fail
            var logs = await GetLogs(db, sub.ID);
            var parent = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);

            // 2. Cursor không tiến
            await AssertCursorUnchanged(db, partner.Code, sub.ID, baseTime);

            // 3. NHƯNG File vẫn bị ghi ra đĩa dù HTTP thất bại! (Hành vi F5)
            Assert.False(string.IsNullOrWhiteSpace(parent.FilePath), "FilePath trong log không được rỗng");
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", parent.FilePath!);
            Assert.True(File.Exists(fullPath), $"File [{fullPath}] vẫn bị ghi ra đĩa dù HTTP fail");
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        /// <summary>
        /// Description: TC2 - Sửa cấu hình đối tác giữa 2 kỳ chạy thì worker tự gửi bù đúng các bản ghi cũ và tiến cursor.
        ///              - Kỳ 1: Gửi HTTP lỗi (ClosedPort hoặc AbortOnCall). Log cha ghi nhận Fail, cảnh báo HttpSendFailed,
        ///                ErrorMessage chứa URL đã gọi, file vẫn được lưu đệm (bằng chứng), LastTimeRun / SerialNbr / ShareDataLastSend không đổi.
        ///              - Giữa 2 kỳ: Sửa cấu hình đối tác (Port/phục hồi mạng), ResetDefaults và kích hoạt kỳ chạy tiếp theo (MakeSubscriptionDue).
        ///              - Kỳ 2: Worker tự gửi bù đúng 5 bản ghi cũ, mock nhận đủ 5 biển số, header PartnerCode / PacketCode đúng,
        ///                header SerialNbr bằng giá trị trước kỳ 1 (không tăng khi lỗi). Sau kỳ 2: LastTimeRun tiến lên, SerialNbr tăng 1, log bước gửi thành công.
        /// Created date: 15/10/2026
        /// </summary>
        [Theory]
        [InlineData("ClosedPort")]
        [InlineData("AbortOnCall")]
        public async Task ProcessSubscriptions_WhenPartnerConnectionFixedBetweenRuns_ResendsSameRecordsAndAdvancesCursor_Test(string failureMode)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            if (failureMode == "AbortOnCall")
            {
                partnerServer.AbortOnCall.Add(1);
            }

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_TC2_{unique}";
            var platePrefix = $"TC2{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_TC2_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_TC2_{unique}",
                SubCode = $"SUB_TC2_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => { s.LastTimeRun = baseTime; s.SerialNbr = 0; },
                ConfigurePartner = p =>
                {
                    if (failureMode == "ClosedPort")
                    {
                        p.Port = ShareDataPartnerServerMock.ClosedPort;
                    }
                }
            });

            var seededRows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 5);
            var expectedPlates = seededRows.Select(r => r.LicensePlate!).OrderBy(p => p, StringComparer.Ordinal).ToList();
            var expectedPacketCode = PacketMetadataCatalogTest.Get("103").Packet.Code;

            var subBefore = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            var initialLastTime = subBefore.LastTimeRun;
            var initialSerial = subBefore.SerialNbr ?? 0;

            // Act 1: Kỳ 1 chạy khi kết nối đối tác lỗi
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 1:
            var logsRun1 = await GetLogs(db, sub.ID);
            var parentRun1 = logsRun1.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parentRun1.Success);

            // Cảnh báo HttpSendFailed
            var alertsRun1 = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                .ToListAsync();
            Assert.NotEmpty(alertsRun1);

            // ErrorMessage chứa URL đã gọi
            var expectedUrlPort = failureMode == "ClosedPort" ? ShareDataPartnerServerMock.ClosedPort : ShareDataPartnerServerMock.DefaultPort;
            Assert.Contains(expectedUrlPort.ToString(), parentRun1.ErrorMessage);

            // File vẫn được lưu đệm/xuất (log có FilePath)
            Assert.False(string.IsNullOrWhiteSpace(parentRun1.FilePath));
            var fullPathRun1 = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", parentRun1.FilePath!);
            Assert.True(File.Exists(fullPathRun1), $"File [{fullPathRun1}] phải được lưu đệm trước khi gửi");
            if (File.Exists(fullPathRun1)) File.Delete(fullPathRun1);

            // LastTimeRun, SerialNbr, ShareDataLastSend KHÔNG đổi
            await AssertCursorUnchanged(db, partner.Code, sub.ID, baseTime);

            // 3. Mô phỏng sửa cấu hình đối tác giữa 2 kỳ chạy
            if (failureMode == "ClosedPort")
            {
                await db.Updateable<ShareDataPartner>()
                    .SetColumns(p => p.Port == ShareDataPartnerServerMock.DefaultPort)
                    .Where(p => p.ID == partner.ID)
                    .ExecuteCommandAsync();
            }
            partnerServer.ResetDefaults();
            await MakeSubscriptionDue(db, sub.ID);

            // Act 2: Kỳ 2 chạy sau khi sửa cấu hình
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 2:
            // Mock nhận đúng 5 biển số đã seed
            var receivedPlates = partnerServer.ReceivedRequests.SelectMany(r => ExtractPlates(r.Body, platePrefix)).ToList();
            Assert.Equal(5, receivedPlates.Count);
            Assert.Equal(expectedPlates, receivedPlates);

            // Headers đúng: PartnerCode, PacketCode, SerialNbr bằng giá trị trước kỳ 1
            var successfulRequest = partnerServer.LastRequest!;
            Assert.Equal(partner.Code, successfulRequest.Headers["PartnerCode"]);
            Assert.Equal(expectedPacketCode, successfulRequest.Headers["PacketCode"]);
            Assert.Equal(initialSerial.ToString(), successfulRequest.Headers["SerialNbr"]);

            // Sau kỳ 2: LastTimeRun tiến lên, SerialNbr tăng 1, log gửi thành công
            var subAfterRun2 = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.True(subAfterRun2.LastTimeRun > initialLastTime, "LastTimeRun phải tiến lên sau kỳ gửi thành công");
            Assert.Equal(initialSerial + 1, subAfterRun2.SerialNbr);

            var logsRun2 = await GetLogs(db, sub.ID);
            var parentRun2 = logsRun2.Where(l => l.ParentId == null).OrderByDescending(l => l.OccurredAt).First();
            Assert.Equal(BaseEnums.SuccessEnums.Success, parentRun2.Success);
            Assert.Equal(5, parentRun2.RecordCount);

            if (!string.IsNullOrWhiteSpace(parentRun2.FilePath))
            {
                var fullPathRun2 = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", parentRun2.FilePath!);
                if (File.Exists(fullPathRun2)) File.Delete(fullPathRun2);
            }
        }

        #endregion
    }
}
