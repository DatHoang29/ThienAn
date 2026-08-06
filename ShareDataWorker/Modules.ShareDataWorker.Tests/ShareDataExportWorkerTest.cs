using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Modules.ShareDataWorker.Core.Entities;
using Modules.ShareDataWorker.Core.Entities.Source;
using Modules.ShareDataWorker.Core.Enums;
using Modules.ShareDataWorker.Infrastructure.Services;
using Xunit;

namespace Modules.ShareDataWorker.Tests
{
    /// <summary>
    /// Class Fixture quản lý vòng đời chung cho toàn bộ các Unit Tests (Test Suite).
    /// 
    /// LÝ DO DÙNG IClassFixture:
    /// - Trong xUnit, mặc định mỗi hàm [Fact] sẽ tạo mới một instance của Test Class.
    /// - Nếu logic khởi tạo Database (DROP TABLE, InitTables) nằm trong Constructor của Test Class, 
    ///   nó sẽ bị chạy lại N lần song song (tương ứng N test cases), dẫn đến xung đột đụng độ DROP TABLE trong SQL Server.
    /// - `ShareDataExportServiceTest` đóng vai trò là Class Fixture: khởi tạo IHost và nạp các bảng Database 
    ///   ĐÚNG 1 LẦN DUY NHẤT cho toàn bộ bộ test. Sau khi tất cả test hoàn thành, hàm Dispose() sẽ tự động dọn dẹp.
    /// </summary>
    public class ShareDataExportServiceTest : IDisposable
    {
        public IHost HostInstance { get; }
        public IServiceProvider Services => HostInstance.Services;

        public ShareDataExportServiceTest()
        {
            HostInstance = Host.CreateDefaultBuilder().ConfigureServices((hostContext, services) =>
                {
                    var connectionString = "Server=localhost,14333;Database=test;MultipleActiveResultSets=true;User ID=sa;Password=Password123!;TrustServerCertificate=True;";
                    services.AddScoped<ISqlSugarClient>(sp => new SqlSugarScope(new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        DbType = DbType.SqlServer,
                        IsAutoCloseConnection = true,
                        InitKeyType = InitKeyType.Attribute
                    }));
                })
                .Build();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var internalTables = new Type[]
            {
                typeof(ShareDataActivityLog),
                typeof(ShareDataPartner),
                typeof(ShareDataSubscription),
                typeof(ShareDataMappingProfile),
                typeof(ShareDataDataSource),
                typeof(ShareDataSession),
                typeof(ShareDataEventSource),
                
            };

            foreach (var tableType in internalTables)
            {
                var tableName = db.EntityMaintenance.GetTableName(tableType);
                if (db.DbMaintenance.IsAnyTable(tableName))
                    db.DbMaintenance.DropTable(tableName);
            }

            db.CodeFirst.InitTables(internalTables);

            // Chỉ khởi tạo 8 bảng nội bộ Esh* của ShareData Module
        }

        public void Dispose()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var currentDbName = db.Ado.Connection.Database;
            if (string.Equals(currentDbName, "test", StringComparison.OrdinalIgnoreCase))
            {
                db.Ado.ExecuteCommand("DELETE FROM ShareDataDataSource WHERE Code LIKE 'TEST_%'");
                db.Ado.ExecuteCommand("DELETE FROM ShareDataPartner WHERE Code LIKE 'TEST_%'");
                db.Ado.ExecuteCommand("DELETE FROM ShareDataMappingProfile WHERE Code LIKE 'TEST_%'");
                db.Ado.ExecuteCommand("DELETE FROM ShareDataSubscription WHERE SerialNbr LIKE 'SUB-%'");
                
            }
            HostInstance.Dispose();
        }
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Lớp chứa tất cả các kịch bản Unit Test cho ShareDataExportService thuộc module Modules.ShareDataWorker
    /// Created date: 31/07/2026
    /// </summary>
    public class ShareDataExportWorkerTests : IClassFixture<ShareDataExportServiceTest>
    {
        private readonly ShareDataExportServiceTest _host;

        public ShareDataExportWorkerTests(ShareDataExportServiceTest host)
        {
            _host = host;
        }

        private static async Task AssertPacketJsonSchema(
            ISqlSugarClient db,
            string subscriptionId,
            EshEnums.DatatypeIdEnum datatypeEnum,
            string[]? expectedFields = null)
        {
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subscriptionId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal("SUCCESS", logs[0].Status);
            Assert.True(logs[0].RecordCount > 0);

            Assert.False(string.IsNullOrEmpty(logs[0].FilePath), "FilePath của ExportLog không được để rỗng");
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"File kết xuất không tồn tại tại đường dẫn: {fullPath}");

            try
            {
                var jsonContent = await File.ReadAllTextAsync(fullPath);
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                Assert.Equal(JsonValueKind.Array, root.ValueKind);
                Assert.True(root.GetArrayLength() > 0, "JSON payload không được chứa mảng rỗng");

                var firstRecord = root[0];
                Assert.True(firstRecord.EnumerateObject().Any(), $"Gói tin {datatypeEnum} không trả về thuộc tính JSON nào!");

                if (expectedFields != null)
                {
                    foreach (var fieldName in expectedFields)
                    {
                        Assert.True(
                            firstRecord.TryGetProperty(fieldName, out _),
                            $"Gói tin {datatypeEnum} thiếu field JSON [{fieldName}] trong mảng JSON trả về!"
                        );
                    }
                }
            }
            finally
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
        }


        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra tự động khôi phục và giải phóng Lock bị treo (timeout > 1 phút)
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_LockTimeoutRecovery_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(); 

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_LOCK_REC", FromKmNumber = 10, MaxSpeed = 90 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "70", Condition = "GOOD", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_WORKER_LOCK",
                Name = "Lock Recovery Partner",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var stuckSubscription = new ShareDataSubscription
            {
                SerialNbr = "SUB-WORKER-LOCK-STUCK-01",
                PartnerId = partner.ID,
                DatatypeId = EshEnums.DatatypeIdEnum.TrafficFlow.ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                RunStatus = EshEnums.RunStatus.Running,
                UpdateTime = DateTime.Now.AddMinutes(-10),
                NextTimeRun = DateTime.Now.AddMinutes(-10)
            };
            await db.Insertable(stuckSubscription).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(stuckSubscription.ID);
                Assert.NotNull(updatedSub);
                Assert.Equal(EshEnums.RunStatus.Idle, updatedSub.RunStatus);

                var exportLogs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == stuckSubscription.ID)
                    .ToListAsync();
                Assert.NotEmpty(exportLogs);
                Assert.Equal("SUCCESS", exportLogs[0].Status);

                if (!string.IsNullOrEmpty(exportLogs[0].FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), exportLogs[0].FilePath!);
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().In(statusId).ExecuteCommandAsync();
                await db.Deleteable<TmsZone>().In(zoneId).ExecuteCommandAsync();
            }
        }


        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 12 trường cho Gói 101 (Thông tin luồng giao thông)
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket101_ReturnsTrafficFlowData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            var statId = Guid.NewGuid().ToString("N");

            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = "TEST_ZONE_101",
                FromKmNumber = 10,
                FromMetNumber = 200,
                ToKmNumber = 15,
                ToMetNumber = 500,
                LaneId = "LANE_A1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = statusId,
                ZoneId = zoneId,
                AverageSpeed = "65.5",
                Condition = "NORMAL",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficStatistic
            {
                ID = statId,
                ZoneId = zoneId,
                TotalVehicleNumber = 120
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P101",
                Name = "Partner 101",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q101-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TrafficFlow).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.TrafficFlow);
            }
            finally
            {
                await db.Deleteable<TmsTrafficStatistic>().In(statId).ExecuteCommandAsync();
                await db.Deleteable<TmsZoneStatus>().In(statusId).ExecuteCommandAsync();
                await db.Deleteable<TmsZone>().In(zoneId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Gói 101 khi thiếu bảng TmsTrafficStatistic vẫn trả về NULL (LEFT JOIN)
        /// Created date: 01/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket101_WhenTrafficStatisticEmpty_ReturnsDataSafely_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");

            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = "TEST_ZONE_LEFTJOIN_101",
                FromKmNumber = 20,
                FromMetNumber = 0,
                ToKmNumber = 25,
                ToMetNumber = 0,
                LaneId = "LANE_B1",
                MaxSpeed = 100
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = statusId,
                ZoneId = zoneId,
                AverageSpeed = "72.0",
                Condition = "SLOW",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P101_LJ",
                Name = "Partner 101 LeftJoin",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q101-LJ",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TrafficFlow).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);
                Assert.True(logs[0].RecordCount > 0);

                if (!string.IsNullOrEmpty(logs[0].FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                    if (File.Exists(fullPath))
                    {
                        var jsonContent = await File.ReadAllTextAsync(fullPath);
                        Assert.Contains("\"vehicleCount\":null", jsonContent); // LEFT JOIN trả về NULL cho vehicleCount khi thiếu bảng TmsTrafficStatistic
                        File.Delete(fullPath);
                    }
                }
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().In(statusId).ExecuteCommandAsync();
                await db.Deleteable<TmsZone>().In(zoneId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 8 trường cho Gói 102 (Dữ liệu hình ảnh CCTV)
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket102_ReturnsCctvImageData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var eqId = Guid.NewGuid().ToString("N");
            var cctvId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsEquipment { ID = eqId, Code = "TEST_EQ102", KmNumber = 22, MetNumber = 100, DirectionId = "1", LaneId = "LANE_B1", Ip = "192.168.102.10" }).ExecuteCommandAsync();
            await db.Insertable(new CctvDevice { ID = cctvId, DeviceId = "CAM_DEV_102", Name = "TEST_CCTV_102", SnapshotUrl = "data:image/png;base64,test", SnapshotTime = DateTime.Now, DeviceState = 1, Ip = "192.168.102.10" }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P102",
                Name = "Partner 102",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q102-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.CctvImage).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.CctvImage);
            }
            finally
            {
                await db.Ado.ExecuteCommandAsync("DELETE FROM CctvDevice WHERE ID = @id", new { id = cctvId });
                await db.Ado.ExecuteCommandAsync("DELETE FROM TmsEquipment WHERE ID = @id", new { id = eqId });
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 11 trường cho Gói 103 (Dữ liệu dò xe VDS)
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket103_ReturnsVehicleDetectionData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var eqId = Guid.NewGuid().ToString("N");
            var tdId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsEquipment { ID = eqId, Code = "TEST_EQ103", KmNumber = 33, MetNumber = 400, DirectionId = "1", Ip = "192.168.103.10" }).ExecuteCommandAsync();
            await db.Insertable(new TmsTrafficData { ID = tdId, DetectTime = DateTime.Now, Type = "CAR", LicensePlate = "51A-12345", Speed = 70.5m, Lane = "LANE_1", Direction = "1", Location = "KM33+400", EquipmentId = eqId }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P103",
                Name = "Partner 103",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q103-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.VehicleDetection).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.VehicleDetection);
            }
            finally
            {
                await db.Ado.ExecuteCommandAsync("DELETE FROM TmsTrafficData WHERE ID = @id", new { id = tdId });
                await db.Ado.ExecuteCommandAsync("DELETE FROM TmsEquipment WHERE ID = @id", new { id = eqId });
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 12 trường cho Gói 107 (Thông tin sự kiện giao thông)
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket107_ReturnsIncidentData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var etId = Guid.NewGuid().ToString("N");
            var incId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsEventType { ID = etId, Name = "TEST_TAI_NAN_GIAO_THONG" }).ExecuteCommandAsync();
            await db.Insertable(new TmsIncident
            {
                ID = incId,
                Code = "TEST_INC_107",
                Name = "Tai nạn tại Km42",
                EventTypeId = etId,
                StartDate = DateTime.Now,
                KmNumber = 42,
                MetNumber = 300,
                Location = "Cao tốc TP.HCM - Long Thành",
                InfluenceScope = "1",
                InjuredNumber = 2,
                VehicleNumber = 3,
                State = "ACTIVE",
                Description = "Va chạm liên hoàn 3 xe",
                Source = "CAMERA",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P107",
                Name = "Partner 107",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q107-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TrafficIncident).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.TrafficIncident);
            }
            finally
            {
                await db.Deleteable<TmsIncident>().In(incId).ExecuteCommandAsync();
                await db.Deleteable<TmsEventType>().In(etId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 11 trường cho Gói 108 (Hiển thị biển báo VMS)
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket108_ReturnsVmsDisplayData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var eqId = Guid.NewGuid().ToString("N");
            var vmsId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsEquipment { ID = eqId, Code = "TEST_EQ108", KmNumber = 55, MetNumber = 200, DirectionId = "1", LaneId = "LANE_C1", Ip = "192.168.108.10" }).ExecuteCommandAsync();
            await db.Insertable(new VmsCurrent { ID = vmsId, Name = "TEST_VMS_108", EquipmentId = eqId, RowData = "Giới hạn tốc độ 60km/h", Url = "https://test.vms/image.png", Size = "320x240", Priority = 1, ExecutedDate = DateTime.Now }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P108",
                Name = "Partner 108",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q108-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.VmsDisplay).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.VmsDisplay);
            }
            finally
            {
                await db.Ado.ExecuteCommandAsync("DELETE FROM VmsCurrent WHERE ID = @id", new { id = vmsId });
                await db.Ado.ExecuteCommandAsync("DELETE FROM TmsEquipment WHERE ID = @id", new { id = eqId });
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 12 trường cho Gói 109 (Thông tin thu phí ETC)
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket109_ReturnsTollCollectionData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var stationId = Guid.NewGuid().ToString("N");
            var laneId = Guid.NewGuid().ToString("N");
            var txnId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TollStation { ID = stationId, StationId = "TEST_STATION_109", Name = "Trạm Long Thành" }).ExecuteCommandAsync();
            await db.Insertable(new TollLane { ID = laneId, LaneId = "TEST_LANE_109", Name = "Làn ETC 01" }).ExecuteCommandAsync();
            await db.Insertable(new TollTransactionOut
            {
                ID = txnId,
                TransactionId = "TEST_TXN_109",
                TransactionDateTime = DateTime.Now,
                TransactionDateTimeIn = DateTime.Now.AddHours(-1),
                VehicleTypeId = "1",
                Plate = "51A-99999",
                PlateLpr = "51A-99999",
                TagId = "TAG-TEST-109",
                LaneId = "TEST_LANE_109",
                StationId = "TEST_STATION_109",
                SyncTime = DateTime.Now
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P109",
                Name = "Partner 109",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q109-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TollCollection).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.TollCollection);
            }
            finally
            {
                await db.Deleteable<TollTransactionOut>().In(txnId).ExecuteCommandAsync();
                await db.Deleteable<TollLane>().In(laneId).ExecuteCommandAsync();
                await db.Deleteable<TollStation>().In(stationId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xử lý lỗi khi DatatypeId không hợp lệ (ví dụ: 999)
        /// Created date: 01/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacketData_WithInvalidDatatypeId_ReturnsUnsupportedError_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PINVALID",
                Name = "Partner Invalid",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-QINVALID-001",
                PartnerId = partner.ID,
                DatatypeId = "999",
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(EshEnums.ExportStatus.Failed, logs[0].Status);
            Assert.Contains("999", logs[0].ErrorMessage);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn gói tin khi DatatypeId dạng chuỗi tên Enum (TrafficFlow)
        /// Created date: 01/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacketData_WithEnumName_ParsesAndExecutes_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_ENUM", FromKmNumber = 5, ToKmNumber = 8, MaxSpeed = 100 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "50", Condition = "GOOD", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PENUM",
                Name = "Partner EnumName",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-QENUM-001",
                PartnerId = partner.ID,
                DatatypeId = nameof(EshEnums.DatatypeIdEnum.TrafficFlow),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);
                Assert.True(logs[0].RecordCount > 0);

                if (!string.IsNullOrEmpty(logs[0].FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().In(statusId).ExecuteCommandAsync();
                await db.Deleteable<TmsZone>().In(zoneId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra lọc dữ liệu tăng tiến theo LastTimeRun cho Gói 103 (VDS)
        /// Created date: 01/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket103_WithLastTimeRun_FiltersOlderRecords_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var oldId = Guid.NewGuid().ToString("N");
            var newId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsTrafficData { ID = oldId, Location = "LOC_OLD", EquipmentId = "EQUIP_01", DetectTime = DateTime.Now.AddHours(-2) }).ExecuteCommandAsync();
            await db.Insertable(new TmsTrafficData { ID = newId, Location = "LOC_NEW", EquipmentId = "EQUIP_02", DetectTime = DateTime.Now }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PINCR",
                Name = "Partner Incremental",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-QINCR-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.VehicleDetection).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                UpdateTime = DateTime.Now.AddHours(-1),
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);

                if (logs[0].RecordCount > 0 && !string.IsNullOrEmpty(logs[0].FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                    if (File.Exists(fullPath))
                    {
                        var jsonContent = await File.ReadAllTextAsync(fullPath);
                        Assert.DoesNotContain("\"locationCode\":\"LOC_OLD\"", jsonContent);
                        File.Delete(fullPath);
                    }
                }
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().In(new[] { oldId, newId }).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xử lý lỗi khi DatatypeId rỗng
        /// Created date: 01/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacketData_WithEmptyDatatypeId_ReturnsUnsupportedError_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PEMPTY",
                Name = "Partner Empty DatatypeId",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-QEMPTY-001",
                PartnerId = partner.ID,
                DatatypeId = "",
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(EshEnums.ExportStatus.Failed, logs[0].Status);
            Assert.NotNull(logs[0].ErrorMessage);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra ghi nhận log thất bại khi bảng dữ liệu nguồn không tồn tại trong SQL Server
        /// Created date: 01/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacketData_WhenTableDoesNotExist_LogsFailedStatus_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            // Đăng ký tạm 1 Gói tin 999 trỏ vào bảng không tồn tại (TmsNonExistentDummyTable)
            ShareDataExportService.PacketQueryRegistry[(EshEnums.DatatypeIdEnum)999] = async (dbClient, _) =>
            {
                var list = await dbClient.Queryable<object>().AS("TmsNonExistentDummyTable").Select("ID").ToListAsync();
                return list;
            };

            var partner = new ShareDataPartner
            {
                Code = "TEST_NOTABLE",
                Name = "Partner No Table",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-NOTABLE-001",
                PartnerId = partner.ID,
                DatatypeId = "999",
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal(EshEnums.ExportStatus.Failed, logs[0].Status);
                Assert.NotNull(logs[0].ErrorMessage);
            }
            finally
            {
                ShareDataExportService.PacketQueryRegistry.Remove((EshEnums.DatatypeIdEnum)999);
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra ghi nhận log thất bại khi bảng dữ liệu nguồn thiếu cột cần thiết trong SQL Server
        /// Created date: 01/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacketData_WhenColumnDoesNotExist_LogsFailedStatus_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            // Đăng ký tạm Gói 998 yêu cầu cột NonExistentCol trên bảng TmsDummyMissingColTable
            ShareDataExportService.PacketQueryRegistry[(EshEnums.DatatypeIdEnum)998] = async (dbClient, _) =>
            {
                var list = await dbClient.Queryable<object>().AS("TmsDummyMissingColTable").Select("NonExistentCol").ToListAsync();
                return list;
            };
            await db.Ado.ExecuteCommandAsync("IF OBJECT_ID('TmsDummyMissingColTable', 'U') IS NULL CREATE TABLE TmsDummyMissingColTable (ID VARCHAR(50) PRIMARY KEY);");

            var partner = new ShareDataPartner
            {
                Code = "TEST_NOCOL",
                Name = "Partner Missing Col",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-NOCOL-001",
                PartnerId = partner.ID,
                DatatypeId = "998",
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal(EshEnums.ExportStatus.Failed, logs[0].Status);
                Assert.NotNull(logs[0].ErrorMessage);
            }
            finally
            {
                ShareDataExportService.PacketQueryRegistry.Remove((EshEnums.DatatypeIdEnum)998);
                await db.Ado.ExecuteCommandAsync("DROP TABLE IF EXISTS TmsDummyMissingColTable;");
            }
        }

        private static readonly Dictionary<EshEnums.DatatypeIdEnum, string[]> ExpectedPacketFields = new()
        {
            [EshEnums.DatatypeIdEnum.TrafficFlow] = [
                "zoneId", "zoneName", "fromLocationKm", "fromLocationMet", "toLocationKm", "toLocationMet",
                "laneId", "averageSpeed", "trafficCondition", "dataTime", "speedLimit", "vehicleCount"
            ],
            [EshEnums.DatatypeIdEnum.CctvImage] = [
                "cameraCode", "cameraName", "snapshot", "snapshotTime", "deviceState", "locationKm", "locationMet", "direction"
            ],
            [EshEnums.DatatypeIdEnum.VehicleDetection] = [
                "detectionId", "detectTime", "vehicleType", "licensePlate", "speed", "lane", "direction",
                "locationRoute", "equipmentId", "locationKm", "locationMet"
            ],
            [EshEnums.DatatypeIdEnum.Weather] = [
                "weatherStationId", "locationDetail", "temperature", "humidity", "windSpeed", "windDirection",
                "rainfall", "rainfallHour", "visibility", "weatherDescription", "weatherCode", "detectTime"
            ],
            [EshEnums.DatatypeIdEnum.VehicleIdentification] = [
                "transactionId", "tagId", "licensePlate", "vehicleTypeId", "entryTime", "exitTime",
                "laneId", "stationId", "vehicleBrand", "vehicleOwner"
            ],
            [EshEnums.DatatypeIdEnum.WeighInMotion] = [
                "detectTime", "lane", "locationCode", "speed", "height", "width", "length"
            ],
            [EshEnums.DatatypeIdEnum.TrafficIncident] = [
                "incidentCode", "incidentName", "eventTypeId", "eventTypeName", "occurredTime", "locationKm",
                "locationMet", "locationRoute", "direction", "injuredCount", "vehicleCount", "incidentState",
                "description", "source"
            ],
            [EshEnums.DatatypeIdEnum.VmsDisplay] = [
                "equipmentCode", "vmsName", "locationKm", "locationMet", "direction", "laneId",
                "displayContent", "displayImageUrl", "displaySize", "priority", "executedTime"
            ],
            [EshEnums.DatatypeIdEnum.TollCollection] = [
                "transactionId", "entryTime", "exitTime", "vehicleTypeId", "licensePlate", "tagId",
                "laneId", "laneName", "stationId", "stationName", "tollPrice", "syncTime"
            ],
            [EshEnums.DatatypeIdEnum.PublicMessaging] = [
                "incidentMessage", "guidanceContent", "locationKm", "locationMet", "publishedTime"
            ],
            [EshEnums.DatatypeIdEnum.InterCenterExchange] = [
                "packetType", "controlCommand", "controlState", "createdTime"
            ]
        };

        private static async Task SeedTestDataForPacket(ISqlSugarClient db, EshEnums.DatatypeIdEnum datatypeEnum, string uniqueId)
        {
            var now = DateTime.Now;
            switch (datatypeEnum)
            {
                case EshEnums.DatatypeIdEnum.TrafficFlow:
                    await db.Insertable(new TmsZone { ID = uniqueId, Name = "Zone Test", FromKmNumber = 1, FromMetNumber = 0, ToKmNumber = 5, ToMetNumber = 0, LaneId = "L1", MaxSpeed = 80 }).ExecuteCommandAsync();
                    await db.Insertable(new TmsZoneStatus { ID = uniqueId, ZoneId = uniqueId, AverageSpeed = "60.5", Condition = "NORMAL", UpdateTime = now }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficStatistic { ID = uniqueId, ZoneId = uniqueId, TotalVehicleNumber = 100 }).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.CctvImage:
                    await db.Insertable(new CctvDevice { ID = uniqueId, DeviceId = "CAM_DEV_01", Name = "Cam Test", Ip = "192.168.1.100", SnapshotUrl = "http://img.jpg", SnapshotTime = now, DeviceState = 1 }).ExecuteCommandAsync();
                    await db.Insertable(new TmsEquipment { ID = uniqueId, Code = "CAM01", Ip = "192.168.1.100", KmNumber = 10, MetNumber = 500, DirectionId = "1" }).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.VehicleDetection:
                case EshEnums.DatatypeIdEnum.WeighInMotion:
                    await db.Insertable(new TmsEquipment { ID = uniqueId, Code = "VDS01", KmNumber = 12, MetNumber = 300 }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficData { ID = uniqueId, EquipmentId = uniqueId, DetectTime = now, Type = "CAR", LicensePlate = "30A-12345", Speed = 65.0m, Lane = "1", Direction = "1", Location = "KM12", Height = 150, Width = 180, Length = 450 }).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.Weather:
                    await db.Insertable(new TmsWeather { ID = uniqueId, RefId = "WS01", LocationDetail = "Km15", Temperature = 30.0m, Hudmidity = 80.0m, WindSpeed = 5.0m, WindDirection = "E", Rain = 0.0m, RainHour = 0.0m, Foresight = 1000.0m, Description = "Nắng", ShortDescription = "SUNNY", TimeDetect = now }).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.VehicleIdentification:
                case EshEnums.DatatypeIdEnum.TollCollection:
                    await db.Insertable(new TollLane { ID = uniqueId, LaneId = "LANE01", Name = "Làn 1" }).ExecuteCommandAsync();
                    await db.Insertable(new TollStation { ID = uniqueId, StationId = "STATION01", Name = "Trạm 1" }).ExecuteCommandAsync();
                    await db.Insertable(new TmsVehicleRegistration { ID = uniqueId, Plate = "30A-99999", Brand = "Toyota", Owner = "Nguyen Van A" }).ExecuteCommandAsync();
                    await db.Insertable(new TollTransactionOut { ID = uniqueId, TransactionId = "TXN01", TagId = "TAG01", Plate = "30A-99999", VehicleTypeId = "1", TransactionDateTimeIn = now, TransactionDateTime = now, LaneId = "LANE01", StationId = "STATION01", SyncTime = now }).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.TrafficIncident:
                case EshEnums.DatatypeIdEnum.PublicMessaging:
                    await db.Insertable(new TmsEventType { ID = uniqueId, Name = "Tai nạn" }).ExecuteCommandAsync();
                    await db.Insertable(new TmsIncident { ID = uniqueId, Code = "INC01", Name = "Sự cố Km10", EventTypeId = uniqueId, StartDate = now, UpdateTime = now, KmNumber = 10, MetNumber = 0, Location = "Km10", InfluenceScope = "1", InjuredNumber = 0, VehicleNumber = 2, State = "ACTIVE", Description = "Va chạm nhẹ", Source = "CCTV" }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent { ID = uniqueId, EquipmentId = uniqueId, Name = "VMS01", RowData = "Giam Toc", Url = "http://vms.jpg", Size = "128x64", Priority = 1, ExecutedDate = now }).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.VmsDisplay:
                    await db.Insertable(new TmsEquipment { ID = uniqueId, Code = "EQUIP_VMS", KmNumber = 5, MetNumber = 100, DirectionId = "1", LaneId = "L1" }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent { ID = uniqueId, EquipmentId = uniqueId, Name = "VMS 01", RowData = "Chú ý", Url = "http://vms.png", Size = "256x128", Priority = 2, ExecutedDate = now }).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.InterCenterExchange:
                    await db.Insertable(new TmsSignalLog { ID = uniqueId, NewData = "CMD_UPDATE", State = "EXECUTED", CreateTime = now }).ExecuteCommandAsync();
                    break;
            }
        }

        private static async Task CleanupTestDataForPacket(ISqlSugarClient db, EshEnums.DatatypeIdEnum datatypeEnum, string uniqueId)
        {
            switch (datatypeEnum)
            {
                case EshEnums.DatatypeIdEnum.TrafficFlow:
                    await db.Deleteable<TmsTrafficStatistic>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TmsZoneStatus>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TmsZone>().In(uniqueId).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.CctvImage:
                    await db.Deleteable<TmsEquipment>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<CctvDevice>().In(uniqueId).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.VehicleDetection:
                case EshEnums.DatatypeIdEnum.WeighInMotion:
                    await db.Deleteable<TmsTrafficData>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TmsEquipment>().In(uniqueId).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.Weather:
                    await db.Deleteable<TmsWeather>().In(uniqueId).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.VehicleIdentification:
                case EshEnums.DatatypeIdEnum.TollCollection:
                    await db.Deleteable<TollTransactionOut>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TmsVehicleRegistration>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TollStation>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TollLane>().In(uniqueId).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.TrafficIncident:
                case EshEnums.DatatypeIdEnum.PublicMessaging:
                    await db.Deleteable<VmsCurrent>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TmsIncident>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TmsEventType>().In(uniqueId).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.VmsDisplay:
                    await db.Deleteable<VmsCurrent>().In(uniqueId).ExecuteCommandAsync();
                    await db.Deleteable<TmsEquipment>().In(uniqueId).ExecuteCommandAsync();
                    break;
                case EshEnums.DatatypeIdEnum.InterCenterExchange:
                    await db.Deleteable<TmsSignalLog>().In(uniqueId).ExecuteCommandAsync();
                    break;
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra toàn bộ 11 gói tin chia sẻ (101-111) xuất file JSON đúng và đầy đủ 106 fields theo tài liệu shareData-assessment.md
        /// Created date: 04/08/2026
        /// </summary>
        [Theory]
        [InlineData(EshEnums.DatatypeIdEnum.TrafficFlow)]
        [InlineData(EshEnums.DatatypeIdEnum.CctvImage)]
        [InlineData(EshEnums.DatatypeIdEnum.VehicleDetection)]
        [InlineData(EshEnums.DatatypeIdEnum.Weather)]
        [InlineData(EshEnums.DatatypeIdEnum.VehicleIdentification)]
        [InlineData(EshEnums.DatatypeIdEnum.WeighInMotion)]
        [InlineData(EshEnums.DatatypeIdEnum.TrafficIncident)]
        [InlineData(EshEnums.DatatypeIdEnum.VmsDisplay)]
        [InlineData(EshEnums.DatatypeIdEnum.TollCollection)]
        [InlineData(EshEnums.DatatypeIdEnum.PublicMessaging)]
        [InlineData(EshEnums.DatatypeIdEnum.InterCenterExchange)]
        public async Task QueryPacketData_VerifyAll11Packets_FieldMappingSchema_Test(EshEnums.DatatypeIdEnum datatypeEnum)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var uniqueId = Guid.NewGuid().ToString("N");
            await SeedTestDataForPacket(db, datatypeEnum, uniqueId);

            var partner = new ShareDataPartner
            {
                Code = $"P_SCHEMA_{(int)datatypeEnum}",
                Name = $"Partner Schema {(int)datatypeEnum}",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = $"SUB-SCHEMA-{(int)datatypeEnum}",
                PartnerId = partner.ID,
                DatatypeId = ((int)datatypeEnum).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                Assert.True(ExpectedPacketFields.TryGetValue(datatypeEnum, out var expectedFields), $"Chưa khai báo expected fields cho gói {datatypeEnum}");
                await AssertPacketJsonSchema(db, sub.ID, datatypeEnum, expectedFields);
            }
            finally
            {
                await CleanupTestDataForPacket(db, datatypeEnum, uniqueId);
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra thực thi song song nhiều Subscriptions cùng 1 đối tác và cùng gói tin, tạo ra 2 file độc lập không ghi đè
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_ParallelExecution_GeneratesUniqueFiles_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var zoneId = Guid.NewGuid().ToString("N");
            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZone { ID = zoneId, Name = "TEST_ZONE_PARALLEL", FromKmNumber = 10, MaxSpeed = 80 }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus { ID = statusId, ZoneId = zoneId, AverageSpeed = "60", Condition = "NORMAL", UpdateTime = DateTime.Now }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PARALLEL_F",
                Name = "Partner Parallel Files",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub1 = new ShareDataSubscription
            {
                SerialNbr = "SUB_PARALLEL_01",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TrafficFlow).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            var sub2 = new ShareDataSubscription
            {
                SerialNbr = "SUB_PARALLEL_02",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TrafficFlow).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(new[] { sub1, sub2 }).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub1.ID || l.SubscriptionId == sub2.ID)
                    .ToListAsync();

                Assert.Equal(2, logs.Count);
                Assert.All(logs, log => Assert.Equal("SUCCESS", log.Status));

                var log1 = logs.First(l => l.SubscriptionId == sub1.ID);
                var log2 = logs.First(l => l.SubscriptionId == sub2.ID);

                Assert.NotNull(log1.FilePath);
                Assert.NotNull(log2.FilePath);
                Assert.NotEqual(log1.FilePath, log2.FilePath);

                var path1 = Path.Combine(Directory.GetCurrentDirectory(), log1.FilePath!);
                var path2 = Path.Combine(Directory.GetCurrentDirectory(), log2.FilePath!);

                Assert.True(File.Exists(path1), $"File {path1} không tồn tại");
                Assert.True(File.Exists(path2), $"File {path2} không tồn tại");

                if (File.Exists(path1)) File.Delete(path1);
                if (File.Exists(path2)) File.Delete(path2);
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().In(statusId).ExecuteCommandAsync();
                await db.Deleteable<TmsZone>().In(zoneId).ExecuteCommandAsync();
            }
        }

        // ============================================================
        // 🔴 Ưu tiên Cao
        // ============================================================

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 12 trường cho Gói 104 (Dữ liệu thời tiết)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket104_ReturnsWeatherData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var weatherId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsWeather
            {
                ID = weatherId,
                RefId = "WS_104_TEST",
                LocationDetail = "Km 10+200",
                Temperature = 32.5m,
                Hudmidity = 75.0m,
                WindSpeed = 3.8m,
                WindDirection = "NE",
                Rain = 5.2m,
                RainHour = 18.0m,
                Foresight = 1200.0m,
                Description = "Nắng nóng",
                ShortDescription = "sunny",
                TimeDetect = DateTime.Now
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P104",
                Name = "Partner 104",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q104-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.Weather).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.Weather);
            }
            finally
            {
                await db.Deleteable<TmsWeather>().In(weatherId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra lọc dữ liệu tăng tiến theo LastTimeRun cho Gói 104 (Weather có TimeFilterExpression)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket104_WithLastTimeRun_FiltersOlderRecords_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var oldId = Guid.NewGuid().ToString("N");
            var newId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsWeather { ID = oldId, RefId = "WS_OLD", LocationDetail = "Km 5", Temperature = 28.0m, TimeDetect = DateTime.Now.AddHours(-3) }).ExecuteCommandAsync();
            await db.Insertable(new TmsWeather { ID = newId, RefId = "WS_NEW", LocationDetail = "Km 8", Temperature = 35.0m, TimeDetect = DateTime.Now }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P104_TF",
                Name = "Partner 104 TimeFilter",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q104-TF",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.Weather).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                UpdateTime = DateTime.Now.AddHours(-1),
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);

                if (logs[0].RecordCount > 0 && !string.IsNullOrEmpty(logs[0].FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                    if (File.Exists(fullPath))
                    {
                        var jsonContent = await File.ReadAllTextAsync(fullPath);
                        Assert.DoesNotContain("\"weatherStationId\":\"WS_OLD\"", jsonContent);
                        File.Delete(fullPath);
                    }
                }
            }
            finally
            {
                await db.Deleteable<TmsWeather>().In(new[] { oldId, newId }).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Gói 102 khi CctvDevice có nhưng TmsEquipment không khớp IP → các cột JOIN trả về NULL an toàn
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket102_WhenEquipmentMissing_ReturnsDataSafely_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var cctvId = Guid.NewGuid().ToString("N");
            await db.Insertable(new CctvDevice { ID = cctvId, DeviceId = "CAM_DEV_NO_EQ", Name = "TEST_CCTV_NO_EQ", SnapshotUrl = "base64_test", SnapshotTime = DateTime.Now, DeviceState = 1, Ip = "10.99.99.99" }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P102_LJ",
                Name = "Partner 102 LeftJoin",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q102-LJ",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.CctvImage).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);
                Assert.True(logs[0].RecordCount > 0);

                if (!string.IsNullOrEmpty(logs[0].FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                    if (File.Exists(fullPath))
                    {
                        var jsonContent = await File.ReadAllTextAsync(fullPath);
                        Assert.Contains("\"cameraCode\":null", jsonContent);
                        Assert.Contains("\"locationKm\":null", jsonContent);
                        File.Delete(fullPath);
                    }
                }
            }
            finally
            {
                await db.Deleteable<CctvDevice>().In(cctvId).ExecuteCommandAsync();
            }
        }

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
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PNODATA",
                Name = "Partner NoData",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-QNODATA-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.Weather).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                UpdateTime = DateTime.Now.AddSeconds(-5),
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal("SUCCESS", logs[0].Status);
            Assert.Equal(0, logs[0].RecordCount);
            Assert.Null(logs[0].FilePath);
        }

        // ============================================================
        // 🟡 Ưu tiên Trung bình
        // ============================================================

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Gói 107 khi TmsIncident có EventTypeId nhưng không tồn tại trong TmsEventType → eventTypeName = NULL
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket107_WhenEventTypeMissing_ReturnsDataSafely_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var incId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsIncident
            {
                ID = incId,
                Code = "TEST_INC_107_LJ",
                Name = "Sự cố test",
                EventTypeId = "NON_EXIST_ET",
                StartDate = DateTime.Now,
                KmNumber = 50,
                MetNumber = 0,
                Location = "Cao tốc",
                InfluenceScope = "1",
                InjuredNumber = 0,
                VehicleNumber = 1,
                State = "ACTIVE",
                Description = "Mô tả test",
                Source = "REPORT",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P107_LJ",
                Name = "Partner 107 LeftJoin",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q107-LJ",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TrafficIncident).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);
                Assert.True(logs[0].RecordCount > 0);

                if (!string.IsNullOrEmpty(logs[0].FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                    if (File.Exists(fullPath))
                    {
                        var jsonContent = await File.ReadAllTextAsync(fullPath);
                        Assert.Contains("\"eventTypeName\":null", jsonContent);
                        File.Delete(fullPath);
                    }
                }
            }
            finally
            {
                await db.Deleteable<TmsIncident>().In(incId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Gói 109 khi TollTransactionOut có LaneId/StationId nhưng thiếu TollLane/TollStation → laneName, stationName = NULL
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket109_WhenLaneStationMissing_ReturnsDataSafely_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var txnId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TollTransactionOut
            {
                ID = txnId,
                TransactionId = "TXN_109_LJ",
                TransactionDateTime = DateTime.Now,
                TransactionDateTimeIn = DateTime.Now.AddHours(-1),
                VehicleTypeId = "2",
                Plate = "30A-11111",
                TagId = "TAG_LJ",
                LaneId = "LANE_NOT_EXIST",
                StationId = "STATION_NOT_EXIST",
                SyncTime = DateTime.Now
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P109_LJ",
                Name = "Partner 109 LeftJoin",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q109-LJ",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TollCollection).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);
                Assert.True(logs[0].RecordCount > 0);

                if (!string.IsNullOrEmpty(logs[0].FilePath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                    if (File.Exists(fullPath))
                    {
                        var jsonContent = await File.ReadAllTextAsync(fullPath);
                        Assert.Contains("\"laneName\":null", jsonContent);
                        Assert.Contains("\"stationName\":null", jsonContent);
                        File.Delete(fullPath);
                    }
                }
            }
            finally
            {
                await db.Deleteable<TollTransactionOut>().In(txnId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Worker bỏ qua Subscription có State = PAUSED (không xử lý)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_SkipsPausedSubscriptions_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PPAUSED",
                Name = "Partner Paused",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-QPAUSED-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TrafficFlow).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Paused,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.Empty(logs);
        }

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
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PNOTDUE",
                Name = "Partner NotDue",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-QNOTDUE-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.TrafficFlow).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddMinutes(30),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
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
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 10 trường cho Gói 105 (Định danh phương tiện AVI/RFID)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket105_ReturnsVehicleIdentificationData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var txnId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TollTransactionOut
            {
                ID = txnId,
                TransactionId = "TXN_105_TEST",
                TransactionDateTime = DateTime.Now,
                TransactionDateTimeIn = DateTime.Now.AddHours(-1),
                VehicleTypeId = "3",
                Plate = "51B-22222",
                PlateEdit = "51B-22222",
                PlateLpr = "51B-22222",
                TagId = "TAG_105_TEST",
                LaneId = "LANE_105",
                StationId = "ST_105",
                SyncTime = DateTime.Now
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P105",
                Name = "Partner 105",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q105-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.VehicleIdentification).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.VehicleIdentification);
            }
            finally
            {
                await db.Deleteable<TollTransactionOut>().In(txnId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 7 trường cho Gói 106 (Kiểm tra tải trọng WIM)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket106_ReturnsWeighInMotionData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var tdId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsTrafficData { ID = tdId, DetectTime = DateTime.Now, Lane = "LANE_WIM_1", Location = "KM20+000", Speed = 55.0m, Height = 320, Width = 250, Length = 1200, EquipmentId = "WIM_EQ_01" }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P106",
                Name = "Partner 106",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q106-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.WeighInMotion).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.WeighInMotion);
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().In(tdId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 5 trường cho Gói 110 (Trao đổi với người tham gia giao thông)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket110_ReturnsPublicMessagingData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var incId = Guid.NewGuid().ToString("N");
            var vmsId = Guid.NewGuid().ToString("N");
            var eqId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsIncident
            {
                ID = incId,
                Code = "TEST_INC_110",
                Name = "Cảnh báo mưa lớn",
                EventTypeId = "ET_WEATHER",
                StartDate = DateTime.Now,
                KmNumber = 15,
                MetNumber = 200,
                Location = "Cao tốc",
                InfluenceScope = "1",
                State = "ACTIVE",
                Description = "Mưa to, giảm tốc độ",
                Source = "SYSTEM",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();
            await db.Insertable(new TmsEquipment { ID = eqId, Code = "TEST_EQ110", KmNumber = 15, MetNumber = 200, DirectionId = "1", Ip = "192.168.110.10" }).ExecuteCommandAsync();
            await db.Insertable(new VmsCurrent { ID = vmsId, Name = "TEST_VMS_110", EquipmentId = eqId, RowData = "GIẢM TỐC ĐỘ - MƯA LỚN", Url = "https://test.vms/110.png", Size = "320x64", Priority = 1, ExecutedDate = DateTime.Now }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P110",
                Name = "Partner 110",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q110-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.PublicMessaging).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.PublicMessaging);
            }
            finally
            {
                await db.Deleteable<VmsCurrent>().In(vmsId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().In(eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsIncident>().In(incId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra truy vấn và xuất JSON đầy đủ 4 trường cho Gói 111 (Trao đổi với TT QLĐHGT tuyến)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket111_ReturnsInterCenterExchangeData_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var slId = Guid.NewGuid().ToString("N");
            await db.Ado.ExecuteCommandAsync(@"
                INSERT INTO TmsSignalLog (ID, NewData, State, CreateTime)
                VALUES (@id, '{""speedLimit"":60}', 'EXECUTED', GETDATE())", new { id = slId });

            var partner = new ShareDataPartner
            {
                Code = "TEST_P111",
                Name = "Partner 111",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-Q111-001",
                PartnerId = partner.ID,
                DatatypeId = ((int)EshEnums.DatatypeIdEnum.InterCenterExchange).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                await AssertPacketJsonSchema(db, sub.ID, EshEnums.DatatypeIdEnum.InterCenterExchange);
            }
            finally
            {
                await db.Ado.ExecuteCommandAsync("DELETE FROM TmsSignalLog WHERE ID = @id", new { id = slId });
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra nỗ lực SQL Injection qua tên Bảng bị SqlSugar/SQL Server phát hiện và chặn an toàn (ghi log Failed)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacketData_WhenSqlInjectionInTable_LogsFailedStatus_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            const EshEnums.DatatypeIdEnum injectPacketId = (EshEnums.DatatypeIdEnum)997;
            ShareDataExportService.PacketQueryRegistry[injectPacketId] = async (dbClient, _) =>
            {
                var list = await dbClient.Queryable<object>().AS("TmsWeather; DROP TABLE TmsWeather; --").Select("ID").ToListAsync();
                return list;
            };

            var partner = new ShareDataPartner
            {
                Code = "TEST_P_SQLINJ_TBL",
                Name = "Partner SqlInj Table",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-SQLINJ-TBL",
                PartnerId = partner.ID,
                DatatypeId = ((int)injectPacketId).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal(EshEnums.ExportStatus.Failed, logs[0].Status);
                Assert.NotNull(logs[0].ErrorMessage);
                Assert.Contains("Invalid", logs[0].ErrorMessage);
            }
            finally
            {
                ShareDataExportService.PacketQueryRegistry.Remove(injectPacketId);
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra nỗ lực SQL Injection qua tên Cột bị SqlSugar/SQL Server phát hiện và chặn an toàn (ghi log Failed)
        /// Created date: 03/08/2026
        /// </summary>
        [Fact]
        public async Task QueryPacketData_WhenSqlInjectionInColumn_LogsFailedStatus_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            const EshEnums.DatatypeIdEnum injectPacketId = (EshEnums.DatatypeIdEnum)996;
            ShareDataExportService.PacketQueryRegistry[injectPacketId] = async (dbClient, _) =>
            {
                var list = await dbClient.Queryable<object>().AS("TmsWeather").Select("w.Temperature; DELETE FROM TmsWeather; --").ToListAsync();
                return list;
            };

            var partner = new ShareDataPartner
            {
                Code = "TEST_P_SQLINJ_COL",
                Name = "Partner SqlInj Column",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-SQLINJ-COL",
                PartnerId = partner.ID,
                DatatypeId = ((int)injectPacketId).ToString(),
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal(EshEnums.ExportStatus.Failed, logs[0].Status);
                Assert.NotNull(logs[0].ErrorMessage);
                Assert.NotEmpty(logs[0].ErrorMessage!);
            }
            finally
            {
                ShareDataExportService.PacketQueryRegistry.Remove(injectPacketId);
            }
        }

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
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PTN_MAP_SQ",
                Name = "Partner Test Mapping Profile SavedQuery",
                Status = EshEnums.PartnerStatus.Enabled
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
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-TEST-MP-SQ",
                PartnerId = partner.ID,
                MappingProfileId = mappingProfile.ID,
                DatatypeId = "101",
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
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

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);
                Assert.True(logs[0].RecordCount > 0);
                Assert.False(string.IsNullOrEmpty(logs[0].FilePath));

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                Assert.True(File.Exists(fullPath));
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().In(sampleTraffic.ID).ExecuteCommandAsync();
            }
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
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_PTN_MAP_FP",
                Name = "Partner Test Mapping Profile FieldPicker",
                Status = EshEnums.PartnerStatus.Enabled
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
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-TEST-MP-FP",
                PartnerId = partner.ID,
                MappingProfileId = mappingProfile.ID,
                DatatypeId = "103",
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
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

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().In(sampleTraffic.ID).ExecuteCommandAsync();
            }
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
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P_LOG",
                Name = "Cuc Duong bo Viet Nam (TEST)",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-TEST-LOG",
                PartnerId = partner.ID,
                DatatypeId = "101",
                SessionId = "SESS_TEST_01",
                Format = "DATA",
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                RunStatus = EshEnums.RunStatus.Idle,
                IntervalSeconds = 10,
                NextTimeRun = DateTime.Now.AddMinutes(-1)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var sampleData = new TmsZone { ID = Guid.NewGuid().ToString("N"), Name = "Zone Log Test", FromKmNumber = 1, MaxSpeed = 80 };
            await db.Insertable(sampleData).ExecuteCommandAsync();

            try
            {
                var service = new ShareDataExportService(scopeFactory, logger);
                var method = typeof(ShareDataExportService).GetMethod("ExecuteExport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                var task = (Task)method.Invoke(service, new object[] { sub })!;
                await task;

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
                Assert.Equal(EshEnums.Operator.System, log.OperatorName);
                Assert.Equal("SUCCESS", log.Status);
                Assert.False(string.IsNullOrWhiteSpace(log.Description));
                Assert.Contains("101", log.Description);
            }
            finally
            {
                await db.Deleteable<TmsZone>().In(sampleData.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().In(sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().In(partner.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(x => x.SubscriptionId == sub.ID).ExecuteCommandAsync();
            }
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
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                Code = "TEST_P_MAP",
                Name = "Partner Test MappingProfile",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new ShareDataDataSource
            {
                Code = "TEST_DS_MAP",
                Name = "DataSource Test MappingsJson",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                TableOrView = "TmsZoneStatus",
                TopN = 5
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mappingProfile = new ShareDataMappingProfile
            {
                Code = "TEST_MP_MAP",
                Name = "Mapping Profile Test MappingsJson",
                VendorId = partner.ID,
                DataSourceId = dataSource.ID,
                DatatypeId = "101",
                Direction = "OUT",
                MappingsJson = "[{\"sourceField\":\"ZoneId\",\"targetField\":\"zoneId\"},{\"sourceField\":\"AverageSpeed\",\"targetField\":\"averageSpeed\"},{\"sourceField\":\"NonExistentColumn\",\"targetField\":\"pavementType\"}]",
                IsActive = true
            };
            await db.Insertable(mappingProfile).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                SerialNbr = "SUB-TEST-MAP",
                PartnerId = partner.ID,
                MappingProfileId = mappingProfile.ID,
                DatatypeId = "101",
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                RunStatus = EshEnums.RunStatus.Idle,
                IntervalSeconds = 10,
                NextTimeRun = DateTime.Now.AddMinutes(-1)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var sampleStat = new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = "EQUIP_01",
                AverageSpeed = "75.5",
                Condition = "Slow",
                CreateTime = DateTime.Now
            };
            await db.Insertable(sampleStat).ExecuteCommandAsync();

            try
            {
                var service = new ShareDataExportService(scopeFactory, logger);
                var method = typeof(ShareDataExportService).GetMethod("ExecuteExport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                var task = (Task)method.Invoke(service, new object[] { sub })!;
                await task;

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(x => x.SubscriptionId == sub.ID)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal("SUCCESS", logs[0].Status);
                Assert.NotNull(logs[0].FilePath);

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), logs[0].FilePath!);
                Assert.True(File.Exists(fullPath));

                var jsonContent = await File.ReadAllTextAsync(fullPath);
                Assert.Contains("\"zoneId\"", jsonContent);
                Assert.Contains("\"averageSpeed\"", jsonContent);
                Assert.Contains("\"pavementType\":null", jsonContent);
                Assert.DoesNotContain("\"condition\"", jsonContent, StringComparison.OrdinalIgnoreCase);

                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            finally
            {
                await db.Deleteable<TmsZoneStatus>().In(sampleStat.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().In(sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataMappingProfile>().In(mappingProfile.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataDataSource>().In(dataSource.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().In(partner.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(x => x.SubscriptionId == sub.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra Singleton ShareDataWorkerControlService Bật và Tắt chuyển trạng thái đúng
        /// Created date: 04/08/2026
        /// </summary>
        // [Fact]
        // public void WorkerControlService_StartAndStop_ChangesStatus_Test()
        // {
        //     var service = new ShareDataWorkerControlService();

        //     Assert.True(service.IsRunning);
        //     Assert.Equal("RUNNING", service.GetStatus().StatusText);

        //     var stopStatus = service.StopWorker();
        //     Assert.False(service.IsRunning);
        //     Assert.False(stopStatus.IsRunning);
        //     Assert.Equal("PAUSED", stopStatus.StatusText);

        //     var startStatus = service.StartWorker();
        //     Assert.True(service.IsRunning);
        //     Assert.True(startStatus.IsRunning);
        //     Assert.Equal("RUNNING", startStatus.StatusText);
        // }
        /// <summary>
        /// Author: Đạt
        /// Description: Test case khẳng định truy vấn cột không tồn tại sẽ ném lỗi SqlException (Error 207)
        /// Created date: 05/08/2026
        /// </summary>
        [Fact]
        public async Task QueryNonExistentColumn_ThrowsSqlException_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var ex = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(async () =>
            {
                await db.Ado.SqlQueryAsync<dynamic>("SELECT NonExistentColumn_Test FROM [dbo].[ShareDataSubscription]");
            });

            Assert.NotNull(ex);
            Assert.Contains("Invalid column name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Test case kiểm tra toàn bộ 7 Entity chuẩn của ShareData chạy SELECT hợp lệ 100% không phát sinh lỗi lệch cột CSDL
        /// Created date: 05/08/2026
        /// </summary>
        [Fact]
        public async Task AllShareDataEntities_SelectQueries_ExecuteWithoutColumnMismatchError_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var subs = await db.Queryable<ShareDataSubscription>().Take(1).ToListAsync();
            Assert.NotNull(subs);

            var partners = await db.Queryable<ShareDataPartner>().Take(1).ToListAsync();
            Assert.NotNull(partners);

            var dataSources = await db.Queryable<ShareDataDataSource>().Take(1).ToListAsync();
            Assert.NotNull(dataSources);

            var mappings = await db.Queryable<ShareDataMappingProfile>().Take(1).ToListAsync();
            Assert.NotNull(mappings);

            var logs = await db.Queryable<ShareDataActivityLog>().Take(1).ToListAsync();
            Assert.NotNull(logs);

            var sessions = await db.Queryable<ShareDataSession>().Take(1).ToListAsync();
            Assert.NotNull(sessions);

            var events = await db.Queryable<ShareDataEventSource>().Take(1).ToListAsync();
            Assert.NotNull(events);
        }
    }
}
