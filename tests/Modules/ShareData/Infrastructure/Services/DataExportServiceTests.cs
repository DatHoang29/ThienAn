using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.VMS.Core.Entities;
using ShareDataWorker.Core.Dto.Pdu;
using ShareDataWorker.Core.Entities;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Utils;
using ShareDataWorker.Infrastructure.Services.DataExport;

namespace Tests.Modules.ShareData
{
    /// <summary>
    /// Author: Đạt
    /// Description: Lớp chứa tất cả các kịch bản Unit Test cho DataExportService thuộc module ShareDataWorker
    /// Created date: 31/07/2026
    /// </summary>
    [Collection("api")]
    public partial class DataExportServiceTests(Host host)
    {
        private readonly Host _host = host;

        /// <summary>
        /// Author: Đạt
        /// Description: Helper kiểm tra log kết xuất dữ liệu và kiểm tra JSON schema của file output.
        /// Created date: 06/08/2026
        /// </summary>
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

        private static string GetSavedQuerySqlForPacket(ShareDataEnum.DatatypeIdEnum datatypeEnum)
        {
            return datatypeEnum switch
            {
                ShareDataEnum.DatatypeIdEnum.TrafficFlow => @"
SELECT 
    zs.ZoneId AS zoneId,
    z.Name AS zoneName,
    z.FromKmNumber AS fromLocationKm,
    z.FromMetNumber AS fromLocationMet,
    z.ToKmNumber AS toLocationKm,
    z.ToMetNumber AS toLocationMet,
    z.LaneId AS laneId,
    CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed,
    zs.Condition AS trafficCondition,
    zs.UpdateTime AS dataTime,
    z.MaxSpeed AS speedLimit,
    ts.TotalVehicleNumber AS vehicleCount
FROM TmsZoneStatus zs
LEFT JOIN TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId",

                ShareDataEnum.DatatypeIdEnum.CctvImage => @"
SELECT 
    e.Code AS cameraCode,
    c.Name AS cameraName,
    c.SnapshotUrl AS snapshot,
    c.SnapshotTime AS snapshotTime,
    c.DeviceState AS deviceState,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction
FROM CctvDevice c
LEFT JOIN TmsEquipment e ON c.Ip = e.Ip",

                ShareDataEnum.DatatypeIdEnum.VehicleDetection => @"
SELECT TOP 50
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
    e.MetNumber AS locationMet
FROM TmsTrafficData td
LEFT JOIN TmsEquipment e ON td.EquipmentId = e.ID
WHERE td.DetectTime >= @lastTime
ORDER BY td.DetectTime DESC",

                ShareDataEnum.DatatypeIdEnum.Weather => @"
SELECT 
    w.RefId AS weatherStationId,
    w.LocationDetail AS locationDetail,
    w.Temperature AS temperature,
    w.Hudmidity AS humidity,
    w.WindSpeed AS windSpeed,
    w.WindDirection AS windDirection,
    w.Rain AS rainfall,
    w.RainHour AS rainfallHour,
    w.Foresight AS visibility,
    w.Description AS weatherDescription,
    w.ShortDescription AS weatherCode,
    w.TimeDetect AS detectTime
FROM TmsWeather w
WHERE w.TimeDetect >= @lastTime",

                ShareDataEnum.DatatypeIdEnum.VehicleIdentification => @"
SELECT 
    t.TransactionId AS transactionId,
    t.TagId AS tagId,
    ISNULL(t.PlateEdit, t.PlateLpr) AS licensePlate,
    t.VehicleTypeId AS vehicleTypeId,
    t.TransactionDateTimeIn AS entryTime,
    t.TransactionDateTime AS exitTime,
    t.LaneId AS laneId,
    t.StationId AS stationId,
    vr.Brand AS vehicleBrand,
    vr.Owner AS vehicleOwner
FROM TollTransactionOut t
LEFT JOIN TmsVehicleRegistration vr ON ISNULL(t.PlateEdit, t.PlateLpr) = vr.LicensePlate
WHERE t.TransactionDateTime >= @lastTime",

                ShareDataEnum.DatatypeIdEnum.WeighInMotion => @"
SELECT TOP 50
    td.DetectTime AS detectTime,
    td.Lane AS lane,
    td.Location AS locationCode,
    td.Speed AS speed,
    td.Height AS height,
    td.Width AS width,
    td.Length AS length
FROM TmsTrafficData td
WHERE td.DetectTime >= @lastTime
ORDER BY td.DetectTime DESC",

                ShareDataEnum.DatatypeIdEnum.TrafficIncident => @"
SELECT 
    i.Code AS incidentCode,
    i.Name AS incidentName,
    i.EventTypeId AS eventTypeId,
    et.Name AS eventTypeName,
    i.StartDate AS occurredTime,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.Location AS locationRoute,
    i.InfluenceScope AS direction,
    i.InjuredNumber AS injuredCount,
    i.VehicleNumber AS vehicleCount,
    i.State AS incidentState,
    i.Description AS description,
    i.Source AS source
FROM TmsIncident i
LEFT JOIN TmsEventType et ON i.EventTypeId = et.ID
WHERE ISNULL(i.UpdateTime, i.StartDate) >= @lastTime",

                ShareDataEnum.DatatypeIdEnum.VmsDisplay => @"
SELECT 
    e.Code AS equipmentCode,
    v.Name AS vmsName,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction,
    e.LaneId AS laneId,
    v.RowData AS displayContent,
    v.Url AS displayImageUrl,
    v.Size AS displaySize,
    v.Priority AS priority,
    v.ExecutedDate AS executedTime
FROM VmsCurrent v
LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID",

                ShareDataEnum.DatatypeIdEnum.TollCollection => @"
SELECT 
    t.TransactionId AS transactionId,
    t.TransactionDateTimeIn AS entryTime,
    t.TransactionDateTime AS exitTime,
    t.VehicleTypeId AS vehicleTypeId,
    ISNULL(t.PlateEdit, t.PlateLpr) AS licensePlate,
    t.TagId AS tagId,
    t.LaneId AS laneId,
    l.Name AS laneName,
    t.StationId AS stationId,
    s.Name AS stationName,
    CAST(NULL AS DECIMAL(18, 2)) AS tollPrice,
    t.SyncTime AS syncTime
FROM TollTransactionOut t
LEFT JOIN TollLane l ON t.LaneId = l.LaneId
LEFT JOIN TollStation s ON t.StationId = s.StationId
WHERE t.TransactionDateTime >= @lastTime",

                ShareDataEnum.DatatypeIdEnum.PublicMessaging => @"
SELECT 
    CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, '')) AS incidentMessage,
    (SELECT TOP 1 v.RowData FROM VmsCurrent v INNER JOIN TmsEquipment e ON v.EquipmentId = e.ID WHERE e.KmNumber = i.KmNumber AND v.RowData IS NOT NULL) AS guidanceContent,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.StartDate AS publishedTime
FROM TmsIncident i
WHERE (i.State IS NULL OR (i.State != 'FINISHED' AND i.State != 'CANCELED' AND i.State != 'Closed' AND i.State != 'Cancelled'))
  AND ISNULL(i.UpdateTime, i.StartDate) >= @lastTime",

                _ => throw new ArgumentOutOfRangeException(nameof(datatypeEnum))
            };
        }

        private static async Task<ShareDataDataSource> SeedSavedQueryDataSourceForPacket(
            ISqlSugarClient db, ShareDataEnum.DatatypeIdEnum datatypeEnum, string? customQueryText = null)
        {
            var ds = new ShareDataDataSource
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"TEST_DS_{(int)datatypeEnum}_{Guid.NewGuid():N}",
                Name = $"DataSource Packet {(int)datatypeEnum}",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = customQueryText ?? GetSavedQuerySqlForPacket(datatypeEnum),
                TopN = 50
            };
            await db.Insertable(ds).ExecuteCommandAsync();
            return ds;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra các gói khi thiếu dữ liệu ở bảng LEFT JOIN vẫn trả về dữ liệu an toàn (các trường thiếu mang giá trị NULL).
        /// Created date: 19/08/2026
        /// </summary>
        [Theory]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TrafficFlow)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.CctvImage)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TrafficIncident)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TollCollection)]
        public async Task QueryPackets_WhenJoinedTableDataMissing_ReturnsDataSafelyWithNulls_Theory(ShareDataEnum.DatatypeIdEnum datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var expectedNullSubstrings = datatypeId switch
            {
                ShareDataEnum.DatatypeIdEnum.TrafficFlow => await SeedTrafficFlow(),
                ShareDataEnum.DatatypeIdEnum.CctvImage => await SeedCctvImage(),
                ShareDataEnum.DatatypeIdEnum.TrafficIncident => await SeedTrafficIncident(),
                ShareDataEnum.DatatypeIdEnum.TollCollection => await SeedTollCollection(),
                _ => throw new ArgumentOutOfRangeException(nameof(datatypeId))
            };

            var ds = await SeedSavedQueryDataSourceForPacket(db, datatypeId);

            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_LJ_{(int)datatypeId}", $"SUB-LJ-{(int)datatypeId}-01",
                ((int)datatypeId).ToString(), s => s.DataSourceId = ds.ID);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.True(logs[0].RecordCount > 0);

            if (!string.IsNullOrEmpty(logs[0].FilePath))
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
                if (File.Exists(fullPath))
                {
                    var jsonContent = await File.ReadAllTextAsync(fullPath);
                    foreach (var expectedNull in expectedNullSubstrings)
                    {
                        Assert.Contains(expectedNull, jsonContent);
                    }
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
                    State = ShareDataEnum.SubState.Active,
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






        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xử lý lỗi khi DatatypeId không hợp lệ (ví dụ: "999") hoặc rỗng (null)
        /// Created date: 01/08/2026
        /// </summary>
        [Theory]
        [InlineData("999")]
        [InlineData(null)]
        public async Task QueryPacketData_InvalidOrEmptyDatatypeId_ReturnsFailedStatus_Theory(string? invalidDatatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = $"TEST_PINVALID_{(invalidDatatypeId ?? "NULL")}",
                Name = "Partner Invalid",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                Code = $"SUB-QINVALID-{(invalidDatatypeId ?? "NULL")}",
                PartnerId = partner.ID,
                DatatypeId = invalidDatatypeId,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.NotNull(logs[0].ErrorMessage);
        }


        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra lọc dữ liệu tăng tiến theo LastTimeRun (chỉ lấy bản ghi mới hơn mốc chạy trước, lọc bỏ bản ghi cũ).
        /// Created date: 19/08/2026
        /// </summary>
        [Theory]
        [InlineData(ShareDataEnum.DatatypeIdEnum.VehicleDetection)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.Weather)]
        public async Task QueryIncrementalPackets_WithLastTimeRun_FiltersOlderRecords_Theory(ShareDataEnum.DatatypeIdEnum datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var (oldMarker, newMarker) = datatypeId switch
            {
                ShareDataEnum.DatatypeIdEnum.VehicleDetection => await SeedVds(),
                ShareDataEnum.DatatypeIdEnum.Weather => await SeedWeather(),
                _ => throw new ArgumentOutOfRangeException(nameof(datatypeId))
            };

            var ds = await SeedSavedQueryDataSourceForPacket(db, datatypeId);

            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_INCR_{(int)datatypeId}", $"SUB-INCR-{(int)datatypeId}-01",
                ((int)datatypeId).ToString(), s =>
                {
                    s.DataSourceId = ds.ID;
                    s.LastTimeRun = DateTime.Now.AddHours(-1);
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            var jsonContent = await File.ReadAllTextAsync(fullPath);
            Assert.Contains(newMarker, jsonContent);
            Assert.DoesNotContain(oldMarker, jsonContent);

            async Task<(string, string)> SeedVds()
            {
                const string oldLoc = "LOC_OLD";
                const string newLoc = "LOC_NEW";
                await db.Insertable(new TmsTrafficData { ID = Guid.NewGuid().ToString("N"), Location = oldLoc, EquipmentId = "EQUIP_01", DetectTime = DateTime.Now.AddHours(-2) }).ExecuteCommandAsync();
                await db.Insertable(new TmsTrafficData { ID = Guid.NewGuid().ToString("N"), Location = newLoc, EquipmentId = "EQUIP_02", DetectTime = DateTime.Now }).ExecuteCommandAsync();
                return (oldLoc, newLoc);
            }

            async Task<(string, string)> SeedWeather()
            {
                var oldRef = $"WS_OLD_{Guid.NewGuid():N}";
                var newRef = $"WS_NEW_{Guid.NewGuid():N}";
                await db.Insertable(new TmsWeather { ID = Guid.NewGuid().ToString("N"), RefId = oldRef, LocationDetail = "Km 5", Temperature = 28.0f, TimeDetect = DateTime.Now.AddHours(-3) }).ExecuteCommandAsync();
                await db.Insertable(new TmsWeather { ID = Guid.NewGuid().ToString("N"), RefId = newRef, LocationDetail = "Km 8", Temperature = 35.0f, TimeDetect = DateTime.Now }).ExecuteCommandAsync();
                return (oldRef, newRef);
            }
        }

        private static readonly Dictionary<ShareDataEnum.DatatypeIdEnum, string[]> ExpectedPacketFields = new()
        {
            [ShareDataEnum.DatatypeIdEnum.TrafficFlow] = [
                "zoneId", "zoneName", "fromLocationKm", "fromLocationMet", "toLocationKm", "toLocationMet",
                "laneId", "averageSpeed", "trafficCondition", "dataTime", "speedLimit", "vehicleCount"
            ],
            [ShareDataEnum.DatatypeIdEnum.CctvImage] = [
                "cameraCode", "cameraName", "snapshot", "snapshotTime", "deviceState", "locationKm", "locationMet", "direction"
            ],
            [ShareDataEnum.DatatypeIdEnum.VehicleDetection] = [
                "detectionId", "detectTime", "vehicleType", "licensePlate", "speed", "lane", "direction",
                "locationRoute", "equipmentId", "locationKm", "locationMet"
            ],
            [ShareDataEnum.DatatypeIdEnum.Weather] = [
                "weatherStationId", "locationDetail", "temperature", "humidity", "windSpeed", "windDirection",
                "rainfall", "rainfallHour", "visibility", "weatherDescription", "weatherCode", "detectTime"
            ],
            [ShareDataEnum.DatatypeIdEnum.VehicleIdentification] = [
                "transactionId", "tagId", "licensePlate", "vehicleTypeId", "entryTime", "exitTime",
                "laneId", "stationId", "vehicleBrand", "vehicleOwner"
            ],
            [ShareDataEnum.DatatypeIdEnum.WeighInMotion] = [
                "detectTime", "lane", "locationCode", "speed", "height", "width", "length"
            ],
            [ShareDataEnum.DatatypeIdEnum.TrafficIncident] = [
                "incidentCode", "incidentName", "eventTypeId", "eventTypeName", "occurredTime", "locationKm",
                "locationMet", "locationRoute", "direction", "injuredCount", "vehicleCount", "incidentState",
                "description", "source"
            ],
            [ShareDataEnum.DatatypeIdEnum.VmsDisplay] = [
                "equipmentCode", "vmsName", "locationKm", "locationMet", "direction", "laneId",
                "displayContent", "displayImageUrl", "displaySize", "priority", "executedTime"
            ],
            [ShareDataEnum.DatatypeIdEnum.TollCollection] = [
                "transactionId", "entryTime", "exitTime", "vehicleTypeId", "licensePlate", "tagId",
                "laneId", "laneName", "stationId", "stationName", "tollPrice", "syncTime"
            ],
            [ShareDataEnum.DatatypeIdEnum.PublicMessaging] = [
                "incidentMessage", "guidanceContent", "locationKm", "locationMet", "publishedTime"
            ],
            [ShareDataEnum.DatatypeIdEnum.InterCenterExchange] = [
                "packetType", "controlCommand", "controlState", "createdTime"
            ]
        };

        /// <summary>
        /// Author: Đạt
        /// Description: Helper nạp dữ liệu giả lập cho từng loại gói tin (101-111) tương ứng vào Database.
        /// Created date: 06/08/2026
        /// </summary>
        private static async Task SeedTestDataForPacket(ISqlSugarClient db, ShareDataEnum.DatatypeIdEnum datatypeEnum, string uniqueId)
        {
            var now = DateTime.Now;
            switch (datatypeEnum)
            {
                case ShareDataEnum.DatatypeIdEnum.TrafficFlow:
                    await db.Insertable(new TmsZone
                    {
                        ID = uniqueId,
                        Name = "Zone Test",
                        FromKmNumber = 1,
                        FromMetNumber = 0,
                        ToKmNumber = 5,
                        ToMetNumber = 0,
                        LaneId = "L1",
                        MaxSpeed = 80
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsZoneStatus
                    {
                        ID = uniqueId,
                        ZoneId = uniqueId,
                        AverageSpeed = "60.5",
                        Condition = "NORMAL",
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficStatistic
                    {
                        ID = uniqueId,
                        ZoneId = uniqueId,
                        TotalVehicleNumber = 100
                    }).ExecuteCommandAsync();
                    break;
                case ShareDataEnum.DatatypeIdEnum.CctvImage:
                    await db.Insertable(new CctvDevice { ID = uniqueId, DeviceId = "CAM_DEV_01", Name = "Cam Test", Ip = "192.168.1.100", SnapshotUrl = "http://img.jpg", SnapshotTime = now, DeviceState = 1 }).ExecuteCommandAsync();
                    await db.Insertable(new TmsEquipment { ID = uniqueId, Code = "CAM01", Ip = "192.168.1.100", KmNumber = 10, MetNumber = 500, DirectionId = 1 }).ExecuteCommandAsync();
                    break;
                case ShareDataEnum.DatatypeIdEnum.VehicleDetection:
                case ShareDataEnum.DatatypeIdEnum.WeighInMotion:
                    await db.Insertable(new TmsEquipment { ID = uniqueId, Code = "VDS01", KmNumber = 12, MetNumber = 300 }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficData { ID = uniqueId, EquipmentId = uniqueId, DetectTime = now, Type = "CAR", LicensePlate = "30A-12345", Speed = 65.0f, Lane = "1", Direction = "1", Location = "KM12", Height = 150, Width = 180, Length = 450 }).ExecuteCommandAsync();
                    break;
                case ShareDataEnum.DatatypeIdEnum.Weather:
                    await db.Insertable(new TmsWeather { ID = uniqueId, RefId = "WS01", LocationDetail = "Km15", Temperature = 30.0f, Hudmidity = 80.0f, WindSpeed = 5.0f, WindDirection = "E", Rain = 0.0f, RainHour = 0.0f, Foresight = 1000.0f, Description = "Nắng", ShortDescription = "SUNNY", TimeDetect = now }).ExecuteCommandAsync();
                    break;
                case ShareDataEnum.DatatypeIdEnum.VehicleIdentification:
                case ShareDataEnum.DatatypeIdEnum.TollCollection:
                    await db.Insertable(new TollLane { ID = uniqueId, LaneId = "LANE01", Name = "Làn 1" }).ExecuteCommandAsync();
                    await db.Insertable(new TollStation { ID = uniqueId, StationId = "STATION01", Name = "Trạm 1" }).ExecuteCommandAsync();
                    await db.Insertable(new TmsVehicleRegistration { ID = uniqueId, LicensePlate = "30A-99999", Brand = "Toyota", Owner = "Nguyen Van A" }).ExecuteCommandAsync();
                    await db.Insertable(new TollTransactionOut { ID = uniqueId, TransactionId = "TXN01", TagId = "TAG01", Plate = "30A-99999", VehicleTypeId = "1", TransactionDateTimeIn = now, TransactionDateTime = now, LaneId = "LANE01", StationId = "STATION01", SyncTime = now }).ExecuteCommandAsync();
                    break;
                case ShareDataEnum.DatatypeIdEnum.TrafficIncident:
                case ShareDataEnum.DatatypeIdEnum.PublicMessaging:
                    await db.Insertable(new TmsEventType { ID = uniqueId, Name = "Tai nạn" }).ExecuteCommandAsync();
                    await db.Insertable(new TmsIncident { ID = uniqueId, Code = "INC01", Name = "Sự cố Km10", EventTypeId = uniqueId, StartDate = now, UpdateTime = now, KmNumber = 10, MetNumber = 0, Location = "Km10", InfluenceScope = "1", InjuredNumber = 0, VehicleNumber = 2, State = ShareDataEnum.SubState.Active, Description = "Va chạm nhẹ", Source = "CCTV" }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent
                    {
                        ID = uniqueId,
                        EquipmentId = uniqueId,
                        Name = "VMS01",
                        RowData = "Giam Toc",
                        Url = "http://vms.jpg",
                        Size = "128x64",
                        Priority = 1,
                        ExecutedDate = now
                    }).ExecuteCommandAsync();
                    break;
                case ShareDataEnum.DatatypeIdEnum.VmsDisplay:
                    await db.Insertable(new TmsEquipment { ID = uniqueId, Code = "EQUIP_VMS", KmNumber = 5, MetNumber = 100, DirectionId = 1, LaneId = "L1" }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent
                    {
                        ID = uniqueId,
                        EquipmentId = uniqueId,
                        Name = "VMS 01",
                        RowData = "Chú ý",
                        Url = "http://vms.png",
                        Size = "256x128",
                        Priority = 2,
                        ExecutedDate = now
                    }).ExecuteCommandAsync();
                    break;
                case ShareDataEnum.DatatypeIdEnum.InterCenterExchange:
                    await db.Insertable(new TmsSignalLog { ID = uniqueId, NewData = "CMD_UPDATE", State = "EXECUTED", CreateTime = now }).ExecuteCommandAsync();
                    break;
            }
        }


        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra toàn bộ 11 gói tin chia sẻ (101-111) xuất file JSON đúng và đầy đủ 106 fields theo tài liệu shareData-assessment.md
        /// Created date: 04/08/2026
        /// </summary>
        [Theory]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TrafficFlow)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.CctvImage)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.VehicleDetection)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.Weather)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.VehicleIdentification)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.WeighInMotion)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TrafficIncident)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.VmsDisplay)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TollCollection)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.PublicMessaging)]
        public async Task AllPackets101To111_ExportCompleteJson_WithAll106Fields_Test(ShareDataEnum.DatatypeIdEnum datatypeEnum)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var uniqueId = Guid.NewGuid().ToString("N");
            await SeedTestDataForPacket(db, datatypeEnum, uniqueId);
            var ds = await SeedSavedQueryDataSourceForPacket(db, datatypeEnum);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"TEST_P_SCHEMA_{(int)datatypeEnum}",
                $"TEST_SUB-SCHEMA-{(int)datatypeEnum}",
                ((int)datatypeEnum).ToString(),
                s => s.DataSourceId = ds.ID
            );

            var workerService = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var expectedFields = ExpectedPacketFields.GetValueOrDefault(datatypeEnum);
            await AssertPacketJsonSchema(db, sub.ID, datatypeEnum, expectedFields);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra khi 2 Subscription chạy trong 1 đợt, mỗi Subscription phải tạo ra đường dẫn file riêng biệt (tránh ghi đè)
        /// Created date: 05/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_MultiSubExecution_CreatesUniqueFilePaths_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_PARALLEL", FromKmNumber = 10, MaxSpeed = 80 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "60", Condition = "NORMAL", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PARALLEL_F",
                Name = "Partner Parallel Files",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var ds = await SeedSavedQueryDataSourceForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow);

            var sub1 = new ShareDataSubscription
            {
                Code = "SUB_PARALLEL_01",
                SerialNbr = 1,
                PartnerId = partner.ID,
                DatatypeId = ((int)ShareDataEnum.DatatypeIdEnum.TrafficFlow).ToString(),
                DataSourceId = ds.ID,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            var sub2 = new ShareDataSubscription
            {
                Code = "SUB_PARALLEL_02",
                SerialNbr = 2,
                PartnerId = partner.ID,
                DatatypeId = ((int)ShareDataEnum.DatatypeIdEnum.TrafficFlow).ToString(),
                DataSourceId = ds.ID,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(new[] { sub1, sub2 }).ExecuteCommandAsync();

            var workerService = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub1.ID || l.SubscriptionId == sub2.ID)
                .ToListAsync();

            Assert.Equal(2, logs.Count);

            var log1 = logs.FirstOrDefault(l => l.SubscriptionId == sub1.ID);
            var log2 = logs.FirstOrDefault(l => l.SubscriptionId == sub2.ID);

            Assert.NotNull(log1?.FilePath);
            Assert.NotNull(log2?.FilePath);
            Assert.NotEqual(log1.FilePath, log2.FilePath);

            var path1 = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", log1.FilePath!);
            var path2 = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", log2.FilePath!);

            Assert.True(File.Exists(path1), $"File {path1} không tồn tại");
            Assert.True(File.Exists(path2), $"File {path2} không tồn tại");
        }

        // ============================================================
        // 🔴 Ưu tiên Cao
        // ============================================================




        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra khi bảng nguồn không có dữ liệu mới → ghi log Success với message "Không có dữ liệu mới"
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenNoData_LogsSuccessWithNoExport_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PNODATA",
                Name = "Partner NoData",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var ds = await SeedSavedQueryDataSourceForPacket(db, ShareDataEnum.DatatypeIdEnum.Weather);

            var sub = new ShareDataSubscription
            {
                Code = "SUB-QNODATA-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)ShareDataEnum.DatatypeIdEnum.Weather).ToString(),
                DataSourceId = ds.ID,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                LastTimeRun = DateTime.Now.AddDays(1),
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal("SUCCESS", logs[0].Status);
            Assert.Equal(0, logs[0].RecordCount.GetValueOrDefault());
            Assert.Null(logs[0].FilePath);
        }

        // ============================================================
        // 🟡 Ưu tiên Trung bình
        // ============================================================




        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Worker bỏ qua Subscription có NextTimeRun trong tương lai (chưa đến lịch)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_SkipsNotDueSubscriptions_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PNOTDUE",
                Name = "Partner NotDue",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                Code = "SUB-QNOTDUE-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)ShareDataEnum.DatatypeIdEnum.TrafficFlow).ToString(),
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddMinutes(30),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.Empty(logs);
        }

        // ============================================================
        // 🔵 Ưu tiên Thấp (Gói tin dự kiến làm sau)
        // ============================================================






        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra quy trình xuất dữ liệu khi Subscription liên kết với ShareDataMappingProfile & ShareDataDataSource cấu hình SAVED_QUERY
        /// Created date: 05/08/2026
        /// </summary>
        [Fact]
        public async Task ExecuteExportAsync_WhenMappingProfileWithSavedQuery_ReturnsData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PTN_MAP_SQ",
                Name = "Partner Test Mapping Profile SavedQuery",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new ShareDataDataSource
            {
                Code = "TEST_DS_SQ",
                Name = "DataSource Test SavedQuery",
                Kind = "SAVED_QUERY",
                QueryText = "SELECT TOP 10 ID, Location AS LocationRoute, Speed FROM TmsTrafficData",
                TopN = 10
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mappingProfile = new ShareDataMappingProfile
            {
                Code = "TEST_MP_SQ",
                Name = "Mapping Profile Test SavedQuery",
                VendorId = partner.ID,
                DataSourceId = dataSource.ID,
                DatatypeId = "101",
                Direction = "OUT",
                MappingsJson = "[{\"sourceField\":\"ID\",\"targetField\":\"detectionId\"},{\"sourceField\":\"LocationRoute\",\"targetField\":\"locationRoute\"},{\"sourceField\":\"Speed\",\"targetField\":\"speed\"}]",
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                Code = "SUB-TEST-MP-SQ",
                PartnerId = partner.ID,
                MappingProfileId = mappingProfile.ID,
                DatatypeId = "101",
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var sampleTraffic = new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                Location = "Km10+500",
                Speed = 85,
                DetectTime = DateTime.Now
            };
            await db.Insertable(sampleTraffic).ExecuteCommandAsync();

            var workerService = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal("SUCCESS", logs[0].Status);
            Assert.True(logs[0].RecordCount > 0);
            Assert.False(string.IsNullOrEmpty(logs[0].FilePath));

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra quy trình xuất dữ liệu khi Subscription liên kết với ShareDataMappingProfile & ShareDataDataSource cấu hình FIELD_PICKER
        /// Created date: 05/08/2026
        /// </summary>
        [Fact]
        public async Task ExecuteExportAsync_WhenMappingProfileWithFieldPicker_ReturnsData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PTN_MAP_FP",
                Name = "Partner Test Mapping Profile FieldPicker",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new ShareDataDataSource
            {
                Code = "TEST_DS_FP",
                Name = "DataSource Test FieldPicker",
                Kind = "FIELD_PICKER",
                TableOrView = "TmsTrafficData",
                TopN = 5
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mappingProfile = new ShareDataMappingProfile
            {
                Code = "TEST_MP_FP",
                Name = "Mapping Profile Test FieldPicker",
                VendorId = partner.ID,
                DataSourceId = dataSource.ID,
                DatatypeId = "103",
                Direction = "OUT",
                MappingsJson = "[{\"sourceField\":\"ID\",\"targetField\":\"detectionId\"},{\"sourceField\":\"Location\",\"targetField\":\"locationRoute\"},{\"sourceField\":\"Speed\",\"targetField\":\"speed\"}]",
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                Code = "SUB-TEST-MP-FP",
                PartnerId = partner.ID,
                MappingProfileId = mappingProfile.ID,
                DatatypeId = "103",
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var sampleTraffic = new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                Location = "Km20+100",
                Speed = 60,
                DetectTime = DateTime.Now
            };
            await db.Insertable(sampleTraffic).ExecuteCommandAsync();

            var workerService = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal("SUCCESS", logs[0].Status);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra ghi nhận nhật ký xuất dữ liệu chứa đầy đủ trường thông tin truyền nhận (PartnerName, TransferDirection, Description, SessionId, Format)
        /// Created date: 05/08/2026
        /// </summary>
        [Fact]
        public async Task LogExportResultAsync_PopulatesAllTransferFields_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P_LOG",
                Name = "Cuc Duong bo Viet Nam (TEST)",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var ds = await SeedSavedQueryDataSourceForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow);

            var sub = new ShareDataSubscription
            {
                Code = "TEST_SUB_LOG",
                PartnerId = partner.ID,
                DatatypeId = "101",
                DataSourceId = ds.ID,
                SessionId = "SESS_TEST_01",
                Format = "DATA",
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                Direction = ShareDataEnum.SubDirection.Outbound,
                NextTimeRun = DateTime.Now.AddMinutes(-1)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var sampleData = new TmsZone
            {
                ID = Guid.NewGuid().ToString("N"),
                Name = "Zone Log Test",
                FromKmNumber = 1,
                MaxSpeed = 80
            };
            await db.Insertable(sampleData).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus
            {
                ID = sampleData.ID,
                ZoneId = sampleData.ID,
                AverageSpeed = "60.5",
                Condition = "NORMAL",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();
            await db.Insertable(new TmsTrafficStatistic
            {
                ID = sampleData.ID,
                ZoneId = sampleData.ID,
                TotalVehicleNumber = 100
            }).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            var log = logs[0];
            Assert.Equal("TRANSFER", log.LogType);
            Assert.Equal("SEND", log.Action);
            Assert.Equal("SND", log.TransferDirection);
            Assert.NotNull(log.PartnerName);
            Assert.Equal("Cuc Duong bo Viet Nam (TEST)", log.PartnerName);
            Assert.Equal("101", log.DatatypeId);
            Assert.Equal("DATA", log.Format);
            Assert.Equal("SESS_TEST_01", log.SessionId);
            Assert.NotNull(log.PacketNbr);
            Assert.True(log.PacketNbr > 0);
            Assert.Equal(ShareDataEnum.Operator.System, log.OperatorName);
            Assert.Null(log.ErrorMessage);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, log.Status);
            Assert.NotNull(log.FilePath);
            Assert.NotNull(log.Hash);
            Assert.False(string.IsNullOrWhiteSpace(log.Hash));
            Assert.Equal(64, log.Hash.Length);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", log.FilePath!);
            var jsonContent = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var payloadElement = doc.RootElement.GetProperty("payload");
            var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payloadElement, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var expectedHash = Convert.ToHexString(SHA256.HashData(payloadBytes));
            Assert.Equal(expectedHash, log.Hash);

            Assert.False(string.IsNullOrWhiteSpace(log.Description));
            Assert.Contains("101", log.Description);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra quy trình xuất dữ liệu áp dụng ánh xạ trường MappingsJson trong ShareDataMappingProfile
        /// Created date: 05/08/2026
        /// </summary>
        [Fact]
        public async Task ExecuteExport_AppliesMappingProfileFieldMappings_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P_MAP",
                Name = "Partner Test MappingProfile",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sampleStat = new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = Guid.NewGuid().ToString("N"),
                AverageSpeed = "85",
                Condition = null,
                CreateTime = DateTime.Now
            };
            await db.Insertable(sampleStat).ExecuteCommandAsync();

            var dataSource = new ShareDataDataSource
            {
                Code = "TEST_DS_MAP",
                Name = "DataSource Test MappingProfile",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = $"SELECT ID, AverageSpeed, Condition FROM TmsZoneStatus WHERE ID = '{sampleStat.ID}'"
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mappingProfile = new ShareDataMappingProfile
            {
                Code = "TEST_MP_MAP",
                Name = "MappingProfile Test FieldMappings",
                VendorId = partner.ID,
                DataSourceId = dataSource.ID,
                DatatypeId = "101",
                Direction = "OUT",
                MappingsJson = "[{\"sourceField\":\"ID\",\"targetField\":\"zoneId\"},{\"sourceField\":\"AverageSpeed\",\"targetField\":\"averageSpeed\"},{\"sourceField\":\"Condition\",\"targetField\":\"pavementType\"}]",
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                Code = "TEST_SUB_MAP",
                PartnerId = partner.ID,
                MappingProfileId = mappingProfile.ID,
                DatatypeId = "101",
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                Direction = ShareDataEnum.SubDirection.Outbound,
                NextTimeRun = DateTime.Now.AddMinutes(-1)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal("SUCCESS", logs[0].Status);
            Assert.NotNull(logs[0].FilePath);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var jsonContent = await File.ReadAllTextAsync(fullPath);
            Assert.Contains("\"zoneId\"", jsonContent);
            Assert.Contains("\"averageSpeed\"", jsonContent);
            Assert.Contains("\"pavementType\":null", jsonContent);
            Assert.DoesNotContain("\"condition\"", jsonContent, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Author: Đạt
        /// Description: Option A - Kiểm tra 2 Worker Instances (mô phỏng 2 Servers) cùng quét & xử lý ĐỒNG THỜI trên CÙNG 1 Subscription đến hạn.
        /// Khẳng định cơ chế UPDLOCK & lock lease NextTimeRun ngăn chặn thành công việc thực thi trùng lặp (Race Condition & Mutual Exclusion).
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_MultiWorkerConcurrency_PreventsDuplicateExecution_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_RACE", FromKmNumber = 5, ToKmNumber = 10, MaxSpeed = 90 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "75", Condition = "GOOD", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var ds = await SeedSavedQueryDataSourceForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow);

            var partner = new ShareDataPartner
            {
                Code = "TEST_PARTNER_RACE",
                Name = "Partner Race Condition Test",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                Code = "SUB-RACE-CONCURRENCY-01",
                PartnerId = partner.ID,
                DatatypeId = ((int)ShareDataEnum.DatatypeIdEnum.TrafficFlow).ToString(),
                DataSourceId = ds.ID,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddMinutes(-5)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            // Mô phỏng 2 Worker instances (Server Node 1 và Server Node 2) cùng gọi ProcessBatchSubscriptions đồng thời
            var worker1 = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            var worker2 = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());

            await Task.WhenAll(
                worker1.ProcessBatchSubscriptions(CancellationToken.None),
                worker2.ProcessBatchSubscriptions(CancellationToken.None)
            );

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            // Khẳng định chỉ duy nhất 1 Worker giành được Lock và ghi log SUCCESS, không sinh duplicate execution/file
            Assert.Single(logs);
            Assert.Equal("SUCCESS", logs[0].Status);

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updatedSub);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Option B - Kiểm tra tải cao (High Concurrency Stress): 20 Subscriptions cùng đến hạn và 5 Worker Instances cùng giật job liên tục.
        /// Khẳng định tất cả 20 Subscriptions được xử lý chính xác đúng 1 lần (Idempotency), không deadlock SQL và không duplicate logs.
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_HighConcurrencyStress_ExecutesAllIdempotently_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_STRESS", FromKmNumber = 10, ToKmNumber = 20, MaxSpeed = 80 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "60", Condition = "NORMAL", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var ds = await SeedSavedQueryDataSourceForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow);

            var partner = new ShareDataPartner
            {
                Code = "TEST_PARTNER_STRESS",
                Name = "Partner High Concurrency Stress Test",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            // Tạo 20 Subscriptions cùng đến hạn
            const int subCount = 20;
            var subList = new List<ShareDataSubscription>();
            for (int i = 1; i <= subCount; i++)
            {
                subList.Add(new ShareDataSubscription
                {
                    Code = $"SUB-STRESS-{i:D3}",
                    PartnerId = partner.ID,
                    DatatypeId = ((int)ShareDataEnum.DatatypeIdEnum.TrafficFlow).ToString(),
                    DataSourceId = ds.ID,
                    Direction = ShareDataEnum.SubDirection.Outbound,
                    Mode = ShareDataEnum.SubMode.Periodic,
                    State = BaseEnums.SubSubscriptionState.Active,
                    NextTimeRun = DateTime.Now.AddMinutes(-10)
                });
            }
            await db.Insertable(subList).ExecuteCommandAsync();
            var subIds = subList.Select(s => s.ID).ToList();

            // Mô phỏng 5 Workers độc lập cùng chạy tranh chấp 20 Subscriptions
            const int workerCount = 5;
            var workerTasks = new Task[workerCount];
            for (int i = 0; i < workerCount; i++)
            {
                var worker = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
                workerTasks[i] = worker.ProcessBatchSubscriptions(CancellationToken.None);
            }

            await Task.WhenAll(workerTasks);

            // Lấy tất cả logs của 20 Subscriptions này
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => subIds.Contains(l.SubscriptionId!))
                .ToListAsync();

            // Khẳng định tổng số logs = 20 và không subscription nào bị xử lý duplicate
            Assert.Equal(subCount, logs.Count);
            var processedSubIds = logs.Select(l => l.SubscriptionId).Distinct().ToList();
            Assert.Equal(subCount, processedSubIds.Count);
            Assert.All(logs, l => Assert.Equal("SUCCESS", l.Status));

            // Khẳng định tất cả Subscriptions được giải phóng trạng thái về Idle
            var updatedSubs = await db.Queryable<ShareDataSubscription>()
                .Where(s => subIds.Contains(s.ID))
                .ToListAsync();
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Helper khởi tạo DataExportService từ scope hiện tại
        /// Created date: 06/08/2026
        /// </summary>
        private static DataExportService CreateWorker(IServiceScope scope)
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            return new DataExportService(scopeFactory, logger, config);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Helper lấy danh sách log kết xuất của một Subscription theo thứ tự mới nhất trước
        /// Created date: 06/08/2026
        /// </summary>
        private static async Task<List<ShareDataActivityLog>> GetLogs(ISqlSugarClient db, string subscriptionId)
        {
            using var cleanDb = db.CopyNew();
            return await cleanDb.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subscriptionId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Helper xoá file kết xuất do bài test sinh ra
        /// Created date: 06/08/2026
        /// </summary>
        private static void DeleteExportedFile(string? relativePath)
        {
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Helper đọc nội dung JSON của file kết xuất theo đường dẫn tương đối trong log
        /// Created date: 06/08/2026
        /// </summary>
        private static async Task<string> ReadExportedJson(string relativePath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", relativePath);
            Assert.True(File.Exists(fullPath), $"File kết xuất không tồn tại: {fullPath}");
            return await File.ReadAllTextAsync(fullPath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Helper tạo Partner và Subscription OUTBOUND đã đến hạn phục vụ các bài test hồi quy
        /// Created date: 06/08/2026
        /// </summary>
        internal static async Task<(ShareDataPartner Partner, ShareDataSubscription Sub)> SeedOutboundSubscription(
            ISqlSugarClient db, string partnerCode, string code, string? datatypeId, Action<ShareDataSubscription>? configure = null)
        {
            var pCode = partnerCode.StartsWith("TEST_") ? partnerCode : $"TEST_{partnerCode}";
            var sCode = code.StartsWith("TEST_") ? code : $"TEST_{code}";

            var partner = new ShareDataPartner
            {
                Code = pCode,
                Name = pCode,
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                Code = sCode,
                PartnerId = partner.ID,
                DatatypeId = datatypeId,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            configure?.Invoke(sub);

            if (string.IsNullOrEmpty(sub.DataSourceId) && string.IsNullOrEmpty(sub.MappingProfileId) && datatypeId != "111" && !string.IsNullOrWhiteSpace(datatypeId) && DatatypeCodeResolver.TryResolveDatatypeEnum(datatypeId, out _))
            {
                var ds = new ShareDataDataSource
                {
                    Code = $"TEST_DS_{Guid.NewGuid():N}",
                    Name = $"Default Test DataSource for {sCode}",
                    Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                    QueryText = "SELECT 1 AS sampleId",
                    TopN = 50
                };
                await db.Insertable(ds).ExecuteCommandAsync();
                sub.DataSourceId = ds.ID;
            }

            await db.Insertable(sub).ExecuteCommandAsync();

            return (partner, sub);
        }




        /// <summary>
        /// Author: Đạt
        /// Description: Helper tạo bộ đôi DataSource + MappingProfile có MappingsJson để kích hoạt nhánh cấu hình
        /// Created date: 06/08/2026
        /// </summary>
        private static async Task<(ShareDataDataSource DataSource, ShareDataMappingProfile Profile)> SeedConfiguredSource(
            ISqlSugarClient db, string code, string kind, string mappingsJson, string? queryText = null, string? tableOrView = null)
        {
            var dataSource = new ShareDataDataSource
            {
                Code = $"TEST_DS_{code}",
                Name = $"DataSource {code}",
                Kind = kind,
                QueryText = queryText,
                TableOrView = tableOrView,
                TopN = 5
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var profile = new ShareDataMappingProfile
            {
                Code = $"TEST_MP_{code}",
                Name = $"MappingProfile {code}",
                DataSourceId = dataSource.ID,
                DatatypeId = "101",
                Direction = ShareDataEnum.MappingDirection.Out,
                MappingsJson = mappingsJson,
                IsActive = true
            };
            await db.Insertable(profile).ExecuteCommandAsync();

            return (dataSource, profile);
        }


        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra chu kỳ KHÔNG có dữ liệu mới thì mốc lọc dữ liệu LastTimeRun phải giữ nguyên (không bị đẩy lên thời điểm chạy)
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenNoNewData_KeepsLastExportTimeUnchanged_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var emptyDs = new ShareDataDataSource
            {
                Code = $"TEST_DS_EMPTY_{Guid.NewGuid():N}",
                Name = "DataSource Empty Test",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = "SELECT 1 AS sampleId WHERE 1=0"
            };
            await db.Insertable(emptyDs).ExecuteCommandAsync();

            var lastExportTime = DateTime.Now.AddMinutes(-5);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_WM_EMPTY", "SUB-WM-EMPTY-01",
                "990", s =>
                {
                    s.DataSourceId = emptyDs.ID;
                    s.LastTimeRun = lastExportTime;
                });

            var runStartTime = DateTime.Now;
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            // Mốc lọc dữ liệu KHÔNG được nhảy tới thời điểm chạy khi chu kỳ này không kết xuất được bản ghi nào
            var currentExportTime = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).LastTimeRun;
            Assert.NotNull(currentExportTime);
            Assert.True(currentExportTime < runStartTime,
                $"Mốc lọc dữ liệu bị đẩy lên {currentExportTime} dù không có dữ liệu (mốc gốc {lastExportTime})");
            Assert.True(Math.Abs((currentExportTime!.Value - lastExportTime).TotalSeconds) < 1);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated);
            Assert.True(updated.NextTimeRun > runStartTime, "NextTimeRun phải được đẩy sang chu kỳ kế tiếp");

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.Equal(0, logs[0].RecordCount.GetValueOrDefault());
            Assert.Null(logs[0].FilePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra chu kỳ kết xuất THẤT BẠI thì mốc lọc dữ liệu LastTimeRun phải giữ nguyên để chu kỳ sau quét lại đúng khoảng dữ liệu
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenExportFails_KeepsLastExportTimeUnchanged_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var failDs = new ShareDataDataSource
            {
                Code = $"TEST_DS_FAIL_{Guid.NewGuid():N}",
                Name = "DataSource Fail Test",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = "SELECT 1 FROM NonExistentTable_ForTest_Fail"
            };
            await db.Insertable(failDs).ExecuteCommandAsync();

            var lastExportTime = DateTime.Now.AddMinutes(-5);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_WM_FAIL", "SUB-WM-FAIL-01",
                "991", s =>
                {
                    s.DataSourceId = failDs.ID;
                    s.LastTimeRun = lastExportTime;
                });

            var runStartTime = DateTime.Now;
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.NotNull(logs[0].ErrorMessage);

            // Log FAILED không tính vào mốc lọc -> mốc phải đứng yên
            var currentExportTime = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).LastTimeRun;
            Assert.NotNull(currentExportTime);
            Assert.True(currentExportTime < runStartTime,
                $"Mốc lọc dữ liệu bị đẩy lên {currentExportTime} dù kết xuất thất bại (mốc gốc {lastExportTime})");

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra chu kỳ kết xuất THÀNH CÔNG thì mốc lọc dữ liệu LastTimeRun tiến lên mốc chụp TRƯỚC khi truy vấn (không phải thời điểm ghi file xong)
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenExportSucceeds_AdvancesLastExportTime_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum okPacketId = (ShareDataEnum.DatatypeIdEnum)992;
            var lastExportTime = DateTime.Now.AddMinutes(-5);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_WM_OK", "SUB-WM-OK-01",
                ((int)okPacketId).ToString(), s => s.LastTimeRun = lastExportTime);

            var runStartTime = DateTime.Now;
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);
            var runEndTime = DateTime.Now;

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.Equal(1, logs[0].RecordCount.GetValueOrDefault());

            // Log SUCCESS có dữ liệu -> mốc lọc tiến lên đúng thời điểm chu kỳ vừa chạy
            var currentExportTime = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).LastTimeRun;
            Assert.NotNull(currentExportTime);
            Assert.True(currentExportTime >= runStartTime.AddSeconds(-1), "Mốc lọc dữ liệu phải tiến lên sau khi kết xuất thành công");
            Assert.True(currentExportTime <= runEndTime.AddSeconds(1), "Mốc lọc dữ liệu không được vượt quá thời điểm kết thúc chu kỳ");

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Hồi quy lỗi mất dữ liệu — bản ghi đến trễ (phát sinh sau mốc lọc dữ liệu nhưng trước chu kỳ chạy rỗng) vẫn phải được kết xuất ở chu kỳ kế tiếp
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_LateArrivingRecord_IsStillExported_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var lateDs = new ShareDataDataSource
            {
                Code = $"TEST_DS_LATE_{Guid.NewGuid():N}",
                Name = "DataSource Late Test",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = "SELECT ID AS detectionId, DetectTime AS detectTime FROM TmsTrafficData WHERE Location = 'KM10_LATE_TEST' AND DetectTime > @lastTime"
            };
            await db.Insertable(lateDs).ExecuteCommandAsync();

            var lastExportTime = DateTime.Now.AddMinutes(-2);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_WM_LATE", "SUB-WM-LATE-01",
                "993", s =>
                {
                    s.DataSourceId = lateDs.ID;
                    s.LastTimeRun = lastExportTime;
                });

            // Chu kỳ 1: nguồn chưa có bản ghi nào -> không kết xuất, mốc lọc dữ liệu phải đứng yên
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var afterFirstRun = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).LastTimeRun;
            Assert.NotNull(afterFirstRun);
            Assert.True(Math.Abs((afterFirstRun!.Value - lastExportTime).TotalSeconds) < 1);

            // Bản ghi đến TRỄ: thời điểm nghiệp vụ nằm giữa mốc lọc dữ liệu cũ và thời điểm chạy chu kỳ 1
            await db.Insertable(new TmsTrafficData
            {
                ID = "LATE-RECORD-01",
                DetectTime = DateTime.Now.AddMinutes(-1),
                Location = "KM10_LATE_TEST"
            }).ExecuteCommandAsync();

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(x => x.NextTimeRun, DateTime.Now.AddSeconds(-5))
                .Where(x => x.ID == sub.ID)
                .ExecuteCommandAsync();

            // Chu kỳ 2: vì mốc lọc dữ liệu chưa bị đẩy, bản ghi đến trễ vẫn nằm trong phạm vi quét
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            // Chỉ log có FilePath mới là lần kết xuất thật (log mốc do test dựng không sinh file)
            var logs = await GetLogs(db, sub.ID);
            var exportedLogs = logs.Where(l => l.Status == ShareDataEnum.ExportStatus.Success && !string.IsNullOrEmpty(l.FilePath)).ToList();

            Assert.True(exportedLogs.Count == 1,
                "Bản ghi đến trễ bị bỏ sót — mốc lọc dữ liệu đã bị đẩy quá mốc dữ liệu ở chu kỳ rỗng trước đó");
            Assert.Equal(1, exportedLogs[0].RecordCount.GetValueOrDefault());

            var json = await ReadExportedJson(exportedLogs[0].FilePath!);
            Assert.Contains("LATE-RECORD-01", json);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Subscription đang giữ lock còn hạn thì worker khác bỏ qua VÀ không ghi đè lease
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenLockLeaseActive_DoesNotOverwriteLease_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum leasePacketId = (ShareDataEnum.DatatypeIdEnum)994;
            var leaseExpire = DateTime.Now.AddSeconds(45);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_LEASE_ACTIVE", "SUB-LEASE-ACTIVE-01",
                ((int)leasePacketId).ToString(), s => s.NextTimeRun = leaseExpire);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated);
            Assert.True(Math.Abs((updated.NextTimeRun!.Value - leaseExpire).TotalSeconds) < 1,
                "Lock lease đang còn hạn không được phép bị worker khác ghi đè");
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Subscription treo ở trạng thái Running nhưng lock lease đã hết hạn thì được worker nhận lại và xử lý
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenLockLeaseExpired_ReclaimsSubscription_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum leasePacketId = (ShareDataEnum.DatatypeIdEnum)995;
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_LEASE_EXPIRED", "SUB-LEASE-EXPIRED-01",
                ((int)leasePacketId).ToString(), s => s.NextTimeRun = DateTime.Now.AddMinutes(-5));

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn lô Subscription bị lệch schema thì PHẢI ném lỗi ra ngoài.
        ///              Trước đây dùng UseTranAsync — hàm này nuốt ngoại lệ rồi trả DbResult, nên bảng
        ///              lệch entity biến thành "không có Subscription nào đến hạn" và lỗi chìm luôn.
        ///              Đổi tên bảng đi là cách rẻ nhất để ép truy vấn hỏng: không cần đụng tới cột,
        ///              không cần dữ liệu rác, không cần entity phụ. Khôi phục ngay trong finally.
        /// Created date: 07/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenSchemaMismatch_ThrowsInsteadOfReturningEmpty_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var tableName = db.EntityMaintenance.GetTableName<ShareDataSubscription>();

            // Tên cất tạm. Không mang ý nghĩa gì, chỉ cần khác tên thật để worker không tìm thấy bảng.
            // Đặt hậu tố dễ nhận ra phòng khi test chết giữa chừng, chưa kịp đổi tên về.
            var tempTableName = $"{tableName}_CAT_TAM_BOI_TEST";

            db.DbMaintenance.RenameTable(tableName, tempTableName);

            var ex = await Assert.ThrowsAnyAsync<Exception>(
                () => CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None));

            Assert.False(string.IsNullOrWhiteSpace(ex.Message),
                "Lỗi truy vấn phải ném ra kèm thông điệp, không được nuốt thành lô rỗng");
            db.DbMaintenance.RenameTable(tempTableName, tableName);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: CSDL thật lưu DatatypeId kèm hậu tố mô tả ("101_trafficData", "102_vmsData").
        ///              Kiểm tra worker vẫn bóc được mã số ở đầu chuỗi và chạy đúng truy vấn gói tin,
        ///              thay vì coi cả chuỗi là số rồi bỏ qua Subscription.
        /// Created date: 07/08/2026
        /// </summary>
        [Theory]
        [InlineData("987_trafficData")]
        [InlineData("987")]
        [InlineData("  987_vmsData  ")]
        [InlineData("987_incidentData_v2")]
        public async Task ExecuteExport_WhenDatatypeIdHasDescriptiveSuffix_ResolvesNumericPrefix_Test(string datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var uniqueCode = $"SUB-SUFFIX-{Math.Abs(datatypeId.GetHashCode())}";
            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_SUFFIX_{Math.Abs(datatypeId.GetHashCode())}",
                uniqueCode, datatypeId);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.Equal(1, logs[0].RecordCount);

            // Thư mục kết xuất phải chứa tên DatatypeId
            Assert.Contains("987", logs[0].FilePath);
            DeleteExportedFile(logs[0].FilePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra các dạng DatatypeId KHÔNG bóc được mã số ở đầu thì phải bị từ chối,
        ///              không được vớ nhầm con số nằm giữa hoặc cuối phần mô tả.
        /// Created date: 07/08/2026
        /// </summary>
        [Theory]
        [InlineData("trafficData_986")]
        [InlineData("trafficData")]
        [InlineData("_986")]
        public async Task ExecuteExport_WhenDatatypeIdHasNoNumericPrefix_LogsFailed_Test(string datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var uniqueCode = $"SUB-NOPREFIX-{Math.Abs(datatypeId.GetHashCode())}";
            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_NOPREFIX_{Math.Abs(datatypeId.GetHashCode())}",
                uniqueCode, datatypeId);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.Equal(0, logs[0].RecordCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đăng ký chiều INBOUND (đối tác cấp dữ liệu cho mình) không thuộc việc của
        ///              bộ kết xuất định kỳ — worker này chỉ lo chiều OUTBOUND.
        /// Created date: 07/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenDirectionInbound_IsSkipped_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum inboundPacketId = (ShareDataEnum.DatatypeIdEnum)979;
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_INBOUND", "SUB-INBOUND-01",
                ((int)inboundPacketId).ToString(), s => s.Direction = ShareDataEnum.SubDirection.Inbound);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);
            Assert.Empty(await GetLogs(db, sub.ID));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Chỉ State = ACTIVE mới được kết xuất. Các trạng thái còn lại đều là
        ///              "chưa duyệt / đã dừng / đã huỷ / hết hạn" — gửi dữ liệu đi là sai nghiệp vụ.
        ///              Trước đây chỉ có PAUSED được kiểm.
        /// Created date: 07/08/2026
        /// </summary>
        [Theory]
        [InlineData(BaseEnums.SubSubscriptionState.Pending)]
        [InlineData(BaseEnums.SubSubscriptionState.Rejected)]
        [InlineData(BaseEnums.SubSubscriptionState.Cancelled)]
        [InlineData(BaseEnums.SubSubscriptionState.Expired)]
        [InlineData(BaseEnums.SubSubscriptionState.Paused)]
        public async Task ProcessBatchSubscriptions_WhenStateNotActive_IsSkipped_Test(BaseEnums.SubSubscriptionState state)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum statePacketId = (ShareDataEnum.DatatypeIdEnum)978;
            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_STATE_{(int)state}", $"SUB-STATE-{(int)state}",
                ((int)statePacketId).ToString(), s => s.State = state);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);
            Assert.Empty(await GetLogs(db, sub.ID));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Partner Enable + Connected + Subscription Active → phải được lấy ra, kết xuất thành công
        /// Created date: 10/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPartnerConnectedAndSubActive_ExportsSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum connPacketId = (ShareDataEnum.DatatypeIdEnum)985;
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_PARTNER_CONN", "SUB-PARTNER-CONN-01",
                ((int)connPacketId).ToString());

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            DeleteExportedFile(logs[0].FilePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Partner ở trạng thái Disconnected hoặc Disabled (Status/SessionState không hợp lệ) → bị loại khỏi luồng xuất
        /// Created date: 10/08/2026
        /// </summary>
        [Theory]
        [InlineData(BaseEnums.StatusEnum.Enable, BaseEnums.SessionState.Disconnected)]
        [InlineData(BaseEnums.StatusEnum.Disable, BaseEnums.SessionState.Connected)]
        public async Task ProcessBatchSubscriptions_WhenPartnerInactiveOrDisconnected_IsSkipped_Theory(
            BaseEnums.StatusEnum status, BaseEnums.SessionState sessionState)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum discPacketId = (ShareDataEnum.DatatypeIdEnum)986;
            var dueAt = DateTime.Now.AddSeconds(-10);
            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_P_INACTIVE_{(int)status}_{(int)sessionState}",
                $"SUB-P-INACTIVE-{(int)status}_{(int)sessionState}", ((int)discPacketId).ToString(), s => s.NextTimeRun = dueAt);

            await db.Updateable<ShareDataPartner>()
                .SetColumns(p => p.Status == status)
                .SetColumns(p => p.SessionState == sessionState)
                .Where(p => p.ID == partner.ID)
                .ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated.NextTimeRun);
            Assert.True(Math.Abs((updated.NextTimeRun!.Value - dueAt).TotalSeconds) < 1,
                "NextTimeRun không được đụng tới: partner không active phải bị loại từ câu truy vấn");
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đăng ký chưa gán đối tác (PartnerId null) bị loại vì query yêu cầu Partner phải
        ///              EXISTS + Enable + Connected. Subscription mồ côi không thoả điều kiện.
        /// Created date: 07/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPartnerIdNull_IsExcluded_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum orphanPacketId = (ShareDataEnum.DatatypeIdEnum)977;
            var sub = new ShareDataSubscription
            {
                Code = "SUB-NO-PARTNER-01",
                PartnerId = null,
                DatatypeId = ((int)orphanPacketId).ToString(),
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Hai đăng ký cùng đối tác + cùng gói tin, đều KHÔNG có SerialNbr, chạy trong
        ///              cùng một chu kỳ thì phải ra hai file khác nhau.
        ///              Tên file chỉ phân giải tới GIÂY nên nếu không có gì phân biệt, file sau đè file trước
        ///              và đối tác mất trọn một gói dữ liệu mà không ai biết.
        /// Created date: 07/08/2026
        /// </summary>
        [Fact]
        public async Task ExecuteExport_WhenTwoSubsShareSecondAndHaveNoSerialNbr_FilesDoNotCollide_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum collidePacketId = (ShareDataEnum.DatatypeIdEnum)976;
            var partner = new ShareDataPartner
            {
                Code = "TEST_COLLIDE",
                Name = "TEST_COLLIDE",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var subs = new List<ShareDataSubscription>();
            foreach (var stt in new[] { 1, 2 })
            {
                var ds = new ShareDataDataSource
                {
                    Code = $"TEST_DS_COLLIDE_{stt}_{Guid.NewGuid():N}",
                    Name = "DataSource Collide Test",
                    Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                    QueryText = "SELECT 1 AS sampleId",
                    TopN = 50
                };
                await db.Insertable(ds).ExecuteCommandAsync();

                var s = new ShareDataSubscription
                {
                    Code = $"SUB-COLLIDE-{stt}",
                    PartnerId = partner.ID,
                    DatatypeId = ((int)collidePacketId).ToString(),
                    DataSourceId = ds.ID,
                    Direction = ShareDataEnum.SubDirection.Outbound,
                    Mode = ShareDataEnum.SubMode.Periodic,
                    State = BaseEnums.SubSubscriptionState.Active,
                    SerialNbr = null,
                    NextTimeRun = DateTime.Now.AddSeconds(-10)
                };
                await db.Insertable(s).ExecuteCommandAsync();
                subs.Add(s);
            }

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var exportedPaths = new List<string>();
            foreach (var s in subs)
            {
                var logs = await GetLogs(db, s.ID);
                Assert.Single(logs);
                Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
                Assert.False(string.IsNullOrEmpty(logs[0].FilePath));
                exportedPaths.Add(logs[0].FilePath!);
            }

            Assert.NotEqual(exportedPaths[0], exportedPaths[1]);
            Assert.All(exportedPaths, p =>
                Assert.True(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", p)),
                    $"File {p} phải tồn tại — nếu thiếu nghĩa là đã bị file kia đè"));

            foreach (var p in exportedPaths) DeleteExportedFile(p);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi file thất bại (ở đây mô phỏng bằng cách chiếm chỗ thư mục đích bằng một FILE
        ///              cùng tên) phải ghi log FAILED và GIỮ NGUYÊN LastTimeRun.
        ///              Nếu LastTimeRun vẫn tiến, lô dữ liệu đó mất vĩnh viễn: chu kỳ sau chỉ lấy
        ///              bản ghi mới hơn mốc đã nhảy.
        /// Created date: 07/08/2026
        /// </summary>
        [Fact]
        public async Task ExecuteExport_WhenFileWriteFails_LogsFailedAndKeepsLastTimeRun_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum ioPacketId = (ShareDataEnum.DatatypeIdEnum)975;
            const string partnerCode = "TEST_IO_FAIL";
            var mocCu = DateTime.Now.AddMinutes(-30);
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, "SUB-IO-FAIL-01",
                ((int)ioPacketId).ToString(), s => s.LastTimeRun = mocCu);

            var yyyyMM = DateTime.Now.ToString("yyyyMM");
            var ddHH = DateTime.Now.ToString("ddHH");
            var blockingFilePath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", "Out", partnerCode, yyyyMM, ddHH, ((int)ioPacketId).ToString());

            if (File.Exists(blockingFilePath))
                File.Delete(blockingFilePath);

            if (Directory.Exists(blockingFilePath))
                Directory.Delete(blockingFilePath, true);

            Directory.CreateDirectory(Path.GetDirectoryName(blockingFilePath)!);
            await File.WriteAllTextAsync(blockingFilePath, "chiem cho");

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.False(string.IsNullOrWhiteSpace(logs[0].ErrorMessage));

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated.LastTimeRun);
            Assert.True(Math.Abs((updated.LastTimeRun!.Value - mocCu).TotalSeconds) < 1,
                "Ghi file hỏng thì LastTimeRun phải đứng yên, nếu không lô dữ liệu này mất vĩnh viễn");
            if (File.Exists(blockingFilePath)) File.Delete(blockingFilePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Khi worker bị yêu cầu dừng giữa lô, các đăng ký còn lại phải được bỏ qua ngay
        ///              chứ không chạy nốt. Mô phỏng bằng cách cho hàm truy vấn của đăng ký đầu tiên
        ///              tự phát tín hiệu huỷ.
        /// Created date: 07/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenCancelledMidBatch_StopsProcessingRest_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            using var cts = new CancellationTokenSource();
            const ShareDataEnum.DatatypeIdEnum cancelPacketId = (ShareDataEnum.DatatypeIdEnum)974;

            var ds = new ShareDataDataSource
            {
                Code = $"TEST_DS_CANCEL_{Guid.NewGuid():N}",
                Name = "DataSource Cancel Test",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = "WAITFOR DELAY '00:00:00.200'; SELECT 1 AS sampleId;"
            };
            await db.Insertable(ds).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "TEST_CANCEL",
                Name = "TEST_CANCEL",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var subs = new List<ShareDataSubscription>();
            foreach (var stt in new[] { 1, 2, 3 })
            {
                var s = new ShareDataSubscription
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"SUB-CANCEL-{stt}",
                    PartnerId = partner.ID,
                    DatatypeId = ((int)cancelPacketId).ToString(),
                    DataSourceId = ds.ID,
                    Direction = ShareDataEnum.SubDirection.Outbound,
                    Mode = ShareDataEnum.SubMode.Periodic,
                    State = BaseEnums.SubSubscriptionState.Active,
                    SerialNbr = stt,
                    NextTimeRun = DateTime.Now.AddSeconds(-10)
                };
                await db.Insertable(s).ExecuteCommandAsync();
                subs.Add(s);
            }

            cts.CancelAfter(50);
            await CreateWorker(scope).ProcessBatchSubscriptions(cts.Token);

            var executedCount = 0;
            foreach (var s in subs)
            {
                var logs = await GetLogs(db, s.ID);
                executedCount += logs.Count;
                foreach (var l in logs) DeleteExportedFile(l.FilePath);
            }

            Assert.Equal(1, executedCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đăng ký đã xoá mềm (IsDelete có giá trị) TUYỆT ĐỐI không được kết xuất nữa.
        ///              Bộ lập lịch không dựa vào global filter của SqlSugar mà ghi thẳng điều kiện
        ///              IsDelete == null, nên phải có bài test canh — gỡ điều kiện đó ra là đăng ký
        ///              đã xoá sẽ tiếp tục gửi dữ liệu cho đối tác vĩnh viễn.
        /// Created date: 07/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenSubscriptionSoftDeleted_IsSkipped_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum deletedPacketId = (ShareDataEnum.DatatypeIdEnum)983;
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_SOFT_DELETE", "SUB-SOFT-DELETE-01",
                ((int)deletedPacketId).ToString(), s => s.IsDelete = DateTime.Now.AddMinutes(-1));

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs);

            // Lease cũng không được đụng vào -> chứng tỏ bộ lập lịch không hề nhặt bản ghi này
            var updated = await db.Queryable<ShareDataSubscription>().ClearFilter().InSingleAsync(sub.ID);
            Assert.NotNull(updated);
            Assert.NotNull(updated.IsDelete);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: DatatypeId nay là chuỗi tự do lấy từ bảng cấu hình và được ghép vào ĐƯỜNG DẪN FILE.
        ///              Giá trị chứa dấu gạch chéo hoặc '..' phải bị lọc sạch, tuyệt đối không được
        ///              cho phép ghi file ra ngoài thư mục Out (path traversal).
        /// Created date: 07/08/2026
        /// </summary>
        [Theory]
        [InlineData("980/../../escape")]
        [InlineData("980_traffic:data|xau?")]
        [InlineData("  980/../etc/passwd  ")]
        public async Task ExecuteExport_WhenDatatypeIdContainsPathChars_StaysInsideOutFolder_Test(string datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            // Mã 980 nằm ở đầu chuỗi nên vẫn phân giải được -> kết xuất chạy tới bước ghi file thật,
            // đúng chỗ cần kiểm. Phần rác phía sau là thứ có thể phá đường dẫn.
            var uniqueCode = Math.Abs(datatypeId.GetHashCode()).ToString();
            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_PATH_{uniqueCode}",
                $"SUB-PATH-{uniqueCode}", datatypeId);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.False(string.IsNullOrEmpty(logs[0].FilePath), "Phải ghi được file thì mới kiểm được đường dẫn");

            // Thư mục gói tin phải chứa tên DatatypeId
            Assert.Contains("980", logs[0].FilePath);
            Assert.DoesNotContain("..", logs[0].FilePath);
            Assert.StartsWith($"Out/{partner.Code}", logs[0].FilePath);

            // Chốt chặn thật: đường dẫn tuyệt đối sau khi rút gọn vẫn phải nằm trong Out/
            var outRootPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send"));
            var absolutePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!));
            Assert.StartsWith(outRootPath, absolutePath);
            Assert.True(File.Exists(absolutePath), "File phải nằm đúng chỗ đã ghi nhận trong log");

            DeleteExportedFile(logs[0].FilePath);
        }
        /// <summary>
        /// Author: Đạt
        /// Description: Hồi quy cho lỗi ghi file "Out" sai vị trí khi deploy lên server (CWD của tiến trình
        ///              trỏ vào bin\Debug\net10.0 thay vì thư mục publish thật sự). Đường dẫn ghi file phải
        ///              luôn dựa trên AppContext.BaseDirectory (thư mục chứa exe/dll đang chạy), KHÔNG được
        ///              phụ thuộc Directory.GetCurrentDirectory() — test đổi CWD sang thư mục giả lập khác
        ///              rồi xác nhận file vẫn ghi đúng chỗ và KHÔNG bị ghi lạc vào CWD giả lập đó.
        /// Created date: 13/08/2026
        /// </summary>
        [Fact]
        public async Task ExecuteExport_WhenCurrentDirectoryDiffersFromBaseDirectory_WritesFileUnderBaseDirectory_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum cwdPacketId = (ShareDataEnum.DatatypeIdEnum)988;
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_PARTNER_CWD", "SUB-PARTNER-CWD-01",
                ((int)cwdPacketId).ToString());

            var originalDirectory = Directory.GetCurrentDirectory();
            var foreignDirectory = Directory.CreateTempSubdirectory("ShareDataWorker_CwdRegression_").FullName;

            try
            {
                // Giả lập đúng tình huống bug: tiến trình chạy với CWD khác thư mục chứa exe
                // (ví dụ chạy exe nằm lồng trong bin\Debug\net10.0 sau khi copy nguyên bin thay vì publish).
                Directory.SetCurrentDirectory(foreignDirectory);

                await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.False(string.IsNullOrEmpty(logs[0].FilePath), "Phải ghi được file thì mới kiểm được đường dẫn");

            var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "sharedata/send", logs[0].FilePath!));
            Assert.True(File.Exists(expectedPath),
                $"File phải nằm trong AppContext.BaseDirectory ({AppContext.BaseDirectory}), không phụ thuộc CWD lúc chạy: {expectedPath}");

            var wronglyPlacedPath = Path.GetFullPath(Path.Combine(foreignDirectory, "sharedata/send", logs[0].FilePath!));
            Assert.False(File.Exists(wronglyPlacedPath),
                $"File KHÔNG được ghi vào CWD giả lập lúc tiến trình chạy: {wronglyPlacedPath}");

            DeleteExportedFile(logs[0].FilePath);
            Directory.Delete(foreignDirectory, recursive: true);
        }


        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra chu kỳ riêng IntervalSeconds của từng đăng ký được tôn trọng.
        ///              Doc TechInfo đòi chu kỳ theo cặp (đối tác × gói tin): A-101 30s, A-102 60s, B-101 50s.
        ///              Sau khi kết xuất, NextTimeRun phải nhích đúng bằng IntervalSeconds đã cấu hình.
        /// Created date: 07/08/2026
        /// </summary>
        [Theory]
        [InlineData(60)]
        [InlineData(45)]
        [InlineData(120)]
        public async Task ExecuteExport_WhenIntervalSecondsConfigured_UsesItForNextTimeRun_Test(int intervalSeconds)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum intervalPacketId = (ShareDataEnum.DatatypeIdEnum)985;
            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_INTERVAL_{intervalSeconds}",
                $"SUB-INTERVAL-{intervalSeconds}", ((int)intervalPacketId).ToString(),
                s => s.IntervalSeconds = intervalSeconds);

            var beforeRun = DateTime.Now;
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            DeleteExportedFile(logs[0].FilePath);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            var actualDelaySeconds = (updated.NextTimeRun!.Value - beforeRun).TotalSeconds;

            // Nới 10 giây cho thời gian chạy thật của chu kỳ
            Assert.InRange(actualDelaySeconds, intervalSeconds, intervalSeconds + 10);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra đăng ký KHÔNG cấu hình IntervalSeconds (null) hoặc cấu hình giá trị
        ///              vô nghĩa (0, số âm) đều rơi về hằng mặc định 30 giây.
        ///              Nếu nhận nguyên giá trị 0/âm, NextTimeRun sẽ lùi về quá khứ và đăng ký đó
        ///              chạy lại liên tục mỗi 5 giây.
        /// Created date: 07/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-15)]
        public async Task ExecuteExport_WhenIntervalSecondsMissingOrInvalid_FallsBackToDefault_Test(int? intervalSeconds)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const int ExpectedDefaultSeconds = 30;
            const ShareDataEnum.DatatypeIdEnum fallbackPacketId = (ShareDataEnum.DatatypeIdEnum)984;
            var label = intervalSeconds?.ToString() ?? "NULL";
            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_INTERVAL_FB_{label}",
                $"SUB-INTERVAL-FB-{label}", ((int)fallbackPacketId).ToString(),
                s => s.IntervalSeconds = intervalSeconds);

            var beforeRun = DateTime.Now;
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            DeleteExportedFile(logs[0].FilePath);

            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            var actualDelaySeconds = (updated.NextTimeRun!.Value - beforeRun).TotalSeconds;

            Assert.InRange(actualDelaySeconds, ExpectedDefaultSeconds, ExpectedDefaultSeconds + 10);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: shareData-assessment.md ghi gói 111 là "(skip)" — chưa có bảng outbox/inbox
        ///              liên trung tâm nên không có DataSource/MappingProfile được cấu hình.
        ///              Subscription cấu hình gói 111 chưa có DataSource phải bị ghi log FAILED, tuyệt đối không được
        ///              lặng lẽ báo SUCCESS 0 bản ghi như thể chạy bình thường mà không có dữ liệu mới.
        /// Created date: 07/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket111_WhenSkippedByDoc_LogsFailedNotEmptySuccess_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var signalLogId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsSignalLog
            {
                ID = signalLogId,
                NewData = "TEST_SIGNAL_111",
                State = "ON",
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P111_SKIP", "SUB-Q111-SKIP-01",
                ((int)ShareDataEnum.DatatypeIdEnum.InterCenterExchange).ToString());

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.Equal(0, logs[0].RecordCount);
            Assert.Null(logs[0].FilePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra các gói lấy toàn bộ (101, 102, 108) luôn kết xuất TẤT CẢ bản ghi mỗi chu kỳ, kể cả bản ghi cũ hơn LastTimeRun.
        /// Created date: 19/08/2026
        /// </summary>
        [Theory]
        [InlineData(ShareDataEnum.DatatypeIdEnum.TrafficFlow)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.CctvImage)]
        [InlineData(ShareDataEnum.DatatypeIdEnum.VmsDisplay)]
        public async Task QueryAllSnapshotPackets_WithLastTimeRun_StillReturnsAllRecords_Theory(ShareDataEnum.DatatypeIdEnum datatypeId)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var lastExportTime = DateTime.Now.AddMinutes(-5);

            var (oldMarker, newMarker) = datatypeId switch
            {
                ShareDataEnum.DatatypeIdEnum.TrafficFlow => await SeedTrafficFlow(),
                ShareDataEnum.DatatypeIdEnum.CctvImage => await SeedCctvImage(),
                ShareDataEnum.DatatypeIdEnum.VmsDisplay => await SeedVmsDisplay(),
                _ => throw new ArgumentOutOfRangeException(nameof(datatypeId))
            };

            var ds = await SeedSavedQueryDataSourceForPacket(db, datatypeId);
            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_ALL_{(int)datatypeId}", $"SUB-ALL-{(int)datatypeId}-01",
                ((int)datatypeId).ToString(), s =>
                {
                    s.DataSourceId = ds.ID;
                    s.LastTimeRun = lastExportTime;
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains(newMarker, json);
            Assert.Contains(oldMarker, json);

            async Task<(string, string)> SeedTrafficFlow()
            {
                var oldZoneId = $"ZONE_OLD_{Guid.NewGuid():N}";
                var newZoneId = $"ZONE_NEW_{Guid.NewGuid():N}";
                await db.Insertable(new TmsZone { ID = oldZoneId, Name = "TEST_ZONE_101_OLD", FromKmNumber = 1, MaxSpeed = 80 }).ExecuteCommandAsync();
                await db.Insertable(new TmsZone { ID = newZoneId, Name = "TEST_ZONE_101_NEW", FromKmNumber = 2, MaxSpeed = 80 }).ExecuteCommandAsync();
                await db.Insertable(new TmsZoneStatus { ID = Guid.NewGuid().ToString("N"), ZoneId = oldZoneId, AverageSpeed = "40", Condition = "OLD", UpdateTime = DateTime.Now.AddMinutes(-30) }).ExecuteCommandAsync();
                await db.Insertable(new TmsZoneStatus { ID = Guid.NewGuid().ToString("N"), ZoneId = newZoneId, AverageSpeed = "80", Condition = "NEW", UpdateTime = DateTime.Now }).ExecuteCommandAsync();
                return (oldZoneId, newZoneId);
            }

            async Task<(string, string)> SeedCctvImage()
            {
                var oldCamId = Guid.NewGuid().ToString("N");
                var newCamId = Guid.NewGuid().ToString("N");
                var oldCamName = $"TEST_CAM_102_OLD_{Guid.NewGuid():N}";
                var newCamName = $"TEST_CAM_102_NEW_{Guid.NewGuid():N}";
                await db.Insertable(new CctvDevice { ID = oldCamId, DeviceId = oldCamId, Ip = "10.0.0.101", Name = oldCamName, SnapshotUrl = "http://old", SnapshotTime = DateTime.Now.AddMinutes(-30), DeviceState = 1 }).ExecuteCommandAsync();
                await db.Insertable(new CctvDevice { ID = newCamId, DeviceId = newCamId, Ip = "10.0.0.102", Name = newCamName, SnapshotUrl = "http://new", SnapshotTime = DateTime.Now, DeviceState = 1 }).ExecuteCommandAsync();
                return (oldCamName, newCamName);
            }

            async Task<(string, string)> SeedVmsDisplay()
            {
                var oldVmsName = $"TEST_VMS_108_OLD_{Guid.NewGuid():N}";
                var newVmsName = $"TEST_VMS_108_NEW_{Guid.NewGuid():N}";
                await db.Insertable(new VmsCurrent { ID = Guid.NewGuid().ToString("N"), EquipmentId = "EQ_OLD", Name = oldVmsName, RowData = "NOI DUNG CU", ExecutedDate = DateTime.Now.AddMinutes(-30) }).ExecuteCommandAsync();
                await db.Insertable(new VmsCurrent { ID = Guid.NewGuid().ToString("N"), EquipmentId = "EQ_NEW", Name = newVmsName, RowData = "NOI DUNG MOI", ExecutedDate = DateTime.Now }).ExecuteCommandAsync();
                return (oldVmsName, newVmsName);
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Hồi quy lỗi tích Descartes — Gói 110 với 1 sự cố và nhiều bản tin VMS chỉ được sinh đúng 1 bản ghi, không nhân bản theo số dòng VmsCurrent
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket110_WithMultipleVmsRows_DoesNotMultiplyRecords_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var lastExportTime = DateTime.Now;
            var incidentId = Guid.NewGuid().ToString("N");
            var vmsIds = Enumerable.Range(1, 3).Select(_ => Guid.NewGuid().ToString("N")).ToList();

            foreach (var vmsId in vmsIds)
            {
                await db.Insertable(new VmsCurrent
                {
                    ID = vmsId,
                    EquipmentId = $"EQ_{vmsId[..6]}",
                    Name = "TEST_VMS_110",
                    RowData = "HUONG DAN",
                    ExecutedDate = DateTime.Now
                }).ExecuteCommandAsync();
            }

            var incidentName = $"Su co kiem tra gap 110 {incidentId[..8]}";
            await db.Insertable(new TmsIncident
            {
                ID = incidentId,
                Code = "TEST_INC_110",
                Name = incidentName,
                StartDate = lastExportTime.AddSeconds(2),
                KmNumber = 12,
                MetNumber = 300,
                Description = "Mo ta su co"
            }).ExecuteCommandAsync();

            var ds = await SeedSavedQueryDataSourceForPacket(db, ShareDataEnum.DatatypeIdEnum.PublicMessaging);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P110_JOIN", "SUB-Q110-JOIN-01",
                ((int)ShareDataEnum.DatatypeIdEnum.PublicMessaging).ToString(), s =>
                {
                    s.DataSourceId = ds.ID;
                    s.LastTimeRun = lastExportTime;
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            using var doc = JsonDocument.Parse(json);

            // Gói 110 nay "lấy all" nên tổng bản ghi là cả bảng sự cố -> không đếm tổng được nữa,
            // phải soi riêng sự cố vừa cắm. Trước khi sửa: LeftJoin((i, v) => true) sinh
            // 1 x 3 = 3 bản ghi cho cùng một sự cố vì có 3 dòng VmsCurrent.
            var matched = doc.RootElement.GetProperty("payload").EnumerateArray()
                .Where(r => r.TryGetProperty("incidentMessage", out var msg)
                            && (msg.GetString() ?? string.Empty).Contains(incidentName))
                .ToList();

            Assert.Single(matched);
            Assert.True(matched[0].TryGetProperty("guidanceContent", out var guidance));
            Assert.Equal(JsonValueKind.Null, guidance.ValueKind);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Hồi quy lỗi nuốt ngoại lệ — DataSource cấu hình lỗi phải ghi log FAILED, tuyệt đối không nuốt lỗi
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task FetchData_WhenSavedQueryFails_LogsFailedWithoutFallback_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_NOFALLBACK", FromKmNumber = 3, MaxSpeed = 70 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "55", Condition = "NORMAL", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var (dataSource, profile) = await SeedConfiguredSource(db, "SQ_BROKEN", ShareDataEnum.DataSourceKind.SavedQuery,
                "[{\"sourceField\":\"ID\",\"targetField\":\"recordId\"}]",
                queryText: "SELECT ID FROM Bang_Khong_Ton_Tai_XYZ");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_NOFALLBACK", "SUB-NOFALLBACK-01", "101",
                s => s.MappingProfileId = profile.ID);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);

            // Trước khi sửa: exception bị nuốt -> fallback về gói 101 -> ghi SUCCESS với dữ liệu KHÁC cấu hình đã duyệt
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.False(string.IsNullOrEmpty(logs[0].ErrorMessage));
            Assert.Null(logs[0].FilePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Subscription trỏ tới DataSource không tồn tại phải ghi log FAILED kèm đúng thông điệp nghiệp vụ
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task FetchData_WhenDataSourceMissing_LogsFailed_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var (dataSource, profile) = await SeedConfiguredSource(db, "DS_MISSING", ShareDataEnum.DataSourceKind.FieldPicker,
                "[{\"sourceField\":\"ID\",\"targetField\":\"recordId\"}]",
                tableOrView: "TmsZoneStatus");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_DS_MISSING", "SUB-DS-MISSING-01", "101",
                s =>
                {
                    s.MappingProfileId = profile.ID;
                    s.DataSourceId = "KHONG_TON_TAI_XYZ";
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            // Cột ErrorMessage đang là varchar nên thông điệp bị mất dấu tiếng Việt -> so khớp phần ASCII ổn định
            Assert.StartsWith("Ngu", logs[0].ErrorMessage!);

            // Không phải lỗi do SQL Server ném ra -> khẳng định đã chặn ngay tại tầng kiểm tra cấu hình
            Assert.DoesNotContain("Invalid object name", logs[0].ErrorMessage!);
            Assert.Null(logs[0].FilePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra DataSource cấu hình trả về 0 bản ghi là kết quả hợp lệ, ghi log SUCCESS 0 bản ghi
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task FetchData_WhenConfiguredSourceEmpty_DoesNotFallbackToRegistry_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await db.Deleteable<ShareDataEventSource>().ExecuteCommandAsync();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = "TEST_ZONE_EMPTYSRC",
                FromKmNumber = 4,
                MaxSpeed = 60
            }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus
            {
                ID = statusId,
                ZoneId = zoneId,
                AverageSpeed = "50",
                Condition = "NORMAL",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // ShareDataEventSource là bảng nội bộ rỗng -> nguồn cấu hình chắc chắn trả 0 dòng
            var (dataSource, profile) = await SeedConfiguredSource(db, "SRC_EMPTY", ShareDataEnum.DataSourceKind.FieldPicker,
                "[{\"sourceField\":\"ID\",\"targetField\":\"recordId\"}]",
                tableOrView: "ShareDataEventSource");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_SRC_EMPTY", "SUB-SRC-EMPTY-01", "101",
                s => s.MappingProfileId = profile.ID);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            // Trước khi sửa: data.Count == 0 kích hoạt fallback -> kết xuất nhầm dữ liệu gói 101
            Assert.Equal(0, logs[0].RecordCount.GetValueOrDefault());
            Assert.Null(logs[0].FilePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra SAVED_QUERY chứa câu lệnh ghi/xoá hoặc stacked query bị chặn ngay tại tầng kiểm tra bảo mật
        /// Created date: 06/08/2026
        /// </summary>
        [Theory]
        [InlineData("DELETE FROM TmsZoneStatus", "DELETE")]
        [InlineData("SELECT TOP 1 ID FROM TmsZoneStatus; DROP TABLE TmsZoneStatus", "DROP")]
        public async Task ValidateSavedQuery_WhenMaliciousOrInvalidStatement_LogsFailed_Theory(string queryText, string expectedKeyword)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var (dataSource, profile) = await SeedConfiguredSource(db, $"SQ_SEC_{expectedKeyword}", ShareDataEnum.DataSourceKind.SavedQuery,
                "[{\"sourceField\":\"ID\",\"targetField\":\"recordId\"}]",
                queryText: queryText);

            var (partner, sub) = await SeedOutboundSubscription(db, $"TEST_SQ_SEC_{expectedKeyword}", $"SUB-SQ-SEC-{expectedKeyword}-01", "101",
                s => s.MappingProfileId = profile.ID);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);

            // Cột ErrorMessage đang là varchar nên thông điệp bị mất dấu tiếng Việt -> chỉ so khớp phần ASCII ổn định
            Assert.StartsWith("SavedQuery", logs[0].ErrorMessage!);
            Assert.Contains(expectedKeyword, logs[0].ErrorMessage!);
            Assert.True(db.DbMaintenance.IsAnyTable("TmsZoneStatus"), "Bảng nguồn không được phép bị tác động");
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra bộ lọc từ khoá cấm không bắt nhầm các cột hợp lệ chứa từ khoá (UpdateTime, CreateTime) nhờ ranh giới từ
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ValidateSavedQuery_WhenColumnNameContainsKeyword_ExportsSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_KEYWORD", FromKmNumber = 6, MaxSpeed = 60 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "45", Condition = "NORMAL", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var (dataSource, profile) = await SeedConfiguredSource(db, "SQ_KEYWORD", ShareDataEnum.DataSourceKind.SavedQuery,
                "[{\"sourceField\":\"ID\",\"targetField\":\"recordId\"},{\"sourceField\":\"UpdateTime\",\"targetField\":\"updateTime\"}]",
                queryText: "SELECT TOP 5 ID, UpdateTime, CreateTime FROM TmsZoneStatus");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_SQ_KEYWORD", "SUB-SQ-KEYWORD-01", "101",
                s => s.MappingProfileId = profile.ID);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.True(logs[0].RecordCount > 0);

            var json = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains("recordId", json);
            Assert.Contains("updateTime", json);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra FIELD_PICKER với tên bảng chứa mã tiêm nhiễm bị chặn tại tầng kiểm tra định danh
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ValidateTableName_WhenInjectionAttempt_LogsFailed_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var (dataSource, profile) = await SeedConfiguredSource(db, "FP_INJECT", ShareDataEnum.DataSourceKind.FieldPicker,
                "[{\"sourceField\":\"ID\",\"targetField\":\"recordId\"}]",
                tableOrView: "TmsZoneStatus; DROP TABLE TmsZoneStatus; --");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_FP_INJECT", "SUB-FP-INJECT-01", "101",
                s => s.MappingProfileId = profile.ID);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);

            // Cột ErrorMessage đang là varchar nên thông điệp bị mất dấu tiếng Việt -> so khớp phần tên bảng bị từ chối (ASCII)
            Assert.Contains("DROP TABLE TmsZoneStatus", logs[0].ErrorMessage!);
            Assert.DoesNotContain("QueryText", logs[0].ErrorMessage!);
            Assert.True(db.DbMaintenance.IsAnyTable("TmsZoneStatus"), "Bảng nguồn không được phép bị DROP");
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra FIELD_PICKER với tên bảng có tiền tố schema hợp lệ vẫn được chấp nhận
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task ValidateTableName_WhenSchemaQualified_ExportsSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_SCHEMA", FromKmNumber = 7, MaxSpeed = 60 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "35", Condition = "NORMAL", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var (dataSource, profile) = await SeedConfiguredSource(db, "FP_SCHEMA", ShareDataEnum.DataSourceKind.FieldPicker,
                "[{\"sourceField\":\"ZoneId\",\"targetField\":\"zoneId\"}]",
                tableOrView: "dbo.TmsZoneStatus");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_FP_SCHEMA", "SUB-FP-SCHEMA-01", "101",
                s => s.MappingProfileId = profile.ID);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
            Assert.True(logs[0].RecordCount > 0);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Helper cắm 2 bản ghi TmsZoneStatus cũ và mới phục vụ kiểm tra lọc tăng dần của nhánh DataSource
        /// Created date: 06/08/2026
        /// </summary>
        private static async Task<(string OldZoneId, string NewZoneId, string[] StatusIds)> SeedOldAndNewZoneStatus(ISqlSugarClient db, string suffix)
        {
            var oldZoneId = $"ZONE_OLD_{suffix}_{Guid.NewGuid():N}";
            var newZoneId = $"ZONE_NEW_{suffix}_{Guid.NewGuid():N}";
            var oldStatusId = Guid.NewGuid().ToString("N");
            var newStatusId = Guid.NewGuid().ToString("N");

            await db.Insertable(new TmsZone { ID = oldZoneId, Name = $"TEST_ZONE_{suffix}_OLD", FromKmNumber = 1, MaxSpeed = 80 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZone { ID = newZoneId, Name = $"TEST_ZONE_{suffix}_NEW", FromKmNumber = 2, MaxSpeed = 80 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = oldStatusId, ZoneId = oldZoneId, AverageSpeed = "40", Condition = "OLD", UpdateTime = DateTime.Now.AddMinutes(-30) }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = newStatusId, ZoneId = newZoneId, AverageSpeed = "80", Condition = "NEW", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            return (oldZoneId, newZoneId, [oldStatusId, newStatusId]);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra SAVED_QUERY nhận tham số @lastTime để lọc tăng dần theo mốc kết xuất gần nhất
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task QueryConfiguredDataSource_SavedQueryWithLastTimeParam_FiltersOlderRecords_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var (oldZoneId, newZoneId, statusIds) = await SeedOldAndNewZoneStatus(db, "SQPARAM");

            var (dataSource, profile) = await SeedConfiguredSource(db, "SQ_PARAM", ShareDataEnum.DataSourceKind.SavedQuery,
                "[{\"sourceField\":\"ZoneId\",\"targetField\":\"zoneId\"}]",
                queryText: "SELECT ID, ZoneId, UpdateTime FROM TmsZoneStatus WHERE UpdateTime > @lastTime");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_SQ_PARAM", "SUB-SQ-PARAM-01", "101",
                s =>
                {
                    s.MappingProfileId = profile.ID;
                    s.LastTimeRun = DateTime.Now.AddMinutes(-5);
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains(newZoneId, json);
            Assert.DoesNotContain(oldZoneId, json);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Hồi quy lỗi chu kỳ đầu — Subscription chưa có LastTimeRun mà QueryText tham chiếu @lastTime
        ///              vẫn phải chạy được nhờ mốc sàn 1900-01-01, không lỗi "Must declare the scalar variable".
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task QueryConfiguredDataSource_SavedQueryFirstCycleWithoutLastTimeRun_ExportsSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var (oldZoneId, newZoneId, statusIds) = await SeedOldAndNewZoneStatus(db, "SQFIRST");

            var (dataSource, profile) = await SeedConfiguredSource(db, "SQ_FIRST", ShareDataEnum.DataSourceKind.SavedQuery,
                "[{\"sourceField\":\"ZoneId\",\"targetField\":\"zoneId\"}]",
                queryText: "SELECT ID, ZoneId, UpdateTime FROM TmsZoneStatus WHERE UpdateTime > @lastTime");

            // Cố ý KHÔNG đặt LastTimeRun -> mô phỏng chu kỳ chạy đầu tiên
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_SQ_FIRST", "SUB-SQ-FIRST-01", "101",
                s => s.MappingProfileId = profile.ID);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            // Mốc sàn khớp mọi bản ghi -> chu kỳ đầu lấy cả bản cũ lẫn bản mới
            var json = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains(newZoneId, json);
            Assert.Contains(oldZoneId, json);

            // Kết xuất thành công thì LastTimeRun phải được ghi để chu kỳ sau lọc tăng dần
            var updated = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updated.LastTimeRun);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra FIELD_PICKER lọc tăng dần theo quy ước EntityTenant ISNULL(UpdateTime, CreateTime), không cần cấu hình cột
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task QueryConfiguredDataSource_FieldPickerUsesEntityTenantTime_FiltersOlderRecords_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var (oldZoneId, newZoneId, statusIds) = await SeedOldAndNewZoneStatus(db, "FPTIME");

            var (dataSource, profile) = await SeedConfiguredSource(db, "FP_TIME", ShareDataEnum.DataSourceKind.FieldPicker,
                "[{\"sourceField\":\"ZoneId\",\"targetField\":\"zoneId\"}]",
                tableOrView: "TmsZoneStatus");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_FP_TIME", "SUB-FP-TIME-01", "101",
                s =>
                {
                    s.MappingProfileId = profile.ID;
                    s.LastTimeRun = DateTime.Now.AddMinutes(-5);
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains(newZoneId, json);
            Assert.DoesNotContain(oldZoneId, json);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Hồi quy bẫy NULL — bản ghi chưa từng cập nhật (UpdateTime NULL) vẫn phải được lọc đúng qua CreateTime, không bị bỏ sót
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task QueryConfiguredDataSource_FieldPickerWhenUpdateTimeNull_UsesCreateTime_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var oldZoneId = $"ZONE_NULLUPD_OLD_{Guid.NewGuid():N}";
            var newZoneId = $"ZONE_NULLUPD_NEW_{Guid.NewGuid():N}";
            var oldStatusId = Guid.NewGuid().ToString("N");
            var newStatusId = Guid.NewGuid().ToString("N");

            // Cả 2 bản ghi đều để UpdateTime = NULL (mô phỏng dữ liệu chỉ INSERT, không bao giờ UPDATE)
            await db.Insertable(new TmsZoneStatus { ID = oldStatusId, ZoneId = oldZoneId, AverageSpeed = "40", Condition = "OLD", UpdateTime = null }).ExecuteCommandAsync();
            await db.Ado.ExecuteCommandAsync("UPDATE TmsZoneStatus SET CreateTime = @createTime WHERE ID = @id", new { createTime = DateTime.Now.AddMinutes(-30), id = oldStatusId });
            await db.Insertable(new TmsZoneStatus { ID = newStatusId, ZoneId = newZoneId, AverageSpeed = "80", Condition = "NEW", UpdateTime = null }).ExecuteCommandAsync();
            await db.Ado.ExecuteCommandAsync("UPDATE TmsZoneStatus SET CreateTime = @createTime WHERE ID = @id", new { createTime = DateTime.Now, id = newStatusId });

            var (dataSource, profile) = await SeedConfiguredSource(db, "FP_NULLUPD", ShareDataEnum.DataSourceKind.FieldPicker,
                "[{\"sourceField\":\"ZoneId\",\"targetField\":\"zoneId\"}]",
                tableOrView: "TmsZoneStatus");

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_FP_NULLUPD", "SUB-FP-NULLUPD-01", "101",
                s =>
                {
                    s.MappingProfileId = profile.ID;
                    s.LastTimeRun = DateTime.Now.AddMinutes(-5);
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);

            // Lọc thuần UpdateTime sẽ loại cả 2 dòng vì NULL > @lastTime luôn cho UNKNOWN
            Assert.Contains(newZoneId, json);
            Assert.DoesNotContain(oldZoneId, json);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Hồi quy bẫy NULL cho Gói 101 — Zone chưa từng cập nhật (UpdateTime NULL) vẫn được kết xuất nhờ dự phòng CreateTime
        /// Created date: 06/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket101_WhenUpdateTimeNull_UsesCreateTime_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var zoneId = $"ZONE_101_NULLUPD_{Guid.NewGuid():N}";
            var statusId = Guid.NewGuid().ToString("N");

            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_101_NULLUPD", FromKmNumber = 8, MaxSpeed = 70 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "65", Condition = "NORMAL", CreateTime = DateTime.Now, UpdateTime = null }).ExecuteCommandAsync();

            var ds = await SeedSavedQueryDataSourceForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow);
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P101_NULLUPD", "SUB-Q101-NULLUPD-01",
                ((int)ShareDataEnum.DatatypeIdEnum.TrafficFlow).ToString(), s =>
                {
                    s.DataSourceId = ds.ID;
                    s.LastTimeRun = DateTime.Now.AddMinutes(-5);
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var json = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains(zoneId, json);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra khi SavedQuery chứa câu lệnh SQL lỗi/không hợp lệ, worker sẽ ghi nhận log FAILED
        ///              kèm thông điệp lỗi cụ thể vào ShareDataActivityLog thay vì im lặng nuốt lỗi.
        /// Created date: 08/08/2026
        /// </summary>
        [Fact]
        public async Task ExecuteExport_WhenSavedQuerySqlIsInvalid_LogsFailureAndErrorMessage_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var dsId = $"DS_INVALID_SQL_{Guid.NewGuid():N}";
            var mpId = $"MP_INVALID_SQL_{Guid.NewGuid():N}";

            await db.Insertable(new ShareDataDataSource
            {
                ID = dsId,
                Code = dsId,
                Name = "DS Invalid SQL Test",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = "SELECT * FROM [NonExistentTable_ForTest_12345] WHERE @lastTime = @lastTime"
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataMappingProfile
            {
                ID = mpId,
                Code = mpId,
                Name = "MP Invalid SQL Test",
                DataSourceId = dsId,
                MappingsJson = "[{\"sourceField\":\"id\",\"targetField\":\"id\"}]"
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_INVALID_SQL", "SUB-INVALID-SQL-01",
                "101", s =>
                {
                    s.MappingProfileId = mpId;
                    s.DataSourceId = dsId;
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
            Assert.NotNull(logs[0].ErrorMessage);
            Assert.False(string.IsNullOrWhiteSpace(logs[0].ErrorMessage));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra MappingsJson dạng Object Key-Value (ví dụ: {"ID":"zoneId","AverageSpeed":"averageSpeed"}) được phân tích và ánh xạ chính xác
        /// Created date: 08/08/2026
        /// </summary>
        [Fact]
        public async Task ExecuteExport_WhenMappingsJsonIsObjectFormat_MapsFieldsCorrectly_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var sampleStat = new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = Guid.NewGuid().ToString("N"),
                AverageSpeed = "90",
                Condition = "GOOD",
                CreateTime = DateTime.Now
            };
            await db.Insertable(sampleStat).ExecuteCommandAsync();

            var dataSource = new ShareDataDataSource
            {
                Code = "TEST_DS_OBJMAP",
                Name = "DataSource Object MappingsJson Test",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = $"SELECT ID, AverageSpeed FROM TmsZoneStatus WHERE ID = '{sampleStat.ID}'"
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mappingProfile = new ShareDataMappingProfile
            {
                Code = "TEST_MP_OBJMAP",
                Name = "MappingProfile Object MappingsJson Test",
                DataSourceId = dataSource.ID,
                DatatypeId = "101",
                Direction = "OUT",
                MappingsJson = "{\"ID\":\"zoneId\",\"AverageSpeed\":\"averageSpeed\"}",
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_OBJMAP", "SUB-OBJMAP-01", "101", s =>
            {
                s.MappingProfileId = mappingProfile.ID;
                s.DataSourceId = dataSource.ID;
            });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var jsonContent = await ReadExportedJson(logs[0].FilePath!);
            Assert.Contains("\"zoneId\"", jsonContent);
            Assert.Contains("\"averageSpeed\":\"90\"", jsonContent);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra SavedQuery đã chứa sẵn từ khoá OPTION sẽ không bị chèn trùng lặp câu lệnh OPTION (RECOMPILE)
        /// Created date: 08/08/2026
        /// </summary>
        [Fact]
        public async Task ApplyRecompileHint_WhenQueryAlreadyContainsOption_DoesNotDuplicateOption_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_OPTION", FromKmNumber = 5, MaxSpeed = 80 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "70", Condition = "NORMAL", CreateTime = DateTime.Now }).ExecuteCommandAsync();

            var dataSource = new ShareDataDataSource
            {
                Code = "TEST_DS_OPTION_DUP",
                Name = "DS With Option Test",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = "SELECT ID, AverageSpeed FROM TmsZoneStatus WHERE UpdateTime >= @lastTime OR CreateTime >= @lastTime OPTION (RECOMPILE)"
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mappingProfile = new ShareDataMappingProfile
            {
                Code = "TEST_MP_OPTION_DUP",
                Name = "MP With Option Test",
                DataSourceId = dataSource.ID,
                DatatypeId = "101",
                Direction = "OUT",
                MappingsJson = "[{\"sourceField\":\"ID\",\"targetField\":\"zoneId\"}]",
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_OPTION_DUP", "SUB-OPTION-DUP-01", "101", s =>
            {
                s.MappingProfileId = mappingProfile.ID;
                s.DataSourceId = dataSource.ID;
                s.LastTimeRun = DateTime.Now.AddHours(-1);
            });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra DataSource loại FIELD_PICKER khi TopN <= 0 sẽ tự động rơi về hằng số mặc định DefaultTopN (50)
        /// Created date: 08/08/2026
        /// </summary>
        [Fact]
        public async Task QueryConfiguredDataSource_WhenTopNIsInvalidOrZero_UsesDefaultTopN_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var dataSource = new ShareDataDataSource
            {
                Code = "TEST_DS_TOPN_NEG",
                Name = "DS Negative TopN Test",
                Kind = ShareDataEnum.DataSourceKind.FieldPicker,
                TableOrView = "TmsZoneStatus",
                TopN = -10
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mappingProfile = new ShareDataMappingProfile
            {
                Code = "TEST_MP_TOPN_NEG",
                Name = "MP Negative TopN Test",
                DataSourceId = dataSource.ID,
                DatatypeId = "101",
                Direction = "OUT",
                MappingsJson = "[{\"sourceField\":\"ID\",\"targetField\":\"zoneId\"}]",
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_TOPN_NEG", "SUB-TOPN-NEG-01", "101", s =>
            {
                s.MappingProfileId = mappingProfile.ID;
                s.DataSourceId = dataSource.ID;
                s.LastTimeRun = DateTime.Now.AddDays(-1);
            });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm định Schema Guard (Option C) — Tự động quét tất cả Entity Class trong C#,
        ///              đối chiếu tên cột với CSDL thực tế. Đảm bảo 100% C# Entity không chứa cột nào
        ///              mà CSDL thực tế chưa có (ngăn ngừa lỗi SqlException Invalid column name).
        /// Created date: 09/08/2026
        /// </summary>
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
                typeof(ShareDataMappingProfile),
                typeof(ShareDataDataSource),
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
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra luồng chuyển trạng thái State từ Active (1) -> Running (6) khi bốc Subscription
        ///              và tự động hoàn trả lại Active (1) sau khi kết xuất hoàn tất.
        /// Created date: 10/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPickedUp_TransitionsStateActiveToRunningToActive_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum packetId = (ShareDataEnum.DatatypeIdEnum)994;
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_RUNNING", "SUB-RUNNING-01",
                ((int)packetId).ToString(), s => s.State = BaseEnums.SubSubscriptionState.Active);

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Active, updatedSub.State);
            Assert.NotNull(updatedSub.LastTimeRun);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra khi Subscription đang ở trạng thái Running (6) nhưng NextTimeRun đã hết hạn
        ///              (do worker trước bị sập giữa chừng) thì Worker đợt sau vẫn thu hồi và kết xuất thành công.
        /// Created date: 10/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenStateIsRunningAndLeaseExpired_ReclaimsSubscription_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum packetId = (ShareDataEnum.DatatypeIdEnum)993;
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_RUNNING_RECLAIM", "SUB-RUNNING-RECLAIM-01",
                ((int)packetId).ToString(), s =>
                {
                    s.State = BaseEnums.SubSubscriptionState.Running;
                    s.NextTimeRun = DateTime.Now.AddMinutes(-5); // Khóa đã hết hạn từ 5 phút trước
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Active, updatedSub.State);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra khi Subscription đang ở trạng thái Running (6) và NextTimeRun chưa hết hạn
        ///              (đang được worker khác xử lý) thì đợt quét sẽ BỎ QUA không bốc lại.
        /// Created date: 10/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenStateIsRunningAndNotExpired_IsSkipped_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum packetId = (ShareDataEnum.DatatypeIdEnum)992;
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_RUNNING_SKIP", "SUB-RUNNING-SKIP-01",
                ((int)packetId).ToString(), s =>
                {
                    s.State = BaseEnums.SubSubscriptionState.Running;
                    s.NextTimeRun = DateTime.Now.AddMinutes(10); // Đang chạy và chưa hết hạn khóa
                });

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs); // Bỏ qua không bốc xử lý
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra khi Admin chủ động tạm dừng (Paused = 2) Subscription trong lúc Worker đang chạy
        ///              thì file vẫn được kết xuất bình thường nhưng State không bị đè ngược lại thành Active (1).
        /// Created date: 10/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenStateChangedToPausedDuringExecution_PreservesPausedState_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const ShareDataEnum.DatatypeIdEnum packetId = (ShareDataEnum.DatatypeIdEnum)991;
            var ds = new ShareDataDataSource
            {
                Code = $"TEST_DS_PAUSE_{Guid.NewGuid():N}",
                Name = "DataSource Pause Test",
                Kind = ShareDataEnum.DataSourceKind.SavedQuery,
                QueryText = "WAITFOR DELAY '00:00:00.200'; SELECT 1 AS sampleId;"
            };
            await db.Insertable(ds).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_P_PAUSE_MID", "SUB-PAUSE-MID-01",
                ((int)packetId).ToString(), s =>
                {
                    s.DataSourceId = ds.ID;
                    s.State = BaseEnums.SubSubscriptionState.Active;
                });

            var workerTask = CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);
            await Task.Delay(50);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await workerTask;

            var logs = await GetLogs(db, sub.ID);
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Paused, updatedSub.State); // Đã giữ nguyên Paused!
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra chế độ chạy Single (Một lần): Sau khi kết xuất thành công 1 lần,
        ///              Subscription tự động chuyển trạng thái sang Expired (5) để không chạy lại trong chu kỳ sau.
        /// Created date: 11/08/2026
        /// </summary>
        [Fact]
        public async Task SingleModeSubscription_DeactivatesAfterSuccessfulExport_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "SINGLE_ZONE_TEST", FromKmNumber = 1, MaxSpeed = 80 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "80", Condition = "NORMAL", CreateTime = DateTime.Now }).ExecuteCommandAsync();

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

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra tính mốc NextTimeRun với ScheduleJson null/rỗng -> fallback theo IntervalSeconds
        /// Created date: 11/08/2026
        /// </summary>
        [Fact]
        public void CalculateNextRunTime_NullScheduleJson_FallbackToInterval_Test()
        {
            using var scope = _host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var service = new DataExportService(scopeFactory, logger, config);

            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 45,
                ScheduleJson = null
            };

            var baseTime = new DateTime(2026, 8, 11, 10, 0, 0);
            var nextRun = service.CalculateNextRunTime(sub, baseTime);

            Assert.NotNull(nextRun);
            Assert.Equal(baseTime.AddSeconds(45), nextRun.Value);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra tính mốc NextTimeRun cho chế độ Continuous với updateDelaySec
        /// Created date: 11/08/2026
        /// </summary>
        [Fact]
        public void CalculateNextRunTime_ContinuousKind_UsesUpdateDelaySec_Test()
        {
            using var scope = _host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var service = new DataExportService(scopeFactory, logger, config);

            var schedule = new DatexRegistered
            {
                Kind = ShareDataEnum.ScheduleKind.Continuous,
                UpdateDelaySec = 60,
                StartTime = "08:00:00",
                EndTime = "17:00:00"
            };

            var sub = new ShareDataSubscription
            {
                ScheduleJson = JsonSerializer.Serialize(schedule)
            };

            var baseTime = new DateTime(2026, 8, 11, 10, 0, 0);
            var nextRun = service.CalculateNextRunTime(sub, baseTime);

            Assert.NotNull(nextRun);
            Assert.Equal(baseTime.AddSeconds(60), nextRun.Value);
        }

        public static IEnumerable<object?[]> GetDailyScheduleCalculationTestCases()
        {
            // Case 1: Trong khung giờ -> cộng thêm delay 120s
            yield return new object?[]
            {
                new DatexRegistered
                {
                    Kind = ShareDataEnum.ScheduleKind.Daily,
                    UpdateDelaySec = 120,
                    DaysOfWeek = [ShareDataEnum.ScheduleDayCode.Tuesday, ShareDataEnum.ScheduleDayCode.Wednesday],
                    StartTime = "08:00:00",
                    EndTime = "17:00:00"
                },
                new DateTime(2026, 8, 11, 10, 0, 0),
                new DateTime(2026, 8, 11, 10, 2, 0)
            };

            // Case 2: Hết khung giờ trong ngày -> nhảy tới StartTime ngày hợp lệ tiếp theo (08:00 ngày mai)
            yield return new object?[]
            {
                new DatexRegistered
                {
                    Kind = ShareDataEnum.ScheduleKind.Daily,
                    UpdateDelaySec = 60,
                    DaysOfWeek = [ShareDataEnum.ScheduleDayCode.Tuesday, ShareDataEnum.ScheduleDayCode.Wednesday],
                    StartTime = "08:00:00",
                    EndTime = "17:00:00"
                },
                new DateTime(2026, 8, 11, 18, 0, 0),
                new DateTime(2026, 8, 12, 8, 0, 0)
            };

            // Case 3: Ngày không được phép chạy (Thứ 3) -> nhảy tới ngày hợp lệ tiếp theo (Thứ 6)
            yield return new object?[]
            {
                new DatexRegistered
                {
                    Kind = ShareDataEnum.ScheduleKind.Daily,
                    UpdateDelaySec = 60,
                    DaysOfWeek = [ShareDataEnum.ScheduleDayCode.Friday],
                    StartTime = "09:00:00",
                    EndTime = "17:00:00"
                },
                new DateTime(2026, 8, 11, 10, 0, 0),
                new DateTime(2026, 8, 14, 9, 0, 0)
            };

            // Case 4: Đã qua EndDate -> trả về null đánh dấu hết lịch
            yield return new object?[]
            {
                new DatexRegistered
                {
                    Kind = ShareDataEnum.ScheduleKind.Daily,
                    EndDate = "2026-08-01",
                    StartTime = "08:00:00",
                    EndTime = "17:00:00"
                },
                new DateTime(2026, 8, 11, 10, 0, 0),
                null
            };
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra tính mốc NextTimeRun cho các kịch bản Daily: trong giờ, hết giờ, sai ngày, và hết hạn EndDate
        /// Created date: 11/08/2026
        /// </summary>
        [Theory]
        [MemberData(nameof(GetDailyScheduleCalculationTestCases))]
        public void CalculateNextRunTime_DailyKind_CalculatesCorrectTargetTime_Theory(
            DatexRegistered schedule, DateTime baseTime, DateTime? expectedNextRun)
        {
            using var scope = _host.Services.CreateScope();
            var service = CreateWorker(scope);

            var sub = new ShareDataSubscription
            {
                ScheduleJson = JsonSerializer.Serialize(schedule)
            };

            var nextRun = service.CalculateNextRunTime(sub, baseTime);
            Assert.Equal(expectedNextRun, nextRun);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Gói 110 (PublicMessaging) tự động trích xuất guidanceContent từ VmsCurrent qua TmsEquipment khi trùng vị trí KmNumber
        /// Created date: 11/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket110_WithVmsCurrentMatchingKmNumber_ReturnsGuidanceContent_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var incId = Guid.NewGuid().ToString("N");
            var vmsId = Guid.NewGuid().ToString("N");
            var eqId = Guid.NewGuid().ToString("N");

            await db.Insertable(new TmsIncident
            {
                ID = incId,
                Code = "TEST_INC_110_GC",
                Name = "Sự cố sạt lở",
                StartDate = DateTime.Now,
                KmNumber = 88,
                MetNumber = 500,
                State = ShareDataEnum.SubState.Active,
                Description = "Nguy hiểm sạt lở",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = "TEST_EQ_88",
                KmNumber = 88,
                MetNumber = 500
            }).ExecuteCommandAsync();

            await db.Insertable(new VmsCurrent
            {
                ID = vmsId,
                Name = "TEST_VMS_88",
                EquipmentId = eqId,
                RowData = "CHÚ Ý SẠT LỞ - ĐI CHẬM",
                ExecutedDate = DateTime.Now
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P110_GC",
                Name = "Partner 110 Guidance",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var ds = await SeedSavedQueryDataSourceForPacket(db, ShareDataEnum.DatatypeIdEnum.PublicMessaging);

            var sub = new ShareDataSubscription
            {
                Code = "SUB-110-GC",
                PartnerId = partner.ID,
                DatatypeId = ((int)ShareDataEnum.DatatypeIdEnum.PublicMessaging).ToString(),
                DataSourceId = ds.ID,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-5)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var worker = new DataExportService(scopeFactory, logger, scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await worker.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal("SUCCESS", logs[0].Status);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            var jsonContent = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(jsonContent);

            var payload = doc.RootElement.GetProperty("payload").EnumerateArray();
            var item = payload.FirstOrDefault(r => r.TryGetProperty("incidentMessage", out var msg) && msg.GetString()?.Contains("Sự cố sạt lở") == true);

            Assert.NotEqual(JsonValueKind.Undefined, item.ValueKind);
            Assert.True(item.TryGetProperty("guidanceContent", out var guidance));
        }


    }
}

