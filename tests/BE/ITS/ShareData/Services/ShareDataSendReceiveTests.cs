using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Dto.Inbound;
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums.Encrypt;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models.Formats;
using Modules.Integration.Delivery.Encrypt;
using Modules.Integration.Delivery.Formats;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Senders;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using Tests.ShareData.Common;

namespace Tests.ShareData.Services
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ kiểm thử tích hợp gửi thật -> nhận thật -> MERGE.
    ///              - Không mock HTTP: DataOutboundService với HttpDeliverySender THẬT gọi in-process qua Host.CreateApiClient()
    ///              - Dữ liệu nguồn trích xuất từ SQL mẫu THẬT (SeedScriptReader)
    ///              - Kiểm thử 1 gói Snapshot (105_rfidData) và 1 gói Incremental (104_weatherData)
    ///              - Kiểm thử tính lũy tiến cursor và tính MERGE idempotency (không nhân đôi bản ghi)
    ///              - Kiểm thử âm tính: Endpoint 404 không làm tiến mốc nhưng vẫn lưu tệp bằng chứng
    ///              - Kiểm thử định dạng Formats, mã hóa EncryptManager và roundtrip MERGE hai chiều
    /// Created date: 16/10/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataSendReceiveTests(Host host)
    {
        private readonly Host _host = host;
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly ISqlSugarClient _inboundDb = host.Services.GetRequiredService<ISqlSugarClient>()
            .AsTenant().GetConnectionScope(ShareDataWorker.Extensions.ShareDataWorkerExtensions.InboundConnectionKey);

        private static readonly byte[] TestAes256Key =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
        ];

        private sealed class DelegatingClientFactory(Func<HttpClient> factory) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => factory();
        }

        private static DataOutboundService CreateWorker(
            IServiceScope scope,
            IConfiguration configuration,
            Func<HttpClient> clientFactory)
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();
            var extraction = scope.ServiceProvider.GetRequiredService<DataOutboundExtractionProcess>();
            var clientFact = new DelegatingClientFactory(clientFactory);
            var fileConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ExportPathHelper.BasePathConfigKey] = configuration.GetValue<string>(ExportPathHelper.BasePathConfigKey) ?? Path.Combine(Path.GetTempPath(), "sharedata_send")
            }).Build();
            var dispatcher = new DeliveryDispatcher([
                new HttpDeliverySender(clientFact),
                new FileDeliverySender(fileConfig, scope.ServiceProvider.GetRequiredService<ILogger<FileDeliverySender>>()),
                new KafkaDeliverySender(),
                new FtpDeliverySender()
            ]);

            return new DataOutboundService(
                scopeFactory,
                logger,
                extraction,
                new DataOutboundTransport(dispatcher, configuration),
                scope.ServiceProvider.GetRequiredService<DataWorkerConfigService>());
        }

        #region 1. Test Gói Snapshot (Gói 105 - RFID Data)

        [Fact]
        public async Task SendReceive_WhenSnapshotPacket105_TransfersDataAndMergesIdempotently_Test()
        {
            // Arrange
            var unique = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"P_SR105_{unique}";
            var packetCode = $"105_rfid_{unique}";
            var destTable = $"TestDest105_{unique}";
            var tempExportDir = Path.Combine(Path.GetTempPath(), $"sharedata-sr-test-{unique}");
            Directory.CreateDirectory(tempExportDir);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NasStorage:BasePath"] = tempExportDir
                })
                .Build();

            // 1. Tạo bảng đích ở CSDL inbound
            await _inboundDb.Ado.ExecuteCommandAsync($"""
                CREATE TABLE dbo.{destTable} (
                    transactionId NVARCHAR(64) PRIMARY KEY,
                    tagId NVARCHAR(64) NULL,
                    licensePlate NVARCHAR(64) NULL,
                    vehicleTypeId NVARCHAR(64) NULL,
                    entryTime DATETIME NULL,
                    exitTime DATETIME NULL,
                    laneId NVARCHAR(64) NULL,
                    stationId NVARCHAR(64) NULL,
                    vehicleBrand NVARCHAR(128) NULL,
                    vehicleOwner NVARCHAR(128) NULL
                )
            """);

            var targetShapeJson = """
            {
                "data": [
                    {
                        "transactionId": { "$field": "transactionId" },
                        "tagId": { "$field": "tagId" },
                        "licensePlate": { "$field": "licensePlate" },
                        "vehicleTypeId": { "$field": "vehicleTypeId" },
                        "entryTime": { "$field": "entryTime" },
                        "exitTime": { "$field": "exitTime" },
                        "laneId": { "$field": "laneId" },
                        "stationId": { "$field": "stationId" },
                        "vehicleBrand": { "$field": "vehicleBrand" },
                        "vehicleOwner": { "$field": "vehicleOwner" }
                    }
                ]
            }
            """;

            var writeSql = $"""
            MERGE dbo.{destTable} AS target
            USING (
                SELECT transactionId, tagId, licensePlate, vehicleTypeId, entryTime, exitTime, laneId, stationId, vehicleBrand, vehicleOwner
                FROM OPENJSON(@records)
                WITH (
                    transactionId NVARCHAR(64) '$.transactionId',
                    tagId NVARCHAR(64) '$.tagId',
                    licensePlate NVARCHAR(64) '$.licensePlate',
                    vehicleTypeId NVARCHAR(64) '$.vehicleTypeId',
                    entryTime DATETIME '$.entryTime',
                    exitTime DATETIME '$.exitTime',
                    laneId NVARCHAR(64) '$.laneId',
                    stationId NVARCHAR(64) '$.stationId',
                    vehicleBrand NVARCHAR(128) '$.vehicleBrand',
                    vehicleOwner NVARCHAR(128) '$.vehicleOwner'
                )
            ) AS src
            ON target.transactionId = src.transactionId
            WHEN MATCHED THEN
                UPDATE SET
                    target.tagId = src.tagId,
                    target.licensePlate = src.licensePlate,
                    target.vehicleTypeId = src.vehicleTypeId,
                    target.entryTime = src.entryTime,
                    target.exitTime = src.exitTime,
                    target.laneId = src.laneId,
                    target.stationId = src.stationId,
                    target.vehicleBrand = src.vehicleBrand,
                    target.vehicleOwner = src.vehicleOwner
            WHEN NOT MATCHED THEN
                INSERT (transactionId, tagId, licensePlate, vehicleTypeId, entryTime, exitTime, laneId, stationId, vehicleBrand, vehicleOwner)
                VALUES (src.transactionId, src.tagId, src.licensePlate, src.vehicleTypeId, src.entryTime, src.exitTime, src.laneId, src.stationId, src.vehicleBrand, src.vehicleOwner);
            """;

            var txnId1 = $"TXN1_{unique}";
            var txnId2 = $"TXN2_{unique}";
            var plate1 = $"30A-{unique}-1";
            var plate2 = $"30A-{unique}-2";
            var now = await _db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            // 2. Chèn 2 bản ghi nguồn TollTransactionIn và TmsVehicleRegistration
            await _db.Insertable(new TollTransactionIn
            {
                ID = Guid.NewGuid().ToString("N"),
                TransactionId = txnId1,
                PlateLpr = plate1,
                PlateEdit = plate1,
                TagId = $"TAG1_{unique}",
                LaneId = "LANE_01",
                StationId = "STATION_01",
                VehicleTypeId = "VT_1",
                TransactionDateTime = now.AddMinutes(-10)
            }).ExecuteCommandAsync();

            await _db.Insertable(new TmsVehicleRegistration
            {
                ID = Guid.NewGuid().ToString("N"),
                LicensePlate = plate1,
                Brand = "Toyota",
                Owner = "Nguyen Van A"
            }).ExecuteCommandAsync();

            await _db.Insertable(new TollTransactionIn
            {
                ID = Guid.NewGuid().ToString("N"),
                TransactionId = txnId2,
                PlateLpr = plate2,
                PlateEdit = string.Empty,
                TagId = $"TAG2_{unique}",
                LaneId = "LANE_02",
                StationId = "STATION_01",
                VehicleTypeId = "VT_2",
                TransactionDateTime = now.AddMinutes(-5)
            }).ExecuteCommandAsync();

            await _db.Insertable(new TmsVehicleRegistration
            {
                ID = Guid.NewGuid().ToString("N"),
                LicensePlate = plate2,
                Brand = "Honda",
                Owner = "Tran Thi B"
            }).ExecuteCommandAsync();

            // 3. Tính toán kỳ vọng bằng cách chạy THẲNG SQL cũ (LegacyPacketSql gói 105 bỏ TOP) trên DB test
            var legacySql105 = LegacyPacketSql.Packet105.Replace("TOP (@snapshotTop)", string.Empty);
            var allLegacyRows = await _db.Ado.SqlQueryAsync<dynamic>(legacySql105);
            var foundTxns = allLegacyRows
                .Select(r => ((IDictionary<string, object>)r).TryGetValue("transactionId", out var v) ? v?.ToString() : null)
                .Where(t => t == txnId1 || t == txnId2)
                .ToList();
            Assert.True(foundTxns.Count == 2, $"allLegacyRows count={allLegacyRows.Count}, foundTxns=[{string.Join(", ", foundTxns)}], expected [{txnId1}, {txnId2}]");
            var expectedRows = foundTxns;

            // 4. Seed bên gửi và bên nhận
            var (partner, packet, sub) = await SeedPartnerAndPacketPipelineAsync(
                partnerCode: partnerCode,
                packetCode: packetCode,
                packetExtractCode: "105",
                targetShapeJson: targetShapeJson,
                inboundWriteSql: writeSql);

            try
            {
                // Act 1: Chạy DataOutboundService gửi dữ liệu sang in-process API
                using (var scope = _host.Services.CreateScope())
                {
                    var worker = CreateWorker(scope, config, () => _host.CreateApiClient());
                    await worker.ProcessSubscriptions(CancellationToken.None);
                }

                // Assert 1: Kiểm tra chiều nhận đã tiếp nhận gói tin thô
                var inboundPackets = await _inboundDb.Queryable<ShareDataInboundPacket>()
                    .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code)
                    .ToListAsync();
                Assert.Single(inboundPackets);

                var inboundPkt = inboundPackets[0];
                Assert.Equal(1, inboundPkt.SerialNbr);
                Assert.False(string.IsNullOrWhiteSpace(inboundPkt.RawContent));

                // Kiểm tra tệp bằng chứng đã lưu trên đĩa và khớp đúng RawContent
                var archiveFiles = Directory.GetFiles(tempExportDir, "*.*", SearchOption.AllDirectories);
                Assert.NotEmpty(archiveFiles);
                var archiveContent = await File.ReadAllTextAsync(archiveFiles[0]);
                Assert.Equal(archiveContent, inboundPkt.RawContent);

                // Act 2: Chạy DataInboundService để MERGE vào bảng đích
                using (var scope = _host.Services.CreateScope())
                {
                    var inboundWorker = scope.ServiceProvider.GetRequiredService<DataInboundService>();
                    await inboundWorker.ProcessPendingPackets(CancellationToken.None);
                }

                // Assert 2: Kiểm tra dữ liệu trong bảng đích khớp từng cột với nguồn
                var destRows = await _inboundDb.Ado.SqlQueryAsync<dynamic>($"SELECT * FROM dbo.{destTable} ORDER BY transactionId");
                Assert.Equal(allLegacyRows.Count, destRows.Count);

                var row1 = await _inboundDb.Ado.SqlQuerySingleAsync<dynamic>($"SELECT * FROM dbo.{destTable} WHERE transactionId = '{txnId1}'");
                Assert.NotNull(row1);
                IDictionary<string, object> dict1 = (IDictionary<string, object>)row1;
                Assert.Equal(plate1, dict1["licensePlate"]?.ToString());
                Assert.Equal("Toyota", dict1["vehicleBrand"]?.ToString());
                Assert.Equal("Nguyen Van A", dict1["vehicleOwner"]?.ToString());
                Assert.Null(dict1["exitTime"]); // NULL exitTime khớp đúng logic

                var row2 = await _inboundDb.Ado.SqlQuerySingleAsync<dynamic>($"SELECT * FROM dbo.{destTable} WHERE transactionId = '{txnId2}'");
                Assert.NotNull(row2);
                IDictionary<string, object> dict2 = (IDictionary<string, object>)row2;
                Assert.Equal(plate2, dict2["licensePlate"]?.ToString());
                Assert.Equal("Honda", dict2["vehicleBrand"]?.ToString());
                Assert.Equal("Tran Thi B", dict2["vehicleOwner"]?.ToString());

                // Assert 3: Log cha - con ghi Success
                var activityLogs = await _db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .ToListAsync();
                Assert.NotEmpty(activityLogs);
                Assert.Contains(activityLogs, l => l.Success == BaseEnums.SuccessEnums.Success);

                // Mốc ShareDataLastSend KHÔNG ghi nhận cho gói Snapshot
                var lastSend = await _db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partner.Code && c.PacketCode == packet.Code)
                    .FirstAsync();
                Assert.Null(lastSend);

                // Act 3 & Assert 4: POST lại đúng payload (với SerialNbr = 2) để chứng minh MERGE idempotency
                using (var scope = _host.Services.CreateScope())
                {
                    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                    await bus.InvokeAsync(new ShareDataAddInboundInput
                    {
                        PartnerCode = partner.Code,
                        PacketCode = packet.Code,
                        SerialNbr = 2,
                        RawContent = inboundPkt.RawContent
                    });

                    var inboundWorker = scope.ServiceProvider.GetRequiredService<DataInboundService>();
                    await inboundWorker.ProcessPendingPackets(CancellationToken.None);
                }

                var finalDestCount = await _inboundDb.Ado.GetIntAsync($"SELECT COUNT(1) FROM dbo.{destTable}");
                Assert.Equal(allLegacyRows.Count, finalDestCount); // Số dòng bảng đích KHÔNG đổi (MERGE không nhân đôi)
            }
            finally
            {
                // Dọn dẹp
                await _inboundDb.Ado.ExecuteCommandAsync($"DROP TABLE dbo.{destTable}");
                await _db.Deleteable<TollTransactionIn>().Where(t => t.TransactionId == txnId1 || t.TransactionId == txnId2).ExecuteCommandAsync();
                await _db.Deleteable<TmsVehicleRegistration>().Where(r => r.LicensePlate == plate1 || r.LicensePlate == plate2).ExecuteCommandAsync();
                await CleanupPipelineDataAsync(partner.ID, packet.ID, partner.Code, packet.Code);
                if (Directory.Exists(tempExportDir))
                {
                    Directory.Delete(tempExportDir, true);
                }
            }
        }

        #endregion

        #region 2. Test Gói Incremental (Gói 104 - Weather Data)

        [Fact]
        public async Task SendReceive_WhenIncrementalPacket104_TransfersDataAndTracksCursor_Test()
        {
            // Arrange
            var unique = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"P_SR104_{unique}";
            var packetCode = $"104_weather_{unique}";
            var destTable = $"TestDest104_{unique}";
            var tempExportDir = Path.Combine(Path.GetTempPath(), $"sharedata-sr-test-{unique}");
            Directory.CreateDirectory(tempExportDir);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NasStorage:BasePath"] = tempExportDir
                })
                .Build();

            // 1. Tạo bảng đích ở CSDL inbound
            await _inboundDb.Ado.ExecuteCommandAsync($"""
                CREATE TABLE dbo.{destTable} (
                    refId NVARCHAR(64) PRIMARY KEY,
                    locationDetail NVARCHAR(256) NULL,
                    temperature DECIMAL(18,2) NULL,
                    weatherDescription NVARCHAR(256) NULL,
                    detectTime DATETIME NULL
                )
            """);

            var targetShapeJson = """
            {
                "data": [
                    {
                        "weatherStationId": { "$field": "weatherStationId" },
                        "locationDetail": { "$field": "locationDetail" },
                        "temperature": { "$field": "temperature" },
                        "weatherDescription": { "$field": "weatherDescription" },
                        "detectTime": { "$field": "detectTime" }
                    }
                ]
            }
            """;

            var writeSql = $"""
            MERGE dbo.{destTable} AS target
            USING (
                SELECT weatherStationId, locationDetail, temperature, weatherDescription, detectTime
                FROM OPENJSON(@records)
                WITH (
                    weatherStationId NVARCHAR(64) '$.weatherStationId',
                    locationDetail NVARCHAR(256) '$.locationDetail',
                    temperature DECIMAL(18,2) '$.temperature',
                    weatherDescription NVARCHAR(256) '$.weatherDescription',
                    detectTime DATETIME '$.detectTime'
                )
            ) AS src
            ON target.refId = src.weatherStationId
            WHEN MATCHED THEN
                UPDATE SET
                    target.locationDetail = src.locationDetail,
                    target.temperature = src.temperature,
                    target.weatherDescription = src.weatherDescription,
                    target.detectTime = src.detectTime
            WHEN NOT MATCHED THEN
                INSERT (refId, locationDetail, temperature, weatherDescription, detectTime)
                VALUES (src.weatherStationId, src.locationDetail, src.temperature, src.weatherDescription, src.detectTime);
            """;

            var refId1 = $"W1_{unique}";
            var refId2 = $"W2_{unique}";
            var refId3 = $"W3_{unique}";
            var refId4 = $"W4_{unique}";
            var maxExistingTime = await _db.Ado.GetDateTimeAsync("SELECT ISNULL(MAX(UpdateTime), '2000-01-01') FROM dbo.TmsWeather");
            var now = await _db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = (maxExistingTime > now ? maxExistingTime : now).AddMinutes(10);

            // 2. Chèn 3 dòng nguồn TmsWeather ban đầu
            var weather1 = new TmsWeather
            {
                ID = $"w1_{unique}",
                RefId = refId1,
                LocationDetail = "Tram 1",
                Temperature = 25.0f,
                Description = "Nang",
                TimeDetect = baseTime,
                CreateTime = baseTime,
                UpdateTime = baseTime
            };
            var weather2 = new TmsWeather
            {
                ID = $"w2_{unique}",
                RefId = refId2,
                LocationDetail = "Tram 2",
                Temperature = 26.5f,
                Description = "May",
                TimeDetect = baseTime.AddMinutes(5),
                CreateTime = baseTime.AddMinutes(5),
                UpdateTime = baseTime.AddMinutes(5)
            };
            var weather3 = new TmsWeather
            {
                ID = $"w3_{unique}",
                RefId = refId3,
                LocationDetail = "Tram 3",
                Temperature = 28.0f,
                Description = "Mua nhe",
                TimeDetect = baseTime.AddMinutes(10),
                CreateTime = baseTime.AddMinutes(10),
                UpdateTime = baseTime.AddMinutes(10)
            };
            await _db.Insertable(new[] { weather1, weather2, weather3 }).ExecuteCommandAsync();

            // 3. Seed bên gửi và nhận (SyncMode = Incremental, CursorTimeColumn = "UpdateTime")
            var (partner, packet, sub) = await SeedPartnerAndPacketPipelineAsync(
                partnerCode: partnerCode,
                packetCode: packetCode,
                packetExtractCode: "104",
                targetShapeJson: targetShapeJson,
                inboundWriteSql: writeSql);

            try
            {
                // Act 1: Chạy lượt 1 -> gửi 3 dòng nguồn
                using (var scope = _host.Services.CreateScope())
                {
                    var worker = CreateWorker(scope, config, () => _host.CreateApiClient());
                    await worker.ProcessSubscriptions(CancellationToken.None);

                    var inboundWorker = scope.ServiceProvider.GetRequiredService<DataInboundService>();
                    await inboundWorker.ProcessPendingPackets(CancellationToken.None);
                }

                // Assert 1: Bảng đích có tối thiểu 3 dòng và mốc ShareDataLastSend đã tiến
                var destCount1 = await _inboundDb.Ado.GetIntAsync($"SELECT COUNT(1) FROM dbo.{destTable}");
                Assert.True(destCount1 >= 3, $"destCount1={destCount1}");

                var row1 = await _inboundDb.Ado.SqlQuerySingleAsync<dynamic>($"SELECT * FROM dbo.{destTable} WHERE refId = '{refId1}'");
                var row2 = await _inboundDb.Ado.SqlQuerySingleAsync<dynamic>($"SELECT * FROM dbo.{destTable} WHERE refId = '{refId2}'");
                var row3 = await _inboundDb.Ado.SqlQuerySingleAsync<dynamic>($"SELECT * FROM dbo.{destTable} WHERE refId = '{refId3}'");
                Assert.NotNull(row1);
                Assert.NotNull(row2);
                Assert.NotNull(row3);

                var cursor1 = await _db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partner.Code && c.PacketCode == packet.Code)
                    .FirstAsync();
                Assert.NotNull(cursor1);
                Assert.NotNull(cursor1.LastTime);
                Assert.Equal(weather3.ID, cursor1.LastKey);

                // Act 2: Chèn thêm 1 dòng mới (weather4) và cập nhật 1 dòng cũ (weather1)
                var newUpdateTime = baseTime.AddMinutes(20);
                var weather4 = new TmsWeather
                {
                    ID = $"w4_{unique}",
                    RefId = refId4,
                    LocationDetail = "Tram 4",
                    Temperature = 30.0f,
                    Description = "Gio manh",
                    TimeDetect = newUpdateTime,
                    CreateTime = newUpdateTime,
                    UpdateTime = newUpdateTime
                };
                await _db.Insertable(weather4).ExecuteCommandAsync();

                // Cập nhật dòng 1
                await _db.Updateable<TmsWeather>()
                    .SetColumns(w => w.Temperature == 35.5f)
                    .SetColumns(w => w.UpdateTime == newUpdateTime)
                    .Where(w => w.ID == weather1.ID)
                    .ExecuteCommandAsync();

                // Chạy lượt 2
                using (var scope = _host.Services.CreateScope())
                {
                    // Đặt NextTimeRun về quá khứ để worker quét lại
                    var nowDb = await _db.Ado.GetDateTimeAsync("SELECT GETDATE()");
                    await _db.Updateable<ShareDataSubscription>()
                        .SetColumns(s => s.NextTimeRun == nowDb.AddSeconds(-60))
                        .Where(s => s.ID == sub.ID)
                        .ExecuteCommandAsync();

                    var worker = CreateWorker(scope, config, () => _host.CreateApiClient());
                    await worker.ProcessSubscriptions(CancellationToken.None);

                    var inboundWorker = scope.ServiceProvider.GetRequiredService<DataInboundService>();
                    await inboundWorker.ProcessPendingPackets(CancellationToken.None);
                }

                // Assert 2: Gói tin lượt 2 chỉ chứa đúng 2 bản ghi
                var secondPkt = await _inboundDb.Queryable<ShareDataInboundPacket>()
                    .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code && p.SerialNbr == 2)
                    .FirstAsync();
                Assert.NotNull(secondPkt);

                using (var doc = JsonDocument.Parse(secondPkt.RawContent!))
                {
                    var dataArray = doc.RootElement.GetProperty("data");
                    Assert.Equal(2, dataArray.GetArrayLength());
                }

                // Bảng đích có thêm đúng 1 dòng mới và dòng sửa được cập nhật giá trị mới
                var destCount2 = await _inboundDb.Ado.GetIntAsync($"SELECT COUNT(1) FROM dbo.{destTable}");
                Assert.Equal(destCount1 + 1, destCount2);

                var updatedRow1 = await _inboundDb.Ado.SqlQuerySingleAsync<dynamic>($"SELECT * FROM dbo.{destTable} WHERE refId = '{refId1}'");
                Assert.NotNull(updatedRow1);
                IDictionary<string, object> dict1 = (IDictionary<string, object>)updatedRow1;
                Assert.Equal(35.5m, Convert.ToDecimal(dict1["temperature"]));
            }
            finally
            {
                // Dọn dẹp
                await _inboundDb.Ado.ExecuteCommandAsync($"DROP TABLE dbo.{destTable}");
                await _db.Deleteable<TmsWeather>().Where(w => w.ID == weather1.ID || w.ID == weather2.ID || w.ID == weather3.ID || w.ID == $"w4_{unique}").ExecuteCommandAsync();
                await CleanupPipelineDataAsync(partner.ID, packet.ID, partner.Code, packet.Code);
                if (Directory.Exists(tempExportDir))
                {
                    Directory.Delete(tempExportDir, true);
                }
            }
        }

        #endregion

        #region 3. Test Âm Tính: Endpoint 404 Không Tiến Mốc Nhưng Vẫn Lưu Bằng Chứng

        [Fact]
        public async Task SendReceive_WhenEndpointNotFound_FailsCleanlyAndCursorDoesNotAdvance_Test()
        {
            // Arrange
            var unique = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"P_404_{unique}";
            var packetCode = $"104_err_{unique}";
            var tempExportDir = Path.Combine(Path.GetTempPath(), $"sharedata-sr-test-{unique}");
            Directory.CreateDirectory(tempExportDir);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NasStorage:BasePath"] = tempExportDir
                })
                .Build();

            var weather = new TmsWeather
            {
                ID = $"w_err_{unique}",
                RefId = $"W_ERR_{unique}",
                LocationDetail = "Tram Loi",
                Temperature = 20.0f,
                TimeDetect = DateTime.Now,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(weather).ExecuteCommandAsync();

            // Seed đối tác với EndPointApiUrl trỏ tới URL không tồn tại (404)
            var (partner, packet, sub) = await SeedPartnerAndPacketPipelineAsync(
                partnerCode: partnerCode,
                packetCode: packetCode,
                packetExtractCode: "104",
                targetShapeJson: """{"data":[{"refId":{"$field":"weatherStationId"}}]}""",
                inboundWriteSql: "SELECT 1",
                endPointApiUrl: $"/api/sharedata/not-found-endpoint-{unique}");

            try
            {
                // Act: Thực hiện xuất bản
                using (var scope = _host.Services.CreateScope())
                {
                    var worker = CreateWorker(scope, config, () => _host.CreateApiClient());
                    await worker.ProcessSubscriptions(CancellationToken.None);
                }

                // Assert 1: Không có dòng ShareDataInboundPacket mới nào được tạo
                var inboundPacketCount = await _inboundDb.Queryable<ShareDataInboundPacket>()
                    .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code)
                    .CountAsync();
                Assert.Equal(0, inboundPacketCount);

                // Assert 2: Mốc ShareDataLastSend KHÔNG tiến (LastKey vẫn null, SerialNbr vẫn null hoặc 0)
                var lastSend = await _db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partner.Code && c.PacketCode == packet.Code)
                    .FirstAsync();
                if (lastSend != null)
                {
                    Assert.Null(lastSend.LastKey);
                }
                var refreshedSub = await _db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.Equal(sub.SerialNbr, refreshedSub.SerialNbr);

                // Assert 3: Tệp bằng chứng VẪN được lưu trên đĩa (trước khi gọi kênh)
                var archiveFiles = Directory.GetFiles(tempExportDir, "*.*", SearchOption.AllDirectories);
                Assert.NotEmpty(archiveFiles);

                // Assert 4: Có log cảnh báo HttpSendFailed hoặc lỗi tương ứng
                var alertLogs = await _db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID)
                    .ToListAsync();
                Assert.NotEmpty(alertLogs);
                Assert.Contains(alertLogs, a => a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed);
            }
            finally
            {
                // Dọn dẹp
                await _db.Deleteable<TmsWeather>().Where(w => w.ID == weather.ID).ExecuteCommandAsync();
                await CleanupPipelineDataAsync(partner.ID, packet.ID, partner.Code, packet.Code);
                if (Directory.Exists(tempExportDir))
                {
                    Directory.Delete(tempExportDir, true);
                }
            }
        }

        #endregion

        #region 4. Unit Tests tầng Định dạng (Formats) & EncryptManager

        [Fact]
        public void JsonFormat_ToAndFromTransport_RoundtripMatches_Test()
        {
            // Arrange
            var format = new JsonFormat();
            var ctx = new FormatContext("PARTNER_01", "101", 1, DateTime.UtcNow);
            var originalJson = """[{"id":"Z1","val":100},{"id":"Z2","val":200}]""";

            // Act
            var payload = format.ToTransport(originalJson, ctx);
            var restoredJson = format.FromTransport(payload.Bytes, ctx);

            // Assert
            Assert.Equal("application/json", payload.ContentType);
            Assert.Equal(".json", payload.FileExtension);
            Assert.Equal(originalJson, restoredJson);
        }

        [Fact]
        public void KafkaJsonFormat_ToTransport_HasExpectedHeadersAndRoundtripMatches_Test()
        {
            // Arrange
            var format = new KafkaJsonFormat();
            var now = DateTime.UtcNow;
            var ctx = new FormatContext("PARTNER_KAFKA", "103", 42, now);
            var originalJson = """{"records":[{"plate":"29A-12345","speed":80}]}""";

            // Act
            var payload = format.ToTransport(originalJson, ctx);
            var restoredJson = format.FromTransport(payload.Bytes, ctx);

            // Assert
            Assert.Equal("application/json", payload.ContentType);
            Assert.True(payload.Headers.TryGetValue("content-type", out var ct) && ct == "application/json");
            Assert.True(payload.Headers.TryGetValue("PartnerCode", out var pCode) && pCode == "PARTNER_KAFKA");
            Assert.True(payload.Headers.TryGetValue("PacketCode", out var pktCode) && pktCode == "103");
            Assert.True(payload.Headers.TryGetValue("SerialNbr", out var sNbr) && sNbr == "42");
            Assert.True(payload.Headers.TryGetValue("ProcessedAt", out var pAt) && !string.IsNullOrWhiteSpace(pAt));
            Assert.Equal(originalJson, restoredJson);
        }

        [Fact]
        public void CsvFormat_FromTransport_ThrowsNotSupportedException_Test()
        {
            // Arrange
            var format = new CsvFormat();
            var ctx = new FormatContext("PARTNER_CSV", "101", 1);
            var dummyBytes = Encoding.UTF8.GetBytes("id,name\r\n1,Test");

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() => format.FromTransport(dummyBytes, ctx));
            Assert.Contains("Chiều nhận hiện chỉ hỗ trợ định dạng JSON", ex.Message);
        }

        [Fact]
        public void GetEncryptor_WhenInbound_ReadsInboundEncryptorAndDecryptionKeyRef_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ShareDataSecurity:Keys:test-inbound-key"] = Convert.ToBase64String(TestAes256Key)
                })
                .Build();
            var keyReader = new EncryptKeyReader(cfg);
            var encryptManager = new EncryptManager(keyReader);
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Encryptor] = EncryptNames.None,
                [ConnectKeys.EncryptionKeyRef] = "outbound-key",
                [ConnectKeys.InboundEncryptor] = EncryptNames.AesGcm,
                [ConnectKeys.DecryptionKeyRef] = "test-inbound-key"
            };

            // Act
            var (encryptor, ctx) = encryptManager.GetEncryptor(config, PayloadDirection.Inbound);

            // Assert
            Assert.IsType<AesGcmEncrypt>(encryptor);
            Assert.Equal("test-inbound-key", ctx.KeyRef);
            Assert.Equal(TestAes256Key, ctx.KeyBytes);
        }

        [Fact]
        public void GetEncryptor_WhenInboundMissingKeyRef_ThrowsInvalidOperationException_Test()
        {
            // Arrange
            var cfg = new ConfigurationBuilder().Build();
            var keyReader = new EncryptKeyReader(cfg);
            var encryptManager = new EncryptManager(keyReader);
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.InboundEncryptor] = EncryptNames.AesGcm
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => encryptManager.GetEncryptor(config, PayloadDirection.Inbound));
            Assert.Contains("DecryptionKeyRef", ex.Message);
        }

        #endregion

        #region 5. Roundtrip Inbound MERGE Tests (Qua IMessageBus)

        [Fact]
        public async Task Roundtrip_Packet101_WithCodeSetAndDateFormat_SavesToDestinationTable_Test()
        {
            // Arrange: Thiết lập bảng đích tạm và câu WriteSql MERGE
            var unique = Guid.NewGuid().ToString("N")[..8];
            var destTable = $"TestDest101_{unique}";
            await _inboundDb.Ado.ExecuteCommandAsync($"""
                CREATE TABLE dbo.{destTable} (
                    id NVARCHAR(64) PRIMARY KEY,
                    code NVARCHAR(64) NULL,
                    recorded_time DATETIME2 NULL,
                    val INT NULL
                )
            """);

            // Cùng bộ khung TargetShapeJson S dùng cho cả gửi và nhận
            var targetShapeJson = """
            {
                "data": [
                    {
                        "id": { "$field": "id" },
                        "code": { "$field": "code", "$extend": { "codeSet": "TEST_STATUS" } },
                        "time": { "$field": "time", "$extend": { "dateFormat": "yyyy-MM-dd HH:mm:ss" } },
                        "val": { "$field": "val" }
                    }
                ]
            }
            """;

            var writeSql = $"""
            MERGE dbo.{destTable} AS target
            USING (
                SELECT id, code, recorded_time, val
                FROM OPENJSON(@records)
                WITH (
                    id NVARCHAR(64) '$.id',
                    code NVARCHAR(64) '$.code',
                    recorded_time DATETIME2 '$.time',
                    val INT '$.val'
                )
            ) AS src
            ON target.id = src.id
            WHEN MATCHED THEN
                UPDATE SET target.code = src.code, target.recorded_time = src.recorded_time, target.val = src.val
            WHEN NOT MATCHED THEN
                INSERT (id, code, recorded_time, val) VALUES (src.id, src.code, src.recorded_time, src.val)
            OUTPUT $action;
            """;

            var (partner, packet, sub) = await SeedPartnerAndPacketAsync(unique, null, packetCode: "101", targetShapeJson: targetShapeJson, writeSql: writeSql);

            // Giả lập dữ liệu JSON chuẩn sinh ra từ bộ khung S của gói 101
            var payloadJson = """
            {
                "data": [
                    {
                        "id": "ITEM_01",
                        "code": "ACTIVE",
                        "time": "2026-10-14 08:30:00",
                        "val": 150
                    },
                    {
                        "id": "ITEM_02",
                        "code": "STANDBY",
                        "time": "2026-10-14 09:45:00",
                        "val": 280
                    }
                ]
            }
            """;

            using var scope = _host.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            // Act 1: WebAPI MessageBus tiếp nhận qua điểm vào thật
            var input = new ShareDataAddInboundInput
            {
                PartnerCode = partner.Code,
                PacketCode = packet.Code,
                SerialNbr = 1,
                RawContent = payloadJson
            };
            await bus.InvokeAsync(input);

            // Act 2: Worker xử lý
            var inboundService = scope.ServiceProvider.GetRequiredService<DataInboundService>();
            await inboundService.ProcessPendingPackets(CancellationToken.None);

            // Assert: Bản ghi vào bảng đích đúng cấu trúc và giá trị
            var count = await _inboundDb.Ado.GetIntAsync($"SELECT COUNT(1) FROM dbo.{destTable}");
            Assert.Equal(2, count);

            var item1Val = await _inboundDb.Ado.GetIntAsync($"SELECT val FROM dbo.{destTable} WHERE id = 'ITEM_01'");
            Assert.Equal(150, item1Val);

            var item2Code = await _inboundDb.Ado.GetStringAsync($"SELECT code FROM dbo.{destTable} WHERE id = 'ITEM_02'");
            Assert.Equal("STANDBY", item2Code);

            // Dọn dẹp bảng tạm
            await _inboundDb.Ado.ExecuteCommandAsync($"DROP TABLE dbo.{destTable}");
            await CleanupPipelineDataAsync(partner.ID, packet.ID, partner.Code, packet.Code);
        }

        [Fact]
        public async Task Roundtrip_WhenDuplicatePacketSent_MergePreventsRowDuplication_Test()
        {
            // Arrange
            var unique = Guid.NewGuid().ToString("N")[..8];
            var destTable = $"TestDestMerge_{unique}";
            await _inboundDb.Ado.ExecuteCommandAsync($"""
                CREATE TABLE dbo.{destTable} (
                    id NVARCHAR(64) PRIMARY KEY,
                    val INT NULL
                )
            """);

            var targetShapeJson = """
            {
                "data": [
                    {
                        "id": { "$field": "id" },
                        "val": { "$field": "val" }
                    }
                ]
            }
            """;

            var writeSql = $"""
            MERGE dbo.{destTable} AS target
            USING (
                SELECT id, val
                FROM OPENJSON(@records)
                WITH (
                    id NVARCHAR(64) '$.id',
                    val INT '$.val'
                )
            ) AS src
            ON target.id = src.id
            WHEN MATCHED THEN
                UPDATE SET target.val = src.val
            WHEN NOT MATCHED THEN
                INSERT (id, val) VALUES (src.id, src.val)
            OUTPUT $action;
            """;

            var (partner, packet, _) = await SeedPartnerAndPacketAsync(unique, null, packetCode: "103", targetShapeJson: targetShapeJson, writeSql: writeSql);
            var payloadJson = """{"data":[{"id":"DUP_01","val":500}]}""";

            using var scope = _host.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var inboundService = scope.ServiceProvider.GetRequiredService<DataInboundService>();

            // Act: Gửi lần 1 và xử lý qua điểm vào MessageBus
            await bus.InvokeAsync(new ShareDataAddInboundInput
            {
                PartnerCode = partner.Code,
                PacketCode = packet.Code,
                SerialNbr = 1,
                RawContent = payloadJson
            });
            await inboundService.ProcessPendingPackets(CancellationToken.None);

            // Gửi lần 2 (cùng payload do chu kỳ trích xuất lại hoặc gửi lại)
            await bus.InvokeAsync(new ShareDataAddInboundInput
            {
                PartnerCode = partner.Code,
                PacketCode = packet.Code,
                SerialNbr = 2,
                RawContent = payloadJson
            });
            await inboundService.ProcessPendingPackets(CancellationToken.None);

            // Assert: Có 2 bản ghi packet trong ShareDataInboundPacket nhưng bảng đích vẫn chỉ 1 dòng nhờ MERGE
            var packetCount = await _inboundDb.Queryable<ShareDataInboundPacket>()
                .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code)
                .CountAsync();
            Assert.Equal(2, packetCount);

            var destCount = await _inboundDb.Ado.GetIntAsync($"SELECT COUNT(1) FROM dbo.{destTable}");
            Assert.Equal(1, destCount);

            await _inboundDb.Ado.ExecuteCommandAsync($"DROP TABLE dbo.{destTable}");
            await CleanupPipelineDataAsync(partner.ID, packet.ID, partner.Code, packet.Code);
        }

        #endregion

        #region Helpers

        private async Task<(ShareDataPartner Partner, ShareDataPacket Packet, ShareDataSubscription Subscription)>
            SeedPartnerAndPacketPipelineAsync(
                string partnerCode,
                string packetCode,
                string packetExtractCode,
                string targetShapeJson,
                string inboundWriteSql,
                string endPointApiUrl = "/api/sharedata/shareDataInbound/receive")
        {
            var dbNow = await _db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            // Partner dùng chung cho cả gửi và nhận
            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = partnerCode,
                Name = $"Partner {partnerCode}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected,
                ConnectionType = ConnectionType.Api,
                Address = "http://localhost",
                Port = 80,
                EndPointApiUrl = endPointApiUrl,
                ConnectionConfig = "{}"
            };
            await _db.Insertable(partner).ExecuteCommandAsync();

            // Packet dùng ExtractSql & SyncMode đọc thật từ script seed 02
            var extractSql = SeedScriptReader.GetExtractSql(packetExtractCode);
            var syncMode = SeedScriptReader.GetSyncMode(packetExtractCode);

            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                OrderNo = 1,
                Status = BaseEnums.StatusEnum.Enable,
                ExtractSql = extractSql,
                SyncMode = (BaseEnums.SyncMode)syncMode,
                CursorTimeColumn = syncMode == 2 ? "UpdateTime" : null
            };
            await _db.Insertable(packet).ExecuteCommandAsync();

            // Outbound Subscription & Mapping
            var outboundSub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"SUB_OUT_{partnerCode}",
                PartnerId = partner.ID,
                DatatypeId = packet.ID,
                Direction = BaseEnums.Direction.Outbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = dbNow.AddMinutes(-5),
                IntervalSeconds = 30,
                SerialNbr = 1
            };
            await _db.Insertable(outboundSub).ExecuteCommandAsync();

            var outboundMapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = packet.ID,
                Direction = BaseEnums.Direction.Outbound,
                IsActive = true,
                TargetShapeJson = targetShapeJson,
                UpdateTime = dbNow
            };
            await _db.Insertable(outboundMapping).ExecuteCommandAsync();

            // Inbound Subscription & Mapping (lưu trên _db hệ thống)
            var inboundSub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"SUB_IN_{partnerCode}",
                PartnerId = partner.ID,
                DatatypeId = packet.ID,
                Direction = BaseEnums.Direction.Inbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                CreateTime = dbNow
            };
            await _db.Insertable(inboundSub).ExecuteCommandAsync();

            var inboundMapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = packet.ID,
                Direction = BaseEnums.Direction.Inbound,
                IsActive = true,
                TargetShapeJson = targetShapeJson,
                UpdateTime = dbNow
            };
            await _db.Insertable(inboundMapping).ExecuteCommandAsync();

            var inboundPacketSql = new ShareDataPacketSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packet.Code,
                OrderNo = 1,
                Status = BaseEnums.StatusEnum.Enable,
                WriteSql = inboundWriteSql
            };
            await _db.Insertable(inboundPacketSql).ExecuteCommandAsync();

            return (partner, packet, outboundSub);
        }

        private async Task CleanupPipelineDataAsync(string partnerId, string packetId, string partnerCode, string packetCode)
        {
            await _db.Deleteable<ShareDataMapping>().Where(m => m.PartnerId == partnerId).ExecuteCommandAsync();
            await _db.Deleteable<ShareDataSubscription>().Where(s => s.PartnerId == partnerId).ExecuteCommandAsync();
            await _db.Deleteable<ShareDataPacketSql>().Where(s => s.PacketCode == packetCode).ExecuteCommandAsync();
            await _db.Deleteable<ShareDataPacket>().Where(p => p.ID == packetId).ExecuteCommandAsync();
            await _db.Deleteable<ShareDataPartner>().Where(p => p.ID == partnerId).ExecuteCommandAsync();
            await _db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode).ExecuteCommandAsync();
            await _db.Deleteable<ShareDataActivityLog>().Where(l => l.PartnerId == partnerId).ExecuteCommandAsync();
            await _db.Deleteable<ShareDataAlertLog>().Where(a => a.PartnerId == partnerId).ExecuteCommandAsync();

            await _inboundDb.Deleteable<ShareDataInboundPacket>().Where(p => p.PartnerCode == partnerCode && p.PacketCode == packetCode).ExecuteCommandAsync();
        }

        private async Task<(ShareDataPartner Partner, ShareDataPacket Packet, ShareDataSubscription Subscription)>
            SeedPartnerAndPacketAsync(
                string unique,
                string? connectionConfig,
                string packetCode = "101",
                string? targetShapeJson = null,
                string? writeSql = null)
        {
            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"PARTNER_RT_{unique}",
                Name = $"Partner RT {unique}",
                Status = BaseEnums.StatusEnum.Enable,
                ConnectionType = ConnectionType.Api,
                ConnectionConfig = connectionConfig
            };
            await _db.Insertable(partner).ExecuteCommandAsync();

            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"PKT_RT_{packetCode}_{unique}",
                Name = $"Packet RT {packetCode} {unique}",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(packet).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = packet.ID,
                Direction = BaseEnums.Direction.Inbound,
                State = BaseEnums.SubSubscriptionState.Active,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(sub).ExecuteCommandAsync();

            var mapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = packet.ID,
                Direction = BaseEnums.Direction.Inbound,
                IsActive = true,
                TargetShapeJson = targetShapeJson ?? """{"data":[{"id":{"$field":"id"},"val":{"$field":"val"}}]}""",
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(mapping).ExecuteCommandAsync();

            var packetSql = new ShareDataPacketSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packet.Code,
                OrderNo = 1,
                Status = BaseEnums.StatusEnum.Enable,
                WriteSql = writeSql ?? "SELECT 'INSERT' FROM OPENJSON(@records)"
            };
            await _db.Insertable(packetSql).ExecuteCommandAsync();

            return (partner, packet, sub);
        }

        #endregion
    }
}
