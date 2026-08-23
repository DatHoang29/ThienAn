using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.VMS.Core.Entities;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Core.Entities;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Exceptions;
using ShareDataWorker.Core.Utils;
using ShareDataWorker.Infrastructure.Services.DataExport;

namespace Tests.Modules.ShareData.Infrastructure.Services.DataExport
{
    /// <summary>
    /// Lớp chứa tất cả các kịch bản kiểm thử Integration Test cho DataExportService thuộc module ShareDataWorker.
    /// Hoạt động trực tiếp trên cơ sở dữ liệu Test Local, kiểm tra toàn diện luồng quét Subscription,
    /// sinh SQL động từ ShareDataPacket + ShareDataTable, áp dụng phễu lọc ShareDataMapping và đóng gói PDU.
    /// Author: Đạt
    /// Created date: 31/07/2026
    /// </summary>
    [Collection("api")]
    public partial class DataExportServiceTests(Host host)
    {
        private readonly Host _host = host;

        private static readonly Dictionary<ShareDataEnum.DatatypeIdEnum, string[]> ExpectedPacketFields = new()
        {
            [ShareDataEnum.DatatypeIdEnum.TrafficFlow] = ["zoneId", "zoneName", "fromLocationKm", "fromLocationMet", "toLocationKm", "toLocationMet", "laneId", "averageSpeed", "trafficCondition", "dataTime", "speedLimit", "vehicleCount"],
            [ShareDataEnum.DatatypeIdEnum.CctvImage] = ["cameraCode", "cameraName", "snapshot", "snapshotTime", "deviceState", "locationKm", "locationMet", "direction"],
            [ShareDataEnum.DatatypeIdEnum.VehicleDetection] = ["detectionId", "detectTime", "vehicleType", "licensePlate", "speed", "lane", "direction", "locationRoute", "equipmentId", "locationKm", "locationMet"],
            [ShareDataEnum.DatatypeIdEnum.Weather] = ["weatherStationId", "locationDetail", "temperature", "humidity", "windSpeed", "windDirection", "rainfall", "rainfallHour", "visibility", "weatherDescription", "weatherCode", "detectTime"],
            [ShareDataEnum.DatatypeIdEnum.VehicleIdentification] = ["transactionId", "tagId", "licensePlate", "vehicleTypeId", "entryTime", "exitTime", "laneId", "stationId", "vehicleBrand", "vehicleOwner"],
            [ShareDataEnum.DatatypeIdEnum.WeighInMotion] = ["detectTime", "lane", "locationCode", "speed", "height", "width", "length"],
            [ShareDataEnum.DatatypeIdEnum.TrafficIncident] = ["incidentCode", "incidentName", "eventTypeId", "eventTypeName", "occurredTime", "locationKm", "locationMet", "locationRoute", "direction", "injuredCount", "vehicleCount", "incidentState", "description", "source"],
            [ShareDataEnum.DatatypeIdEnum.VmsDisplay] = ["equipmentCode", "vmsName", "locationKm", "locationMet", "direction", "laneId", "displayContent", "displayImageUrl", "displaySize", "priority", "executedTime"],
            [ShareDataEnum.DatatypeIdEnum.TollCollection] = ["transactionId", "entryTime", "exitTime", "vehicleTypeId", "licensePlate", "tagId", "laneId", "laneName", "stationId", "stationName", "tollPrice", "syncTime"],
            [ShareDataEnum.DatatypeIdEnum.PublicMessaging] = ["incidentMessage", "guidanceContent", "locationKm", "locationMet", "publishedTime"],
            [ShareDataEnum.DatatypeIdEnum.InterCenterExchange] = ["incidentCode", "incidentName", "locationKm", "locationMet", "description"]
        };

        public static IEnumerable<object[]> AllPacketsData =>
            Enum.GetValues<ShareDataEnum.DatatypeIdEnum>()
                .Where(e => (int)e >= 101 && (int)e <= 111)
                .Select(e => new object[] { e });

        private static async Task AssertPacketJsonSchema(
            ISqlSugarClient db,
            string subscriptionId,
            ShareDataEnum.DatatypeIdEnum datatypeEnum,
            string[]? expectedFields = null)
        {
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subscriptionId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.True(logs[0].Status == "SUCCESS", $"Export failed. DB ErrorMessage: {logs[0].ErrorMessage}");
            Assert.True(logs[0].RecordCount > 0);

            Assert.False(string.IsNullOrEmpty(logs[0].FilePath), "FilePath của ExportLog không được để rỗng");
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"File kết xuất không tồn tại tại đường dẫn: {fullPath}");

            var jsonContent = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            Assert.True(root.TryGetProperty("pduType", out _), "ISO 14827 PDU Envelope phải chứa thuộc tính pduType");
            Assert.True(root.TryGetProperty("hash", out _), "ISO 14827 PDU Envelope phải chứa thuộc tính hash");
            Assert.True(root.TryGetProperty("payload", out var payloadElement), "ISO 14827 PDU Envelope phải chứa thuộc tính payload");
            Assert.Equal(JsonValueKind.Array, payloadElement.ValueKind);
            Assert.True(payloadElement.GetArrayLength() > 0, "JSON payload không được chứa mảng rỗng");

            var firstRecord = payloadElement[0];
            Assert.True(firstRecord.EnumerateObject().Any(), $"Gói tin {datatypeEnum} không trả về thuộc tính JSON nào!");

            if (expectedFields != null)
            {
                foreach (var fieldName in expectedFields)
                {
                    Assert.True(
                        firstRecord.TryGetProperty(fieldName, out _),
                        $"Gói tin {datatypeEnum} thiếu field JSON [{fieldName}] trong payload JSON trả về!"
                    );
                }
            }
        }

        private static async Task<(ShareDataPartner Partner, ShareDataSubscription Subscription)> SeedOutboundSubscription(
            ISqlSugarClient db,
            string partnerCode,
            string subCode,
            string datatypeId,
            Action<ShareDataSubscription>? configureSub = null)
        {
            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = partnerCode,
                Name = $"Partner {partnerCode}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = subCode,
                PartnerId = partner.ID,
                DatatypeId = datatypeId,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
                IntervalSeconds = 30
            };

            configureSub?.Invoke(sub);
            await db.Insertable(sub).ExecuteCommandAsync();

            return (partner, sub);
        }

        private static async Task SeedTestDataForPacket(ISqlSugarClient db, ShareDataEnum.DatatypeIdEnum datatypeEnum, string uniqueId)
        {
            var now = DateTime.Now;

            switch (datatypeEnum)
            {
                case ShareDataEnum.DatatypeIdEnum.TrafficFlow:
                    var zoneId = $"ZONE_{uniqueId}";
                    await db.Insertable(new TmsZone
                    {
                        ID = zoneId,
                        Name = $"Tuyến Test {uniqueId}",
                        FromKmNumber = 10,
                        FromMetNumber = 500,
                        ToKmNumber = 20,
                        ToMetNumber = 0,
                        LaneId = "LANE_1",
                        MaxSpeed = 80
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsZoneStatus
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        ZoneId = zoneId,
                        AverageSpeed = "65.5",
                        Condition = "NORMAL",
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficStatistic
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        ZoneId = zoneId,
                        TotalVehicleNumber = 150
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.CctvImage:
                    var eqIdCctv = $"EQ_CCTV_{uniqueId}";
                    var ip = $"192.168.1.{Random.Shared.Next(10, 250)}";
                    await db.Insertable(new TmsEquipment
                    {
                        ID = eqIdCctv,
                        Code = $"CAM_EQ_{uniqueId}",
                        Ip = ip,
                        KmNumber = 15,
                        MetNumber = 200,
                        DirectionId = 1
                    }).ExecuteCommandAsync();
                    await db.Insertable(new CctvDevice
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        DeviceId = $"DEV_{uniqueId}",
                        Name = $"Camera Test {uniqueId}",
                        Ip = ip,
                        SnapshotUrl = "data:image/jpeg;base64,sample_snapshot",
                        SnapshotTime = now,
                        DeviceState = 1
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.VehicleDetection:
                case ShareDataEnum.DatatypeIdEnum.WeighInMotion:
                    var eqIdVds = $"EQ_VDS_{uniqueId}";
                    await db.Insertable(new TmsEquipment
                    {
                        ID = eqIdVds,
                        Code = $"VDS_EQ_{uniqueId}",
                        KmNumber = 30,
                        MetNumber = 0
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficData
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        EquipmentId = eqIdVds,
                        DetectTime = now,
                        Type = "CAR",
                        LicensePlate = "30A-99999",
                        Speed = 75.0f,
                        Lane = "L1",
                        Direction = "NORTH",
                        Location = "KM30",
                        Height = 150,
                        Width = 180,
                        Length = 450
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.Weather:
                    await db.Insertable(new TmsWeather
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        RefId = $"WS_{uniqueId}",
                        LocationDetail = "Trạm Thời Tiết Km45",
                        Temperature = 28.5f,
                        Hudmidity = 75.0f,
                        WindSpeed = 12.0f,
                        WindDirection = "NE",
                        Rain = 0.0f,
                        RainHour = 0.0f,
                        Foresight = 10.0f,
                        Description = "Trời quang",
                        ShortDescription = "CLEAR",
                        TimeDetect = now
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.VehicleIdentification:
                    await db.Insertable(new TollTransactionOut
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        TransactionId = $"TXN_{uniqueId}",
                        TagId = $"TAG_{uniqueId}",
                        PlateEdit = "30A-12345",
                        VehicleTypeId = "1",
                        TransactionDateTimeIn = now.AddMinutes(-30),
                        TransactionDateTime = now,
                        LaneId = "LANE_01",
                        StationId = "STA_01",
                        SyncTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsVehicleRegistration
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        LicensePlate = "30A-12345",
                        Brand = "Toyota",
                        Owner = "Nguyen Van A"
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.TrafficIncident:
                    var eventTypeId = $"ET_{uniqueId}";
                    await db.Insertable(new TmsEventType
                    {
                        ID = eventTypeId,
                        Name = "Va chạm giao thông"
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsIncident
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        Code = $"INC_{uniqueId}",
                        Name = "Sự cố giao thông",
                        EventTypeId = eventTypeId,
                        StartDate = now.AddHours(-1),
                        KmNumber = 55,
                        MetNumber = 500,
                        Location = "KM55+500",
                        InfluenceScope = "1",
                        InjuredNumber = 0,
                        VehicleNumber = 2,
                        State = ShareDataEnum.IncidentState.InProgress,
                        Description = "Va chạm nhẹ 2 xe ô tô con",
                        Source = "CCTV",
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.VmsDisplay:
                    var eqIdVms = $"EQ_VMS_{uniqueId}";
                    await db.Insertable(new TmsEquipment
                    {
                        ID = eqIdVms,
                        Code = $"VMS_EQ_{uniqueId}",
                        KmNumber = 70,
                        MetNumber = 0,
                        DirectionId = 1,
                        LaneId = "LANE_ALL"
                    }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        EquipmentId = eqIdVms,
                        Name = $"Biển Báo VMS Km70 {uniqueId}",
                        RowData = "CHU Y LAI XE AN TOAN",
                        Url = "http://sample.vms/preview.png",
                        Size = "192x64",
                        Priority = 1,
                        ExecutedDate = now
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.TollCollection:
                    await db.Insertable(new TollTransactionOut
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        TransactionId = $"TOLL_{uniqueId}",
                        TransactionDateTimeIn = now.AddMinutes(-20),
                        TransactionDateTime = now,
                        VehicleTypeId = "1",
                        PlateLpr = "29A-88888",
                        TagId = $"TAG_{uniqueId[..8]}",
                        LaneId = "LANE_ETC_01",
                        StationId = "STA_ETC_01",
                        SyncTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TollLane
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        LaneId = "LANE_ETC_01",
                        Name = "Làn ETC 01"
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TollStation
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        StationId = "STA_ETC_01",
                        Name = "Trạm Thu Phí Km10"
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.PublicMessaging:
                    var eqIdPm = $"EQ_PM_{uniqueId}";
                    await db.Insertable(new TmsIncident
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        Code = $"INC_PM_{uniqueId}",
                        Name = "Cảnh báo sương mù",
                        StartDate = now,
                        KmNumber = 80,
                        MetNumber = 0,
                        State = ShareDataEnum.IncidentState.InProgress,
                        Description = "Sương mù dày đặc tầm nhìn giảm",
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsEquipment
                    {
                        ID = eqIdPm,
                        Code = $"EQ_PM_{uniqueId}",
                        KmNumber = 80,
                        MetNumber = 0
                    }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        EquipmentId = eqIdPm,
                        RowData = "SUONG MU DAY - GIAM TOC DO",
                        ExecutedDate = now
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.InterCenterExchange:
                    await db.Insertable(new TmsIncident
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        Code = $"INC_IC_{uniqueId}",
                        Name = "Sự cố liên trung tâm",
                        KmNumber = 95,
                        MetNumber = 100,
                        Description = "Thông báo điều phối xe cứu hộ",
                        State = ShareDataEnum.IncidentState.InProgress,
                        StartDate = now,
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    break;
            }
        }

        private static DataExportService CreateWorker(IServiceScope scope)
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            return new DataExportService(scopeFactory, logger, config);
        }

        private static async Task<List<ShareDataActivityLog>> GetLogs(ISqlSugarClient db, string subId)
        {
            return await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();
        }

        private static async Task<string> ReadExportedJson(string relativePath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", relativePath);
            return await File.ReadAllTextAsync(fullPath);
        }

        [Theory]
        [MemberData(nameof(AllPacketsData))]
        public async Task AllPackets101To111_ExportCompleteJson_WithAllFields_Test(ShareDataEnum.DatatypeIdEnum datatypeEnum)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packetCode = ((int)datatypeEnum).ToString();
            await PacketMetadataCatalogTest.SeedPacketToDb(db, packetCode);

            var uniqueId = Guid.NewGuid().ToString("N");
            await SeedTestDataForPacket(db, datatypeEnum, uniqueId);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"TEST_P_META_{packetCode}",
                $"TEST_SUB_META_{packetCode}",
                packetCode
            );

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var expectedFields = ExpectedPacketFields.GetValueOrDefault(datatypeEnum);
            await AssertPacketJsonSchema(db, sub.ID, datatypeEnum, expectedFields);
        }

        [Theory]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TrafficFlow)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.CctvImage)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TrafficIncident)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TollCollection)]
        public async Task QueryPackets_WhenJoinedTableDataMissing_ReturnsDataSafelyWithNulls_Test(ShareDataEnum.DatatypeIdEnum datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packetCode = ((int)datatypeId).ToString();
            await PacketMetadataCatalogTest.SeedPacketToDb(db, packetCode);

            var expectedNullSubstrings = datatypeId switch
            {
                ShareDataEnum.DatatypeIdEnum.TrafficFlow => await SeedTrafficFlow(),
                ShareDataEnum.DatatypeIdEnum.CctvImage => await SeedCctvImage(),
                ShareDataEnum.DatatypeIdEnum.TrafficIncident => await SeedTrafficIncident(),
                ShareDataEnum.DatatypeIdEnum.TollCollection => await SeedTollCollection(),
                _ => throw new ArgumentOutOfRangeException(nameof(datatypeId))
            };

            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_LJ_{packetCode}", $"SUB-LJ-{packetCode}-01", packetCode);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.True(logs[0].RecordCount > 0);

            if (!string.IsNullOrEmpty(logs[0].FilePath))
            {
                var jsonContent = await ReadExportedJson(logs[0].FilePath!);
                foreach (var expectedNull in expectedNullSubstrings)
                {
                    Assert.Contains(expectedNull, jsonContent);
                }
            }

            async Task<string[]> SeedTrafficFlow()
            {
                var zoneId = Guid.NewGuid().ToString("N");
                await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_LJ_101", FromKmNumber = 20, MaxSpeed = 100 }).ExecuteCommandAsync();
                await db.Insertable(new TmsZoneStatus { ID = Guid.NewGuid().ToString("N"), ZoneId = zoneId, AverageSpeed = "72.0", Condition = "SLOW", UpdateTime = DateTime.Now }).ExecuteCommandAsync();
                return ["\"vehicleCount\":null"];
            }

            async Task<string[]> SeedCctvImage()
            {
                await db.Insertable(new CctvDevice { ID = Guid.NewGuid().ToString("N"), DeviceId = "CAM_DEV_NO_EQ", Name = "TEST_CCTV_NO_EQ", SnapshotUrl = "base64_test", SnapshotTime = DateTime.Now, DeviceState = 1, Ip = "10.99.99.99" }).ExecuteCommandAsync();
                return ["\"cameraCode\":null", "\"locationKm\":null"];
            }

            async Task<string[]> SeedTrafficIncident()
            {
                await db.Insertable(new TmsIncident
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = "TEST_INC_107_LJ",
                    Name = "Sự cố test",
                    EventTypeId = "NON_EXIST_ET",
                    StartDate = DateTime.Now,
                    KmNumber = 50,
                    State = ShareDataEnum.IncidentState.InProgress,
                    UpdateTime = DateTime.Now
                }).ExecuteCommandAsync();
                return ["\"eventTypeName\":null"];
            }

            async Task<string[]> SeedTollCollection()
            {
                await db.Insertable(new TollTransactionOut
                {
                    ID = Guid.NewGuid().ToString("N"),
                    TransactionId = "TXN_109_LJ",
                    TransactionDateTime = DateTime.Now,
                    VehicleTypeId = "2",
                    Plate = "30A-11111",
                    TagId = "TAG_LJ",
                    LaneId = "LANE_NOT_EXIST",
                    StationId = "STATION_NOT_EXIST",
                    SyncTime = DateTime.Now
                }).ExecuteCommandAsync();
                return ["\"laneName\":null", "\"stationName\":null"];
            }
        }

        [Theory]
        [InlineData(ShareDataEnum.DatatypeIdEnum.VehicleDetection)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.Weather)]
        public async Task QueryIncrementalPackets_WithLastTimeRun_FiltersOlderRecords_Test(ShareDataEnum.DatatypeIdEnum datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packetCode = ((int)datatypeId).ToString();
            await PacketMetadataCatalogTest.SeedPacketToDb(db, packetCode);

            var (oldMarker, newMarker) = datatypeId switch
            {
                ShareDataEnum.DatatypeIdEnum.VehicleDetection => await SeedVds(),
                ShareDataEnum.DatatypeIdEnum.Weather => await SeedWeather(),
                _ => throw new ArgumentOutOfRangeException(nameof(datatypeId))
            };

            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_INCR_{packetCode}", $"SUB-INCR-{packetCode}-01",
                packetCode, s => s.LastTimeRun = DateTime.Now.AddHours(-1));

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var jsonContent = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains(newMarker, jsonContent);
            Assert.DoesNotContain(oldMarker, jsonContent);

            async Task<(string OldMarker, string NewMarker)> SeedVds()
            {
                var eqId = $"EQ_VDS_INCR_{Guid.NewGuid():N}";
                await db.Insertable(new TmsEquipment { ID = eqId, Code = "VDS_INCR", KmNumber = 10 }).ExecuteCommandAsync();
                await db.Insertable(new TmsTrafficData { ID = Guid.NewGuid().ToString("N"), EquipmentId = eqId, DetectTime = DateTime.Now.AddHours(-2), LicensePlate = "29A-OLD-01", Speed = 50 }).ExecuteCommandAsync();
                await db.Insertable(new TmsTrafficData { ID = Guid.NewGuid().ToString("N"), EquipmentId = eqId, DetectTime = DateTime.Now, LicensePlate = "29A-NEW-01", Speed = 80 }).ExecuteCommandAsync();
                return ("29A-OLD-01", "29A-NEW-01");
            }

            async Task<(string OldMarker, string NewMarker)> SeedWeather()
            {
                await db.Insertable(new TmsWeather { ID = Guid.NewGuid().ToString("N"), RefId = "WS_OLD", LocationDetail = "OLD_STATION", TimeDetect = DateTime.Now.AddHours(-2) }).ExecuteCommandAsync();
                await db.Insertable(new TmsWeather { ID = Guid.NewGuid().ToString("N"), RefId = "WS_NEW", LocationDetail = "NEW_STATION", TimeDetect = DateTime.Now }).ExecuteCommandAsync();
                return ("OLD_STATION", "NEW_STATION");
            }
        }

        [Theory]
        [InlineData(ShareDataEnum.DatatypeIdEnum.CctvImage)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.VmsDisplay)]
        public async Task QueryAllSnapshotPackets_WithLastTimeRun_StillReturnsAllRecords_Test(ShareDataEnum.DatatypeIdEnum datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packetCode = ((int)datatypeId).ToString();
            await PacketMetadataCatalogTest.SeedPacketToDb(db, packetCode);

            var (marker1, marker2) = datatypeId switch
            {
                ShareDataEnum.DatatypeIdEnum.CctvImage => await SeedSnapshotCctv(),
                ShareDataEnum.DatatypeIdEnum.VmsDisplay => await SeedSnapshotVms(),
                _ => throw new ArgumentOutOfRangeException(nameof(datatypeId))
            };

            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_SNAP_{packetCode}", $"SUB-SNAP-{packetCode}-01",
                packetCode, s => s.LastTimeRun = DateTime.Now.AddHours(1));

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var jsonContent = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains(marker1, jsonContent);
            Assert.Contains(marker2, jsonContent);

            async Task<(string Marker1, string Marker2)> SeedSnapshotCctv()
            {
                var ip1 = $"10.0.1.{Random.Shared.Next(10, 250)}";
                var ip2 = $"10.0.2.{Random.Shared.Next(10, 250)}";
                await db.Insertable(new CctvDevice { ID = Guid.NewGuid().ToString("N"), DeviceId = "CAM_SNAP_1", Name = "CAM_SNAP_ONE", SnapshotUrl = "snap1", Ip = ip1 }).ExecuteCommandAsync();
                await db.Insertable(new CctvDevice { ID = Guid.NewGuid().ToString("N"), DeviceId = "CAM_SNAP_2", Name = "CAM_SNAP_TWO", SnapshotUrl = "snap2", Ip = ip2 }).ExecuteCommandAsync();
                return ("CAM_SNAP_ONE", "CAM_SNAP_TWO");
            }

            async Task<(string Marker1, string Marker2)> SeedSnapshotVms()
            {
                var eq1 = Guid.NewGuid().ToString("N");
                var eq2 = Guid.NewGuid().ToString("N");
                await db.Insertable(new TmsEquipment { ID = eq1, Code = "VMS_EQ1", KmNumber = 10 }).ExecuteCommandAsync();
                await db.Insertable(new TmsEquipment { ID = eq2, Code = "VMS_EQ2", KmNumber = 20 }).ExecuteCommandAsync();
                await db.Insertable(new VmsCurrent { ID = Guid.NewGuid().ToString("N"), EquipmentId = eq1, Name = "VMS_SNAP_ONE", RowData = "MSG1" }).ExecuteCommandAsync();
                await db.Insertable(new VmsCurrent { ID = Guid.NewGuid().ToString("N"), EquipmentId = eq2, Name = "VMS_SNAP_TWO", RowData = "MSG2" }).ExecuteCommandAsync();
                return ("VMS_SNAP_ONE", "VMS_SNAP_TWO");
            }
        }

        [Fact]
        public async Task QueryPacket110_WithMultipleVmsRows_DoesNotMultiplyRecords_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "110");

            var kmNumber = 120;
            var incidentName = $"SỰ CỐ KHÔNG NHÂN BẢN {Guid.NewGuid():N}";
            await db.Insertable(new TmsIncident
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "TEST_INC_110_NOMULTI",
                Name = incidentName,
                StartDate = DateTime.Now,
                KmNumber = kmNumber,
                MetNumber = 0,
                State = ShareDataEnum.IncidentState.InProgress,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            for (var i = 1; i <= 3; i++)
            {
                var eqId = Guid.NewGuid().ToString("N");
                await db.Insertable(new TmsEquipment { ID = eqId, Code = $"EQ_110_VMS_{i}", KmNumber = kmNumber, MetNumber = 0 }).ExecuteCommandAsync();
                await db.Insertable(new VmsCurrent { ID = Guid.NewGuid().ToString("N"), EquipmentId = eqId, Name = $"VMS_{i}", RowData = $"ROW_{i}", ExecutedDate = DateTime.Now }).ExecuteCommandAsync();
            }

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P110_NOMULTI", "SUB-110-NOMULTI-01", "110");

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            using var doc = JsonDocument.Parse(json);
            var matched = doc.RootElement.GetProperty("payload").EnumerateArray()
                .Where(r => r.TryGetProperty("incidentMessage", out var msg) && (msg.GetString() ?? string.Empty).Contains(incidentName))
                .ToList();

            Assert.Single(matched);
        }

        [Fact]
        public async Task QueryPacket110_WhenIncidentClosed_IsExcludedFromPayload_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "110");

            var kmNumber = 130;
            
            // 1. Incident InProgress
            var activeIncidentCode = "INC_ACTIVE_110";
            await db.Insertable(new TmsIncident
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = activeIncidentCode,
                Name = "Sự cố đang mở",
                StartDate = DateTime.Now.AddHours(-1),
                KmNumber = kmNumber,
                MetNumber = 0,
                State = ShareDataEnum.IncidentState.InProgress,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // 2. Incident Finished
            var closedIncidentCode = "INC_CLOSED_110";
            await db.Insertable(new TmsIncident
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = closedIncidentCode,
                Name = "Sự cố đã đóng",
                StartDate = DateTime.Now.AddHours(-2),
                KmNumber = kmNumber,
                MetNumber = 0,
                State = "FINISHED", // Using exact string from ExtraWhere, because ShareDataEnum.IncidentState.Finished is "4"
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_110_CLOSED", "SUB-110-CLOSED", "110", s => s.LastTimeRun = new DateTime(2024, 1, 1));
            var worker = CreateWorker(scope);

            await worker.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            using var doc = JsonDocument.Parse(json);
            
            var payload = doc.RootElement.GetProperty("payload").EnumerateArray().ToList();
            
            // Phải chứa sự cố đang mở
            Assert.Contains(payload, r => r.TryGetProperty("incidentMessage", out var msg) && msg.GetString()!.Contains("Sự cố đang mở"));
            
            // KHÔNG được chứa sự cố đã đóng
            Assert.DoesNotContain(payload, r => r.TryGetProperty("incidentMessage", out var msg) && msg.GetString()!.Contains("Sự cố đã đóng"));
        }

        [Fact]
        public async Task QueryPacket110_WithMultipleEquipments_OnlyOneHasVms_ReturnsGuidanceContent_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "110");

            var kmNumber = 125;
            var incidentName = $"SỰ CỐ 3 THIẾT BỊ {Guid.NewGuid():N}";
            await db.Insertable(new TmsIncident
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "TEST_INC_110_EQ3",
                Name = incidentName,
                StartDate = DateTime.Now,
                KmNumber = kmNumber,
                MetNumber = 0,
                State = ShareDataEnum.IncidentState.InProgress,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            for (var i = 1; i <= 3; i++)
            {
                var eqId = Guid.NewGuid().ToString("N");
                await db.Insertable(new TmsEquipment { ID = eqId, Code = $"EQ_110_3EQ_{i}", KmNumber = kmNumber, MetNumber = 0 }).ExecuteCommandAsync();
                
                // Only the second equipment has VmsCurrent data
                if (i == 2)
                {
                    await db.Insertable(new VmsCurrent { ID = Guid.NewGuid().ToString("N"), EquipmentId = eqId, Name = $"VMS_{i}", RowData = $"VALID_ROW_DATA", ExecutedDate = DateTime.Now }).ExecuteCommandAsync();
                }
            }

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P110_EQ3", "SUB-110-EQ3-01", "110");

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            using var doc = JsonDocument.Parse(json);
            var matched = doc.RootElement.GetProperty("payload").EnumerateArray()
                .Where(r => r.TryGetProperty("incidentMessage", out var msg) && (msg.GetString() ?? string.Empty).Contains(incidentName))
                .ToList();

            Assert.Single(matched);
            Assert.True(matched[0].TryGetProperty("guidanceContent", out var guidance));
            Assert.Equal("VALID_ROW_DATA", guidance.GetString());
        }

        [Fact]
        public async Task QueryPacket103_WithTopN_ASC_Integration_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var eqId = $"EQ_103_{Guid.NewGuid():N}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = "EQ_103", KmNumber = 15 }).ExecuteCommandAsync();

            // Insert 3 records with different DetectTime
            var time1 = new DateTime(2025, 1, 1, 10, 0, 0);
            var time2 = new DateTime(2025, 1, 1, 10, 5, 0);
            var time3 = new DateTime(2025, 1, 1, 10, 10, 0);

            var id1 = Guid.NewGuid().ToString("N");
            var id2 = Guid.NewGuid().ToString("N");
            var id3 = Guid.NewGuid().ToString("N");

            await db.Insertable(new List<TmsTrafficData>
            {
                new() { ID = id1, EquipmentId = eqId, DetectTime = time1, Type = "A" },
                new() { ID = id2, EquipmentId = eqId, DetectTime = time2, Type = "B" },
                new() { ID = id3, EquipmentId = eqId, DetectTime = time3, Type = "C" }
            }).ExecuteCommandAsync();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            
            // Override TopN to 1 for this test
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.TopN == 1).Where(p => p.Code == "103").ExecuteCommandAsync();

            var partnerCode = "TEST_103_TOP";
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, "SUB-103-TOP", "103", s => s.LastTimeRun = new DateTime(2024, 1, 1));

            var worker = CreateWorker(scope);
            var allExportedIds = new List<string>();

            async Task RunCycleAndCollectIds(DateTime expectedTime)
            {
                await worker.ProcessBatchSubscriptions(CancellationToken.None);
                
                var currentSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.Equal(expectedTime, currentSub.LastTimeRun);

                var log = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .FirstAsync();

                Assert.Equal(ShareDataEnum.ExportStatus.Success, log.Status);
                Assert.NotNull(log.FilePath);

                var json = await ReadExportedJson(log.FilePath);
                using var doc = JsonDocument.Parse(json);
                var payload = doc.RootElement.GetProperty("payload").EnumerateArray().ToList();
                
                Assert.Single(payload); // TopN = 1
                var detId = payload[0].GetProperty("detectionId").GetString();
                Assert.NotNull(detId);
                allExportedIds.Add(detId);

                // Add new cycle ready
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.NextTimeRun == DateTime.Now.AddDays(-1))
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }

            await RunCycleAndCollectIds(time1);
            await RunCycleAndCollectIds(time2);
            await RunCycleAndCollectIds(time3);

            // Assert total 3 distinct IDs are exported
            Assert.Equal(3, allExportedIds.Distinct().Count());
            Assert.Contains(id1, allExportedIds);
            Assert.Contains(id2, allExportedIds);
            Assert.Contains(id3, allExportedIds);
        }

        [Fact]
        public async Task QueryPacket103_WhenManyRecordsShareSameDetectTime_ExportsAllAcrossCycles_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var eqId = $"EQ_103_TIES_{Guid.NewGuid():N}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = "EQ_103_T", KmNumber = 15 }).ExecuteCommandAsync();

            var sameTime = new DateTime(2035, 2, 2, 10, 0, 0);

            var id1 = Guid.NewGuid().ToString("N");
            var id2 = Guid.NewGuid().ToString("N");
            var id3 = Guid.NewGuid().ToString("N");

            await db.Insertable(new List<TmsTrafficData>
            {
                new() { ID = id1, EquipmentId = eqId, DetectTime = sameTime, Type = "A" },
                new() { ID = id2, EquipmentId = eqId, DetectTime = sameTime, Type = "B" },
                new() { ID = id3, EquipmentId = eqId, DetectTime = sameTime, Type = "C" }
            }).ExecuteCommandAsync();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            
            // Override TopN to 2 for this test
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.TopN == 2).Where(p => p.Code == "103").ExecuteCommandAsync();

            var partnerCode = "TEST_103_TIES";
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, "SUB-103-TIES", "103", s => 
            {
                s.LastTimeRun = new DateTime(2035, 2, 2, 9, 0, 0);
                s.LastId = null;
            });

            var worker = CreateWorker(scope);
            var allExportedIds = new List<string>();

            // Cycle 1
            await worker.ProcessBatchSubscriptions(CancellationToken.None);
            
            var sub1 = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(sameTime, sub1.LastTimeRun);
            Assert.NotNull(sub1.LastId); // LastId MUST be set after first tie cycle

            var log1 = await db.Queryable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).OrderByDescending(l => l.OccurredAt).FirstAsync();
            var json1 = await ReadExportedJson(log1.FilePath!);
            var doc1 = JsonDocument.Parse(json1);
            var payload1 = doc1.RootElement.GetProperty("payload").EnumerateArray().ToList();
            Assert.Equal(2, payload1.Count);
            
            foreach (var r in payload1)
                allExportedIds.Add(r.GetProperty("detectionId").GetString()!);

            // Ensure LastId matches the detectionId of the last element in payload1
            var lastIdInBatch = payload1[^1].GetProperty("detectionId").GetString()!;
            Assert.Equal(lastIdInBatch, sub1.LastId);

            // Trigger next cycle
            await db.Updateable<ShareDataSubscription>().SetColumns(s => s.NextTimeRun == DateTime.Now.AddDays(-1)).Where(s => s.ID == sub.ID).ExecuteCommandAsync();

            // Cycle 2
            await worker.ProcessBatchSubscriptions(CancellationToken.None);

            var sub2 = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(sameTime, sub2.LastTimeRun); // Time should be the same
            Assert.NotNull(sub2.LastId);

            var log2 = await db.Queryable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).OrderByDescending(l => l.OccurredAt).FirstAsync();
            var json2 = await ReadExportedJson(log2.FilePath!);
            var doc2 = JsonDocument.Parse(json2);
            var payload2 = doc2.RootElement.GetProperty("payload").EnumerateArray().ToList();
            Assert.Single(payload2); // Only 1 left

            allExportedIds.Add(payload2[0].GetProperty("detectionId").GetString()!);

            // Assert total 3 distinct IDs are exported! No missing data despite ties!
            Assert.Equal(3, allExportedIds.Count);
            Assert.Equal(3, allExportedIds.Distinct().Count());
            Assert.Contains(id1, allExportedIds);
            Assert.Contains(id2, allExportedIds);
            Assert.Contains(id3, allExportedIds);

            // Clean up test data
            await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
            await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
        }

        [Fact]
        public async Task QueryPacket103_WhenTieRecordsHaveMixedCaseIds_ExportsExactlyThreeWithoutDuplicates_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var eqId = $"EQ_103_MIXED_{Guid.NewGuid():N}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = "EQ_103_M", KmNumber = 15 }).ExecuteCommandAsync();

            var sameTime = new DateTime(2036, 3, 3, 10, 0, 0);

            // In SQL (Latin1_General_CI_AS): "a_tie_..." < "B_tie_..." < "c_tie_..."
            // In C# Ordinal: 'B' (66) < 'a' (97), so "B_tie_..." < "a_tie_...", causing C# Ordinal Max to pick "a_tie_..." instead of "B_tie_..."
            var id1 = $"a_tie_{Guid.NewGuid():N}";
            var id2 = $"B_tie_{Guid.NewGuid():N}";
            var id3 = $"c_tie_{Guid.NewGuid():N}";

            await db.Insertable(new List<TmsTrafficData>
            {
                new() { ID = id1, EquipmentId = eqId, DetectTime = sameTime, Type = "A" },
                new() { ID = id2, EquipmentId = eqId, DetectTime = sameTime, Type = "B" },
                new() { ID = id3, EquipmentId = eqId, DetectTime = sameTime, Type = "C" }
            }).ExecuteCommandAsync();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            
            // Override TopN to 2 for this test
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.TopN == 2).Where(p => p.Code == "103").ExecuteCommandAsync();

            var partnerCode = "TEST_103_MIXED";
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, "SUB-103-MIXED", "103", s => 
            {
                s.LastTimeRun = new DateTime(2036, 3, 3, 9, 0, 0);
                s.LastId = null;
            });

            var worker = CreateWorker(scope);
            var allExportedIds = new List<string>();

            // Cycle 1
            await worker.ProcessBatchSubscriptions(CancellationToken.None);
            
            var sub1 = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(sameTime, sub1.LastTimeRun);
            Assert.NotNull(sub1.LastId);

            var log1 = await db.Queryable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).OrderByDescending(l => l.OccurredAt).FirstAsync();
            var json1 = await ReadExportedJson(log1.FilePath!);
            var doc1 = JsonDocument.Parse(json1);
            var payload1 = doc1.RootElement.GetProperty("payload").EnumerateArray().ToList();
            Assert.Equal(2, payload1.Count);
            
            foreach (var r in payload1)
                allExportedIds.Add(r.GetProperty("detectionId").GetString()!);

            // Assert sub1.LastId is the detectionId of the last element in payload1
            var lastIdInBatch = payload1[^1].GetProperty("detectionId").GetString()!;
            Assert.Equal(lastIdInBatch, sub1.LastId);

            // Trigger next cycle
            await db.Updateable<ShareDataSubscription>().SetColumns(s => s.NextTimeRun == DateTime.Now.AddDays(-1)).Where(s => s.ID == sub.ID).ExecuteCommandAsync();

            // Cycle 2
            await worker.ProcessBatchSubscriptions(CancellationToken.None);

            var sub2 = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(sameTime, sub2.LastTimeRun);
            Assert.NotNull(sub2.LastId);

            var log2 = await db.Queryable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).OrderByDescending(l => l.OccurredAt).FirstAsync();
            var json2 = await ReadExportedJson(log2.FilePath!);
            var doc2 = JsonDocument.Parse(json2);
            var payload2 = doc2.RootElement.GetProperty("payload").EnumerateArray().ToList();
            Assert.Single(payload2); // Only 1 left, NO duplicates

            allExportedIds.Add(payload2[0].GetProperty("detectionId").GetString()!);

            // Assert exactly 3 distinct IDs are exported with NO duplicates
            Assert.Equal(3, allExportedIds.Count);
            Assert.Equal(3, allExportedIds.Distinct().Count());
            Assert.Contains(id1, allExportedIds);
            Assert.Contains(id2, allExportedIds);
            Assert.Contains(id3, allExportedIds);

            // Clean up test data
            await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
            await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
        }

        [Fact]
        public async Task FetchData_WhenPacketMissing_LogsFailed_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_PKT_MISSING", "SUB-PKT-MISSING-01", "999_NON_EXISTENT");

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.Contains("active", logs[0].ErrorMessage);
        }

        [Fact]
        public async Task FetchData_WhenMetadataTableInvalid_LogsFailedWithoutFallback_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var badPacketCode = $"BAD_{Guid.NewGuid():N}"[..32];
            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = badPacketCode,
                Name = "Bad Packet",
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = badPacketCode,
                TableName = "Bang_Khong_Ton_Tai_XYZ",
                Alias = "bad",
                IsRoot = true,
                IsActive = true
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_BAD_TBL", "SUB-BAD-TBL-01", badPacketCode);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.NotNull(logs[0].ErrorMessage);
        }

        [Fact]
        public async Task ExecuteExport_WhenMappingItemsConfigured_AppliesFunnelRules_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var uniqueId = Guid.NewGuid().ToString("N");
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, uniqueId);

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_MAPPING", "SUB-MAP-01", "101");

            var mapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = ShareDataEnum.SubDirection.Outbound,
                IsActive = true,
                TargetRootEntity = "BB",
                ItemsJson = JsonSerializer.Serialize(new List<object>
                {
                    new { fieldKey = "averageSpeed", targetKey = "vanToc", targetEntity = "BB" },
                    new { fieldKey = "trafficCondition", isExcluded = true }
                })
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains("\"BB\"", json);
            Assert.Contains("\"vanToc\"", json);
            Assert.DoesNotContain("\"trafficCondition\"", json);
        }

        [Theory]
        [InlineData("INBOUND", false, true, true, true, false)]
        [InlineData("OUTBOUND", true, true, true, true, false)]
        [InlineData("OUTBOUND", false, false, true, true, false)]
        [InlineData("OUTBOUND", false, true, false, true, false)]
        [InlineData("OUTBOUND", false, true, true, false, false)]
        [InlineData("OUTBOUND", false, true, true, true, true)]
        public async Task ProcessBatchSubscriptions_ScanExclusions_IsSkipped_Test(
            string direction, bool isSoftDeleted, bool isPartnerActive, bool isSessionConnected, bool isDue, bool expectPickedUp)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"P_SCAN_{Guid.NewGuid():N}",
                Name = "Partner Scan",
                Status = isPartnerActive ? BaseEnums.StatusEnum.Enable : BaseEnums.StatusEnum.Disable,
                SessionState = isSessionConnected ? BaseEnums.SessionState.Connected : BaseEnums.SessionState.Disconnected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"SUB_SCAN_{Guid.NewGuid():N}",
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = direction,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = isDue ? DateTime.Now.AddSeconds(-10) : DateTime.Now.AddMinutes(10),
                IsDelete = isSoftDeleted ? DateTime.Now : null
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            if (expectPickedUp)
                Assert.NotEmpty(logs);
            else
                Assert.Empty(logs);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_MultiWorkerConcurrency_PreventsDuplicateExecution_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, "CONC");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_CONC", "SUB-CONC-01", "101");

            var w1 = CreateWorker(scope);
            var w2 = CreateWorker(scope);

            var t1 = Task.Run(() => w1.ProcessBatchSubscriptions(CancellationToken.None));
            var t2 = Task.Run(() => w2.ProcessBatchSubscriptions(CancellationToken.None));
            await Task.WhenAll(t1, t2);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_HighConcurrencyStress_ExecutesAllIdempotently_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, "STRESS");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_STRESS", "SUB-STRESS-01", "101");

            var tasks = Enumerable.Range(0, 5)
                .Select(_ => Task.Run(async () =>
                {
                    using var s = _host.Services.CreateScope();
                    await CreateWorker(s).ProcessBatchSubscriptions(CancellationToken.None);
                }));

            await Task.WhenAll(tasks);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenStateChangedToPausedDuringExecution_PreservesPausedState_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_PAUSE_MID", "SUB-PAUSE-MID-01", "101");

            var workerTask = CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);
            await Task.Delay(20);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await workerTask;

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Paused, updatedSub.State);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenCancelledMidBatch_StopsProcessingRest_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_CANCEL", "SUB-CANCEL-01", "101");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await CreateWorker(scope).ProcessBatchSubscriptions(cts.Token);

            var logs = await GetLogs(db, sub.ID);
            Assert.True(logs.Count <= 1);
        }

        [Fact]
        public async Task LogExportResultAsync_PopulatesAllTransferFields_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, "TRANSFER_LOG");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_TRLOG", "SUB-TRLOG-01", "101");

            var mapping = new ShareDataMapping
            {
                ID = "MAP_TRLOG_01",
                PartnerId = partner.ID,
                DatatypeId = "101",
                PacketVersion = "1.0",
                Direction = ShareDataEnum.SubDirection.Outbound,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            var log = logs[0];

            Assert.Equal(ShareDataEnum.LogType.Transfer, log.LogType);
            Assert.Equal(ShareDataEnum.LogAction.Send, log.Action);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, log.Status);
            Assert.NotNull(log.ByteSize);
            Assert.NotNull(log.RecordCount);
            Assert.NotNull(log.Hash);
            Assert.NotNull(log.FilePath);
            Assert.Equal("MAP_TRLOG_01", log.MappingId);
            Assert.Equal("1.0", log.PacketVersion);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenLockLeaseActive_DoesNotOverwriteLease_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var leaseExpire = DateTime.Now.AddSeconds(45);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_LEASE_ACTIVE", "SUB-LEASE-ACTIVE-01",
                "101", s => s.NextTimeRun = leaseExpire);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated);
            Assert.True(Math.Abs((updated.NextTimeRun!.Value - leaseExpire).TotalSeconds) < 1,
                "Lock lease đang còn hạn không được phép bị worker khác ghi đè");
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenLockLeaseExpired_ReclaimsSubscription_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, "LEASE_EXP");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_LEASE_EXPIRED", "SUB-LEASE-EXPIRED-01",
                "101", s => s.NextTimeRun = DateTime.Now.AddMinutes(-5));

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenNoNewData_KeepsLastExportTimeUnchanged_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var lastRun = DateTime.Now.AddYears(100);

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_NO_NEW", "SUB-NO-NEW-01",
                "103", s => s.LastTimeRun = lastRun);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.Equal(0, logs[0].RecordCount);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated.LastTimeRun);
            Assert.True(Math.Abs((updated.LastTimeRun!.Value - lastRun).TotalSeconds) < 1);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenExportSucceeds_AdvancesLastExportTime_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var oldLastRun = DateTime.Now.AddMinutes(-30);
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.VehicleDetection, "ADVANCE");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_ADVANCE", "SUB-ADVANCE-01",
                "103", s => s.LastTimeRun = oldLastRun);

            var beforeRun = DateTime.Now;
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.True(logs[0].RecordCount > 0);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated.LastTimeRun);
            Assert.True(updated.LastTimeRun > oldLastRun);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_LateArrivingRecord_IsStillExported_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var lastExportTime = DateTime.Now.AddYears(20);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_LATE", "SUB-LATE-01",
                "103", s => s.LastTimeRun = lastExportTime);

            // Run 1: No data yet -> LastTimeRun remains unchanged
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var afterFirstRun = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).LastTimeRun;
            Assert.NotNull(afterFirstRun);
            Assert.True(Math.Abs((afterFirstRun!.Value - lastExportTime).TotalSeconds) < 1);

            // Record arrives late with DetectTime after lastExportTime
            var eqId = $"EQ_LATE_{Guid.NewGuid():N}";
            var lateRecordId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsEquipment { ID = eqId, Code = "EQ_LATE", KmNumber = 15 }).ExecuteCommandAsync();
            await db.Insertable(new TmsTrafficData
            {
                ID = lateRecordId,
                EquipmentId = eqId,
                DetectTime = lastExportTime.AddMinutes(5),
                LicensePlate = "29A-LATE-99",
                Speed = 60
            }).ExecuteCommandAsync();

            try
            {
                // Run 2: Late record is picked up
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.NextTimeRun == DateTime.Now.AddSeconds(-10))
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();

                await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await GetLogs(db, sub.ID);
                Assert.True(logs.Count >= 2);
                var latestLog = logs[0];
                Assert.Equal(ShareDataEnum.ExportStatus.Success, latestLog.Status);
                Assert.True(latestLog.RecordCount > 0);

                var json = await ReadExportedJson(latestLog.FilePath!);
                Assert.Contains("29A-LATE-99", json);
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().Where(d => d.ID == lateRecordId).ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task ExecuteExport_WhenFileWriteFails_LogsFailedAndKeepsLastTimeRun_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, "IO_FAIL");

            const string partnerCode = "TEST_IO_FAIL";
            var mocCu = DateTime.Now.AddMinutes(-30);
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, "SUB-IO-FAIL-01",
                "101", s => s.LastTimeRun = mocCu);

            var yyyyMM = DateTime.Now.ToString("yyyyMM");
            var ddHH = DateTime.Now.ToString("ddHH");
            var blockingFilePath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", "Out", partnerCode, yyyyMM, ddHH, "101");

            if (File.Exists(blockingFilePath))
                File.Delete(blockingFilePath);

            if (Directory.Exists(blockingFilePath))
                Directory.Delete(blockingFilePath, true);

            Directory.CreateDirectory(Path.GetDirectoryName(blockingFilePath)!);
            await File.WriteAllTextAsync(blockingFilePath, "chiem cho");

            try
            {
                await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await GetLogs(db, sub.ID);
                Assert.Single(logs);
                Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
                Assert.False(string.IsNullOrWhiteSpace(logs[0].ErrorMessage));

                var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.NotNull(updated.LastTimeRun);
                Assert.True(Math.Abs((updated.LastTimeRun!.Value - mocCu).TotalSeconds) < 1,
                    "Ghi file hỏng thì LastTimeRun phải đứng yên để không mất dữ liệu");
            }
            finally
            {
                if (File.Exists(blockingFilePath))
                    File.Delete(blockingFilePath);
            }
        }

        [Fact]
        public async Task SingleModeSubscription_DeactivatesAfterSuccessfulExport_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, "SINGLE");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_SINGLE", "SUB-SINGLE-01", "101", s =>
            {
                s.Mode = ShareDataEnum.SubMode.Single;
                s.NextTimeRun = DateTime.Now.AddSeconds(-10);
            });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Expired, updatedSub.State);
        }

        [Fact]
        public async Task ValidateAllEntities_MatchDatabaseSchema_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var entityTypes = new Type[]
            {
                typeof(ShareDataActivityLog),
                typeof(ShareDataPartner),
                typeof(ShareDataSubscription),
                typeof(ShareDataPacket),
                typeof(ShareDataTable),
                typeof(ShareDataMapping),
                typeof(ShareDataCodeSet),
                typeof(ShareDataAlertLog),
                typeof(ShareDataSession),
                typeof(ShareDataEventSource),
                typeof(TmsZoneStatus),
                typeof(TmsZone),
                typeof(TmsTrafficStatistic),
                typeof(CctvDevice),
                typeof(TmsEquipment),
                typeof(TmsTrafficData),
                typeof(TmsWeather),
                typeof(TollTransactionOut),
                typeof(TmsVehicleRegistration),
                typeof(TmsIncident),
                typeof(TmsEventType),
                typeof(VmsCurrent),
                typeof(TollLane),
                typeof(TollStation),
                typeof(TmsSignalLog)
            };

            var schemaErrors = new List<string>();

            foreach (var type in entityTypes)
            {
                var entityInfo = db.EntityMaintenance.GetEntityInfo(type);
                var tableName = entityInfo.DbTableName;

                var dbColumns = db.DbMaintenance.GetColumnInfosByTableName(tableName);
                if (dbColumns == null || dbColumns.Count == 0)
                {
                    schemaErrors.Add($"Bảng [{tableName}] (Entity [{type.Name}]) không tồn tại hoặc không có cột nào trong CSDL!");
                    continue;
                }

                var dbColumnNames = dbColumns.Select(c => c.DbColumnName.ToLowerInvariant()).ToHashSet();
                var entityColumns = entityInfo.Columns.Where(c => !c.IsIgnore).ToList();

                foreach (var col in entityColumns)
                {
                    var colName = col.DbColumnName.ToLowerInvariant();
                    if (!dbColumnNames.Contains(colName))
                    {
                        schemaErrors.Add($"Bảng [{tableName}] - Entity [{type.Name}] chứa cột '{col.DbColumnName}' không tồn tại trong CSDL!");
                    }
                }
            }

            Assert.Empty(schemaErrors);

            var actLogColumns = db.DbMaintenance.GetColumnInfosByTableName("ShareDataActivityLog")
                .Select(c => c.DbColumnName.ToLowerInvariant())
                .ToHashSet();
            Assert.Contains("mappingid", actLogColumns);
            Assert.Contains("packetversion", actLogColumns);
        }

        [Theory]
        [InlineData("101", ShareDataEnum.DatatypeIdEnum.TrafficFlow)]
        [InlineData("101 - Lưu lượng giao thông", ShareDataEnum.DatatypeIdEnum.TrafficFlow)]
        [InlineData("102", ShareDataEnum.DatatypeIdEnum.CctvImage)]
        [InlineData("102 - Ảnh camera CCTV", ShareDataEnum.DatatypeIdEnum.CctvImage)]
        [InlineData("103", ShareDataEnum.DatatypeIdEnum.VehicleDetection)]
        [InlineData("104", ShareDataEnum.DatatypeIdEnum.Weather)]
        [InlineData("105", ShareDataEnum.DatatypeIdEnum.VehicleIdentification)]
        [InlineData("106", ShareDataEnum.DatatypeIdEnum.WeighInMotion)]
        [InlineData("107", ShareDataEnum.DatatypeIdEnum.TrafficIncident)]
        [InlineData("108", ShareDataEnum.DatatypeIdEnum.VmsDisplay)]
        [InlineData("109", ShareDataEnum.DatatypeIdEnum.TollCollection)]
        [InlineData("110", ShareDataEnum.DatatypeIdEnum.PublicMessaging)]
        [InlineData("111", ShareDataEnum.DatatypeIdEnum.InterCenterExchange)]
        public void TryResolveDatatypeEnum_ValidPrefix_ReturnsTrueAndEnum_Test(string input, ShareDataEnum.DatatypeIdEnum expectedEnum)
        {
            var success = DataExportService.TryResolveDatatypeEnum(input, out var result);
            Assert.True(success);
            Assert.Equal(expectedEnum, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("KhongCoSo")]
        [InlineData("ABC - 101")]
        [InlineData("99999 - KhongTonTai")]
        public void TryResolveDatatypeEnum_InvalidInput_ReturnsFalse_Test(string? invalidInput)
        {
            var success = DataExportService.TryResolveDatatypeEnum(invalidInput, out _);
            Assert.False(success);
        }

        [Fact]
        public void CalculateNextRunTime_NullScheduleJson_FallbackToInterval_Test()
        {
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 45,
                ScheduleJson = null
            };

            var now = new DateTime(2026, 8, 22, 10, 0, 0);
            var nextRun = DataExportService.CalculateNextRunTime(sub, now);

            Assert.NotNull(nextRun);
            Assert.Equal(now.AddSeconds(45), nextRun);
        }

        [Fact]
        public void CalculateNextRunTime_ContinuousMode_CalculatesIntervalFromNow_Test()
        {
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 60,
                ScheduleJson = @"{ ""Type"": ""Continuous"", ""IntervalSeconds"": 60 }"
            };

            var now = new DateTime(2026, 8, 22, 14, 30, 0);
            var nextRun = DataExportService.CalculateNextRunTime(sub, now);

            Assert.NotNull(nextRun);
            Assert.Equal(now.AddSeconds(60), nextRun);
        }

        [Fact]
        public void CalculateNextRunTime_WhenEndDateExpired_ReturnsNull_Test()
        {
            var now = new DateTime(2026, 8, 22, 10, 0, 0);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = @"{ ""Type"": ""Continuous"", ""IntervalSeconds"": 30, ""EndDate"": ""2026-08-20T00:00:00"" }"
            };

            var nextRun = DataExportService.CalculateNextRunTime(sub, now);
            Assert.Null(nextRun);
        }

        [Fact]
        public void GenerateExportRelativePath_StandardInputs_ReturnsExpectedPath_Test()
        {
            var now = new DateTime(2026, 8, 22, 14, 30, 0);
            var path = DataExportService.GenerateExportRelativePath("PARTNER_01", "101", now);

            Assert.Equal("Out/PARTNER_01/202608/2214/101/101_20260822143000.json", path.Replace('\\', '/'));
        }

        [Fact]
        public void GenerateExportRelativePath_SanitizesUnsafePartnerCode_Test()
        {
            var now = new DateTime(2026, 8, 22, 14, 30, 0);
            var path = DataExportService.GenerateExportRelativePath("../PARTNER/EVIL", "101", now);

            Assert.DoesNotContain("..", path);
            Assert.DoesNotContain("/", path.Split('/')[1]);
            Assert.Equal("Out/PARTNEREVIL/202608/2214/101/101_20260822143000.json", path.Replace('\\', '/'));
        }

        [Fact]
        public void GenerateExportRelativePath_CustomExtensionAndDiscriminator_ReturnsCorrectPath_Test()
        {
            var now = new DateTime(2026, 8, 22, 14, 30, 0);
            var path = DataExportService.GenerateExportRelativePath("PARTNER_01", "101", now, "part1", ".xml");

            Assert.Equal("Out/PARTNER_01/202608/2214/101/101_20260822143000_part1.xml", path.Replace('\\', '/'));
        }

        [Fact]
        public void SerializePayload_ProducesValidCamelCaseJson_Test()
        {
            var testObj = new { FieldName = "TestValue", NumericCode = 123 };
            var bytes = DataExportService.SerializePayload(testObj);

            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);

            var json = System.Text.Encoding.UTF8.GetString(bytes);
            Assert.Contains("\"fieldName\":", json);
            Assert.Contains("\"TestValue\"", json);
            Assert.Contains("\"numericCode\":123", json);
        }

        [Fact]
        public void SerializePayload_WithPduEnvelope_SerializesCorrectly_Test()
        {
            var envelope = new Dictionary<string, object?>
            {
                ["pduType"] = ShareDataEnum.PduType.DataPacket,
                ["serialNbr"] = 1,
                ["sender"] = ShareDataEnum.Operator.System,
                ["destination"] = "PARTNER_01",
                ["payload"] = new List<object> { new { zoneId = "Z01" } }
            };

            var bytes = DataExportService.SerializePayload(envelope);
            Assert.NotNull(bytes);

            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;

            Assert.Equal(ShareDataEnum.PduType.DataPacket, root.GetProperty("pduType").GetString());
            Assert.Equal(1, root.GetProperty("serialNbr").GetInt32());
            Assert.Equal("PARTNER_01", root.GetProperty("destination").GetString());
            Assert.Equal(JsonValueKind.Array, root.GetProperty("payload").ValueKind);
        }

        [Fact]
        public void BuildQuery_Packet101_MatchesGoldenQueryStructure_Test()
        {
            var def = PacketMetadataCatalogTest.All["101"];
            var result = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));

            var normSql = NormalizeSql(result.Sql);

            Assert.Contains("zs.ZoneId AS zoneId", normSql);
            Assert.Contains("z.Name AS zoneName", normSql);
            Assert.Contains("CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed", normSql);
            Assert.Contains("ts.TotalVehicleNumber AS vehicleCount", normSql);
            Assert.Contains("FROM TmsZoneStatus zs", normSql);
            Assert.Contains("LEFT JOIN TmsZone z ON zs.ZoneId = z.ID", normSql);
            Assert.Contains("LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId", normSql);
            Assert.Contains("WHERE (ISNULL(zs.UpdateTime, zs.CreateTime) > @lastTime OR (ISNULL(zs.UpdateTime, zs.CreateTime) = @lastTime AND zs.ID > @lastId))", normSql);
            Assert.Contains("ORDER BY ISNULL(zs.UpdateTime, zs.CreateTime) ASC, zs.ID ASC", normSql);
            Assert.Contains("OPTION (RECOMPILE)", normSql);
        }

        [Fact]
        public void BuildQuery_Packet109_MatchesGoldenQueryStructure_Test()
        {
            var def = PacketMetadataCatalogTest.All["109"];
            var result = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));

            var normSql = NormalizeSql(result.Sql);

            Assert.Contains("ISNULL(t.PlateEdit, t.PlateLpr) AS licensePlate", normSql);
            Assert.Contains("CAST(NULL AS DECIMAL(18, 2)) AS tollPrice", normSql);
            Assert.Contains("FROM TollTransactionOut t", normSql);
            Assert.Contains("LEFT JOIN TollLane l ON t.LaneId = l.LaneId", normSql);
            Assert.Contains("LEFT JOIN TollStation s ON t.StationId = s.StationId", normSql);
            Assert.Contains("WHERE (ISNULL(t.TransactionDateTime, t.CreateTime) > @lastTime OR (ISNULL(t.TransactionDateTime, t.CreateTime) = @lastTime AND t.ID > @lastId))", normSql);
        }

        [Fact]
        public void BuildQuery_Packet110_UsesOuterApplyWithoutSubquery_Test()
        {
            var def = PacketMetadataCatalogTest.All["110"];
            var result = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));

            var normSql = NormalizeSql(result.Sql);

            Assert.Contains("CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, '')) AS incidentMessage", normSql);
            Assert.Contains("v.RowData AS guidanceContent", normSql);
            Assert.Contains("OUTER APPLY (SELECT TOP 1 v.RowData FROM VmsCurrent v INNER JOIN TmsEquipment e2 ON v.EquipmentId = e2.ID WHERE e2.KmNumber = i.KmNumber AND (v.RowData IS NOT NULL) ORDER BY v.ExecutedDate DESC) v", normSql);
            Assert.Contains("WHERE (i.State IS NULL OR (i.State != 'FINISHED' AND i.State != 'CANCELED' AND i.State != 'Closed' AND i.State != 'Cancelled')) AND (ISNULL(i.UpdateTime, i.StartDate) > @lastTime OR (ISNULL(i.UpdateTime, i.StartDate) = @lastTime AND i.ID > @lastId))", normSql);
        }

        [Fact]
        public void BuildQuery_IncrementalMode_AlwaysUsesIsNullAndGreaterThan_Test()
        {
            var def = PacketMetadataCatalogTest.All["103"];
            var result = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));

            var normSql = NormalizeSql(result.Sql);
            Assert.Contains("(ISNULL(td.DetectTime, td.CreateTime) > @lastTime OR (ISNULL(td.DetectTime, td.CreateTime) = @lastTime AND td.ID > @lastId))", normSql);
            Assert.DoesNotContain(">=", normSql);
        }

        [Fact]
        public void BuildQuery_SnapshotMode_DoesNotContainLastTime_Test()
        {
            var def = PacketMetadataCatalogTest.All["102"];
            var result = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));

            var normSql = NormalizeSql(result.Sql);
            Assert.DoesNotContain("@lastTime", normSql);
        }

        [Fact]
        public void BuildQuery_WhenFilterModeNull_Throws_Test()
        {
            var packet = new ShareDataPacket { Code = "TEST_NULL_FILTER", FilterMode = null };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "TestTbl", Alias = "t", IsRoot = true, FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]" }
            };

            Assert.Throws<InvalidOperationException>(() => DataExportService.BuildQuery(packet, tables));
        }

        [Fact]
        public void BuildQuery_WhenTopNConfigured_AlwaysGeneratesOrderBy_Test()
        {
            var packet = new ShareDataPacket
            {
                Code = "TEST_TOPN",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                TopN = 25
            };
            var tables = new List<ShareDataTable>
            {
                new()
                {
                    TableName = "TestTbl",
                    Alias = "t",
                    IsRoot = true,
                    IncrementalColumn = "CreateTime",
                    FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]"
                }
            };

            var result = DataExportService.BuildQuery(packet, tables, null);
            var normSql = NormalizeSql(result.Sql);

            Assert.Contains("SELECT TOP 25", normSql);
            Assert.Contains("ORDER BY ISNULL(t.CreateTime, t.CreateTime) DESC", normSql);
        }

        [Fact]
        public void BuildQuery_WhenFieldsEmptyOrInvalid_ThrowsWithoutFallback_Test()
        {
            var packet = new ShareDataPacket
            {
                Code = "TEST_NO_FIELDS",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot
            };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "TestTbl", Alias = "t", IsRoot = true, FieldsJson = "[]" }
            };

            Assert.Throws<InvalidOperationException>(() => DataExportService.BuildQuery(packet, tables));
        }

        [Fact]
        public void BuildQuery_WhenSecondaryTableMissingAlias_Throws_Test()
        {
            var packet = new ShareDataPacket
            {
                Code = "TEST_NO_ALIAS",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot
            };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "RootTbl", Alias = "r", IsRoot = true, FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]" },
                new() { TableName = "JoinTbl", Alias = null, IsRoot = false, JoinCondition = "r.Id = j.Id", FieldsJson = "[{\"fieldKey\":\"name\",\"column\":\"Name\"}]" }
            };

            Assert.Throws<InvalidOperationException>(() => DataExportService.BuildQuery(packet, tables));
        }

        [Fact]
        public void BuildQuery_OrdersSelectColumnsByOrderNoAcrossWholePacket_Test()
        {
            var packet = new ShareDataPacket
            {
                Code = "TEST_ORDER",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot
            };
            var tables = new List<ShareDataTable>
            {
                new()
                {
                    TableName = "RootTbl",
                    Alias = "r",
                    IsRoot = true,
                    OrderNo = 1,
                    FieldsJson = @"[
                        { ""fieldKey"": ""field1"", ""column"": ""Col1"", ""orderNo"": 1 },
                        { ""fieldKey"": ""field4"", ""column"": ""Col4"", ""orderNo"": 4 }
                    ]"
                },
                new()
                {
                    TableName = "JoinTbl",
                    Alias = "j",
                    IsRoot = false,
                    JoinCondition = "r.Id = j.Id",
                    OrderNo = 2,
                    FieldsJson = @"[
                        { ""fieldKey"": ""field2"", ""column"": ""Col2"", ""orderNo"": 2 },
                        { ""fieldKey"": ""field3"", ""column"": ""Col3"", ""orderNo"": 3 }
                    ]"
                }
            };

            var result = DataExportService.BuildQuery(packet, tables);
            var normSql = NormalizeSql(result.Sql);

            var idx1 = normSql.IndexOf("r.Col1 AS field1", StringComparison.Ordinal);
            var idx2 = normSql.IndexOf("j.Col2 AS field2", StringComparison.Ordinal);
            var idx3 = normSql.IndexOf("j.Col3 AS field3", StringComparison.Ordinal);
            var idx4 = normSql.IndexOf("r.Col4 AS field4", StringComparison.Ordinal);

            Assert.True(idx1 < idx2, "field1 phải đứng trước field2");
            Assert.True(idx2 < idx3, "field2 phải đứng trước field3");
            Assert.True(idx3 < idx4, "field3 phải đứng trước field4");
        }

        [Fact]
        public void BuildQuery_WhenSecondaryTableMissingJoinCondition_Throws_Test()
        {
            var packet = new ShareDataPacket
            {
                Code = "TEST_NO_JOIN",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot
            };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "RootTbl", Alias = "r", IsRoot = true, FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]" },
                new() { TableName = "JoinTbl", Alias = "j", IsRoot = false, JoinCondition = null, FieldsJson = "[{\"fieldKey\":\"name\",\"column\":\"Name\"}]" }
            };

            Assert.Throws<InvalidOperationException>(() => DataExportService.BuildQuery(packet, tables));
        }

        [Fact]
        public void ParseFields_WhenValidJson_ReturnsOrderedList_Test()
        {
            var json = @"[
                { ""fieldKey"": ""laneId"", ""column"": ""LaneId"", ""dataType"": ""string"", ""orderNo"": 7 },
                { ""fieldKey"": ""zoneId"", ""column"": ""ZoneId"", ""dataType"": ""string"", ""orderNo"": 1 }
            ]";

            var fields = DataExportService.ParseFields(json);

            Assert.Equal(2, fields.Count);
            Assert.Equal("zoneId", fields[0].FieldKey);
            Assert.Equal("laneId", fields[1].FieldKey);
        }

        [Fact]
        public void ParseFields_WhenCorruptedJson_ReturnsEmptyWithoutThrowing_Test()
        {
            var invalidJson = "{ this is not a valid json [}";
            var fields = DataExportService.ParseFields(invalidJson);

            Assert.NotNull(fields);
            Assert.Empty(fields);
        }

        [Fact]
        public void ParseFields_WhenOneElementMissingFieldKey_SkipsItAndKeepsOthers_Test()
        {
            var json = @"[
                { ""column"": ""NoKeyColumn"", ""orderNo"": 1 },
                { ""fieldKey"": ""validKey"", ""column"": ""ValidCol"", ""orderNo"": 2 }
            ]";

            var fields = DataExportService.ParseFields(json);

            Assert.Single(fields);
            Assert.Equal("validKey", fields[0].FieldKey);
            Assert.Equal("ValidCol", fields[0].Column);
        }

        [Fact]
        public void ParseFields_WhenEmptyOrNull_ReturnsEmpty_Test()
        {
            Assert.Empty(DataExportService.ParseFields(null));
            Assert.Empty(DataExportService.ParseFields("   "));
        }

        [Theory]
        [InlineData("ISNULL(a.X, a.Y)")]
        [InlineData("CAST(a.X AS DECIMAL(18, 2))")]
        [InlineData("CONCAT(a.X, ' - ', a.Y)")]
        [InlineData("COALESCE(a.X, a.Y, a.Z)")]
        [InlineData("NULLIF(a.X, '')")]
        [InlineData("CONVERT(VARCHAR(32), a.TimeDetect, 120)")]
        [InlineData("UPPER(a.Plate)")]
        [InlineData("LOWER(a.Code)")]
        [InlineData("LTRIM(RTRIM(a.Name))")]
        [InlineData("ROUND(a.Speed, 2)")]
        [InlineData("ABS(a.MetNumber)")]
        [InlineData("DATEDIFF(minute, a.StartTime, a.EndTime)")]
        [InlineData("DATEADD(hour, 7, a.DetectTime)")]
        [InlineData("LEN(a.LicensePlate)")]
        [InlineData("a.Speed * 3.6")]
        [InlineData("a.KmNumber + a.MetNumber / 1000.0")]
        [InlineData("ISNULL(t.PlateEdit, t.PlateLpr)")]
        [InlineData("CAST(NULL AS DECIMAL(18, 2))")]
        [InlineData("CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, ''))")]
        public void ValidateExpression_WhenScalarExpressionValid_Passes_Test(string expression)
        {
            var ex = Record.Exception(() => DataExportService.ValidateExpression(expression));
            Assert.Null(ex);
        }

        [Theory]
        [InlineData("(SELECT TOP 1 PasswordHash FROM ShareDataPartner)")]
        [InlineData("a.X UNION SELECT 1")]
        [InlineData("WAITFOR DELAY '00:00:05'")]
        [InlineData("DECLARE @x INT")]
        [InlineData("EXEC xp_cmdshell 'dir'")]
        [InlineData("EXECUTE sp_executesql N'SELECT 1'")]
        [InlineData("OPENROWSET('SQLNCLI', 'Server=...', 'SELECT 1')")]
        [InlineData("OPENDATASOURCE('SQLNCLI', '...').db.dbo.table")]
        [InlineData("OPENQUERY(oracle_srv, 'SELECT 1')")]
        [InlineData("a.X; DROP TABLE X")]
        [InlineData("a.X -- comment")]
        [InlineData("a.X /* block comment */")]
        [InlineData("a.X @@version")]
        [InlineData("ISNULL(a.X, a.Y")]
        [InlineData("ISNULL(a.X, a.Y))")]
        [InlineData("CONCAT(a.X, ' - , a.Y)")]
        [InlineData("USER_NAME()")]
        [InlineData("PASSWORD_HASH(a.Password)")]
        [InlineData("a.X $ 10")]
        [InlineData("a.X # 10")]
        [InlineData("a.X & 10")]
        public void ValidateExpression_WhenMaliciousOrInvalid_Throws_Test(string maliciousExpression)
        {
            Assert.Throws<InvalidOperationException>(() => DataExportService.ValidateExpression(maliciousExpression));
        }

        [Fact]
        public void ValidateExpression_WhenAliasNotDeclared_Throws_Test()
        {
            var declaredAliases = new[] { "zs", "z", "ts" };
            Assert.Throws<InvalidOperationException>(() =>
                DataExportService.ValidateExpression("CAST(unknown.AverageSpeed AS DECIMAL(18, 2))", declaredAliases));
        }

        [Fact]
        public void ValidateExpression_WhenAliasDeclared_Passes_Test()
        {
            var declaredAliases = new[] { "zs", "z", "ts" };
            var ex = Record.Exception(() =>
                DataExportService.ValidateExpression("CAST(zs.AverageSpeed AS DECIMAL(18, 2))", declaredAliases));
            Assert.Null(ex);
        }

        [Theory]
        [InlineData("a.ZoneId = b.ID")]
        [InlineData("a.ZoneId = b.ID AND a.TenantId = b.TenantId")]
        [InlineData("ISNULL(t.PlateEdit, t.PlateLpr) = vr.LicensePlate")]
        [InlineData("t.LaneId = l.LaneId AND t.StationId = s.StationId")]
        public void ValidateJoinCondition_WhenStructureValid_Passes_Test(string joinCondition)
        {
            var declaredAliases = new[] { "a", "b", "t", "vr", "l", "s" };
            var ex = Record.Exception(() => DataExportService.ValidateJoinCondition(joinCondition, declaredAliases));
            Assert.Null(ex);
        }

        [Theory]
        [InlineData("a.ZoneId = b.ID OR 1=1")]
        [InlineData("1=1")]
        [InlineData("a.ZoneId = (SELECT TOP 1 ID FROM B)")]
        [InlineData("a.ZoneId = b.ID; DROP TABLE B")]
        [InlineData("a.ZoneId = b.ID -- comment")]
        public void ValidateJoinCondition_WhenMaliciousStructure_Throws_Test(string maliciousJoin)
        {
            var declaredAliases = new[] { "a", "b" };
            Assert.Throws<InvalidOperationException>(() => DataExportService.ValidateJoinCondition(maliciousJoin, declaredAliases));
        }

        [Fact]
        public void ValidateJoinCondition_WhenUndeclaredAlias_Throws_Test()
        {
            var declaredAliases = new[] { "a" };
            Assert.Throws<InvalidOperationException>(() =>
                DataExportService.ValidateJoinCondition("a.ZoneId = ghost.ID", declaredAliases));
        }

        [Theory]
        [InlineData("ZoneId")]
        [InlineData("AverageSpeed")]
        [InlineData("_temp")]
        [InlineData("tbl_01")]
        [InlineData("lane1")]
        public void ValidateIdentifier_WhenValid_Passes_Test(string identifier)
        {
            var ex = Record.Exception(() => DataExportService.ValidateIdentifier(identifier));
            Assert.Null(ex);
        }

        [Theory]
        [InlineData("123abc")]
        [InlineData("a-b")]
        [InlineData("a.b")]
        [InlineData("a;b")]
        [InlineData("a b")]
        [InlineData("a*b")]
        [InlineData("a'b")]
        public void ValidateIdentifier_WhenInvalid_Throws_Test(string invalidIdentifier)
        {
            Assert.Throws<InvalidOperationException>(() => DataExportService.ValidateIdentifier(invalidIdentifier));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidateIdentifier_WhenNullOrEmpty_ThrowsArgumentException_Test(string? emptyIdentifier)
        {
            Assert.Throws<ArgumentException>(() => DataExportService.ValidateIdentifier(emptyIdentifier));
        }

        [Theory]
        [InlineData("TmsZoneStatus")]
        [InlineData("dbo.TmsZoneStatus")]
        [InlineData("[dbo].[TmsZoneStatus]")]
        [InlineData("[TmsZoneStatus]")]
        public void ValidateTableName_WhenValid_Passes_Test(string tableName)
        {
            var ex = Record.Exception(() => DataExportService.ValidateTableName(tableName));
            Assert.Null(ex);
        }

        [Theory]
        [InlineData("TmsZoneStatus; DROP TABLE X")]
        [InlineData("dbo.TmsZone --")]
        [InlineData("dbo.TmsZone/*comment*/")]
        public void ValidateTableName_WhenInjectionAttempt_Throws_Test(string maliciousTable)
        {
            Assert.Throws<InvalidOperationException>(() => DataExportService.ValidateTableName(maliciousTable));
        }

        [Fact]
        public void Transform_WhenFieldExcluded_DoesNotAppearInResult_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z01",
                    ["averageSpeed"] = 80.5m,
                    ["vehicleCount"] = 120
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId" },
                new() { FieldKey = "averageSpeed", Column = "AverageSpeed", DataType = "decimal" },
                new() { FieldKey = "vehicleCount", Column = "VehicleCount", DataType = "int" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "vehicleCount", IsExcluded = true }
            };

            var result = DataExportService.Transform(rawRows, fields, mappingItems);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.True(row.ContainsKey("zoneId"));
            Assert.True(row.ContainsKey("averageSpeed"));
            Assert.False(row.ContainsKey("vehicleCount"));
        }

        [Fact]
        public void Transform_WhenTargetKeyAndEntityConfigured_RenamesAndGroupsCorrectly_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z01",
                    ["averageSpeed"] = 65.0m
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId" },
                new() { FieldKey = "averageSpeed", Column = "AverageSpeed", DataType = "decimal" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "averageSpeed", TargetKey = "tocDoTB", TargetEntity = "trafficMetric" }
            };

            var result = DataExportService.Transform(rawRows, fields, mappingItems);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("Z01", row["zoneId"]);

            Assert.True(row.ContainsKey("trafficMetric"));
            var subEntity = Assert.IsAssignableFrom<IDictionary<string, object?>>(row["trafficMetric"]);
            Assert.Equal(65.0m, subEntity["tocDoTB"]);
        }

        [Fact]
        public void Transform_WhenValueNull_AppliesDefaultValueAndCoercesType_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z01",
                    ["averageSpeed"] = null
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId" },
                new() { FieldKey = "averageSpeed", Column = "AverageSpeed", DataType = "decimal" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "averageSpeed", DefaultValue = "0.0" }
            };

            var result = DataExportService.Transform(rawRows, fields, mappingItems);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(0.0m, row["averageSpeed"]);
            Assert.IsType<decimal>(row["averageSpeed"]);
        }

        [Fact]
        public void Transform_CoercesDataTypes_Correctly_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["decVal"] = "123.45",
                    ["intVal"] = "42",
                    ["longVal"] = "999999999",
                    ["doubleVal"] = "3.14159",
                    ["boolVal"] = "True",
                    ["strVal"] = 12345
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "decVal", DataType = "decimal" },
                new() { FieldKey = "intVal", DataType = "int" },
                new() { FieldKey = "longVal", DataType = "long" },
                new() { FieldKey = "doubleVal", DataType = "double" },
                new() { FieldKey = "boolVal", DataType = "bool" },
                new() { FieldKey = "strVal", DataType = "string" }
            };

            var result = DataExportService.Transform(rawRows, fields);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(123.45m, row["decVal"]);
            Assert.Equal(42, row["intVal"]);
            Assert.Equal(999999999L, row["longVal"]);
            Assert.Equal(3.14159d, (double)row["doubleVal"]!, 4);
            Assert.True((bool)row["boolVal"]!);
            Assert.Equal("12345", row["strVal"]);
        }

        [Fact]
        public void ParseMappingItems_WhenArrayFormat_ParsesCorrectly_Test()
        {
            var json = @"[
                { ""fieldKey"": ""averageSpeed"", ""targetKey"": ""tocDoTB"", ""isExcluded"": false },
                { ""fieldKey"": ""vehicleCount"", ""isExcluded"": true }
            ]";

            var items = DataExportService.ParseMappingItems(json);

            Assert.Equal(2, items.Count);
            Assert.Equal("averageSpeed", items[0].FieldKey);
            Assert.Equal("tocDoTB", items[0].TargetKey);
            Assert.False(items[0].IsExcluded);

            Assert.Equal("vehicleCount", items[1].FieldKey);
            Assert.True(items[1].IsExcluded);
        }

        [Fact]
        public void ParseMappingItems_WhenObjectDictionaryFormat_ParsesCorrectly_Test()
        {
            var json = @"{
                ""averageSpeed"": ""tocDoTrungBinh"",
                ""trafficCondition"": { ""targetKey"": ""tinhTrangGiaoThong"", ""defaultValue"": ""BINH_THUONG"" }
            }";

            var items = DataExportService.ParseMappingItems(json);

            Assert.Equal(2, items.Count);
            var item1 = items.FirstOrDefault(i => i.FieldKey == "averageSpeed");
            Assert.NotNull(item1);
            Assert.Equal("tocDoTrungBinh", item1.TargetKey);

            var item2 = items.FirstOrDefault(i => i.FieldKey == "trafficCondition");
            Assert.NotNull(item2);
            Assert.Equal("tinhTrangGiaoThong", item2.TargetKey);
            Assert.Equal("BINH_THUONG", item2.DefaultValue?.ToString());
        }

        [Fact]
        public void ParseMappingItems_WhenInvalidJson_ReturnsEmpty_Test()
        {
            Assert.Empty(DataExportService.ParseMappingItems("invalid json"));
            Assert.Empty(DataExportService.ParseMappingItems(null));
        }

        [Fact]
        public void BuildQuery_WhenExtraWhereValid_Passes_Test()
        {
            var packet = new ShareDataPacket { Code = "TEST_EW", FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "RootTbl", Alias = "r", IsRoot = true, FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]" },
                new()
                {
                    TableName = "ApplyTbl",
                    Alias = "a",
                    IsRoot = false,
                    JoinType = "OUTER APPLY",
                    JoinCondition = "a.Id = r.Id",
                    ExtraWhere = "a.RowData IS NOT NULL",
                    ApplyTopN = 1,
                    ApplyOrderBy = "ExecutedDate",
                    FieldsJson = "[{\"fieldKey\":\"rd\",\"column\":\"RowData\"}]"
                }
            };

            var result = DataExportService.BuildQuery(packet, tables);
            Assert.Contains("WHERE a.Id = r.Id AND (a.RowData IS NOT NULL)", NormalizeSql(result.Sql));
        }

        [Theory]
        [InlineData("1=1; DROP TABLE VmsCurrent")]
        [InlineData("1=1 UNION SELECT PasswordHash FROM ShareDataPartner")]
        public void BuildQuery_WhenExtraWhereMalicious_Throws_Test(string maliciousExtraWhere)
        {
            var packet = new ShareDataPacket { Code = "TEST_EW_BAD", FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "RootTbl", Alias = "r", IsRoot = true, FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]" },
                new()
                {
                    TableName = "ApplyTbl",
                    Alias = "a",
                    IsRoot = false,
                    JoinType = "OUTER APPLY",
                    JoinCondition = "a.Id = r.Id",
                    ExtraWhere = maliciousExtraWhere,
                    ApplyTopN = 1,
                    ApplyOrderBy = "ExecutedDate",
                    FieldsJson = "[{\"fieldKey\":\"rd\",\"column\":\"RowData\"}]"
                }
            };

            Assert.Throws<InvalidOperationException>(() => DataExportService.BuildQuery(packet, tables));
        }

        [Fact]
        public void QueryPacket_KeysetPagination_SqlGen_Test()
        {
            var packetIncr = new ShareDataPacket { Code = "103_INC", FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental, TopN = 50 };
            var packetSnap = new ShareDataPacket { Code = "103_SNAP", FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot, TopN = 50 };
            var tables = new List<ShareDataTable>
            {
                new()
                {
                    TableName = "TmsTrafficData",
                    Alias = "td",
                    IsRoot = true,
                    IncrementalColumn = "DetectTime",
                    IncrementalFallbackColumn = "CreateTime",
                    FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]"
                }
            };
            var tablesNone = new List<ShareDataTable>
            {
                new()
                {
                    TableName = "TmsTrafficData",
                    Alias = "td",
                    IsRoot = true,
                    IncrementalColumn = "DetectTime",
                    IncrementalFallbackColumn = "NONE",
                    FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]"
                }
            };

            // 1. Incremental with ISNULL
            var queryIncr = DataExportService.BuildQuery(packetIncr, tables, new DateTime(2025, 1, 1));
            var normIncr = NormalizeSql(queryIncr.Sql);
            Assert.Contains("td.ID AS __rowid", normIncr);
            Assert.Contains("ISNULL(td.DetectTime, td.CreateTime) AS __watermark", normIncr);
            Assert.Contains("OR (ISNULL(td.DetectTime, td.CreateTime) = @lastTime AND td.ID > @lastId)", normIncr);
            Assert.Contains("ORDER BY ISNULL(td.DetectTime, td.CreateTime) ASC, td.ID ASC", normIncr);
            
            // 2. Incremental with NONE fallback
            var queryNone = DataExportService.BuildQuery(packetIncr, tablesNone, new DateTime(2025, 1, 1));
            var normNone = NormalizeSql(queryNone.Sql);
            Assert.Contains("td.DetectTime AS __watermark", normNone);
            Assert.Contains("OR (td.DetectTime = @lastTime AND td.ID > @lastId)", normNone);
            Assert.Contains("ORDER BY td.DetectTime ASC, td.ID ASC", normNone);

            // 3. Snapshot
            var querySnap = DataExportService.BuildQuery(packetSnap, tables, new DateTime(2025, 1, 1));
            var normSnap = NormalizeSql(querySnap.Sql);
            Assert.DoesNotContain("__rowid", normSnap);
            Assert.DoesNotContain("@lastId", normSnap);
            Assert.Contains("ORDER BY ISNULL(td.DetectTime, td.CreateTime) DESC", normSnap);
            Assert.DoesNotContain("td.ID ASC", normSnap);
        }

        [Fact]
        public void BuildQuery_WhenExtraWhereUsesUndeclaredAlias_Throws_Test()
        {
            var packet = new ShareDataPacket { Code = "TEST_EW_ALIAS", FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "RootTbl", Alias = "r", IsRoot = true, FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]" },
                new()
                {
                    TableName = "ApplyTbl",
                    Alias = "a",
                    IsRoot = false,
                    JoinType = "OUTER APPLY",
                    JoinCondition = "a.Id = r.Id",
                    ExtraWhere = "ghost.Col IS NOT NULL",
                    ApplyTopN = 1,
                    ApplyOrderBy = "ExecutedDate",
                    FieldsJson = "[{\"fieldKey\":\"rd\",\"column\":\"RowData\"}]"
                }
            };

            Assert.Throws<InvalidOperationException>(() => DataExportService.BuildQuery(packet, tables));
        }

        [Fact]
        public void BuildQuery_WhenApplyTopNWithoutOrderBy_Throws_Test()
        {
            var packet = new ShareDataPacket { Code = "TEST_TOPN_NO_ORDER", FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "RootTbl", Alias = "r", IsRoot = true, FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]" },
                new()
                {
                    TableName = "ApplyTbl",
                    Alias = "a",
                    IsRoot = false,
                    JoinType = "OUTER APPLY",
                    JoinCondition = "a.Id = r.Id",
                    ApplyTopN = 1,
                    ApplyOrderBy = null,
                    FieldsJson = "[{\"fieldKey\":\"rd\",\"column\":\"RowData\"}]"
                }
            };

            Assert.Throws<InvalidOperationException>(() => DataExportService.BuildQuery(packet, tables));
        }

        [Fact]
        public void BuildQuery_WhenOuterApplyHasEmptyFields_Throws_Test()
        {
            var packet = new ShareDataPacket { Code = "TEST_APPLY_EMPTY_FIELDS", FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot };
            var tables = new List<ShareDataTable>
            {
                new() { TableName = "RootTbl", Alias = "r", IsRoot = true, FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]" },
                new()
                {
                    TableName = "ApplyTbl",
                    Alias = "a",
                    IsRoot = false,
                    JoinType = "OUTER APPLY",
                    JoinCondition = "a.Id = r.Id",
                    ApplyTopN = 1,
                    ApplyOrderBy = "ID",
                    FieldsJson = "[]"
                }
            };

            Assert.Throws<InvalidOperationException>(() => DataExportService.BuildQuery(packet, tables));
        }

        [Fact]
        public void BuildQuery_WhenFallbackNone_DoesNotWrapWithIsNull_Test()
        {
            var packet = new ShareDataPacket { Code = "TEST_FALLBACK_NONE", FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental };
            var tables = new List<ShareDataTable>
            {
                new()
                {
                    TableName = "RootTbl",
                    Alias = "r",
                    IsRoot = true,
                    IncrementalColumn = "TransactionDateTime",
                    IncrementalFallbackColumn = "NONE",
                    FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]"
                }
            };

            var result = DataExportService.BuildQuery(packet, tables, new DateTime(2026, 8, 22));
            var normSql = NormalizeSql(result.Sql);

            Assert.Contains("WHERE (r.TransactionDateTime > @lastTime OR (r.TransactionDateTime = @lastTime AND r.ID > @lastId))", normSql);
            Assert.DoesNotContain("ISNULL(r.TransactionDateTime", normSql);
        }

        [Fact]
        public void Transform_Packet101_KeysOrderMatchesOrderNo1To12_Test()
        {
            var def = PacketMetadataCatalogTest.All["101"];
            var queryResult = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));

            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z01",
                    ["zoneName"] = "Zone 01",
                    ["fromLocationKm"] = 10,
                    ["fromLocationMet"] = 500,
                    ["toLocationKm"] = 20,
                    ["toLocationMet"] = 0,
                    ["laneId"] = "L01",
                    ["averageSpeed"] = 75.5m,
                    ["trafficCondition"] = "NORMAL",
                    ["dataTime"] = DateTime.Now,
                    ["speedLimit"] = 100,
                    ["vehicleCount"] = 250
                }
            };

            var transformed = DataExportService.Transform(rawRows, queryResult.AllFields);
            Assert.Single(transformed);

            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(transformed[0]);
            var expectedKeys = new[]
            {
                "zoneId", "zoneName", "fromLocationKm", "fromLocationMet",
                "toLocationKm", "toLocationMet", "laneId", "averageSpeed",
                "trafficCondition", "dataTime", "speedLimit", "vehicleCount"
            };

            Assert.Equal(expectedKeys, row.Keys.ToArray());
        }

        [Theory]
        [InlineData("101")]
        [InlineData("102")]
        [InlineData("103")]
        [InlineData("104")]
        [InlineData("105")]
        [InlineData("106")]
        [InlineData("107")]
        [InlineData("108")]
        [InlineData("109")]
        [InlineData("111")]
        public void BuildQuery_All11Packets_SelectAndFromJoinMatchGoldenSql_Test(string packetCode)
        {
            var def = PacketMetadataCatalogTest.All[packetCode];
            var result = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));
            var normActual = NormalizeSql(result.Sql);

            var golden = GoldenSqlCatalog[packetCode];
            var normGoldenSelect = NormalizeSql(golden.SelectClause);
            var normGoldenFromJoin = NormalizeSql(golden.FromJoinClause);

            Assert.Contains(normGoldenSelect, normActual);
            Assert.Contains(normGoldenFromJoin, normActual);

            var businessPredicate = GetBusinessPredicate(golden.WhereClause);
            if (!string.IsNullOrEmpty(businessPredicate))
            {
                Assert.Contains(NormalizeSql(businessPredicate), normActual);
            }

            if (def.Packet.TopN.HasValue && def.Packet.TopN.Value > 0)
            {
                Assert.Contains($"TOP {def.Packet.TopN.Value}", normActual);
                Assert.Contains("ORDER BY", normActual);
            }
        }

        public static string GetBusinessPredicate(string goldenWhere)
        {
            if (string.IsNullOrWhiteSpace(goldenWhere)) return "";
            var clean = Regex.Replace(goldenWhere, @"^\s*WHERE\s+", "", RegexOptions.IgnoreCase).Trim();

            var segments = new List<string>();
            int depth = 0;
            int lastStart = 0;

            for (int i = 0; i < clean.Length; i++)
            {
                if (clean[i] == '(') depth++;
                else if (clean[i] == ')') depth--;
                else if (depth == 0 && i >= 4 && clean.Substring(i - 4, 4).Equals(" AND", StringComparison.OrdinalIgnoreCase))
                {
                    segments.Add(clean.Substring(lastStart, i - 4 - lastStart).Trim());
                    lastStart = i;
                }
            }
            if (lastStart < clean.Length)
            {
                segments.Add(clean.Substring(lastStart).Trim());
            }

            var keptSegments = new List<string>();
            foreach (var segment in segments)
            {
                var s = segment.StartsWith("AND ", StringComparison.OrdinalIgnoreCase) ? segment.Substring(4).Trim() : segment;
                if (!s.Contains("@lastTime", StringComparison.OrdinalIgnoreCase) && !s.Contains("@lastId", StringComparison.OrdinalIgnoreCase))
                {
                    keptSegments.Add(s);
                }
            }

            return string.Join(" AND ", keptSegments);
        }

        [Fact]
        public void GetBusinessPredicate_WithNestedParentheses_StripsTimeConditionCorrectly_Test()
        {
            var sql = "WHERE (i.State IS NULL OR (i.State != 'FINISHED' AND i.State != 'CANCELED')) AND ISNULL(CAST(i.UpdateTime AS INT), 0) > @lastTime";
            var result = GetBusinessPredicate(sql);
            Assert.Equal("(i.State IS NULL OR (i.State != 'FINISHED' AND i.State != 'CANCELED'))", result);

            var sql2 = "WHERE (ISNULL(td.DetectTime, td.CreateTime) > @lastTime OR (ISNULL(td.DetectTime, td.CreateTime) = @lastTime AND td.ID > @lastId)) AND td.KmNumber = 100";
            var result2 = GetBusinessPredicate(sql2);
            Assert.Equal("td.KmNumber = 100", result2);
        }

        /// <summary>
        /// Gói 110 thay đổi cấu trúc OUTER APPLY có chủ ý so với SQL vàng vì SQL vàng dùng subquery tương quan.
        /// Test riêng gói 110 theo cấu trúc mới và assert có business predicate.
        /// </summary>
        [Fact]
        public void BuildQuery_Packet110_MatchesNewStructure_Test()
        {
            var def = PacketMetadataCatalogTest.All["110"];
            var result = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));
            var normActual = NormalizeSql(result.Sql);

            var golden = GoldenSqlCatalog["110"];
            var businessPredicate = GetBusinessPredicate(golden.WhereClause);
            
            Assert.Contains(NormalizeSql(businessPredicate), normActual);
            
            // Expected new From/Join structure with INNER JOIN in OUTER APPLY
            Assert.Contains("OUTER APPLY (SELECT TOP 1 v.RowData FROM VmsCurrent v INNER JOIN TmsEquipment e2 ON v.EquipmentId = e2.ID WHERE e2.KmNumber = i.KmNumber AND (v.RowData IS NOT NULL) ORDER BY v.ExecutedDate DESC) v", normActual);
        }

        [Fact]
        public void BuildQuery_ApplyJoinMissingCondition_Throws_Test()
        {
            var def = PacketMetadataCatalogTest.All["110"];
            var tablesJson = JsonSerializer.Serialize(def.Tables);
            var tables = JsonSerializer.Deserialize<List<ShareDataTable>>(tablesJson)!;
            tables.First(t => t.TableName == "VmsCurrent").ApplyJoinCondition = null;

            Action act = () => DataExportService.BuildQuery(def.Packet, tables, new DateTime(2026, 8, 22));
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("không đủ bộ 3 trường Table/Alias/Condition", ex.Message);
        }

        [Fact]
        public void BuildQuery_ApplyJoinAliasConflict_Throws_Test()
        {
            var def = PacketMetadataCatalogTest.All["110"];
            var tablesJson = JsonSerializer.Serialize(def.Tables);
            var tables = JsonSerializer.Deserialize<List<ShareDataTable>>(tablesJson)!;
            tables.First(t => t.TableName == "VmsCurrent").ApplyJoinAlias = "i"; // 'i' is already used by TmsIncident

            Action act = () => DataExportService.BuildQuery(def.Packet, tables, new DateTime(2026, 8, 22));
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("trùng với bí danh đã khai báo bên ngoài", ex.Message);
        }

        [Fact]
        public void BuildQuery_OuterApplyMissingColumn_Throws_Test()
        {
            var def = PacketMetadataCatalogTest.All["110"];
            var tablesJson = JsonSerializer.Serialize(def.Tables);
            var tables = JsonSerializer.Deserialize<List<ShareDataTable>>(tablesJson)!;
            var fields = JsonSerializer.Deserialize<List<PacketFieldDto>>(tables.First(t => t.TableName == "VmsCurrent").FieldsJson!)!;
            fields[0].Column = null;
            tables.First(t => t.TableName == "VmsCurrent").FieldsJson = JsonSerializer.Serialize(fields);

            Action act = () => DataExportService.BuildQuery(def.Packet, tables, new DateTime(2026, 8, 22));
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("thiếu định nghĩa Column", ex.Message);
        }

        [Fact]
        public void Transform_WhenIntFieldConvertedToMs_RetainsDecimalPlaces_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["speedLimit"] = 80
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "speedLimit", Column = "MaxSpeed", DataType = "int", Unit = "km/h" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "speedLimit", TargetUnit = "m/s", TargetKey = "speedLimit" }
            };

            var result = DataExportService.Transform(rawRows, fields, mappingItems);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(22.2222m, row["speedLimit"]);
        }

        [Fact]
        public void UnitConverter_TryConvert_ReturnsExpectedFlags_Test()
        {
            var r1 = UnitConverter.TryConvert(80, "km/h", "m/s", out var res1);
            Assert.True(r1);
            Assert.Equal(22.2222m, res1);

            var r2 = UnitConverter.TryConvert(100, "m", "m", out var res2);
            Assert.False(r2);
            Assert.Equal(100, res2);

            var unknownTriggered = false;
            var r3 = UnitConverter.TryConvert(50, "furlongs", "parsecs", out var res3, "fieldX", (f, fromU, toU) =>
            {
                unknownTriggered = true;
            });
            Assert.False(r3);
            Assert.Equal(50, res3);
            Assert.True(unknownTriggered);
        }

        [Fact]
        public void Transform_WhenIntFieldKmConvertedToM_ProducesExpectedNumber_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["distance"] = 5
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "distance", Column = "Distance", DataType = "int", Unit = "km" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "distance", TargetUnit = "m", TargetKey = "distance" }
            };

            var result = DataExportService.Transform(rawRows, fields, mappingItems);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(5000m, row["distance"]);
        }

        [Fact]
        public void Transform_WhenIntFieldWithoutTargetUnit_CoercesToInt_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["speedLimit"] = "80"
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "speedLimit", Column = "MaxSpeed", DataType = "int", Unit = "km/h" }
            };

            var result = DataExportService.Transform(rawRows, fields);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(80, row["speedLimit"]);
            Assert.IsType<int>(row["speedLimit"]);
        }

        [Fact]
        public async Task ExecuteExport_WhenIntFieldConvertedWithDecimals_LogsAlertEsh1204OncePerField_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = CreateWorker(scope);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"PKT_1204_{uniqueId}";

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet 1204 {uniqueId}",
                PacketVersion = "1.0",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
                ExtraWhere = $"zs.ZoneId = 'Z_1204_{uniqueId}'",
                IsActive = true,
                FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                {
                    new() { FieldKey = "zoneId", Column = "ZoneId", Required = true },
                    new() { FieldKey = "speedLimit", Column = "AverageSpeed", DataType = "int", Unit = "km/h" }
                })
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_1204_{uniqueId}", $"SUB_1204_{uniqueId}", packetCode);

            var mappingId = Guid.NewGuid().ToString("N");
            await db.Insertable(new ShareDataMapping
            {
                ID = mappingId,
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                Direction = sub.Direction,
                Format = sub.Format,
                IsActive = true,
                ItemsJson = JsonSerializer.Serialize(new List<MappingItemDto>
                {
                    new() { FieldKey = "speedLimit", TargetUnit = "m/s", TargetKey = "speedLimit" }
                })
            }).ExecuteCommandAsync();

            var id1 = Guid.NewGuid().ToString("N");
            var id2 = Guid.NewGuid().ToString("N");
            await db.Insertable(new List<TmsZoneStatus>
            {
                new() { ID = id1, ZoneId = $"Z_1204_{uniqueId}", AverageSpeed = "80", UpdateTime = DateTime.Now },
                new() { ID = id2, ZoneId = $"Z_1204_{uniqueId}", AverageSpeed = "90", UpdateTime = DateTime.Now }
            }).ExecuteCommandAsync();

            try
            {
                var exportedAt = DateTime.Now;
                var (lastTimeRunUpdate, lastIdUpdate) = await service.ExecuteExportForSubscription(db, sub, partner, exportedAt, CancellationToken.None);

                Assert.NotNull(lastTimeRunUpdate);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1204")
                    .ToListAsync();

                Assert.Single(alerts);
                Assert.Equal("warning", alerts[0].Severity);
                Assert.Equal("funnel", alerts[0].AlertSource);
                Assert.Contains("speedLimit", alerts[0].Message);
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().Where(z => z.ID == id1 || z.ID == id2).ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task ExecuteExport_WhenMappingHasMultipleExpressions_LogsAlertEsh1203ExactlyOnce_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = CreateWorker(scope);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"PKT_1203_{uniqueId}";

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet 1203 {uniqueId}",
                PacketVersion = "1.0",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
                ExtraWhere = $"zs.ZoneId = 'Z_1203_{uniqueId}'",
                IsActive = true,
                FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                {
                    new() { FieldKey = "zoneId", Column = "ZoneId", Required = true },
                    new() { FieldKey = "fieldA", Column = "Condition" },
                    new() { FieldKey = "fieldB", Column = "AverageSpeed" }
                })
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_1203_{uniqueId}", $"SUB_1203_{uniqueId}", packetCode);

            var mappingId = Guid.NewGuid().ToString("N");
            await db.Insertable(new ShareDataMapping
            {
                ID = mappingId,
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                Direction = sub.Direction,
                Format = sub.Format,
                IsActive = true,
                ItemsJson = JsonSerializer.Serialize(new List<MappingItemDto>
                {
                    new() { FieldKey = "fieldA", Expression = "CONCAT(fieldA, '_custom')" },
                    new() { FieldKey = "fieldB", Expression = "fieldB * 2" }
                })
            }).ExecuteCommandAsync();

            var id1 = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZoneStatus
            {
                ID = id1,
                ZoneId = $"Z_1203_{uniqueId}",
                Condition = "1",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            try
            {
                var exportedAt = DateTime.Now;
                var (lastTimeRunUpdate, lastIdUpdate) = await service.ExecuteExportForSubscription(db, sub, partner, exportedAt, CancellationToken.None);

                Assert.NotNull(lastTimeRunUpdate);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1203")
                    .ToListAsync();

                Assert.Single(alerts);
                Assert.Equal("warning", alerts[0].Severity);
                Assert.Equal("funnel", alerts[0].AlertSource);
                Assert.Contains("2 biểu thức expression", alerts[0].Message);
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().Where(z => z.ID == id1).ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task ExecuteExport_WhenIncrementalPacketHasNullWatermark_DoesNotAdvanceLastTimeRunToClock_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = CreateWorker(scope);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"PKT_A1_{uniqueId}";

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet A1 {uniqueId}",
                PacketVersion = "1.0",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
                IncrementalColumn = "UpdateTime",
                IncrementalFallbackColumn = "NONE",
                ExtraWhere = $"zs.ZoneId = 'ZONE_A1_{uniqueId}'",
                IsActive = true,
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"ZoneId\"}]"
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_A1_{uniqueId}", $"SUB_A1_{uniqueId}", packetCode, s =>
            {
                s.LastTimeRun = null;
                s.LastId = null;
            });

            var statusId = Guid.NewGuid().ToString("N");
            var zoneId = $"ZONE_A1_{uniqueId}";
            await db.Insertable(new TmsZoneStatus
            {
                ID = statusId,
                ZoneId = zoneId,
                UpdateTime = null
            }).ExecuteCommandAsync();

            try
            {
                var exportedAt = DateTime.Now;
                var (lastTimeRunUpdate, lastIdUpdate) = await service.ExecuteExportForSubscription(db, sub, partner, exportedAt, CancellationToken.None);

                Assert.Null(lastTimeRunUpdate);
                Assert.Null(lastIdUpdate);
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().Where(z => z.ID == statusId).ExecuteCommandAsync();
            }
        }

        [Fact]
        public void BuildQuery_WatermarkExpressionIdenticalInSelectWhereOrderBy_Test()
        {
            var packet = new ShareDataPacket
            {
                ID = "packet_test_a4",
                Code = "101",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                IsActive = true
            };

            var tableWithFallback = new ShareDataTable
            {
                ID = "tbl_1",
                PacketCode = "101",
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                IncrementalColumn = "UpdateTime",
                IncrementalFallbackColumn = "CreateTime",
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"ZoneId\"}]"
            };

            var lastTime = new DateTime(2026, 8, 23, 10, 0, 0);
            var queryResult1 = DataExportService.BuildQuery(packet, [tableWithFallback], lastTime, "last_id_01");

            var selectMatch1 = Regex.Match(queryResult1.Sql, @"(?i)(.*?)\s+AS\s+__watermark");
            Assert.True(selectMatch1.Success, "Không tìm thấy SELECT ... AS __watermark");
            var selectWatermarkExpr1 = selectMatch1.Groups[1].Value.Trim();

            var whereMatch1 = Regex.Match(queryResult1.Sql, @"(?i)\((.*?)\s*>\s*@lastTime");
            Assert.True(whereMatch1.Success, "Không tìm thấy WHERE (watermarkExpr > @lastTime ...)");
            var whereWatermarkExpr1 = whereMatch1.Groups[1].Value.Trim();

            var orderMatch1 = Regex.Match(queryResult1.Sql, @"(?i)ORDER\s+BY\s+(.*?)\s+ASC");
            Assert.True(orderMatch1.Success, "Không tìm thấy ORDER BY watermarkExpr ASC");
            var orderWatermarkExpr1 = orderMatch1.Groups[1].Value.Trim();

            Assert.Equal("ISNULL(zs.UpdateTime, zs.CreateTime)", selectWatermarkExpr1);
            Assert.Equal(selectWatermarkExpr1, whereWatermarkExpr1);
            Assert.Equal(selectWatermarkExpr1, orderWatermarkExpr1);

            var tableNoFallback = new ShareDataTable
            {
                ID = "tbl_2",
                PacketCode = "101",
                Alias = "t",
                TableName = "TmsTrafficData",
                IsRoot = true,
                IncrementalColumn = "DetectTime",
                IncrementalFallbackColumn = "NONE",
                FieldsJson = "[{\"fieldKey\":\"speed\",\"column\":\"Speed\"}]"
            };

            var queryResult2 = DataExportService.BuildQuery(packet, [tableNoFallback], lastTime, "last_id_02");

            var selectMatch2 = Regex.Match(queryResult2.Sql, @"(?i)(.*?)\s+AS\s+__watermark");
            Assert.True(selectMatch2.Success);
            var selectWatermarkExpr2 = selectMatch2.Groups[1].Value.Trim();

            var whereMatch2 = Regex.Match(queryResult2.Sql, @"(?i)\((.*?)\s*>\s*@lastTime");
            Assert.True(whereMatch2.Success);
            var whereWatermarkExpr2 = whereMatch2.Groups[1].Value.Trim();

            var orderMatch2 = Regex.Match(queryResult2.Sql, @"(?i)ORDER\s+BY\s+(.*?)\s+ASC");
            Assert.True(orderMatch2.Success);
            var orderWatermarkExpr2 = orderMatch2.Groups[1].Value.Trim();

            Assert.Equal("t.DetectTime", selectWatermarkExpr2);
            Assert.Equal(selectWatermarkExpr2, whereWatermarkExpr2);
            Assert.Equal(selectWatermarkExpr2, orderWatermarkExpr2);
        }

        [Fact]
        public void Transform_WhenTwoFieldsShareSameTargetKey_OverwritesAndTriggersWarning_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["fieldA"] = "ValueA",
                    ["fieldB"] = "ValueB"
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "fieldA", Column = "fieldA" },
                new() { FieldKey = "fieldB", Column = "fieldB" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "fieldA", TargetKey = "sameKey" },
                new() { FieldKey = "fieldB", TargetKey = "sameKey" }
            };

            var warnings = new List<(string TargetKey, string OldField, string NewField)>();
            var result = DataExportService.Transform(rawRows, fields, mappingItems, onDuplicateTargetKey: (key, oldF, newF) =>
            {
                warnings.Add((key, oldF, newF));
            });

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.True(row.ContainsKey("sameKey"));
            Assert.Equal("ValueB", row["sameKey"]);
            Assert.Single(warnings);
            Assert.Equal("sameKey", warnings[0].TargetKey);
        }

        [Fact]
        public void GenerateExportRelativePath_WhenTwoSubsShareSecondAndHaveSameSerialNbr_DiscriminatorIncludesSubId_Test()
        {
            var sub1 = new ShareDataSubscription
            {
                ID = "SUB_ID_11111111",
                SerialNbr = 5
            };

            var sub2 = new ShareDataSubscription
            {
                ID = "SUB_ID_22222222",
                SerialNbr = 5
            };

            var time = new DateTime(2026, 8, 23, 15, 30, 45);
            var path1 = DataExportService.GenerateExportRelativePath("PARTNER_A", "101", time, DataExportService.ResolveFileDiscriminator(sub1));
            var path2 = DataExportService.GenerateExportRelativePath("PARTNER_A", "101", time, DataExportService.ResolveFileDiscriminator(sub2));

            Assert.NotEqual(path1, path2);
            Assert.Contains("5_SUB_ID_1", path1);
            Assert.Contains("5_SUB_ID_2", path2);
        }

        [Fact]
        public void ParseCodeValues_WithValidAndCorruptedJson_ParsesCorrectly_Test()
        {
            var validJson = "[{\"sourceValue\":\"1\",\"standardValue\":\"slow\",\"displayName\":\"Chậm\",\"orderNo\":1},{\"sourceValue\":\"2\",\"standardValue\":\"normal\",\"displayName\":\"Bình thường\",\"isDefault\":true,\"orderNo\":2}]";
            var parsed = DataExportService.ParseCodeValues(validJson);
            Assert.Equal(2, parsed.Count);
            Assert.Equal("1", parsed[0].SourceValue);
            Assert.Equal("slow", parsed[0].StandardValue);
            Assert.Equal("Chậm", parsed[0].DisplayName);
            Assert.Equal(1, parsed[0].OrderNo);
            Assert.True(parsed[1].IsDefault);

            var empty = DataExportService.ParseCodeValues("{invalid-json}");
            Assert.Empty(empty);

            var partialJson = "[{\"sourceValue\":\"1\",\"standardValue\":\"slow\"}, \"bad_element\", {\"sourceValue\":\"2\",\"standardValue\":\"normal\"}]";
            var partialParsed = DataExportService.ParseCodeValues(partialJson);
            Assert.Equal(2, partialParsed.Count);
        }

        [Fact]
        public void MapCode_StandardMappingAndDefaultFallback_BehavesCorrectly_Test()
        {
            var codeValues = new List<CodeValueDto>
            {
                new() { SourceValue = "1", StandardValue = "slow", DisplayName = "Chậm" },
                new() { SourceValue = "2", StandardValue = "normal", DisplayName = "Bình thường" },
                new() { SourceValue = null, StandardValue = "unknown", DisplayName = "Không rõ", IsDefault = true }
            };

            var r1 = DataExportService.MapCode(codeValues, "1");
            Assert.Equal("slow", r1);

            var r2 = DataExportService.MapCode(codeValues, "2");
            Assert.Equal("normal", r2);

            var r3 = DataExportService.MapCode(codeValues, "999");
            Assert.Equal("unknown", r3);

            var noDefaultSet = new List<CodeValueDto>
            {
                new() { SourceValue = "A", StandardValue = "Alpha" }
            };
            var r4 = DataExportService.MapCode(noDefaultSet, "Z");
            Assert.Equal("Z", r4);
        }

        [Fact]
        public void UnitConverter_TwoWayConversionsAndRounding_BehavesCorrectly_Test()
        {
            var r1 = UnitConverter.Convert(62.5m, "km/h", "m/s");
            Assert.Equal(17.3611m, r1);

            var r2 = UnitConverter.Convert(10m, "m/s", "km/h");
            Assert.Equal(36.0m, r2);

            var r3 = UnitConverter.Convert(5.5m, "km", "m");
            Assert.Equal(5500.0m, r3);

            var r4 = UnitConverter.Convert(1500m, "m", "km");
            Assert.Equal(1.5m, r4);

            var r5 = UnitConverter.Convert(2.345m, "m", "cm");
            Assert.Equal(234.5m, r5);

            var r6 = UnitConverter.Convert(2500m, "kg", "tấn");
            Assert.Equal(2.5m, r6);

            var r7 = UnitConverter.Convert(3m, "tấn", "kg");
            Assert.Equal(3000.0m, r7);

            var r8 = UnitConverter.Convert(100m, "m", "m");
            Assert.Equal(100m, r8);

            var unknownTriggered = false;
            var r9 = UnitConverter.Convert(50m, "furlongs", "parsecs", "fieldX", (f, fromU, toU) =>
            {
                unknownTriggered = true;
            });
            Assert.Equal(50m, r9);
            Assert.True(unknownTriggered);
        }

        [Fact]
        public void Transform_Step2AndStep3Sequence_ConvertsStandardThenPartnerCodeSet_Test()
        {
            var codeSets = new Dictionary<string, List<CodeValueDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["TRAFFIC_COND_STD"] =
                [
                    new() { SourceValue = "1", StandardValue = "slow", DisplayName = "Chậm (std)" },
                    new() { SourceValue = "2", StandardValue = "normal", DisplayName = "Bình thường (std)" }
                ],
                ["TRAFFIC_COND_PARTNER"] =
                [
                    new() { SourceValue = "slow", StandardValue = "Chậm", DisplayName = "Chậm (vn)" },
                    new() { SourceValue = "normal", StandardValue = "Bình thường", DisplayName = "Bình thường (vn)" }
                ]
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["condition"] = "1"
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "condition", Column = "Condition", CodeSetCode = "TRAFFIC_COND_STD", DataType = "string" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "condition", CodeSetId = "TRAFFIC_COND_PARTNER", TargetKey = "tinhTrang" }
            };

            var result = DataExportService.Transform(rawRows, fields, mappingItems, codeSets: codeSets);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.True(row.ContainsKey("tinhTrang"));
            Assert.Equal("Chậm", row["tinhTrang"]);
        }

        [Fact]
        public void Transform_WhenFieldHasCodeSet_DoesNotCoerceToNumber_Test()
        {
            var codeSets = new Dictionary<string, List<CodeValueDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["COND_SET"] =
                [
                    new() { SourceValue = "1", StandardValue = "slow" }
                ]
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["condition"] = "1"
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "condition", Column = "Condition", CodeSetCode = "COND_SET", DataType = "decimal" }
            };

            var result = DataExportService.Transform(rawRows, fields, codeSets: codeSets);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("slow", row["condition"]);
            Assert.IsType<string>(row["condition"]);
        }

        [Fact]
        public void Transform_WhenRequiredFieldMissing_ThrowsEshMissingRequiredFieldException_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z01",
                    ["averageSpeed"] = null
                },
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z02",
                    ["averageSpeed"] = 70.0m
                }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId", Required = true },
                new() { FieldKey = "averageSpeed", Column = "AverageSpeed", Required = true, DataType = "decimal" }
            };

            var ex = Assert.Throws<EshMissingRequiredFieldException>(() =>
            {
                DataExportService.Transform(rawRows, fields);
            });

            Assert.Contains("averageSpeed", ex.MissingFieldKeys);
            Assert.Equal(1, ex.MissingRowCount);
            Assert.Equal(2, ex.TotalRowCount);
        }

        [Fact]
        public async Task ExecuteExport_WhenRequiredFieldMissing_AbortsExportAndLogsAlertEsh1202_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = CreateWorker(scope);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"PKT_REQ_{uniqueId}";

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet Req {uniqueId}",
                PacketVersion = "1.0",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
                IncrementalColumn = "UpdateTime",
                IncrementalFallbackColumn = "CreateTime",
                ExtraWhere = $"zs.ZoneId = 'Z_{uniqueId}'",
                IsActive = true,
                FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                {
                    new() { FieldKey = "zoneId", Column = "ZoneId", Required = true },
                    new() { FieldKey = "averageSpeed", Column = "AverageSpeed", Required = true }
                })
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_REQ_{uniqueId}", $"SUB_REQ_{uniqueId}", packetCode, s =>
            {
                s.LastTimeRun = new DateTime(2026, 1, 1);
            });

            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZoneStatus
            {
                ID = statusId,
                ZoneId = $"Z_{uniqueId}",
                AverageSpeed = null,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            try
            {
                var exportedAt = DateTime.Now;
                var (lastTimeRunUpdate, lastIdUpdate) = await service.ExecuteExportForSubscription(db, sub, partner, exportedAt, CancellationToken.None);

                Assert.Null(lastTimeRunUpdate);
                Assert.Null(lastIdUpdate);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1202")
                    .ToListAsync();

                Assert.NotEmpty(alerts);
                Assert.Equal("error", alerts[0].Severity);
                Assert.Equal("funnel", alerts[0].AlertSource);
                Assert.Contains("averageSpeed", alerts[0].Message);
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().Where(z => z.ID == statusId).ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task ExecuteExport_WhenCodeSetMissingInDb_LogsAlertEsh1201AndContinuesExport_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = CreateWorker(scope);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"PKT_MISS_CS_{uniqueId}";

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet Miss CS {uniqueId}",
                PacketVersion = "1.0",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
                ExtraWhere = $"zs.ZoneId = 'Z_{uniqueId}'",
                IsActive = true,
                FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                {
                    new() { FieldKey = "zoneId", Column = "ZoneId", Required = true },
                    new() { FieldKey = "trafficCondition", Column = "Condition", CodeSetCode = "NON_EXISTING_CODESET_1201" }
                })
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_MISS_CS_{uniqueId}", $"SUB_MISS_CS_{uniqueId}", packetCode);

            var missCsStatusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZoneStatus
            {
                ID = missCsStatusId,
                ZoneId = $"Z_{uniqueId}",
                Condition = "1",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            try
            {
                var exportedAt = DateTime.Now;
                var (lastTimeRunUpdate, lastIdUpdate) = await service.ExecuteExportForSubscription(db, sub, partner, exportedAt, CancellationToken.None);

                Assert.NotNull(lastTimeRunUpdate);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1201")
                    .ToListAsync();

                Assert.NotEmpty(alerts);
                Assert.Equal("warning", alerts[0].Severity);
                Assert.Equal("funnel", alerts[0].AlertSource);
                Assert.Contains("NON_EXISTING_CODESET_1201", alerts[0].Message);
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().Where(z => z.ID == missCsStatusId).ExecuteCommandAsync();
            }
        }

        [Theory]
        [InlineData("101")]
        [InlineData("102")]
        [InlineData("103")]
        [InlineData("104")]
        [InlineData("105")]
        [InlineData("106")]
        [InlineData("107")]
        [InlineData("108")]
        [InlineData("109")]
        [InlineData("110")]
        [InlineData("111")]
        public void BuildQuery_EveryPacketMetadata_ProducesSafeDeterministicSql_Test(string packetCode)
        {
            var def = PacketMetadataCatalogTest.All[packetCode];

            // 1. Chạy với lastTimeRun = null
            var resultNullTime = DataExportService.BuildQuery(def.Packet, def.Tables, null);
            AssertDeterministicProperties(resultNullTime.Sql, def.Packet);

            // 2. Chạy với lastTimeRun có giá trị
            var resultWithTime = DataExportService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));
            AssertDeterministicProperties(resultWithTime.Sql, def.Packet);

            static void AssertDeterministicProperties(string sql, ShareDataPacket packet)
            {
                var norm = NormalizeSql(sql);

                // Nếu có TOP ở ngoài cùng thì phải có ORDER BY
                if (Regex.IsMatch(norm, @"^SELECT TOP \d+"))
                {
                    Assert.Contains("ORDER BY", norm);
                }

                // Nếu có OUTER APPLY với TOP thì trong ngoặc phải có ORDER BY
                if (norm.Contains("OUTER APPLY", StringComparison.OrdinalIgnoreCase) && 
                    norm.Contains("SELECT TOP", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Contains("ORDER BY", norm, StringComparison.OrdinalIgnoreCase);
                }

                // An toàn SQL: không chứa .*, không chứa >=, không chứa ký tự cấm
                Assert.DoesNotContain(".*", norm);
                Assert.DoesNotContain(">=", norm);
                Assert.DoesNotContain(";", norm);
                Assert.DoesNotContain("--", norm);
                Assert.DoesNotContain("/*", norm);
            }
        }

        private static readonly Dictionary<string, (string SelectClause, string FromJoinClause, string WhereClause)> GoldenSqlCatalog = new()
        {
            ["101"] = (
                "zs.ZoneId AS zoneId, z.Name AS zoneName, z.FromKmNumber AS fromLocationKm, z.FromMetNumber AS fromLocationMet, z.ToKmNumber AS toLocationKm, z.ToMetNumber AS toLocationMet, z.LaneId AS laneId, CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed, zs.Condition AS trafficCondition, zs.UpdateTime AS dataTime, z.MaxSpeed AS speedLimit, ts.TotalVehicleNumber AS vehicleCount",
                "FROM TmsZoneStatus zs LEFT JOIN TmsZone z ON zs.ZoneId = z.ID LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId",
                ""
            ),
            ["102"] = (
                "e.Code AS cameraCode, c.Name AS cameraName, c.SnapshotUrl AS snapshot, c.SnapshotTime AS snapshotTime, c.DeviceState AS deviceState, e.KmNumber AS locationKm, e.MetNumber AS locationMet, e.DirectionId AS direction",
                "FROM CctvDevice c LEFT JOIN TmsEquipment e ON c.Ip = e.Ip",
                ""
            ),
            ["103"] = (
                "td.ID AS detectionId, td.DetectTime AS detectTime, td.Type AS vehicleType, td.LicensePlate AS licensePlate, td.Speed AS speed, td.Lane AS lane, td.Direction AS direction, td.Location AS locationRoute, td.EquipmentId AS equipmentId, e.KmNumber AS locationKm, e.MetNumber AS locationMet",
                "FROM TmsTrafficData td LEFT JOIN TmsEquipment e ON td.EquipmentId = e.ID",
                "WHERE td.DetectTime >= @lastTime"
            ),
            ["104"] = (
                "w.RefId AS weatherStationId, w.LocationDetail AS locationDetail, w.Temperature AS temperature, w.Hudmidity AS humidity, w.WindSpeed AS windSpeed, w.WindDirection AS windDirection, w.Rain AS rainfall, w.RainHour AS rainfallHour, w.Foresight AS visibility, w.Description AS weatherDescription, w.ShortDescription AS weatherCode, w.TimeDetect AS detectTime",
                "FROM TmsWeather w",
                "WHERE w.TimeDetect >= @lastTime"
            ),
            ["105"] = (
                "t.TransactionId AS transactionId, t.TagId AS tagId, ISNULL(t.PlateEdit, t.PlateLpr) AS licensePlate, t.VehicleTypeId AS vehicleTypeId, t.TransactionDateTimeIn AS entryTime, t.TransactionDateTime AS exitTime, t.LaneId AS laneId, t.StationId AS stationId, vr.Brand AS vehicleBrand, vr.Owner AS vehicleOwner",
                "FROM TollTransactionOut t LEFT JOIN TmsVehicleRegistration vr ON ISNULL(t.PlateEdit, t.PlateLpr) = vr.LicensePlate",
                "WHERE t.TransactionDateTime >= @lastTime"
            ),
            ["106"] = (
                "td.DetectTime AS detectTime, td.Lane AS lane, td.Location AS locationCode, td.Speed AS speed, td.Height AS height, td.Width AS width, td.Length AS length",
                "FROM TmsTrafficData td",
                "WHERE td.DetectTime >= @lastTime"
            ),
            ["107"] = (
                "i.Code AS incidentCode, i.Name AS incidentName, i.EventTypeId AS eventTypeId, et.Name AS eventTypeName, i.StartDate AS occurredTime, i.KmNumber AS locationKm, i.MetNumber AS locationMet, i.Location AS locationRoute, i.InfluenceScope AS direction, i.InjuredNumber AS injuredCount, i.VehicleNumber AS vehicleCount, i.State AS incidentState, i.Description AS description, i.Source AS source",
                "FROM TmsIncident i LEFT JOIN TmsEventType et ON i.EventTypeId = et.ID",
                "WHERE ISNULL(i.UpdateTime, i.StartDate) >= @lastTime"
            ),
            ["108"] = (
                "e.Code AS equipmentCode, v.Name AS vmsName, e.KmNumber AS locationKm, e.MetNumber AS locationMet, e.DirectionId AS direction, e.LaneId AS laneId, v.RowData AS displayContent, v.Url AS displayImageUrl, v.Size AS displaySize, v.Priority AS priority, v.ExecutedDate AS executedTime",
                "FROM VmsCurrent v LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID",
                ""
            ),
            ["109"] = (
                "t.TransactionId AS transactionId, t.TransactionDateTimeIn AS entryTime, t.TransactionDateTime AS exitTime, t.VehicleTypeId AS vehicleTypeId, ISNULL(t.PlateEdit, t.PlateLpr) AS licensePlate, t.TagId AS tagId, t.LaneId AS laneId, l.Name AS laneName, t.StationId AS stationId, s.Name AS stationName, CAST(NULL AS DECIMAL(18, 2)) AS tollPrice, t.SyncTime AS syncTime",
                "FROM TollTransactionOut t LEFT JOIN TollLane l ON t.LaneId = l.LaneId LEFT JOIN TollStation s ON t.StationId = s.StationId",
                "WHERE t.TransactionDateTime >= @lastTime"
            ),
            ["110"] = (
                "CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, '')) AS incidentMessage, v.RowData AS guidanceContent, i.KmNumber AS locationKm, i.MetNumber AS locationMet, i.StartDate AS publishedTime",
                "FROM TmsIncident i OUTER APPLY (SELECT TOP 1 e.ID FROM TmsEquipment e WHERE e.KmNumber = i.KmNumber ORDER BY e.ID DESC) e OUTER APPLY (SELECT TOP 1 v.RowData FROM VmsCurrent v WHERE v.EquipmentId = e.ID AND v.RowData IS NOT NULL ORDER BY v.ExecutedDate DESC) v",
                "WHERE (i.State IS NULL OR (i.State != 'FINISHED' AND i.State != 'CANCELED' AND i.State != 'Closed' AND i.State != 'Cancelled')) AND ISNULL(i.UpdateTime, i.StartDate) >= @lastTime"
            ),
            ["111"] = (
                "i.Code AS incidentCode, i.Name AS incidentName, i.KmNumber AS locationKm, i.MetNumber AS locationMet, i.Description AS description",
                "FROM TmsIncident i",
                ""
            )
        };

        private static string NormalizeSql(string sql)
        {
            var lines = sql.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l));
            var singleLine = string.Join(" ", lines);
            return System.Text.RegularExpressions.Regex.Replace(singleLine, @"\s+", " ").Trim();
        }

        /// <summary>
        /// Danh mục metadata chuẩn cho 11 gói tin chia sẻ dữ liệu (101 - 111) phục vụ kiểm thử tự động.
        /// Định nghĩa chi tiết cấu trúc bảng (ShareDataTable) và các trường (FieldsJson).
        /// Author: Đạt
        /// Created date: 22/08/2026
        /// </summary>
        public static class PacketMetadataCatalogTest
        {
            public class PacketDefinition
            {
                public ShareDataPacket Packet { get; set; } = new();
                public List<ShareDataTable> Tables { get; set; } = [];
            }

            private static readonly Dictionary<string, PacketDefinition> Definitions = BuildDefinitions();

            public static IReadOnlyDictionary<string, PacketDefinition> All => Definitions;

            public static PacketDefinition Get(string packetCode)
            {
                if (Definitions.TryGetValue(packetCode, out var def))
                    return def;

                throw new KeyNotFoundException($"Không tìm thấy định nghĩa metadata cho gói tin '{packetCode}'.");
            }

            public static PacketDefinition Get(ShareDataEnum.DatatypeIdEnum datatypeEnum) =>
                Get(((int)datatypeEnum).ToString());

            public static bool TryGet(string? packetCode, out PacketDefinition? definition)
            {
                definition = null;
                if (string.IsNullOrWhiteSpace(packetCode))
                    return false;

                return Definitions.TryGetValue(packetCode.Trim(), out definition);
            }

            /// <summary>
            /// Nạp cấu hình metadata của gói tin vào CSDL test (idempotent, không làm sai lệch static cache).
            /// </summary>
            public static async Task SeedPacketToDb(ISqlSugarClient db, string packetCode)
            {
                var def = Get(packetCode);

                var existingPacket = await db.Queryable<ShareDataPacket>()
                    .Where(p => p.Code == def.Packet.Code && p.IsDelete == null)
                    .FirstAsync();

                string packetId;
                if (existingPacket == null)
                {
                    packetId = Guid.NewGuid().ToString("N");
                    var newPacket = new ShareDataPacket
                    {
                        ID = packetId,
                        Code = def.Packet.Code,
                        Name = def.Packet.Name,
                        PacketVersion = def.Packet.PacketVersion,
                        FilterMode = def.Packet.FilterMode,
                        TopN = def.Packet.TopN,
                        TableCount = def.Tables.Count,
                        FieldCount = def.Packet.FieldCount,
                        IsActive = true
                    };
                    await db.Insertable(newPacket).ExecuteCommandAsync();
                }
                else
                {
                    packetId = existingPacket.ID;
                    existingPacket.PacketVersion = def.Packet.PacketVersion;
                    existingPacket.FilterMode = def.Packet.FilterMode;
                    existingPacket.TopN = def.Packet.TopN;
                    existingPacket.TableCount = def.Tables.Count;
                    existingPacket.FieldCount = def.Packet.FieldCount;
                    existingPacket.IsActive = true;
                    await db.Updateable(existingPacket).ExecuteCommandAsync();
                }

                await db.Deleteable<ShareDataTable>().Where(t => t.PacketCode == def.Packet.Code).ExecuteCommandAsync();

                var tablesToInsert = def.Tables.Select(tbl => new ShareDataTable
                {
                    ID = Guid.NewGuid().ToString("N"),
                    PacketCode = tbl.PacketCode,
                    SchemaName = tbl.SchemaName,
                    TableName = tbl.TableName,
                    Alias = tbl.Alias,
                    IsRoot = tbl.IsRoot,
                    JoinType = tbl.JoinType,
                    JoinCondition = tbl.JoinCondition,
                    ApplyJoinTable = tbl.ApplyJoinTable,
                    ApplyJoinAlias = tbl.ApplyJoinAlias,
                    ApplyJoinCondition = tbl.ApplyJoinCondition,
                    ExtraWhere = tbl.ExtraWhere,
                    ApplyTopN = tbl.ApplyTopN,
                    ApplyOrderBy = tbl.ApplyOrderBy,
                    IncrementalColumn = tbl.IncrementalColumn,
                    IncrementalFallbackColumn = tbl.IncrementalFallbackColumn,
                    FieldsJson = tbl.FieldsJson,
                    OrderNo = tbl.OrderNo,
                    IsActive = true
                }).ToList();

                await db.Insertable(tablesToInsert).ExecuteCommandAsync();
            }

            /// <summary>
            /// Nạp toàn bộ 11 gói tin vào CSDL test.
            /// </summary>
            public static async Task SeedAllPacketsToDb(ISqlSugarClient db)
            {
                foreach (var kvp in Definitions)
                {
                    await SeedPacketToDb(db, kvp.Key);
                }
            }

            private static Dictionary<string, PacketDefinition> BuildDefinitions()
            {
                var map = new Dictionary<string, PacketDefinition>();

                // ─── 101. TrafficFlow ───
                map["101"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_101",
                        Code = "101",
                        Name = "Thông tin chung / luồng giao thông",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        IsActive = true,
                        TableCount = 3,
                        FieldCount = 12
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_101_1",
                            PacketCode = "101",
                            Alias = "zs",
                            SchemaName = "dbo",
                            TableName = "TmsZoneStatus",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "UpdateTime",
                            IncrementalFallbackColumn = "CreateTime",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "zoneId", Column = "ZoneId", DataType = "string", Required = true, OrderNo = 1 },
                                new() { FieldKey = "averageSpeed", Column = "AverageSpeed", Expression = "CAST(zs.AverageSpeed AS DECIMAL(18, 2))", DataType = "decimal", Unit = "km/h", Required = true, OrderNo = 8 },
                                new() { FieldKey = "trafficCondition", Column = "Condition", CodeSetCode = "TRAFFIC_COND", DataType = "string", Required = true, OrderNo = 9 },
                                new() { FieldKey = "dataTime", Column = "UpdateTime", DataType = "datetime", Required = true, OrderNo = 10 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_101_2",
                            PacketCode = "101",
                            Alias = "z",
                            SchemaName = "dbo",
                            TableName = "TmsZone",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "zs.ZoneId = z.ID",
                            OrderNo = 2,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "zoneName", Column = "Name", DataType = "string", OrderNo = 2 },
                                new() { FieldKey = "fromLocationKm", Column = "FromKmNumber", Unit = "km", DataType = "int", OrderNo = 3 },
                                new() { FieldKey = "fromLocationMet", Column = "FromMetNumber", Unit = "m", DataType = "int", OrderNo = 4 },
                                new() { FieldKey = "toLocationKm", Column = "ToKmNumber", Unit = "km", DataType = "int", OrderNo = 5 },
                                new() { FieldKey = "toLocationMet", Column = "ToMetNumber", Unit = "m", DataType = "int", OrderNo = 6 },
                                new() { FieldKey = "laneId", Column = "LaneId", CodeSetCode = "LANE_DIR", DataType = "string", OrderNo = 7 },
                                new() { FieldKey = "speedLimit", Column = "MaxSpeed", Unit = "km/h", DataType = "int", OrderNo = 11 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_101_3",
                            PacketCode = "101",
                            Alias = "ts",
                            SchemaName = "dbo",
                            TableName = "TmsTrafficStatistic",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "zs.ZoneId = ts.ZoneId",
                            OrderNo = 3,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "vehicleCount", Column = "TotalVehicleNumber", DataType = "int", OrderNo = 12 }
                            })
                        }
                    ]
                };

                // ─── 102. CctvImage (SNAPSHOT) ───
                map["102"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_102",
                        Code = "102",
                        Name = "Dữ liệu hình ảnh giao thông (CCTV)",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                        IsActive = true,
                        TableCount = 2,
                        FieldCount = 8
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_102_1",
                            PacketCode = "102",
                            Alias = "c",
                            SchemaName = "dbo",
                            TableName = "CctvDevice",
                            IsRoot = true,
                            OrderNo = 1,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "cameraName", Column = "Name", DataType = "string", OrderNo = 2 },
                                new() { FieldKey = "snapshot", Column = "SnapshotUrl", DataType = "string", OrderNo = 3 },
                                new() { FieldKey = "snapshotTime", Column = "SnapshotTime", DataType = "datetime", OrderNo = 4 },
                                new() { FieldKey = "deviceState", Column = "DeviceState", DataType = "int", OrderNo = 5 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_102_2",
                            PacketCode = "102",
                            Alias = "e",
                            SchemaName = "dbo",
                            TableName = "TmsEquipment",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "c.Ip = e.Ip",
                            OrderNo = 2,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "cameraCode", Column = "Code", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 6 },
                                new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 7 },
                                new() { FieldKey = "direction", Column = "DirectionId", DataType = "int", OrderNo = 8 }
                            })
                        }
                    ]
                };

                // ─── 103. VehicleDetection ───
                map["103"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_103",
                        Code = "103",
                        Name = "Dữ liệu dò xe (VDS)",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        TopN = 50,
                        IsActive = true,
                        TableCount = 2,
                        FieldCount = 11
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_103_1",
                            PacketCode = "103",
                            Alias = "td",
                            SchemaName = "dbo",
                            TableName = "TmsTrafficData",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "DetectTime",
                            IncrementalFallbackColumn = "CreateTime",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "detectionId", Column = "ID", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "detectTime", Column = "DetectTime", DataType = "datetime", Required = true, OrderNo = 2 },
                                new() { FieldKey = "vehicleType", Column = "Type", DataType = "string", OrderNo = 3 },
                                new() { FieldKey = "licensePlate", Column = "LicensePlate", DataType = "string", OrderNo = 4 },
                                new() { FieldKey = "speed", Column = "Speed", DataType = "decimal", Unit = "km/h", OrderNo = 5 },
                                new() { FieldKey = "lane", Column = "Lane", DataType = "string", OrderNo = 6 },
                                new() { FieldKey = "direction", Column = "Direction", DataType = "string", OrderNo = 7 },
                                new() { FieldKey = "locationRoute", Column = "Location", DataType = "string", OrderNo = 8 },
                                new() { FieldKey = "equipmentId", Column = "EquipmentId", DataType = "string", OrderNo = 9 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_103_2",
                            PacketCode = "103",
                            Alias = "e",
                            SchemaName = "dbo",
                            TableName = "TmsEquipment",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "td.EquipmentId = e.ID",
                            OrderNo = 2,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 10 },
                                new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 11 }
                            })
                        }
                    ]
                };

                // ─── 104. Weather ───
                map["104"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_104",
                        Code = "104",
                        Name = "Dữ liệu thời tiết",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        IsActive = true,
                        TableCount = 1,
                        FieldCount = 12
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_104_1",
                            PacketCode = "104",
                            Alias = "w",
                            SchemaName = "dbo",
                            TableName = "TmsWeather",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "TimeDetect",
                            IncrementalFallbackColumn = "CreateTime",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "weatherStationId", Column = "RefId", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "locationDetail", Column = "LocationDetail", DataType = "string", OrderNo = 2 },
                                new() { FieldKey = "temperature", Column = "Temperature", DataType = "decimal", Unit = "°C", OrderNo = 3 },
                                new() { FieldKey = "humidity", Column = "Hudmidity", DataType = "decimal", Unit = "%", OrderNo = 4 },
                                new() { FieldKey = "windSpeed", Column = "WindSpeed", DataType = "decimal", Unit = "m/s", OrderNo = 5 },
                                new() { FieldKey = "windDirection", Column = "WindDirection", DataType = "string", OrderNo = 6 },
                                new() { FieldKey = "rainfall", Column = "Rain", DataType = "decimal", Unit = "mm", OrderNo = 7 },
                                new() { FieldKey = "rainfallHour", Column = "RainHour", DataType = "decimal", Unit = "mm", OrderNo = 8 },
                                new() { FieldKey = "visibility", Column = "Foresight", DataType = "decimal", Unit = "m", OrderNo = 9 },
                                new() { FieldKey = "weatherDescription", Column = "Description", DataType = "string", OrderNo = 10 },
                                new() { FieldKey = "weatherCode", Column = "ShortDescription", DataType = "string", OrderNo = 11 },
                                new() { FieldKey = "detectTime", Column = "TimeDetect", DataType = "datetime", Required = true, OrderNo = 12 }
                            })
                        }
                    ]
                };

                // ─── 105. VehicleIdentification ───
                map["105"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_105",
                        Code = "105",
                        Name = "Dữ liệu định danh phương tiện (AVI/RFID)",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        IsActive = true,
                        TableCount = 2,
                        FieldCount = 10
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_105_1",
                            PacketCode = "105",
                            Alias = "t",
                            SchemaName = "dbo",
                            TableName = "TollTransactionOut",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "TransactionDateTime",
                            IncrementalFallbackColumn = "CreateTime",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "transactionId", Column = "TransactionId", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "tagId", Column = "TagId", DataType = "string", OrderNo = 2 },
                                new() { FieldKey = "licensePlate", Expression = "ISNULL(t.PlateEdit, t.PlateLpr)", DataType = "string", OrderNo = 3 },
                                new() { FieldKey = "vehicleTypeId", Column = "VehicleTypeId", DataType = "string", OrderNo = 4 },
                                new() { FieldKey = "entryTime", Column = "TransactionDateTimeIn", DataType = "datetime", OrderNo = 5 },
                                new() { FieldKey = "exitTime", Column = "TransactionDateTime", DataType = "datetime", OrderNo = 6 },
                                new() { FieldKey = "laneId", Column = "LaneId", DataType = "string", OrderNo = 7 },
                                new() { FieldKey = "stationId", Column = "StationId", DataType = "string", OrderNo = 8 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_105_2",
                            PacketCode = "105",
                            Alias = "vr",
                            SchemaName = "dbo",
                            TableName = "TmsVehicleRegistration",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "ISNULL(t.PlateEdit, t.PlateLpr) = vr.LicensePlate",
                            OrderNo = 2,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "vehicleBrand", Column = "Brand", DataType = "string", OrderNo = 9 },
                                new() { FieldKey = "vehicleOwner", Column = "Owner", DataType = "string", OrderNo = 10 }
                            })
                        }
                    ]
                };

                // ─── 106. WeighInMotion ───
                map["106"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_106",
                        Code = "106",
                        Name = "Dữ liệu kiểm tra tải trọng xe (WIM)",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        TopN = 50,
                        IsActive = true,
                        TableCount = 1,
                        FieldCount = 7
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_106_1",
                            PacketCode = "106",
                            Alias = "td",
                            SchemaName = "dbo",
                            TableName = "TmsTrafficData",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "DetectTime",
                            IncrementalFallbackColumn = "CreateTime",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "detectTime", Column = "DetectTime", DataType = "datetime", Required = true, OrderNo = 1 },
                                new() { FieldKey = "lane", Column = "Lane", DataType = "string", OrderNo = 2 },
                                new() { FieldKey = "locationCode", Column = "Location", DataType = "string", OrderNo = 3 },
                                new() { FieldKey = "speed", Column = "Speed", DataType = "decimal", Unit = "km/h", OrderNo = 4 },
                                new() { FieldKey = "height", Column = "Height", DataType = "decimal", Unit = "cm", OrderNo = 5 },
                                new() { FieldKey = "width", Column = "Width", DataType = "decimal", Unit = "cm", OrderNo = 6 },
                                new() { FieldKey = "length", Column = "Length", DataType = "decimal", Unit = "cm", OrderNo = 7 }
                            })
                        }
                    ]
                };

                // ─── 107. TrafficIncident ───
                map["107"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_107",
                        Code = "107",
                        Name = "Thông tin sự kiện giao thông",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        IsActive = true,
                        TableCount = 2,
                        FieldCount = 14
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_107_1",
                            PacketCode = "107",
                            Alias = "i",
                            SchemaName = "dbo",
                            TableName = "TmsIncident",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "UpdateTime",
                            IncrementalFallbackColumn = "StartDate",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "incidentCode", Column = "Code", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "incidentName", Column = "Name", DataType = "string", OrderNo = 2 },
                                new() { FieldKey = "eventTypeId", Column = "EventTypeId", DataType = "string", OrderNo = 3 },
                                new() { FieldKey = "occurredTime", Column = "StartDate", DataType = "datetime", OrderNo = 5 },
                                new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 6 },
                                new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 7 },
                                new() { FieldKey = "locationRoute", Column = "Location", DataType = "string", OrderNo = 8 },
                                new() { FieldKey = "direction", Column = "InfluenceScope", DataType = "int", OrderNo = 9 },
                                new() { FieldKey = "injuredCount", Column = "InjuredNumber", DataType = "int", OrderNo = 10 },
                                new() { FieldKey = "vehicleCount", Column = "VehicleNumber", DataType = "int", OrderNo = 11 },
                                new() { FieldKey = "incidentState", Column = "State", DataType = "string", OrderNo = 12 },
                                new() { FieldKey = "description", Column = "Description", DataType = "string", OrderNo = 13 },
                                new() { FieldKey = "source", Column = "Source", DataType = "string", OrderNo = 14 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_107_2",
                            PacketCode = "107",
                            Alias = "et",
                            SchemaName = "dbo",
                            TableName = "TmsEventType",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "i.EventTypeId = et.ID",
                            OrderNo = 2,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "eventTypeName", Column = "Name", DataType = "string", OrderNo = 4 }
                            })
                        }
                    ]
                };

                // ─── 108. VmsDisplay (SNAPSHOT) ───
                map["108"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_108",
                        Code = "108",
                        Name = "Thông tin biển báo điện tử (VMS)",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                        IsActive = true,
                        TableCount = 2,
                        FieldCount = 11
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_108_1",
                            PacketCode = "108",
                            Alias = "v",
                            SchemaName = "dbo",
                            TableName = "VmsCurrent",
                            IsRoot = true,
                            OrderNo = 1,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "vmsName", Column = "Name", DataType = "string", OrderNo = 2 },
                                new() { FieldKey = "displayContent", Column = "RowData", DataType = "string", OrderNo = 7 },
                                new() { FieldKey = "displayImageUrl", Column = "Url", DataType = "string", OrderNo = 8 },
                                new() { FieldKey = "displaySize", Column = "Size", DataType = "string", OrderNo = 9 },
                                new() { FieldKey = "priority", Column = "Priority", DataType = "int", OrderNo = 10 },
                                new() { FieldKey = "executedTime", Column = "ExecutedDate", DataType = "datetime", OrderNo = 11 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_108_2",
                            PacketCode = "108",
                            Alias = "e",
                            SchemaName = "dbo",
                            TableName = "TmsEquipment",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "v.EquipmentId = e.ID",
                            OrderNo = 2,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "equipmentCode", Column = "Code", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 3 },
                                new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 4 },
                                new() { FieldKey = "direction", Column = "DirectionId", DataType = "int", OrderNo = 5 },
                                new() { FieldKey = "laneId", Column = "LaneId", DataType = "string", OrderNo = 6 }
                            })
                        }
                    ]
                };

                // ─── 109. TollCollection ───
                map["109"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_109",
                        Code = "109",
                        Name = "Dữ liệu thu phí (ETC/MTC)",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        IsActive = true,
                        TableCount = 3,
                        FieldCount = 12
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_109_1",
                            PacketCode = "109",
                            Alias = "t",
                            SchemaName = "dbo",
                            TableName = "TollTransactionOut",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "TransactionDateTime",
                            IncrementalFallbackColumn = "CreateTime",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "transactionId", Column = "TransactionId", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "entryTime", Column = "TransactionDateTimeIn", DataType = "datetime", OrderNo = 2 },
                                new() { FieldKey = "exitTime", Column = "TransactionDateTime", DataType = "datetime", OrderNo = 3 },
                                new() { FieldKey = "vehicleTypeId", Column = "VehicleTypeId", DataType = "string", OrderNo = 4 },
                                new() { FieldKey = "licensePlate", Expression = "ISNULL(t.PlateEdit, t.PlateLpr)", DataType = "string", OrderNo = 5 },
                                new() { FieldKey = "tagId", Column = "TagId", DataType = "string", OrderNo = 6 },
                                new() { FieldKey = "laneId", Column = "LaneId", DataType = "string", OrderNo = 7 },
                                new() { FieldKey = "stationId", Column = "StationId", DataType = "string", OrderNo = 9 },
                                new() { FieldKey = "tollPrice", Expression = "CAST(NULL AS DECIMAL(18, 2))", DataType = "decimal", OrderNo = 11 },
                                new() { FieldKey = "syncTime", Column = "SyncTime", DataType = "datetime", OrderNo = 12 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_109_2",
                            PacketCode = "109",
                            Alias = "l",
                            SchemaName = "dbo",
                            TableName = "TollLane",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "t.LaneId = l.LaneId",
                            OrderNo = 2,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "laneName", Column = "Name", DataType = "string", OrderNo = 8 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_109_3",
                            PacketCode = "109",
                            Alias = "s",
                            SchemaName = "dbo",
                            TableName = "TollStation",
                            IsRoot = false,
                            JoinType = "LEFT",
                            JoinCondition = "t.StationId = s.StationId",
                            OrderNo = 3,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "stationName", Column = "Name", DataType = "string", OrderNo = 10 }
                            })
                        }
                    ]
                };

                // ─── 110. PublicMessaging ───
                map["110"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_110",
                        Code = "110",
                        Name = "Trao đổi với người tham gia giao thông",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        IsActive = true,
                        TableCount = 2,
                        FieldCount = 5
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_110_1",
                            PacketCode = "110",
                            Alias = "i",
                            SchemaName = "dbo",
                            TableName = "TmsIncident",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "UpdateTime",
                            IncrementalFallbackColumn = "StartDate",
                            ExtraWhere = "i.State IS NULL OR (i.State != 'FINISHED' AND i.State != 'CANCELED' AND i.State != 'Closed' AND i.State != 'Cancelled')",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "incidentMessage", Expression = "CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, ''))", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 3 },
                                new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 4 },
                                new() { FieldKey = "publishedTime", Column = "StartDate", DataType = "datetime", OrderNo = 5 }
                            })
                        },
                        new ShareDataTable
                        {
                            ID = "table_110_2",
                            PacketCode = "110",
                            Alias = "v",
                            SchemaName = "dbo",
                            TableName = "VmsCurrent",
                            IsRoot = false,
                            JoinType = "OUTER APPLY",
                            ApplyJoinTable = "TmsEquipment",
                            ApplyJoinAlias = "e2",
                            ApplyJoinCondition = "v.EquipmentId = e2.ID",
                            JoinCondition = "e2.KmNumber = i.KmNumber",
                            ExtraWhere = "v.RowData IS NOT NULL",
                            ApplyTopN = 1,
                            ApplyOrderBy = "ExecutedDate",
                            OrderNo = 2,
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "guidanceContent", Column = "RowData", DataType = "string", OrderNo = 2 }
                            })
                        }
                    ]
                };

                // ─── 111. InterCenterExchange ───
                map["111"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_111",
                        Code = "111",
                        Name = "Trao đổi với TT QLĐHGT tuyến",
                        PacketVersion = "1.0",
                        FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                        IsActive = true,
                        TableCount = 1,
                        FieldCount = 5
                    },
                    Tables =
                    [
                        new ShareDataTable
                        {
                            ID = "table_111_1",
                            PacketCode = "111",
                            Alias = "i",
                            SchemaName = "dbo",
                            TableName = "TmsIncident",
                            IsRoot = true,
                            OrderNo = 1,
                            IncrementalColumn = "UpdateTime",
                            IncrementalFallbackColumn = "StartDate",
                            IsActive = true,
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "incidentCode", Column = "Code", DataType = "string", OrderNo = 1 },
                                new() { FieldKey = "incidentName", Column = "Name", DataType = "string", OrderNo = 2 },
                                new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 3 },
                                new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 4 },
                                new() { FieldKey = "description", Column = "Description", DataType = "string", OrderNo = 5 }
                            })
                        }
                    ]
                };

                return map;
            }
        }
    }
}
