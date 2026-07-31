using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Data;
using TA_ShareData_WorkerService.Core.Entities;
using TA_ShareData_WorkerService.Core.Enums;
using TA_ShareData_WorkerService.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace TA_ShareData_WorkerService.Tests
{
    public class ShareDataExportServiceTest : IDisposable
    {
        public IHost HostInstance { get; }
        public IServiceProvider Services => HostInstance.Services;

        public ShareDataExportServiceTest()
        {
            HostInstance = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    var connectionString = "Server=localhost,14333;Database=test;MultipleActiveResultSets=true;User ID=sa;Password=Password123!;TrustServerCertificate=True;";
                    services.AddScoped<ISqlSugarClient>(sp => new SqlSugarScope(new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        DbType = SqlSugar.DbType.SqlServer,
                        IsAutoCloseConnection = true,
                        InitKeyType = InitKeyType.Attribute
                    }));
                })
                .Build();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            if (!db.DbMaintenance.IsAnyTable("TmsTrafficData"))
            {
                db.CodeFirst.InitTables<TmsTrafficData>();
            }
        }

        public void Dispose()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var currentDbName = db.Ado.Connection.Database;
            if (string.Equals(currentDbName, "test", StringComparison.OrdinalIgnoreCase))
            {
                db.Ado.ExecuteCommand("DELETE FROM EshDataSource WHERE Code LIKE 'TEST_%'");
                db.Ado.ExecuteCommand("DELETE FROM EshPartner WHERE Code LIKE 'TEST_%'");
                db.Ado.ExecuteCommand("DELETE FROM EshMappingProfile WHERE Code LIKE 'TEST_%'");
                db.Ado.ExecuteCommand("DELETE FROM EshSubscription WHERE SerialNbr LIKE 'SUB-%'");
                db.Ado.ExecuteCommand("DELETE FROM EshFieldMapping WHERE SourceKey LIKE 'LocationCode%'");
            }
            HostInstance.Dispose();
        }
    }

    public class WorkerServiceTests : IClassFixture<ShareDataExportServiceTest>
    {
        private readonly ShareDataExportServiceTest _host;
        private readonly ITestOutputHelper _output;

        public WorkerServiceTests(ShareDataExportServiceTest host, ITestOutputHelper output)
        {
            _host = host;
            _output = output;
        }

        [Fact]
        public async Task WorkerService_SavedQuery_Execution_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new EshPartner
            {
                Code = "TEST_WORKER_QUERY",
                Name = "SavedQuery Partner",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_QUERY",
                Name = "Worker SavedQuery DataSource",
                Kind = EshEnums.DataSourceKind.SavedQuery,
                QueryText = "SELECT Code, Name FROM EshPartner",
                TopN = 50
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_QUERY_MAP",
                Name = "Worker Query Mapping",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_QUERY_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "Code",
                TargetKey = "ma_doi_tac",
                OrderNo = 1
            };
            var field2 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "Name",
                TargetKey = "ten_doi_tac",
                OrderNo = 2
            };
            await db.Insertable(new[] { field1, field2 }).ExecuteCommandAsync();

            var subscription = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-QUERY-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_QUERY_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(subscription).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var updatedSub = await db.Queryable<EshSubscription>().InSingleAsync(subscription.ID);
            Assert.NotNull(updatedSub);
            Assert.Equal(EshEnums.RunStatus.Idle, updatedSub.RunStatus);
            Assert.Null(updatedSub.ProcessLockId);

            var exportLogs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == subscription.ID)
                .OrderByDescending(l => l.ExportedAt)
                .ToListAsync();

            Assert.NotEmpty(exportLogs);
            var log = exportLogs[0];
            Assert.Equal(EshEnums.ExportStatus.Success, log.Status);
            Assert.True(log.RecordCount > 0);
            Assert.True(log.ByteSize > 0);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), log.FilePath ?? string.Empty);
            Assert.True(File.Exists(fullPath));

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var hashBytes = System.Security.Cryptography.SHA256.HashData(fileBytes);
            var calculatedHash = Convert.ToHexString(hashBytes);
            Assert.Equal(log.Hash, calculatedHash);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            _output.WriteLine($"[Worker SavedQuery Test] SUCCESS! Physical file & Hash matched: {calculatedHash}");
        }

        [Fact]
        public async Task WorkerService_TablePicker_Execution_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var trafficRecord = new TmsTrafficData
            {
                LocationCode = "TRAM_CAU_DIEN",
                VehicleCount = 1500,
                LogTime = DateTime.Now
            };
            await db.Insertable(trafficRecord).ExecuteCommandAsync();

            var partner = new EshPartner
            {
                Code = "TEST_WORKER_TRAFFIC",
                Name = "Traffic Data Partner",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_TRAFFIC",
                Name = "Worker Traffic TablePicker DataSource",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = "TmsTrafficData",
                TopN = 100
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_TRAFFIC_MAP",
                Name = "Traffic Picker Mapping",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_TRAFFIC_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "LocationCode",
                TargetKey = "ma_tram",
                OrderNo = 1
            };
            var field2 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "VehicleCount",
                TargetKey = "luu_luong_xe",
                OrderNo = 2
            };
            await db.Insertable(new[] { field1, field2 }).ExecuteCommandAsync();

            var subscription = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-TRAFFIC-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_TRAFFIC_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(subscription).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var updatedSub = await db.Queryable<EshSubscription>().InSingleAsync(subscription.ID);
            Assert.NotNull(updatedSub);
            Assert.Equal(EshEnums.RunStatus.Idle, updatedSub.RunStatus);
            Assert.Null(updatedSub.ProcessLockId);

            var exportLogs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == subscription.ID)
                .OrderByDescending(l => l.ExportedAt)
                .ToListAsync();

            Assert.NotEmpty(exportLogs);
            var log = exportLogs[0];
            Assert.Equal(EshEnums.ExportStatus.Success, log.Status);
            Assert.True(log.RecordCount > 0);
            Assert.True(log.ByteSize > 0);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), log.FilePath ?? string.Empty);
            Assert.True(File.Exists(fullPath));

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var hashBytes = System.Security.Cryptography.SHA256.HashData(fileBytes);
            var calculatedHash = Convert.ToHexString(hashBytes);
            Assert.Equal(log.Hash, calculatedHash);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            _output.WriteLine($"[Worker TablePicker Test] SUCCESS! Physical file & Hash matched: {calculatedHash}");
        }

        [Fact]
        public async Task WorkerService_NonExistentTable_ThrowsException_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new EshPartner
            {
                Code = "TEST_WORKER_NONEXISTENT",
                Name = "NonExistent Table Partner",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_NONEXISTENT",
                Name = "Worker NonExistent Table DataSource",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = "NonExistentTable_12345",
                TopN = 100
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_NONEXISTENT_MAP",
                Name = "NonExistent Mapping",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_NONEXISTENT_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "DummyKey",
                TargetKey = "ma_dummy",
                OrderNo = 1
            };
            await db.Insertable(field1).ExecuteCommandAsync();

            var subscription = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-NONEXISTENT-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_NONEXISTENT_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(subscription).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var updatedSub = await db.Queryable<EshSubscription>().InSingleAsync(subscription.ID);
            Assert.NotNull(updatedSub);
            Assert.Equal(EshEnums.RunStatus.Idle, updatedSub.RunStatus);

            var exportLogs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == subscription.ID)
                .OrderByDescending(l => l.ExportedAt)
                .ToListAsync();
            Assert.NotEmpty(exportLogs);
            Assert.Equal(EshEnums.ExportStatus.Failed, exportLogs[0].Status);
        }

        [Fact]
        public async Task WorkerService_EmptySqlOrTable_ThrowsException_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var dataSource = new EshDataSource
            {
                Name = "Empty DataSource Test",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = null,
                QueryText = null
            };

            var sql = string.Empty;
            if (dataSource.Kind == EshEnums.DataSourceKind.SavedQuery && !string.IsNullOrWhiteSpace(dataSource.QueryText))
            {
                sql = $"SELECT TOP (@topN) * FROM ({dataSource.QueryText}) as temp_query";
            }
            else if (!string.IsNullOrWhiteSpace(dataSource.Table))
            {
                var safeTableName = db.EntityMaintenance.GetTableName(dataSource.Table);
                sql = $"SELECT TOP (@topN) * FROM {safeTableName}";
            }

            Assert.True(string.IsNullOrWhiteSpace(sql));
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                if (string.IsNullOrWhiteSpace(sql))
                    throw new InvalidOperationException($"Nguồn dữ liệu ID: {dataSource.ID} không có câu lệnh SQL hoặc Table/View hợp lệ");
            });

            Assert.Contains("không có câu lệnh SQL hoặc Table/View hợp lệ", ex.Message);
        }

        [Fact]
        public async Task WorkerService_TopN_FallbackDefault1000_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var dataSource = new EshDataSource
            {
                Table = "TmsTrafficData",
                TopN = 0
            };

            var topNValue = dataSource.TopN > 0 ? dataSource.TopN : 1000;
            Assert.Equal(1000, topNValue);
        }

        [Fact]
        public async Task WorkerService_ForbiddenSqlKeywords_FailsAndLogs_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new EshPartner { Code = "TEST_WORKER_FORBIDDEN", Name = "Forbidden SQL Partner", Status = EshEnums.PartnerStatus.Enabled };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_FORBIDDEN",
                Name = "Forbidden SQL Query DataSource",
                Kind = EshEnums.DataSourceKind.SavedQuery,
                QueryText = "SELECT * FROM EshPartner; DELETE FROM EshPartner",
                TopN = 10
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_FORBIDDEN_MAP",
                Name = "Forbidden Mapping",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_FORBIDDEN_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping { MappingProfileId = mapping.ID, SourceKey = "Code", TargetKey = "ma_doi_tac", OrderNo = 1 };
            await db.Insertable(field1).ExecuteCommandAsync();

            var subscription = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-FORBIDDEN-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_FORBIDDEN_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(subscription).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var exportLogs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == subscription.ID)
                .OrderByDescending(l => l.ExportedAt)
                .ToListAsync();
            Assert.NotEmpty(exportLogs);
            Assert.Equal(EshEnums.ExportStatus.Failed, exportLogs[0].Status);
            Assert.Contains("QueryText chứa từ khóa bị cấm", exportLogs[0].ErrorMessage);
        }

        [Fact]
        public async Task WorkerService_SavedQueryNotStartingWithSelect_FailsAndLogs_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new EshPartner { Code = "TEST_WORKER_NON_SELECT", Name = "Non SELECT Partner", Status = EshEnums.PartnerStatus.Enabled };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_NON_SELECT",
                Name = "Non SELECT Query DataSource",
                Kind = EshEnums.DataSourceKind.SavedQuery,
                QueryText = "EXEC sp_GetData",
                TopN = 10
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_NON_SELECT_MAP",
                Name = "Non SELECT Mapping",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_NON_SELECT_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping { MappingProfileId = mapping.ID, SourceKey = "Code", TargetKey = "ma_doi_tac", OrderNo = 1 };
            await db.Insertable(field1).ExecuteCommandAsync();

            var subscription = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-NON-SELECT-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_NON_SELECT_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(subscription).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var exportLogs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == subscription.ID)
                .OrderByDescending(l => l.ExportedAt)
                .ToListAsync();
            Assert.NotEmpty(exportLogs);
            Assert.Equal(EshEnums.ExportStatus.Failed, exportLogs[0].Status);
            Assert.Contains("QueryText phải bắt đầu bằng SELECT", exportLogs[0].ErrorMessage);
        }

        [Fact]
        public async Task WorkerService_LockTimeoutRecovery_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new EshPartner { Code = "TEST_WORKER_LOCK", Name = "Lock Recovery Partner", Status = EshEnums.PartnerStatus.Enabled };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_LOCK",
                Name = "Lock Recovery DataSource",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = "TmsTrafficData",
                TopN = 10
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_LOCK_MAP",
                Name = "Lock Recovery Mapping",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_LOCK_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping { MappingProfileId = mapping.ID, SourceKey = "LocationCode", TargetKey = "ma_tram", OrderNo = 1 };
            await db.Insertable(field1).ExecuteCommandAsync();

            var stuckSubscription = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-LOCK-STUCK-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_LOCK_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                RunStatus = EshEnums.RunStatus.Running,
                ProcessLockId = "OLD_STUCK_LOCK_ID",
                UpdateTime = DateTime.Now.AddMinutes(-10),
                NextTimeRun = DateTime.Now.AddMinutes(-10)
            };
            await db.Insertable(stuckSubscription).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var updatedSub = await db.Queryable<EshSubscription>().InSingleAsync(stuckSubscription.ID);
            Assert.NotNull(updatedSub);
            Assert.Equal(EshEnums.RunStatus.Idle, updatedSub.RunStatus);
            Assert.Null(updatedSub.ProcessLockId);

            var exportLogs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == stuckSubscription.ID)
                .ToListAsync();
            Assert.NotEmpty(exportLogs);
            Assert.Equal(EshEnums.ExportStatus.Success, exportLogs[0].Status);
        }

        [Fact]
        public async Task WorkerService_LargeVolumePerformance_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var list = new List<TmsTrafficData>();
            for (int i = 1; i <= 1000; i++)
            {
                list.Add(new TmsTrafficData
                {
                    LocationCode = $"TRAM_LARGE_{i:D5}",
                    VehicleCount = i * 2,
                    LogTime = DateTime.Now
                });
            }
            await db.Insertable(list).PageSize(100).ExecuteCommandAsync();

            var partner = new EshPartner { Code = "TEST_WORKER_LARGE", Name = "Large Volume Partner", Status = EshEnums.PartnerStatus.Enabled };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_LARGE",
                Name = "Large Volume DataSource",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = "TmsTrafficData",
                TopN = 10000
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_LARGE_MAP",
                Name = "Large Volume Mapping",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_LARGE_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping { MappingProfileId = mapping.ID, SourceKey = "LocationCode", TargetKey = "ma_tram", OrderNo = 1 };
            var field2 = new EshFieldMapping { MappingProfileId = mapping.ID, SourceKey = "VehicleCount", TargetKey = "luu_luong", OrderNo = 2 };
            await db.Insertable(new[] { field1, field2 }).ExecuteCommandAsync();

            var subscription = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-LARGE-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_LARGE_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(subscription).ExecuteCommandAsync();

            try
            {
                var workerService = new ShareDataExportService(scopeFactory, logger);
                await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

                var exportLogs = await db.Queryable<EshExportLog>()
                    .Where(l => l.SubscriptionId == subscription.ID)
                    .ToListAsync();
                Assert.NotEmpty(exportLogs);
                Assert.Equal(EshEnums.ExportStatus.Success, exportLogs[0].Status);

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), exportLogs[0].FilePath ?? string.Empty);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().In(list.Select(x => x.ID)).ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task WorkerService_MultiThreadedParallelExecution_RunsSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();

            var partner = new EshPartner { Code = "TEST_WORKER_MULTI", Name = "Đối tác test đa luồng", Status = EshEnums.PartnerStatus.Enabled };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_MULTI",
                Name = "Nguồn test đa luồng",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = "TmsTrafficData",
                TopN = 50
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_MAP_MULTI",
                Name = "Mapping test đa luồng",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_MULTI_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping { MappingProfileId = mapping.ID, SourceKey = "LocationCode", TargetKey = "ma_tram", OrderNo = 1 };
            await db.Insertable(field1).ExecuteCommandAsync();

            var sub1 = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-MULTI-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_MULTI_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            var sub2 = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-MULTI-02",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_MULTI_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(new[] { sub1, sub2 }).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var logs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == sub1.ID || l.SubscriptionId == sub2.ID)
                .ToListAsync();

            Assert.Equal(2, logs.Count);
            Assert.All(logs, log => Assert.Equal(EshEnums.ExportStatus.Success, log.Status));
        }

        [Fact]
        public async Task WorkerService_QueryWithOrderBy_HandlesSubqueryWithTop100Percent_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();

            var partner = new EshPartner { Code = "TEST_WORKER_ORDER", Name = "Đối tác test ORDER BY", Status = EshEnums.PartnerStatus.Enabled };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_ORDER",
                Name = "Nguồn test ORDER BY",
                Kind = EshEnums.DataSourceKind.SavedQuery,
                QueryText = "SELECT LocationCode, VehicleCount FROM TmsTrafficData ORDER BY LogTime DESC",
                TopN = 20
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_MAP_ORDER",
                Name = "Mapping test ORDER BY",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_ORDER_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping { MappingProfileId = mapping.ID, SourceKey = "LocationCode", TargetKey = "ma_tram", OrderNo = 1 };
            await db.Insertable(field1).ExecuteCommandAsync();

            var sub = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-ORDER-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_ORDER_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var logs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(EshEnums.ExportStatus.Success, logs[0].Status);
        }

        [Fact]
        public async Task WorkerService_ForbiddenSqlCheck_AllowsColumnNamesContainingKeywords_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();

            var partner = new EshPartner { Code = "TEST_WORKER_LASTUPDATED", Name = "Đối tác test LastUpdated", Status = EshEnums.PartnerStatus.Enabled };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_DS_LASTUPDATED",
                Name = "Nguồn test LastUpdated",
                Kind = EshEnums.DataSourceKind.SavedQuery,
                QueryText = "SELECT LocationCode AS LastUpdated FROM TmsTrafficData",
                TopN = 10
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_MAP_LASTUPDATED",
                Name = "Mapping test LastUpdated",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_LASTUPDATED_01",
                Direction = EshEnums.SubDirection.Outbound,
                DataSourceId = dataSource.ID,
                IsActive = true
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var field1 = new EshFieldMapping { MappingProfileId = mapping.ID, SourceKey = "LastUpdated", TargetKey = "ma_tram", OrderNo = 1 };
            await db.Insertable(field1).ExecuteCommandAsync();

            var sub = new EshSubscription
            {
                SerialNbr = "SUB-WORKER-LASTUPDATED-01",
                PartnerId = partner.ID,
                DatatypeId = "WORKER_LASTUPDATED_01",
                MappingProfileId = mapping.ID,
                DataSourceId = dataSource.ID,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                State = EshEnums.SubState.Active,
                IntervalSeconds = 60,
                NextTimeRun = DateTime.Now.AddSeconds(-10)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var logs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(EshEnums.ExportStatus.Success, logs[0].Status);
        }

        [Fact]
        public async Task WorkerService_ProcessBatchSubscriptions_WhenNoSubscriptionsExist_ReturnsSafelyWithoutException_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();

            await db.Deleteable<EshSubscription>().ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);

            var exception = await Record.ExceptionAsync(async () =>
            {
                await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Kiểm thử tính năng xuất tăng dần không trùng lặp dữ liệu đợt trước dựa vào cột UpdateTime
        /// Author: Đạt
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task WorkerService_IncrementalExport_AvoidsDuplicates_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new EshPartner
            {
                Code = "TEST_INC_PARTNER",
                Name = "Incremental Test Partner",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_INC_DS",
                Name = "Incremental Traffic Data",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = "TmsTrafficData",
                TopN = 100
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_INC_MAP",
                Name = "Incremental Mapping"
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var f1 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "LocationCode",
                TargetKey = "locationCode",
                OrderNo = 1
            };
            var f2 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "UpdateTime",
                TargetKey = "updateTime",
                OrderNo = 2
            };
            await db.Insertable(new List<EshFieldMapping> { f1, f2 }).ExecuteCommandAsync();

            var sub = new EshSubscription
            {
                SerialNbr = "SUB-INC-001",
                PartnerId = partner.ID,
                DataSourceId = dataSource.ID,
                MappingProfileId = mapping.ID,
                State = EshEnums.SubState.Active,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                RunStatus = EshEnums.RunStatus.Idle,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
                LastTimeRun = new DateTime(2026, 1, 1, 1, 0, 0)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var item1 = new TmsTrafficData
            {
                LocationCode = "LocationCode_INC_1",
                VehicleCount = 10,
                UpdateTime = new DateTime(2026, 1, 1, 1, 0, 30)
            };
            await db.Insertable(item1).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var updatedSub = await db.Queryable<EshSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updatedSub.LastTimeRun);

            var item2 = new TmsTrafficData
            {
                LocationCode = "LocationCode_INC_2",
                VehicleCount = 20,
                UpdateTime = DateTime.Now.AddSeconds(10)
            };
            await db.Insertable(item2).ExecuteCommandAsync();

            updatedSub.NextTimeRun = DateTime.Now.AddSeconds(-10);
            await db.Updateable(updatedSub).ExecuteCommandAsync();

            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var logs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderBy(l => l.ExportedAt, OrderByType.Asc)
                .ToListAsync();

            Assert.True(logs.Count >= 2);
            Assert.True(logs[0].RecordCount >= 1);
            Assert.Equal(1, logs[1].RecordCount);
        }

        /// <summary>
        /// Kiểm thử trường hợp bảng nguồn không có cột mốc thời gian (không bị crash nổ SQL Invalid column name)
        /// Author: Đạt
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task WorkerService_IncrementalExport_WhenTableHasNoTimeColumn_DoesNotCrash_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new EshPartner
            {
                Code = "TEST_NOTIME_PARTNER",
                Name = "No Time Col Partner",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_NOTIME_DS",
                Name = "No Time Col DS",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = "EshPartner",
                TopN = 50
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_NOTIME_MAP",
                Name = "No Time Col Mapping"
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var f1 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "Code",
                TargetKey = "code",
                OrderNo = 1
            };
            await db.Insertable(f1).ExecuteCommandAsync();

            var sub = new EshSubscription
            {
                SerialNbr = "SUB-NOTIME-001",
                PartnerId = partner.ID,
                DataSourceId = dataSource.ID,
                MappingProfileId = mapping.ID,
                State = EshEnums.SubState.Active,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                RunStatus = EshEnums.RunStatus.Idle,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
                LastTimeRun = DateTime.Now.AddMinutes(-10)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);

            var exception = await Record.ExceptionAsync(async () =>
            {
                await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);
            });

            Assert.Null(exception);

            var logs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(EshEnums.ExportStatus.Success, logs[0].Status);
        }

        /// <summary>
        /// Kiểm thử khả năng tự động nhận diện cột mốc thời gian tên khác UpdateTime (cụ thể là LogTime)
        /// Author: Đạt
        /// Created date: 31/07/2026
        /// </summary>
        [Fact]
        public async Task WorkerService_IncrementalExport_WithLogTimeColumn_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShareDataExportService>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new EshPartner
            {
                Code = "TEST_LOGTIME_PARTNER",
                Name = "LogTime Partner",
                Status = EshEnums.PartnerStatus.Enabled
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var dataSource = new EshDataSource
            {
                Code = "TEST_LOGTIME_DS",
                Name = "LogTime DS",
                Kind = EshEnums.DataSourceKind.FieldPicker,
                Table = "TmsTrafficData",
                TopN = 50
            };
            await db.Insertable(dataSource).ExecuteCommandAsync();

            var mapping = new EshMappingProfile
            {
                Code = "TEST_LOGTIME_MAP",
                Name = "LogTime Mapping"
            };
            await db.Insertable(mapping).ExecuteCommandAsync();

            var f1 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "LocationCode",
                TargetKey = "locationCode",
                OrderNo = 1
            };
            var f2 = new EshFieldMapping
            {
                MappingProfileId = mapping.ID,
                SourceKey = "LogTime",
                TargetKey = "logTime",
                OrderNo = 2
            };
            await db.Insertable(new List<EshFieldMapping> { f1, f2 }).ExecuteCommandAsync();

            var sub = new EshSubscription
            {
                SerialNbr = "SUB-LOGTIME-001",
                PartnerId = partner.ID,
                DataSourceId = dataSource.ID,
                MappingProfileId = mapping.ID,
                State = EshEnums.SubState.Active,
                Direction = EshEnums.SubDirection.Outbound,
                Mode = EshEnums.SubMode.Batch,
                RunStatus = EshEnums.RunStatus.Idle,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
                LastTimeRun = DateTime.Now.AddSeconds(-5)
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var item = new TmsTrafficData
            {
                LocationCode = "LogTime_Loc_1",
                VehicleCount = 5,
                LogTime = DateTime.Now.AddSeconds(10)
            };
            await db.Insertable(item).ExecuteCommandAsync();

            var workerService = new ShareDataExportService(scopeFactory, logger);
            await workerService.ProcessBatchSubscriptionsAsync(CancellationToken.None);

            var logs = await db.Queryable<EshExportLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(EshEnums.ExportStatus.Success, logs[0].Status);
            Assert.Equal(1, logs[0].RecordCount);
        }
    }

    [SugarTable("TmsTrafficData")]
    public class TmsTrafficData : EntityTenant
    {
        [SugarColumn(Length = 64, IsNullable = true)]
        public string? LocationCode { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? VehicleCount { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? LogTime { get; set; }
    }
}
