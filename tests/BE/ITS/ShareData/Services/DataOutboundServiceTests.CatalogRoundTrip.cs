using System.Data;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.VMS.Core.Entities;
using Shared.Core.Domain;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Chạy vòng đủ từng gói catalog đang bật trên staging (101–104, 106–109): câu trích mẫu chung và câu ghi chiều nhận
    ///              đọc từ tệp seed ShareDataSql, bộ khung hai chiều dựng trong bài test (header hệ thống, data đủ trường của gói), bộ mã chuẩn hóa nạp
    ///              bằng chính tệp seed bộ mã; gửi qua controller nhận thật,
    ///              bản nhận chạy câu ghi thật vào CSDL nhận. Bảng ở CSDL nhận chép cấu trúc bảng nguồn, nên đọc lại bằng cùng một câu SELECT ở hai CSDL
    ///              phải ra cùng dòng cho các cột mà gói chở đi được.
    /// Created date: 11/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>Tệp seed bộ mã chuẩn hóa (dữ liệu chuẩn, chạy mọi CSDL).</summary>
        private const string CodeSetSeedScript = "sharedata-seed-bo-ma-ShareDataCodeSet.sql";

        /// <summary>Mốc lần đầu của câu nối đuôi: chỉ lấy các dòng nguồn của bài test (giờ năm 2099), bỏ dữ liệu sẵn có.</summary>
        private const string RoundTripCursorStart = "2098-01-01 00:00:00.000_";

        /// <summary>Số đối tác cùng chạy vòng một gói: nửa sau dùng câu riêng biến tấu từ câu trích thật, đủ 5 kiểu biến tấu (số thứ tự chia 5 dư 0..4).</summary>
        private const int RoundTripPartnerCount = 10;

        /// <summary>Giờ của dòng nguồn: số giây tròn để kiểu datetime ở hai CSDL không lệch phần nghìn giây.</summary>
        private static readonly DateTime RoundTripTime = new(2099, 1, 1, 8, 0, 0);

        /// <summary>Một phép đối chiếu: câu đọc ở CSDL nguồn và câu đọc ở CSDL nhận, cùng cột cùng thứ tự, lọc theo @prefix.</summary>
        private sealed record RoundTripCheck(string SourceSql, string InboundSql)
        {
            public RoundTripCheck(string sql) : this(sql, sql)
            {
            }
        }

        /// <summary>Các trường của từng gói mà bộ khung chở đi, theo đặc tả gói tin.</summary>
        private static readonly Dictionary<string, string[]> RoundTripFields = new()
        {
            ["101"] = ["zoneStatusId", "zoneId", "zoneName", "fromLocationKm", "fromLocationMet", "toLocationKm", "toLocationMet", "laneId", "averageSpeed", "trafficCondition", "dataTime", "speedLimit", "vehicleCount"],
            ["102"] = ["cameraCode", "cameraName", "snapshot", "snapshotTime", "deviceState", "locationKm", "locationMet", "direction"],
            ["103"] = ["detectionId", "detectTime", "vehicleType", "licensePlate", "speed", "lane", "direction", "locationRoute", "equipmentId", "locationKm", "locationMet"],
            ["104"] = ["weatherStationId", "locationDetail", "temperature", "humidity", "windSpeed", "windDirection", "rainfall", "rainfallHour", "visibility", "weatherDescription", "weatherCode", "detectTime"],
            ["106"] = ["detectTime", "lane", "locationCode", "speed", "height", "width", "length"],
            ["107"] = ["incidentCode", "incidentName", "eventTypeId", "eventTypeName", "occurredTime", "locationKm", "locationMet", "locationRoute", "direction", "injuredCount", "vehicleCount", "incidentState", "description", "source"],
            ["108"] = ["equipmentCode", "vmsName", "locationKm", "locationMet", "direction", "laneId", "displayContent", "displayImageUrl", "displaySize", "priority", "executedTime"],
            ["109"] = ["transactionId", "entryTime", "exitTime", "vehicleTypeId", "licensePlate", "tagId", "laneId", "laneName", "stationId", "stationName", "tollPrice", "syncTime"]
        };

        /// <summary>Trường của gói 101 đi qua bộ mã chuẩn hóa: gửi đổi sang mã đối tác, nhận đổi ngược về mã gốc.</summary>
        private static readonly Dictionary<string, string> RoundTripCodeSets = new()
        {
            ["laneId"] = "lane_code",
            ["trafficCondition"] = "traffic_condition"
        };

        /// <summary>Đặt UpdateTime của dòng nguồn (SqlSugar không ghi UpdateTime khi thêm) để dòng lọt qua Mốc lần đầu của câu nối đuôi.</summary>
        private static Task<int> SetRoundTripRowTimeAsync<T>(ISqlSugarClient db, string id, DateTime time) where T : EntityTenant, new() =>
            db.Updateable<T>().SetColumns(x => x.UpdateTime == time).Where(x => x.ID == id).ExecuteCommandAsync();

        /// <summary>
        /// Author: DatHQ
        /// Description: Bộ khung hai chiều của gói: header là giá trị hệ thống (giờ, mã đối tác, mã gói, số thứ tự), một phần tử data giữ nguyên tên mọi trường;
        ///              gói 101 gắn bộ mã cho mã làn và tình trạng giao thông, ép kiểu chuỗi.
        /// Created date: 11/11/2026
        /// </summary>
        private static string RoundTripShapeJson(string code, string partnerCode, string packetCode)
        {
            var record = new JsonObject();
            foreach (var field in RoundTripFields[code])
            {
                var leaf = new JsonObject { ["$field"] = field };
                if (code == "101" && RoundTripCodeSets.TryGetValue(field, out var codeSet))
                {
                    leaf["$extend"] = new JsonObject { ["targetType"] = "string", ["codeSet"] = codeSet };
                }

                record[field] = leaf;
            }

            var header = new JsonObject
            {
                ["dataTime"] = new JsonObject { ["$meta"] = "Now" },
                ["partnerCode"] = new JsonObject { ["$meta"] = "PartnerCode", ["$value"] = partnerCode },
                ["packetCode"] = new JsonObject { ["$meta"] = "PacketCode", ["$value"] = packetCode },
                ["serial"] = new JsonObject { ["$meta"] = "Serial" }
            };
            var shape = new JsonObject { ["header"] = header, ["data"] = new JsonArray(record) };
            return shape.ToJsonString();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chạy tệp seed dữ liệu chuẩn như trên SSMS: bỏ dòng USE, chạy từng khối giữa các GO.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task RunSqlSeedScriptAsync(ISqlSugarClient db, string fileName)
        {
            var script = SeedScriptReader.ReadSqlScript(fileName);
            var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline);
            foreach (var batch in batches)
            {
                var body = Regex.Replace(batch, @"^USE \[[^\]]+\];\s*$", string.Empty, RegexOptions.Multiline);
                if (string.IsNullOrWhiteSpace(body))
                {
                    continue;
                }

                await db.Ado.ExecuteCommandAsync(body);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Gói catalog của bài test: câu trích mẫu chung đúng tệp seed, bộ biến như bước 1b của tệp seed (thêm Mốc lần đầu để chỉ lấy dòng của bài test),
        ///              câu ghi chiều nhận đúng khối 6.3 của tệp seed. Mã gói không bắt đầu bằng số gói, để đăng ký khai DatatypeId "101".. của test khác
        ///              (Worker tìm gói theo tiền tố mã) không nhận nhầm gói này.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task<ClonePacket> SeedRoundTripPacketAsync(ISqlSugarClient db, string code, string unique, string prefix)
        {
            var packetId = Guid.NewGuid().ToString("N");
            var packetCode = $"RT{code}_{unique}";
            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();

            var extractSql = SeedScriptReader.GetExtractSql(code);
            var variables = new List<SqlVariableItem>();
            if (extractSql.Contains("@pageSize", StringComparison.OrdinalIgnoreCase))
            {
                variables.Add(new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "100", OrderNo = 1 });
            }

            if (extractSql.Contains("@lastKey", StringComparison.OrdinalIgnoreCase))
            {
                variables.Add(new SqlVariableItem { Code = "lastKey", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastKey, DefaultValue = RoundTripCursorStart, OrderNo = 2 });
            }

            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, extractSql);
            var write = SeedScriptReader.ReadWriteSqlStatements().Single(w => w.PacketCode.StartsWith($"{code}_", StringComparison.Ordinal));
            await ShareDataSqlSeed.SetWriteAsync(db, packetId, write.WriteSql, write.AffectedTables);
            return new ClonePacket(unique, packetId, packetCode, prefix, []);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dòng nguồn của gói (mã mang tiền tố prefix), dòng có sẵn ở CSDL nhận cho câu ghi chỉ cập nhật, và các phép đối chiếu theo đặc tả câu ghi của gói.
        /// Created date: 11/11/2026
        /// </summary>
        private static async Task<List<RoundTripCheck>> SeedRoundTripSourceAsync(ISqlSugarClient db, ISqlSugarClient inboundDb, string code, string p)
        {
            switch (code)
            {
                case "101":
                    // Hai đoạn: mã làn và tình trạng đi qua bộ mã lane_code, traffic_condition (gửi đổi sang mã đối tác, nhận đổi ngược về mã gốc)
                    foreach (var (suffix, lane, condition, speed, vehicles, minute) in new[] { ("A", "2", "CONGES", "65.50", 150, 0), ("B", "0", "GOOD", "80.25", 20, 1) })
                    {
                        var zoneId = $"{p}{suffix}";
                        await db.Insertable(new TmsZone { ID = zoneId, Name = $"Doan {zoneId}", FromKmNumber = 10, FromMetNumber = 500, ToKmNumber = 20, ToMetNumber = 0, LaneId = lane, MaxSpeed = 80 }).ExecuteCommandAsync();
                        await db.Insertable(new TmsTrafficStatistic { ID = $"{zoneId}_T", ZoneId = zoneId, TotalVehicleNumber = vehicles }).ExecuteCommandAsync();
                        await db.Insertable(new TmsZoneStatus { ID = $"{zoneId}_S", ZoneId = zoneId, AverageSpeed = speed, Condition = condition }).ExecuteCommandAsync();
                        var statusTime = RoundTripTime.AddMinutes(minute);
                        await SetRoundTripRowTimeAsync<TmsZoneStatus>(db, $"{zoneId}_S", statusTime);
                    }

                    return
                    [
                        new("SELECT ID, Name, FromKmNumber, FromMetNumber, ToKmNumber, ToMetNumber, LaneId, MaxSpeed FROM TmsZone WHERE ID LIKE @prefix ORDER BY ID"),
                        new("SELECT ZoneId, TotalVehicleNumber FROM TmsTrafficStatistic WHERE ZoneId LIKE @prefix ORDER BY ZoneId"),
                        new("SELECT ID, ZoneId, AverageSpeed, Condition, UpdateTime FROM TmsZoneStatus WHERE ID LIKE @prefix ORDER BY ID")
                    ];

                case "102":
                    await db.Insertable(new TmsEquipment { ID = $"{p}_EQ", Code = $"{p}_CAM", Ip = $"ip-{p}", KmNumber = 15, MetNumber = 200, DirectionId = 1 }).ExecuteCommandAsync();
                    await db.Insertable(new CctvDevice { ID = $"{p}_CC", DeviceId = $"{p}_DEV", Name = $"Camera {p}", Ip = $"ip-{p}", SnapshotTime = RoundTripTime, DeviceState = 1 }).ExecuteCommandAsync();
                    return
                    [
                        new("SELECT Code, KmNumber, MetNumber, DirectionId FROM TmsEquipment WHERE Code LIKE @prefix ORDER BY Code"),
                        new(
                            "SELECT e.Code, c.Name, c.SnapshotTime, c.DeviceState FROM CctvDevice c JOIN TmsEquipment e ON c.Ip = e.Ip WHERE e.Code LIKE @prefix ORDER BY e.Code",
                            "SELECT c.Code, c.Name, c.SnapshotTime, c.DeviceState FROM CctvDevice c WHERE c.Code LIKE @prefix ORDER BY c.Code")
                    ];

                case "103":
                    await db.Insertable(new TmsEquipment { ID = $"{p}_EQ", Code = $"{p}_VDS", KmNumber = 30, MetNumber = 250 }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficData { ID = $"{p}_TD", EquipmentId = $"{p}_EQ", DetectTime = RoundTripTime, Type = "CAR", LicensePlate = $"{p}-1", Speed = 75.5f, Lane = "L1", Direction = "NORTH", Location = p }).ExecuteCommandAsync();
                    await SetRoundTripRowTimeAsync<TmsTrafficData>(db, $"{p}_TD", RoundTripTime);
                    // Câu ghi thiết bị chỉ cập nhật: thiết bị phải có sẵn ở CSDL nhận
                    await inboundDb.Insertable(new TmsEquipment { ID = $"{p}_EQ", Code = $"{p}_VDS", KmNumber = 0, MetNumber = 0 }).ExecuteCommandAsync();
                    return
                    [
                        new("SELECT ID, DetectTime, Type, LicensePlate, Speed, Lane, Direction, Location, EquipmentId FROM TmsTrafficData WHERE ID LIKE @prefix ORDER BY ID"),
                        new("SELECT ID, KmNumber, MetNumber FROM TmsEquipment WHERE ID LIKE @prefix ORDER BY ID")
                    ];

                case "104":
                    await db.Insertable(new TmsWeather { ID = $"{p}_W", RefId = $"{p}_WS", LocationDetail = $"Tram {p}", Temperature = 28.5f, Hudmidity = 75.25f, WindSpeed = 12.5f, WindDirection = "NE", Rain = 0.5f, RainHour = 1.25f, Foresight = 10f, Description = $"Mua {p}", ShortDescription = "RAIN", TimeDetect = RoundTripTime }).ExecuteCommandAsync();
                    await SetRoundTripRowTimeAsync<TmsWeather>(db, $"{p}_W", RoundTripTime);
                    return
                    [
                        new("SELECT RefId, TimeDetect, LocationDetail, Temperature, Hudmidity, WindSpeed, WindDirection, Rain, RainHour, Foresight, Description, ShortDescription FROM TmsWeather WHERE RefId LIKE @prefix ORDER BY RefId")
                    ];

                case "106":
                    await db.Insertable(new TmsTrafficData { ID = $"{p}_TD", DetectTime = RoundTripTime, Lane = "L2", Location = p, Speed = 60.5f, Height = 150, Width = 180, Length = 450 }).ExecuteCommandAsync();
                    await SetRoundTripRowTimeAsync<TmsTrafficData>(db, $"{p}_TD", RoundTripTime);
                    return
                    [
                        new("SELECT DetectTime, Lane, Location, Speed, Height, Width, Length FROM TmsTrafficData WHERE Location LIKE @prefix ORDER BY DetectTime")
                    ];

                case "107":
                    await db.Insertable(new TmsEventType { ID = $"{p}_ET", Name = $"Va cham {p}" }).ExecuteCommandAsync();
                    await db.Insertable(new TmsIncident { ID = $"{p}_IN", Code = $"{p}_INC", Name = $"Su co {p}", EventTypeId = $"{p}_ET", StartDate = RoundTripTime, KmNumber = 55, MetNumber = 500, Location = p, InfluenceScope = "1", InjuredNumber = 1, VehicleNumber = 2, State = "3", Description = $"Mo ta {p}", Source = "CCTV" }).ExecuteCommandAsync();
                    await SetRoundTripRowTimeAsync<TmsIncident>(db, $"{p}_IN", RoundTripTime);
                    // Câu ghi loại sự cố chỉ cập nhật: loại sự cố phải có sẵn ở CSDL nhận
                    // CSDL nhận chỉ cần các cột câu ghi dùng: bảng ở đó có thể chưa có cột mới của entity
                    await inboundDb.Insertable(new TmsEventType { ID = $"{p}_ET", Name = "old" }).InsertColumns(e => new { e.ID, e.Name }).ExecuteCommandAsync();
                    return
                    [
                        new("SELECT Code, Name, EventTypeId, StartDate, KmNumber, MetNumber, Location, InfluenceScope, InjuredNumber, VehicleNumber, State, Description, Source FROM TmsIncident WHERE Code LIKE @prefix ORDER BY Code"),
                        new("SELECT ID, Name FROM TmsEventType WHERE ID LIKE @prefix ORDER BY ID")
                    ];

                case "108":
                    await db.Insertable(new TmsEquipment { ID = $"{p}_EQ", Code = $"{p}_VMS", KmNumber = 70, MetNumber = 100, DirectionId = 1, LaneId = "LANE_ALL" }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent { ID = $"{p}_VM", EquipmentId = $"{p}_EQ", Name = $"VMS {p}", RowData = $"CHU Y {p}", Url = "http://vms.local/preview.png", Size = "192x64", Priority = 1, ExecutedDate = RoundTripTime }).ExecuteCommandAsync();
                    // Câu ghi biển báo nối thiết bị theo mã ở CSDL nhận: thiết bị phải có sẵn ở đó (ID khác bên nguồn)
                    await inboundDb.Insertable(new TmsEquipment { ID = $"{p}_EQIN", Code = $"{p}_VMS", KmNumber = 0, MetNumber = 0, DirectionId = 0, LaneId = "OLD" }).ExecuteCommandAsync();
                    return
                    [
                        new("SELECT Code, KmNumber, MetNumber, DirectionId, LaneId FROM TmsEquipment WHERE Code LIKE @prefix ORDER BY Code"),
                        new("SELECT e.Code, v.Name, v.RowData, v.Url, v.Size, v.Priority, v.ExecutedDate FROM VmsCurrent v JOIN TmsEquipment e ON v.EquipmentId = e.ID WHERE e.Code LIKE @prefix ORDER BY e.Code")
                    ];

                case "109":
                    var entryTime = RoundTripTime.AddMinutes(-20);
                    await db.Insertable(new TollTransactionOut { ID = $"{p}_TX", TransactionId = $"{p}_TXN", TransactionDateTimeIn = entryTime, TransactionDateTime = RoundTripTime, VehicleTypeId = "1", PlateEdit = $"{p}-2", TagId = $"{p}_TAG", LaneId = $"{p}_L", StationId = $"{p}_S", SyncTime = RoundTripTime }).ExecuteCommandAsync();
                    await db.Insertable(new TollLane { ID = $"{p}_LN", LaneId = $"{p}_L", Name = $"Lan {p}" }).ExecuteCommandAsync();
                    await db.Insertable(new TollStation { ID = $"{p}_ST", StationId = $"{p}_S", Name = $"Tram {p}" }).ExecuteCommandAsync();
                    await SetRoundTripRowTimeAsync<TollTransactionOut>(db, $"{p}_TX", RoundTripTime);
                    // Câu ghi làn và trạm chỉ cập nhật tên: làn, trạm phải có sẵn ở CSDL nhận
                    await inboundDb.Insertable(new TollLane { ID = $"{p}_LNIN", LaneId = $"{p}_L", Name = "old" }).InsertColumns(l => new { l.ID, l.LaneId, l.Name }).ExecuteCommandAsync();
                    await inboundDb.Insertable(new TollStation { ID = $"{p}_STIN", StationId = $"{p}_S", Name = "old" }).InsertColumns(s => new { s.ID, s.StationId, s.Name }).ExecuteCommandAsync();
                    return
                    [
                        new("SELECT TransactionId, TransactionDateTimeIn, TransactionDateTime, VehicleTypeId, ISNULL(PlateEdit, PlateLpr) AS Plate, TagId, LaneId, StationId, SyncTime FROM TollTransactionOut WHERE TransactionId LIKE @prefix ORDER BY TransactionId"),
                        new("SELECT LaneId, Name FROM TollLane WHERE LaneId LIKE @prefix ORDER BY LaneId"),
                        new("SELECT StationId, Name FROM TollStation WHERE StationId LIKE @prefix ORDER BY StationId")
                    ];

                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, "Gói chưa có kịch bản chạy vòng.");
            }
        }

        /// <summary>Đọc các dòng theo câu đối chiếu, mỗi dòng thành chuỗi các ô định dạng bất biến (ngày giờ tới phần nghìn giây, trống là ∅).</summary>
        private static async Task<List<string>> ReadRoundTripRowsAsync(ISqlSugarClient client, string sql, string prefix)
        {
            var pattern = $"{prefix}%";
            var table = await client.Ado.GetDataTableAsync(sql, new { prefix = pattern });
            return table.Rows.Cast<DataRow>().Select(FormatRoundTripRow).ToList();
        }

        /// <summary>Một dòng thành chuỗi các ô ngăn bởi dấu |.</summary>
        private static string FormatRoundTripRow(DataRow row)
        {
            var cells = row.ItemArray.Select(FormatRoundTripCell);
            return string.Join("|", cells);
        }

        /// <summary>Một ô thành chuỗi bất biến theo văn hóa.</summary>
        private static string FormatRoundTripCell(object? value) => value switch
        {
            null or DBNull => "∅",
            DateTime time => time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        /// <summary>
        /// Author: DatHQ
        /// Description: Mỗi gói catalog đang bật trên staging chạy vòng với nhiều đối tác như seed kiểm tải: câu trích và câu ghi đúng tệp seed (câu trích xuống
        ///              dòng CRLF như trên staging), bộ khung đủ trường của gói, bộ mã nạp bằng tệp seed bộ mã; nửa sau số đối tác dùng câu riêng biến tấu từ câu
        ///              trích thật (thêm cột phụ, đổi TOP 100 thành 20 hoặc 500, xuống dòng LF, chú thích cuối dài) và không khai câu ghi riêng.
        ///              4 bản chỉ gửi, 1 bản hai chiều, 1 bản chỉ nhận chạy cùng lúc; gửi qua controller nhận thật rồi bản nhận chạy câu ghi thật.
        ///              Đối tác nào cũng gửi được dòng nguồn của bài test, cột phụ của câu riêng không lên đường dây; gói 101 đi trên đường dây bằng mã đối tác
        ///              (LANE_2, CONGESTED, ALL_LANES, FREE_FLOW). Gói nhận nào cũng Done, không cảnh báo, mỗi gói nhận có dòng cha và bước 2 thành công trong nhật ký.
        ///              Đọc lại ở CSDL nhận bằng cùng câu SELECT phải ra đúng các dòng ở CSDL nguồn: câu ghi MERGE nên nhiều đối tác gửi cùng dòng không nhân dòng,
        ///              câu ghi chỉ cập nhật thì sửa đúng dòng có sẵn, gói 101 ghi lại đúng mã gốc.
        /// Created date: 11/11/2026
        /// Modified date: 12/11/2026
        /// </summary>
        [Theory]
        [InlineData("101", new[] { "LANE_2", "CONGESTED", "ALL_LANES", "FREE_FLOW" })]
        [InlineData("102", new string[0])]
        [InlineData("103", new string[0])]
        [InlineData("104", new string[0])]
        [InlineData("106", new string[0])]
        [InlineData("107", new string[0])]
        [InlineData("108", new string[0])]
        [InlineData("109", new string[0])]
        public async Task ProcessSubscriptions_WhenCatalogPacketRoundTripsThroughReceiveApi_InboundRowsMatchSourceRows_Test(string code, string[] wireTokens)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            using var receiverScope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var inboundDb = InboundDb(db);
            await RunSqlSeedScriptAsync(db, CodeSetSeedScript);
            var unique = Guid.NewGuid().ToString("N")[..6];
            var prefix = $"RT{code}{unique}";
            var packet = await SeedRoundTripPacketAsync(db, code, unique, prefix);
            var checks = await SeedRoundTripSourceAsync(db, inboundDb, code, prefix);
            var seedSql = SeedScriptReader.GetExtractSql(code);
            var commonSql = seedSql.ReplaceLineEndings("\r\n");
            await ShareDataSqlSeed.SetCommonAsync(db, packet.PacketId, commonSql);
            var commonVariables = await ShareDataSqlSeed.GetVariablesAsync(db, packet.PacketId, null);
            var partners = new List<ReceiverPartner>();
            for (var index = 1; index <= RoundTripPartnerCount; index++)
            {
                var name = $"RT{index:00}";
                var partnerCode = ClonePartnerCode(packet, name);
                var shapeJson = RoundTripShapeJson(code, partnerCode, packet.PacketCode);
                var partner = await SeedReceiverPartnerAsync(db, packet, name, ReceiveEndpoint, shapeJson);
                partners.Add(partner);
                if (index > RoundTripPartnerCount / 2)
                {
                    var ownSql = LoadTestOwnSql(commonSql, index, partnerCode);
                    await ShareDataSqlSeed.SetPartnerAsync(db, packet.PacketId, partner.Partner.ID, ownSql);
                    await ShareDataSqlSeed.SetVariablesAsync(db, packet.PacketId, partner.Partner.ID, commonVariables);
                }
            }

            var senders = Enumerable.Range(0, ManyPartnerSenderCount + 1).Select(_ => CreateReceiverClone(scope, null)).ToList();
            var receiveOnly = receiverScope.ServiceProvider.GetRequiredService<DataInboundService>();
            var bothServiceReceiver = CreateInboundService(scope);
            var receivers = new List<DataInboundService> { receiveOnly, bothServiceReceiver };
            var partnerIds = partners.Select(p => p.Partner.ID).ToList();

            // Act: gói nối đuôi gửi hết trong lượt đầu; gói gửi toàn bộ gửi lại ở lượt sau, câu ghi MERGE nên không nhân dòng
            for (var round = 0; round < 2; round++)
            {
                await RunLoadTestSeedRoundAsync(senders, receivers);
                foreach (var partner in partners)
                {
                    await MakeSubscriptionDue(db, partner.Outbound.ID);
                }
            }

            await receiveOnly.ProcessPendingPackets(CancellationToken.None);

            // Assert: đối tác nào (câu chung lẫn câu riêng) cũng gửi được dòng nguồn của bài test và bên nhận xử lý xong mọi gói
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => partnerIds.Contains(a.PartnerId)).ToListAsync();
            Assert.Empty(alerts);
            foreach (var partner in partners)
            {
                var received = await ReceivedPacketsAsync(inboundDb, packet, partner);
                var rawParts = received.Select(p => p.RawContent);
                var rawContents = string.Join("\n", rawParts);
                var receiveLogIds = received.Select(p => p.ReceiveLogId).ToList();
                var receiveParents = await db.Queryable<ShareDataActivityLog>().Where(l => receiveLogIds.Contains(l.ID)).ToListAsync();
                var processSteps = await db.Queryable<ShareDataActivityLog>().Where(l => receiveLogIds.Contains(l.ParentId) && l.StepNbr == 2).ToListAsync();

                Assert.NotEmpty(received);
                Assert.All(received, p => Assert.Equal(BaseEnums.InboundProcessState.Done, p.ProcessState));
                Assert.Contains(prefix, rawContents);
                Assert.DoesNotContain("ktPartner", rawContents);
                Assert.All(wireTokens, token => Assert.Contains(token, rawContents));
                Assert.Equal(received.Count, receiveParents.Count);
                Assert.All(receiveParents, l => Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success));
                Assert.Equal(received.Count, processSteps.Count);
                Assert.All(processSteps, l => Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success));
            }

            // Assert: CSDL nhận khớp CSDL nguồn
            foreach (var check in checks)
            {
                var sourceRows = await ReadRoundTripRowsAsync(db, check.SourceSql, prefix);
                var inboundRows = await ReadRoundTripRowsAsync(inboundDb, check.InboundSql, prefix);
                Assert.NotEmpty(sourceRows);
                Assert.Equal(sourceRows, inboundRows);
            }
        }
    }
}
