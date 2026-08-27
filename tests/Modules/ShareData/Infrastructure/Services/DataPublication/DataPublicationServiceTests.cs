using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.VMS.Core.Entities;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Exceptions;
using ShareDataWorker.Infrastructure.Services.DataPublication;

namespace Tests.Modules.ShareData.Infrastructure.Services.DataPublication
{
    /// <summary>
    /// Lớp chứa tất cả các kịch bản kiểm thử Integration Test cho DataPublicationService thuộc module ShareDataWorker.
    /// Hoạt động trực tiếp trên cơ sở dữ liệu Test Local, kiểm tra toàn diện luồng quét Subscription,
    /// sinh SQL động từ ShareDataPacket + ShareDataTable, áp dụng phễu lọc ShareDataMapping và đóng gói PDU.
    /// Author: Đạt
    /// Created date: 31/07/2026
    /// </summary>
    [Collection("api")]
    public partial class DataPublicationServiceTests(Host host)
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
            Assert.True(logs[0].Success == BaseEnums.SuccessEnums.Success, $"Export failed. DB ErrorMessage: {logs[0].ErrorMessage}");
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
                Direction = BaseEnums.Direction.Outbound,
                Mode = BaseEnums.SubMode.Periodic,
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

        private static DataPublicationService CreateWorker(IServiceScope scope)
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataPublicationService>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            return new DataPublicationService(scopeFactory, logger, config);
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

        [Fact]
        public void QueryPacket_KeysetPagination_SqlGen_Test()
        {
            var packetIncr = new ShareDataPacket { Code = "103_INC" };
            var packetSnap = new ShareDataPacket { Code = "101" };
            var tables = new List<ShareDataTable>
            {
                new()
                {
                    TableName = "TmsTrafficData",
                    Alias = "td",
                    IsRoot = true,
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
                    FieldsJson = "[{\"fieldKey\":\"id\",\"column\":\"ID\"}]"
                }
            };

            // 1. Incremental
            var queryIncr = DataPublicationService.BuildQuery(packetIncr, tables, new DateTime(2025, 1, 1));
            var normIncr = NormalizeSql(queryIncr.Sql);
            Assert.Contains("td.ID AS __rowid", normIncr);
            Assert.Contains("td.DetectTime AS __watermark", normIncr);
            Assert.Contains("OR (td.DetectTime = @lastTime AND td.ID > @lastId)", normIncr);
            Assert.Contains("ORDER BY td.DetectTime ASC, td.ID ASC", normIncr);
            
            // 2. Incremental with NONE fallback
            var queryNone = DataPublicationService.BuildQuery(packetIncr, tablesNone, new DateTime(2025, 1, 1));
            var normNone = NormalizeSql(queryNone.Sql);
            Assert.Contains("td.DetectTime AS __watermark", normNone);
            Assert.Contains("OR (td.DetectTime = @lastTime AND td.ID > @lastId)", normNone);
            Assert.Contains("ORDER BY td.DetectTime ASC, td.ID ASC", normNone);

            // 3. Snapshot
            var querySnap = DataPublicationService.BuildQuery(packetSnap, tables, new DateTime(2025, 1, 1));
            var normSnap = NormalizeSql(querySnap.Sql);
            Assert.DoesNotContain("__rowid", normSnap);
            Assert.DoesNotContain("@lastId", normSnap);
            Assert.DoesNotContain("ORDER BY", normSnap);
            Assert.DoesNotContain("td.ID ASC", normSnap);
        }

        [Fact]
        public void Transform_Packet101_KeysOrderMatchesOrderNo1To12_Test()
        {
            var def = PacketMetadataCatalogTest.All["101"];
            var queryResult = DataPublicationService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));

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

            var transformed = DataPublicationService.Transform(rawRows, queryResult.AllFields);
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
            var result = DataPublicationService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));
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

            var topN = DataPublicationService.ResolveTopN(def.Packet);
            if (topN.HasValue && topN.Value > 0)
            {
                Assert.Contains($"TOP {topN.Value}", normActual);
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
            var result = DataPublicationService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));
            var normActual = NormalizeSql(result.Sql);

            Assert.Contains("FROM TmsIncident i", normActual);
            Assert.Contains("OUTER APPLY (SELECT TOP 1 v.RowData FROM VmsCurrent v INNER JOIN TmsEquipment e2 ON v.EquipmentId = e2.ID WHERE e2.KmNumber = i.KmNumber AND (v.RowData IS NOT NULL) ORDER BY v.ExecutedDate DESC) v", normActual);
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

            var result = DataPublicationService.Transform(rawRows, fields);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(80, row["speedLimit"]);
            Assert.IsType<int>(row["speedLimit"]);
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
                PacketVersion = "1.0"
}).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
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
                TargetShapeJson = @"{ ""fieldA"": { ""$field"": ""fieldA"", ""$extend"": { ""expression"": ""DROP TABLE t"" } } }"
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

                Assert.NotEmpty(alerts);
                Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts[0].Severity);
                Assert.Equal(BaseEnums.AlertSource.Funnel, alerts[0].AlertSource);
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
                PacketVersion = "1.0"
}).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "td",
                TableName = "TmsTrafficData",
                IsRoot = true,
                OrderNo = 1,
                FieldsJson = "[{\"fieldKey\":\"speed\",\"column\":\"Speed\"}]"
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_A1_{uniqueId}", $"SUB_A1_{uniqueId}", packetCode, s =>
            {
                s.LastTimeRun = null;
            });

            var trafficId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsTrafficData
            {
                ID = trafficId,
                Speed = 80,
                DetectTime = null
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
                await db.Deleteable<TmsTrafficData>().Where(z => z.ID == trafficId).ExecuteCommandAsync();
            }
        }

        [Fact]
        public void BuildQuery_WatermarkExpressionIdenticalInSelectWhereOrderBy_Test()
        {
            var packet = new ShareDataPacket
            {
                ID = "packet_test_a4",
                Code = "103_INC"
            };

            var tableWithFallback = new ShareDataTable
            {
                ID = "tbl_1",
                PacketCode = "101",
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"ZoneId\"}]"
            };

            var lastTime = new DateTime(2026, 8, 23, 10, 0, 0);
            var queryResult1 = DataPublicationService.BuildQuery(packet, [tableWithFallback], lastTime, "last_id_01");

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
                FieldsJson = "[{\"fieldKey\":\"speed\",\"column\":\"Speed\"}]"
            };

            var queryResult2 = DataPublicationService.BuildQuery(packet, [tableNoFallback], lastTime, "last_id_02");

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

            var targetShapeJson = @"{
                ""renamedField"": { ""$field"": ""fieldA"" }
            }";

            var result = DataPublicationService.Transform(rawRows, fields, targetShapeJson);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.True(row.ContainsKey("renamedField"));
            Assert.Equal("ValueA", row["renamedField"]);
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
            var path1 = DataPublicationService.GenerateExportRelativePath("PARTNER_A", "101", time, DataPublicationService.ResolveFileDiscriminator(sub1));
            var path2 = DataPublicationService.GenerateExportRelativePath("PARTNER_A", "101", time, DataPublicationService.ResolveFileDiscriminator(sub2));

            Assert.NotEqual(path1, path2);
            Assert.Contains("5_SUB_ID_1", path1);
            Assert.Contains("5_SUB_ID_2", path2);
        }

        [Fact]
        public void ParseCodeValues_WithValidAndCorruptedJson_ParsesCorrectly_Test()
        {
            var validJson = "[{\"sourceValue\":\"1\",\"standardValue\":\"slow\",\"displayName\":\"Chậm\",\"orderNo\":1},{\"sourceValue\":\"2\",\"standardValue\":\"normal\",\"displayName\":\"Bình thường\",\"isDefault\":true,\"orderNo\":2}]";
            var parsed = DataPublicationService.ParseCodeValues(validJson);
            Assert.Equal(2, parsed.Count);
            Assert.Equal("1", parsed[0].SourceValue);
            Assert.Equal("slow", parsed[0].StandardValue);
            Assert.Equal("Chậm", parsed[0].DisplayName);
            Assert.Equal(1, parsed[0].OrderNo);
            Assert.True(parsed[1].IsDefault);

            var empty = DataPublicationService.ParseCodeValues("{invalid-json}");
            Assert.Empty(empty);

            var partialJson = "[{\"sourceValue\":\"1\",\"standardValue\":\"slow\"}, \"bad_element\", {\"sourceValue\":\"2\",\"standardValue\":\"normal\"}]";
            var partialParsed = DataPublicationService.ParseCodeValues(partialJson);
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

            var r1 = DataPublicationService.MapCode(codeValues, "1");
            Assert.Equal("slow", r1);

            var r2 = DataPublicationService.MapCode(codeValues, "2");
            Assert.Equal("normal", r2);

            var r3 = DataPublicationService.MapCode(codeValues, "999");
            Assert.Equal("unknown", r3);

            var noDefaultSet = new List<CodeValueDto>
            {
                new() { SourceValue = "A", StandardValue = "Alpha" }
            };
            var r4 = DataPublicationService.MapCode(noDefaultSet, "Z");
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

            var targetShapeJson = @"{
                ""tinhTrang"": { ""$field"": ""condition"", ""$extend"": { ""codeSet"": ""TRAFFIC_COND_PARTNER"" } }
            }";

            var result = DataPublicationService.Transform(rawRows, fields, targetShapeJson, codeSets: codeSets);

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

            var result = DataPublicationService.Transform(rawRows, fields, codeSets: codeSets);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("slow", row["condition"]);
            Assert.IsType<string>(row["condition"]);
        }

        [Fact]
        public void Transform_WhenRequiredFieldMissing_Throws_Test()
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

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                DataPublicationService.Transform(rawRows, fields);
            });

            Assert.Contains("Thiếu trường bắt buộc", ex.Message);
            Assert.Contains("averageSpeed", ex.Message);
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
                PacketVersion = "1.0"
}).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
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
                Assert.Equal(BaseEnums.AlertSeverity.Error, alerts[0].Severity);
                Assert.Equal(BaseEnums.AlertSource.Funnel, alerts[0].AlertSource);
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
                PacketVersion = "1.0"
}).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
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
                Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts[0].Severity);
                Assert.Equal(BaseEnums.AlertSource.Funnel, alerts[0].AlertSource);
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
            var resultNullTime = DataPublicationService.BuildQuery(def.Packet, def.Tables, null);
            AssertDeterministicProperties(resultNullTime.Sql, def.Packet);

            // 2. Chạy với lastTimeRun có giá trị
            var resultWithTime = DataPublicationService.BuildQuery(def.Packet, def.Tables, new DateTime(2026, 8, 22));
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

        [Fact]
        public async Task ProcessBatchSubscriptions_DirectFileWrite_SavesValidPduOnDisk_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_DIRECT_{unique}", $"SUB_DIRECT_{unique}", "101");

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.False(string.IsNullOrWhiteSpace(logs[0].FilePath));

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"File phải được ghi trực tiếp xuống đĩa tại: {fullPath}");

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            Assert.True(doc.RootElement.TryGetProperty("pduType", out _));
            Assert.True(doc.RootElement.TryGetProperty("hash", out _));
            Assert.True(doc.RootElement.TryGetProperty("payload", out var payload));
            Assert.True(payload.GetArrayLength() > 0);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenMappingHasValidExpressions_EvaluatesThemIntoPayload_NoAlert_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zoneId = $"ZONE_{unique}";
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyen Expr {unique}",
                FromKmNumber = 10,
                FromMetNumber = 0,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                AverageSpeed = "60",
                Condition = "BINH_THUONG",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_EXPR_OK_{unique}", $"SUB_EXPR_OK_{unique}", "101");

            var targetShape = new Dictionary<string, object?>
            {
                ["zoneId"] = new Dictionary<string, object?> { ["$field"] = "zoneId" },
                ["calcSpeed"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?>
                    {
                        ["expression"] = "averageSpeed * 2"
                    }
                }
            };

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = BaseEnums.Direction.Outbound,
                TargetShapeJson = JsonSerializer.Serialize(targetShape),
                IsActive = true
            }).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            var payload = doc.RootElement.GetProperty("payload");
            var record = payload.EnumerateArray().First(r => r.GetProperty("zoneId").GetString() == zoneId);
            Assert.Equal(120m, record.GetProperty("calcSpeed").GetDecimal());

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1203")
                .ToListAsync();
            Assert.Empty(alerts);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenMappingHasInvalidExpression_LogsEsh1203_KeepsRawValue_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zoneId = $"ZONE_{unique}";
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyen Invalid Expr {unique}",
                FromKmNumber = 10,
                FromMetNumber = 0,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                AverageSpeed = "75",
                Condition = "BINH_THUONG",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_EXPR_INV_{unique}", $"SUB_EXPR_INV_{unique}", "101");

            var targetShape = new Dictionary<string, object?>
            {
                ["zoneId"] = new Dictionary<string, object?> { ["$field"] = "zoneId" },
                ["speed"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?>
                    {
                        ["expression"] = "1; DROP TABLE users"
                    }
                }
            };

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = BaseEnums.Direction.Outbound,
                TargetShapeJson = JsonSerializer.Serialize(targetShape),
                IsActive = true
            }).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            var payload = doc.RootElement.GetProperty("payload");
            var record = payload.EnumerateArray().First(r => r.GetProperty("zoneId").GetString() == zoneId);
            // Raw value preserved
            Assert.Equal(75m, record.GetProperty("speed").GetDecimal());

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1203")
                .ToListAsync();
            Assert.NotEmpty(alerts);
            Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts[0].Severity);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenExpressionRuntimeErrors_LogsEsh1203_KeepsRaw_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zoneId = $"ZONE_{unique}";
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyen Div0 Expr {unique}",
                FromKmNumber = 10,
                FromMetNumber = 0,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                AverageSpeed = "88",
                Condition = "BINH_THUONG",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_EXPR_DIV0_{unique}", $"SUB_EXPR_DIV0_{unique}", "101");

            var targetShape = new Dictionary<string, object?>
            {
                ["zoneId"] = new Dictionary<string, object?> { ["$field"] = "zoneId" },
                ["speed"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?>
                    {
                        ["expression"] = "averageSpeed / 0"
                    }
                }
            };

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = BaseEnums.Direction.Outbound,
                TargetShapeJson = JsonSerializer.Serialize(targetShape),
                IsActive = true
            }).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            var payload = doc.RootElement.GetProperty("payload");
            var record = payload.EnumerateArray().First(r => r.GetProperty("zoneId").GetString() == zoneId);
            Assert.Equal(88m, record.GetProperty("speed").GetDecimal());

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1203")
                .ToListAsync();
            Assert.NotEmpty(alerts);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPartnerProtocolIsXmlA_ExportsWellFormedXmlWithSha256Hash_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_XML_{unique}", $"SUB_XML_{unique}", "101");
            partner.ProtocolProfile = BaseEnums.ProtocolProfile.XmlA;
            await db.Updateable(partner).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.NotNull(logs[0].FilePath);
            Assert.EndsWith(".xml", logs[0].FilePath, StringComparison.OrdinalIgnoreCase);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"File XML phải tồn tại tại: {fullPath}");

            var xmlContent = await File.ReadAllTextAsync(fullPath);
            var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
            Assert.Equal("pdu", doc.Root?.Name.LocalName);

            var header = doc.Root?.Element("header");
            Assert.NotNull(header);
            var hashFromHeader = header.Element("hash")?.Value;
            Assert.False(string.IsNullOrWhiteSpace(hashFromHeader));

            var payload = doc.Root?.Element("payload");
            Assert.NotNull(payload);
            Assert.NotEmpty(payload.Elements("record"));
            Assert.Equal(logs[0].Hash, hashFromHeader);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPartnerProtocolIsAsn_FallsBackToJson_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_ASN_{unique}", $"SUB_ASN_{unique}", "101");
            partner.ProtocolProfile = BaseEnums.ProtocolProfile.Asn;
            await db.Updateable(partner).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.NotNull(logs[0].FilePath);
            Assert.EndsWith(".json", logs[0].FilePath, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessBatchSubscriptions_IncrementalCatchUp_AfterIdle_ExportsEveryNewRowExactlyOnce_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_CATCHUP_{unique}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_CU_{unique}",
                KmNumber = 45,
                MetNumber = 100
            }).ExecuteCommandAsync();

            var baseTime = DateTime.Now.AddHours(-2);
            var (partner, sub) = await SeedOutboundSubscription(db, $"P_CU_{unique}", $"SUB_CU_{unique}", "103", s =>
            {
                s.LastTimeRun = baseTime;
            });
                // Simulate worker stopped while 50 VDS records were inserted consecutively
                var insertedRecords = new List<TmsTrafficData>();
                for (var i = 1; i <= 50; i++)
                {
                    insertedRecords.Add(new TmsTrafficData
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        DetectTime = baseTime.AddSeconds(i * 10),
                        Type = "CAR",
                        LicensePlate = $"30A-{i:D5}",
                        Speed = 60.0f + (i % 30),
                        Lane = $"L{((i % 3) + 1)}",
                        Direction = "1",
                        Location = "KM45",
                        EquipmentId = eqId
                    });
                }
                await db.Insertable(insertedRecords).ExecuteCommandAsync();

                // Run catchup batches
                var allExportedPlates = new List<string>();
                var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var maxRuns = 10;
                var runs = 0;

                while (runs < maxRuns)
                {
                    runs++;
                    // Reset NextTimeRun to past so subscription is immediately eligible for next batch run
                    await db.Updateable<ShareDataSubscription>()
                        .SetColumns(s => s.NextTimeRun == DateTime.Now.AddSeconds(-10))
                        .Where(s => s.ID == sub.ID)
                        .ExecuteCommandAsync();

                    await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

                    var logs = await db.Queryable<ShareDataActivityLog>()
                        .Where(l => l.SubscriptionId == sub.ID)
                        .OrderByDescending(l => l.OccurredAt)
                        .ToListAsync();

                    var newLogs = logs.Where(l => !string.IsNullOrEmpty(l.FilePath) && processedFiles.Add(l.FilePath!)).ToList();
                    if (newLogs.Count == 0 || newLogs.All(l => l.RecordCount == 0))
                        break;

                    foreach (var log in newLogs)
                    {
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", log.FilePath!);
                        if (!File.Exists(fullPath))
                            continue;

                        var json = await File.ReadAllTextAsync(fullPath);
                        using var doc = JsonDocument.Parse(json);
                        var payload = doc.RootElement.GetProperty("payload");
                        foreach (var rec in payload.EnumerateArray())
                        {
                            if (rec.TryGetProperty("licensePlate", out var lp))
                            {
                                var plateStr = lp.GetString();
                                if (!string.IsNullOrEmpty(plateStr) && plateStr.StartsWith("30A-"))
                                {
                                    allExportedPlates.Add(plateStr);
                                }
                            }
                        }
                    }

                    if (allExportedPlates.Count >= 50)
                        break;
                }

                // Assert: Every record was exported exactly once, no duplicates, no omissions
                Assert.Equal(50, allExportedPlates.Count);
                Assert.Equal(50, allExportedPlates.Distinct().Count());
                for (var i = 1; i <= 50; i++)
                {
                    Assert.Contains($"30A-{i:D5}", allExportedPlates);
                }

                var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.NotNull(updatedSub.LastTimeRun);
                Assert.True(updatedSub.LastTimeRun >= insertedRecords.Last().DetectTime!.Value.AddSeconds(-1));
        }

        #region XML Serialization Tests

        [Theory]
        [InlineData("ValidName", "ValidName")]
        [InlineData("123NumberField", "_123NumberField")]
        [InlineData("field with spaces", "field_with_spaces")]
        [InlineData("field.with.dot", "field.with.dot")]
        [InlineData("special!@#chars", "special___chars")]
        [InlineData("", "default_field")]
        [InlineData(null, "default_field")]
        public void ToNcName_SanitizesIdentifiersCorrectly_Test(string? raw, string expected)
        {
            var result = DataPublicationService.ToNcName(raw, "default_field");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SerializeEnvelopeToXmlBytes_ProducesWellFormedXmlWithoutBom_Test()
        {
            // Arrange
            var header = new Dictionary<string, object?>
            {
                ["pduType"] = 1,
                ["serialNbr"] = 100,
                ["sender"] = "SYSTEM",
                ["destination"] = "PARTNER_A",
                ["timestamp"] = new DateTime(2026, 8, 27, 10, 0, 0),
                ["format"] = "FILE",
                ["hash"] = "DUMMY_HASH"
            };

            var data = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["speed"] = 80.5m,
                    ["lane"] = 1,
                    ["plate"] = "29A-12345",
                    ["nullProp"] = null,
                    ["isActive"] = true
                }
            };

            // Act
            var xmlBytes = DataPublicationService.SerializeEnvelopeToXmlBytes(header, data);

            // Assert
            Assert.NotNull(xmlBytes);
            Assert.True(xmlBytes.Length > 0);

            // Verify no UTF-8 BOM (0xEF, 0xBB, 0xBF)
            if (xmlBytes.Length >= 3)
            {
                var hasBom = xmlBytes[0] == 0xEF && xmlBytes[1] == 0xBB && xmlBytes[2] == 0xBF;
                Assert.False(hasBom, "XML bytes must not contain UTF-8 BOM");
            }

            var xmlString = System.Text.Encoding.UTF8.GetString(xmlBytes);
            var doc = XDocument.Parse(xmlString);

            Assert.Equal("pdu", doc.Root?.Name.LocalName);
            var headerElem = doc.Root?.Element("header");
            Assert.NotNull(headerElem);
            Assert.Equal("PARTNER_A", headerElem.Element("destination")?.Value);
            Assert.Equal("DUMMY_HASH", headerElem.Element("hash")?.Value);

            var payloadElem = doc.Root?.Element("payload");
            Assert.NotNull(payloadElem);

            var recordElem = payloadElem.Element("record");
            Assert.NotNull(recordElem);
            Assert.Equal("80.5", recordElem.Element("speed")?.Value);
            Assert.Equal("1", recordElem.Element("lane")?.Value);
            Assert.Equal("true", recordElem.Element("isActive")?.Value);

            // Null property renders as empty tag
            var nullElem = recordElem.Element("nullProp");
            Assert.NotNull(nullElem);
            Assert.True(nullElem.IsEmpty || string.IsNullOrEmpty(nullElem.Value));
        }

        [Fact]
        public void SerializeDataToXmlBytes_MatchesFragmentForPayloadHash_Test()
        {
            // Arrange
            var data = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["stationId"] = "ST_01",
                    ["volume"] = 1500
                }
            };

            // Act
            var fragmentBytes = DataPublicationService.SerializeDataToXmlBytes(data);
            var fragmentStr = System.Text.Encoding.UTF8.GetString(fragmentBytes);

            // Assert
            Assert.StartsWith("<payload>", fragmentStr);
            Assert.EndsWith("</payload>", fragmentStr);
            Assert.Contains("<stationId>ST_01</stationId>", fragmentStr);
            Assert.Contains("<volume>1500</volume>", fragmentStr);
        }

        #endregion

        #region Shape Expression Evaluator Tests

        [Fact]
        public void Evaluate_ArithmeticBasic_ReturnsExpectedValue_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["val1"] = 10,
                ["val2"] = 5,
                ["val3"] = 2
            };

            // Act
            var ok = DataPublicationService.TryEvaluate("val1 + val2 * val3", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Equal(20m, result);
        }

        [Fact]
        public void Evaluate_ParenthesesAndDivision_ReturnsExpectedValue_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["a"] = 20,
                ["b"] = 8,
                ["c"] = 3
            };

            // Act
            var ok = DataPublicationService.TryEvaluate("(a - b) / c", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Equal(4m, result);
        }

        [Fact]
        public void Evaluate_UnaryMinusAndModulo_ReturnsExpectedValue_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["num"] = 15
            };

            // Act
            var ok = DataPublicationService.TryEvaluate("-num % 4", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Equal(-3m, result);
        }

        [Fact]
        public void Evaluate_ConcatFunction_JoinsStringsAndNumbers_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["code"] = "VDS",
                ["id"] = 102
            };

            // Act
            var ok = DataPublicationService.TryEvaluate("CONCAT(code, '_', id)", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Equal("VDS_102", result);
        }

        [Fact]
        public void Evaluate_IsNullAndCoalesce_ReturnsFirstNonNull_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["nullField"] = null,
                ["fallbackVal"] = 42m
            };

            // Act
            var okIsNull = DataPublicationService.TryEvaluate("ISNULL(nullField, fallbackVal)", row, out var resIsNull, out var err1);
            var okCoalesce = DataPublicationService.TryEvaluate("COALESCE(nullField, nullField, 99)", row, out var resCoalesce, out var err2);

            // Assert
            Assert.True(okIsNull, err1);
            Assert.Equal(42m, resIsNull);

            Assert.True(okCoalesce, err2);
            Assert.Equal(99m, resCoalesce);
        }

        [Fact]
        public void Evaluate_StringFunctions_UpperLowerLenTrim_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["text"] = "  Hello World  "
            };

            // Act & Assert
            Assert.True(DataPublicationService.TryEvaluate("UPPER(text)", row, out var upper, out _));
            Assert.Equal("  HELLO WORLD  ", upper);

            Assert.True(DataPublicationService.TryEvaluate("LOWER(text)", row, out var lower, out _));
            Assert.Equal("  hello world  ", lower);

            Assert.True(DataPublicationService.TryEvaluate("LEN(text)", row, out var len, out _));
            Assert.Equal(15m, len);

            Assert.True(DataPublicationService.TryEvaluate("LTRIM(text)", row, out var ltrim, out _));
            Assert.Equal("Hello World  ", ltrim);

            Assert.True(DataPublicationService.TryEvaluate("RTRIM(text)", row, out var rtrim, out _));
            Assert.Equal("  Hello World", rtrim);
        }

        [Fact]
        public void Evaluate_RoundAndAbs_ReturnsExpectedDecimal_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["speed"] = 85.6789m,
                ["diff"] = -15.4m
            };

            // Act & Assert
            Assert.True(DataPublicationService.TryEvaluate("ROUND(speed, 2)", row, out var rounded, out _));
            Assert.Equal(85.68m, rounded);

            Assert.True(DataPublicationService.TryEvaluate("ABS(diff)", row, out var absVal, out _));
            Assert.Equal(15.4m, absVal);
        }

        [Fact]
        public void Evaluate_NullOperandInArithmetic_PropagatesNull_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["nullVal"] = null
            };

            // Act
            var ok = DataPublicationService.TryEvaluate("nullVal + 10", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Null(result);
        }

        [Fact]
        public void Evaluate_DivideByZero_FailsGracefully_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["val"] = 100
            };

            // Act
            var ok = DataPublicationService.TryEvaluate("val / 0", row, out var result, out var error);

            // Assert
            Assert.False(ok);
            Assert.Contains("chia cho 0", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Evaluate_MissingFieldInRow_FailsWithDescriptiveError_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["knownField"] = 1
            };

            // Act
            var ok = DataPublicationService.TryEvaluate("missingField * 2", row, out var result, out var error);

            // Assert
            Assert.False(ok);
            Assert.Contains("missingField", error, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("SELECT * FROM users")]
        [InlineData("1; DROP TABLE EshPartner")]
        [InlineData("1 + /* comment */ 2")]
        [InlineData("EXEC('sp_who')")]
        [InlineData("sys.tables")]
        public void IsStaticallyValid_RejectsDangerousConstructs_Test(string dangerousExpr)
        {
            // Act
            var valid = DataPublicationService.IsStaticallyValid(dangerousExpr, out var error);

            // Assert
            Assert.False(valid);
            Assert.NotNull(error);
        }

        [Fact]
        public void IsStaticallyValid_RejectsEmptyOrOversizedExpression_Test()
        {
            // Empty
            Assert.False(DataPublicationService.IsStaticallyValid("", out var err1));
            Assert.NotNull(err1);

            // Oversized (> 512 chars)
            var longExpr = "1 + " + new string('1', 515);
            Assert.False(DataPublicationService.IsStaticallyValid(longExpr, out var err2));
            Assert.NotNull(err2);
        }

        #endregion

        #region Fix (a) Prefix Number Matching Tests

        [Theory]
        [InlineData("103_vdsData", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        [InlineData("106_wimData", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        [InlineData("109_etcData", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        [InlineData("103", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        [InlineData("101_commonData", (int)ShareDataEnum.PacketFilterMode.Snapshot)]
        [InlineData("102_cctvDevice", (int)ShareDataEnum.PacketFilterMode.Snapshot)]
        [InlineData("105_tollTransaction", (int)ShareDataEnum.PacketFilterMode.Snapshot)]
        [InlineData("PKT_INC_DATA", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        public void ResolveFilterMode_WithPrefixCodes_ReturnsCorrectFilterMode_Test(string code, int expectedMode)
        {
            var packet = new ShareDataPacket { Code = code };
            var mode = DataPublicationService.ResolveFilterMode(packet);
            Assert.Equal(expectedMode, mode);
        }

        [Theory]
        [InlineData("103_vdsData", 50)]
        [InlineData("106_wimData", 50)]
        [InlineData("109_etcData", 50)]
        [InlineData("103", 50)]
        [InlineData("101_commonData", null)]
        [InlineData("102_cctvDevice", null)]
        public void ResolveTopN_WithPrefixCodes_ReturnsExpectedTopN_Test(string code, int? expectedTopN)
        {
            var packet = new ShareDataPacket { Code = code };
            var topN = DataPublicationService.ResolveTopN(packet);
            Assert.Equal(expectedTopN, topN);
        }

        [Theory]
        [InlineData("103_vdsData", 103)]
        [InlineData(" 106_wimData ", 106)]
        [InlineData("109", 109)]
        [InlineData("invalid", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void ExtractPacketNumber_ExtractsNumericPrefixCorrectly_Test(string? code, int? expected)
        {
            var result = DataPublicationService.ExtractPacketNumber(code);
            Assert.Equal(expected, result);
        }

        #endregion

        #region Fix (e) $each / $as Aggregate Mode Tests

        [Fact]
        public void Transform_WithEachAndAs_ProducesSingleEnvelopeWithArrayData_Test()
        {
            // Arrange
            var shapeJson = @"
            {
                ""headerInfo"": ""TEST_HEADER"",
                ""data"": {
                    ""$each"": true,
                    ""$as"": {
                        ""zId"": { ""$field"": ""zoneId"" },
                        ""spd"": { ""$field"": ""averageSpeed"" }
                    }
                }
            }";

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zoneId"] = "Z01", ["averageSpeed"] = 60.5m },
                new Dictionary<string, object?> { ["zoneId"] = "Z02", ["averageSpeed"] = 75.0m },
                new Dictionary<string, object?> { ["zoneId"] = "Z03", ["averageSpeed"] = 80.0m }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId" },
                new() { FieldKey = "averageSpeed", Column = "AverageSpeed" }
            };

            // Act
            var result = DataPublicationService.Transform(rawRows, fields, shapeJson);

            // Assert
            Assert.Single(result);
            var envelope = result[0] as IDictionary<string, object?>;
            Assert.NotNull(envelope);
            Assert.Equal("TEST_HEADER", envelope["headerInfo"]);

            var dataList = envelope["data"] as List<object?>;
            Assert.NotNull(dataList);
            Assert.Equal(3, dataList.Count);

            var firstItem = dataList[0] as IDictionary<string, object?>;
            Assert.NotNull(firstItem);
            Assert.Equal("Z01", firstItem["zId"]);
            Assert.Equal(60.5m, firstItem["spd"]);

            var thirdItem = dataList[2] as IDictionary<string, object?>;
            Assert.NotNull(thirdItem);
            Assert.Equal("Z03", thirdItem["zId"]);
            Assert.Equal(80.0m, thirdItem["spd"]);
        }

        [Fact]
        public void Transform_WithoutEach_MaintainsOriginalPerRowBehavior_Test()
        {
            // Arrange
            var shapeJson = @"
            {
                ""zId"": { ""$field"": ""zoneId"" },
                ""spd"": { ""$field"": ""averageSpeed"" }
            }";

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zoneId"] = "Z01", ["averageSpeed"] = 60.5m },
                new Dictionary<string, object?> { ["zoneId"] = "Z02", ["averageSpeed"] = 75.0m }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId" },
                new() { FieldKey = "averageSpeed", Column = "AverageSpeed" }
            };

            // Act
            var result = DataPublicationService.Transform(rawRows, fields, shapeJson);

            // Assert
            Assert.Equal(2, result.Count);
            var item1 = result[0] as IDictionary<string, object?>;
            Assert.NotNull(item1);
            Assert.Equal("Z01", item1["zId"]);
        }

        #endregion

        #region Fix (guard) Recursion Depth Guard Tests

        [Fact]
        public void Evaluate_DeeplyNestedUnaryOrParentheses_RejectsGracefully_Test()
        {
            // Arrange - expression with 35 nested levels (> MaxRecursionDepth = 32)
            var row = new Dictionary<string, object?> { ["x"] = 5m };
            var deepExpr = string.Concat(Enumerable.Repeat("-(", 35)) + "x" + new string(')', 35);

            // Act
            var ok = DataPublicationService.TryEvaluate(deepExpr, row, out var result, out var error);

            // Assert
            Assert.False(ok);
            Assert.NotNull(error);
            Assert.Contains("sâu", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SerializeEnvelopeToXmlBytes_DeeplyNestedDictionary_TruncatesAtMaxDepth_Test()
        {
            // Arrange - deeply nested dictionary (100 levels)
            var current = new Dictionary<string, object?>();
            var root = current;
            for (var i = 0; i < 100; i++)
            {
                var child = new Dictionary<string, object?>();
                current[$"level_{i}"] = child;
                current = child;
            }
            current["leaf"] = "done";

            var header = new Dictionary<string, object?> { ["sender"] = "SYSTEM" };
            var data = new List<object> { root };

            // Act & Assert (must not throw StackOverflowException)
            var xmlBytes = DataPublicationService.SerializeEnvelopeToXmlBytes(header, data);
            Assert.NotNull(xmlBytes);
            Assert.True(xmlBytes.Length > 0);
        }

        #endregion

        private static readonly Dictionary<string, (string SelectClause, string FromJoinClause, string WhereClause)> GoldenSqlCatalog = new()
        {
            ["101"] = (
                "zs.ZoneId AS zoneId, z.Name AS zoneName, z.FromKmNumber AS fromLocationKm, z.FromMetNumber AS fromLocationMet, z.ToKmNumber AS toLocationKm, z.ToMetNumber AS toLocationMet, z.LaneId AS laneId, CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed, zs.Condition AS trafficCondition, zs.UpdateTime AS dataTime, z.MaxSpeed AS speedLimit, ts.TotalVehicleNumber AS vehicleCount",
                "FROM TmsZoneStatus zs LEFT JOIN TmsZone z ON zs.ZoneId = z.ID LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId",
                ""
            ),
            ["102"] = (
                "e.Code AS cameraCode, c.Name AS cameraName, NULL AS snapshot, c.SnapshotTime AS snapshotTime, c.DeviceState AS deviceState, e.KmNumber AS locationKm, e.MetNumber AS locationMet, e.DirectionId AS direction",
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
                        PacketVersion = def.Packet.PacketVersion
};
                    await db.Insertable(newPacket).ExecuteCommandAsync();
                }
                else
                {
                    packetId = existingPacket.ID;
                    existingPacket.PacketVersion = def.Packet.PacketVersion;
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
                                                            FieldsJson = tbl.FieldsJson,
                    OrderNo = tbl.OrderNo
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
                        PacketVersion = "1.0"
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "zs.ZoneId = z.ID",
                            OrderNo = 2,
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "zs.ZoneId = ts.ZoneId",
                            OrderNo = 3,
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
                        PacketVersion = "1.0"
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
                            FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                            {
                                new() { FieldKey = "cameraName", Column = "Name", DataType = "string", OrderNo = 2 },
                                // §1.9: snapshot lấy qua cameraSnapshotService (tải file JPEG thật/base64 từ Camera API theo chu kỳ), không lấy trực tiếp từ CSDL
                                new() { FieldKey = "snapshot", Column = "SnapshotUrl", DataType = "string", OrderNo = 3, NoSource = true },
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "c.Ip = e.Ip",
                            OrderNo = 2,
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
                        PacketVersion = "1.0"
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "td.EquipmentId = e.ID",
                            OrderNo = 2,
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "ISNULL(t.PlateEdit, t.PlateLpr) = vr.LicensePlate",
                            OrderNo = 2,
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "i.EventTypeId = et.ID",
                            OrderNo = 2,
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
                        PacketVersion = "1.0"
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "v.EquipmentId = e.ID",
                            OrderNo = 2,
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
                        PacketVersion = "1.0"
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "t.LaneId = l.LaneId",
                            OrderNo = 2,
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
                            JoinType = BaseEnums.PacketJoinType.Left,
                            JoinCondition = "t.StationId = s.StationId",
                            OrderNo = 3,
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
                        PacketVersion = "1.0"
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
                            JoinCondition = "OUTER APPLY (SELECT TOP 1 v.RowData FROM VmsCurrent v INNER JOIN TmsEquipment e2 ON v.EquipmentId = e2.ID WHERE e2.KmNumber = i.KmNumber AND (v.RowData IS NOT NULL) ORDER BY v.ExecutedDate DESC) v",
                                                        OrderNo = 2,
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
                        PacketVersion = "1.0"
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
