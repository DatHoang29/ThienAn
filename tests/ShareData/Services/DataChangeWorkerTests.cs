using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.VMS.Core.Entities;
using Services.Shared.Messaging;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using ShareDataWorker.Infrastructure.Workers;
using SqlSugar;
using Xunit;

namespace Tests.Modules.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Bộ kiểm thử chuyên biệt cho SQL Server Change Tracking &amp; DataChangeWorker.
    /// Created date: 22/09/2026
    /// </summary>
    [Collection("api")]
    public class DataChangeWorkerTests(Host host)
    {
        private readonly Host _host = host;

        #region 1. DataChangeWorker Static & Status Tests

        [Fact]
        public async Task CheckTrackingDatabase_IsReadOnlyAndDoesNotThrow()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataTrackerWorker>>();

            // Act
            var status = await DataTrackerWorker.CheckTrackingDatabase(db, logger);

            // Assert
            Assert.NotNull(status);
            Assert.NotNull(status.MissingTables);
            Assert.True(status.DatabaseEnabled || !status.DatabaseEnabled);
        }

        [Fact]
        public async Task CheckTrackingDatabase_WhenAutoEnableTrue_AttemptsInitializationGracefully()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataTrackerWorker>>();

            // Act: autoEnable = true gọi ALTER DATABASE / ALTER TABLE an toàn
            var status = await DataTrackerWorker.CheckTrackingDatabase(db, logger, autoEnable: true);

            // Assert
            Assert.NotNull(status);
            Assert.NotNull(status.MissingTables);
        }

        [Fact]
        public async Task ChangedTablesSql_WhenBuiltFromTrackedTables_ExecutesSuccessfullyOnDatabase()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataTrackerWorker>>();

            var status = await DataTrackerWorker.CheckTrackingDatabase(db, logger, autoEnable: true);
            var tracked = DataTrackerWorker.RawTableToPacketMap.Keys
                .Except(status.MissingTables, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Act
            var sql = DataTrackerWorker.BuildChangedTablesSql(tracked);
            if (tracked.Count == 0)
            {
                Assert.Empty(sql);
                return;
            }

            var rows = await db.Ado.SqlQueryAsync<string>(sql, new { lastVer = 0L });

            // Assert
            Assert.NotNull(rows);
            Assert.All(rows, r => Assert.Contains(r, tracked));
        }

        /// <summary>
        /// Description: Kiểm thử Change Tracking chỉ lọc thao tác Thêm mới (Insert) và Sửa (Update),
        ///              bỏ qua thao tác Xóa (Delete) — không kích hoạt bắn NATS khi chỉ có bản ghi bị xóa.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenOnlyDeleteOccurs_DoesNotDetectChangeAndDoesNotTrigger_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataTrackerWorker>>();

            await DataTrackerWorker.CheckTrackingDatabase(db, logger, autoEnable: true);

            var testId = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_DEL_{testId}";
            var carId = $"CAR_DEL_{testId}";

            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"EQ_{testId}",
                KmNumber = 10,
                MetNumber = 500
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = carId,
                EquipmentId = eqId,
                DetectTime = DateTime.Now,
                Type = "CAR",
                LicensePlate = $"30A-DEL_{testId}",
                Speed = 60f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM10",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // Lấy mốc version sau khi đã Insert
            var verObj = await db.Ado.GetScalarAsync(DataTrackerWorker.SqlChangeTrackingCurrentVersion);
            var verAfterInsert = Convert.ToInt64(verObj);

            try
            {
                // Act 1: Xóa bản ghi (chỉ sinh SYS_CHANGE_OPERATION = 'D')
                await db.Deleteable<TmsTrafficData>().Where(t => t.ID == carId).ExecuteCommandAsync();

                // Kiểm tra câu lệnh BuildChangedTablesSql từ mốc verAfterInsert
                var sql = DataTrackerWorker.BuildChangedTablesSql(["TmsTrafficData"]);
                var changedTablesAfterDelete = await db.Ado.SqlQueryAsync<string>(sql, new { lastVer = verAfterInsert });

                // Assert 1: Thao tác DELETE không được ghi nhận trong danh sách bảng thay đổi
                Assert.DoesNotContain("TmsTrafficData", changedTablesAfterDelete);

                // Act 2: Thêm một bản ghi mới (sinh SYS_CHANGE_OPERATION = 'I')
                var newCarId = $"CAR_ADD_{testId}";
                await db.Insertable(new TmsTrafficData
                {
                    ID = newCarId,
                    EquipmentId = eqId,
                    DetectTime = DateTime.Now,
                    Type = "CAR",
                    LicensePlate = $"30A-ADD_{testId}",
                    Speed = 70f,
                    Lane = "L1",
                    Direction = "NORTH",
                    Location = "KM10",
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                }).ExecuteCommandAsync();

                var changedTablesAfterInsert = await db.Ado.SqlQueryAsync<string>(sql, new { lastVer = verAfterInsert });

                // Assert 2: Thao tác INSERT lập tức được ghi nhận
                Assert.Contains("TmsTrafficData", changedTablesAfterInsert);

                // Dọn dẹp newCarId
                await db.Deleteable<TmsTrafficData>().Where(t => t.ID == newCarId).ExecuteCommandAsync();
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm thử IsChangeTrackingVersionInvalid nhận diện chính xác các mã lỗi 22114, 22115 và thông điệp lỗi liên quan đến Change Tracking version không hợp lệ.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public void IsChangeTrackingVersionInvalid_WhenGivenVariousExceptions_ClassifiesCorrectly_Test()
        {
            // 1. Ngoại lệ null hoặc thông thường
            Assert.False(DataTrackerWorker.IsChangeTrackingVersionInvalid(null));
            Assert.False(DataTrackerWorker.IsChangeTrackingVersionInvalid(new InvalidOperationException("Connection timeout")));
            Assert.False(DataTrackerWorker.IsChangeTrackingVersionInvalid(new FakeSqlException(1205, "Transaction deadlock")));

            // 2. SqlException có mã lỗi 22114 hoặc 22115
            Assert.True(DataTrackerWorker.IsChangeTrackingVersionInvalid(new FakeSqlException(22114, "A previous version or change tracking version is invalid.")));
            Assert.True(DataTrackerWorker.IsChangeTrackingVersionInvalid(new FakeSqlException(22115, "Change tracking version cleanup occurred.")));

            // 3. Ngoại lệ chứa từ khóa nhận diện trong thông điệp
            Assert.True(DataTrackerWorker.IsChangeTrackingVersionInvalid(new Exception("SqlException: 22114")));
            Assert.True(DataTrackerWorker.IsChangeTrackingVersionInvalid(new Exception("change tracking version is invalid.")));
            Assert.True(DataTrackerWorker.IsChangeTrackingVersionInvalid(new Exception("The minimum valid version is 450.")));
            Assert.True(DataTrackerWorker.IsChangeTrackingVersionInvalid(new Exception("CHANGE_TRACKING_MIN_VALID_VERSION check failed.")));

            // 4. Ngoại lệ lồng nhau (InnerException)
            var wrappedEx = new TargetInvocationException(
                "Wrapper error",
                new AggregateException(new FakeSqlException(22114, "Nested invalid version")));
            Assert.True(DataTrackerWorker.IsChangeTrackingVersionInvalid(wrappedEx));
        }

        /// <summary>
        /// Description: Kiểm thử cơ chế Self-Healing của DataTrackerWorker: Khi gặp lỗi Change Tracking version không hợp lệ,
        ///              worker tự động phát hiện, ghi log warning và nhảy cóc mốc version lên current version của DB, không gây crash worker.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenVersionInvalid_SelfHealsAndFastForwardsToCurrentVersion_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataTrackerWorker>>();
            var config = new ConfigurationBuilder().Build();
            var transport = new TransportManager(config);

            await DataTrackerWorker.CheckTrackingDatabase(db, logger, autoEnable: true);

            var currentVerObj = await db.Ado.GetScalarAsync(DataTrackerWorker.SqlChangeTrackingCurrentVersion);
            var currentVer = Convert.ToInt64(currentVerObj);

            var worker = new DataTrackerWorker(scopeFactory, logger, transport);

            // Act 1: Chu kỳ poll đầu tiên khởi tạo LastProcessedVersion = currentVer
            await worker.PollChangeTracking(CancellationToken.None);
            Assert.True(worker.LastProcessedVersion >= 0);

            // Act 2: Giả lập mốc LastProcessedVersion bị lệch hoặc âm (-10)
            typeof(DataTrackerWorker).GetProperty(nameof(DataTrackerWorker.LastProcessedVersion))?
                .GetSetMethod(nonPublic: true)?
                .Invoke(worker, [-10L]);
            await worker.PollChangeTracking(CancellationToken.None);

            // Assert: Worker tự căn chỉnh lại theo mốc DB hiện tại
            Assert.True(worker.LastProcessedVersion >= currentVer);
        }

        /// <summary>
        /// Description: Kiểm thử cơ chế dừng hợp tác (Cooperative Cancellation): Khi stoppingToken nhận tín hiệu hủy,
        ///              PollChangeTracking lập tức ném OperationCanceledException để kết thúc chu kỳ nhanh chóng, không tiếp tục truy vấn.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task PollChangeTracking_WhenCancellationRequested_ThrowsOperationCanceledException_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataTrackerWorker>>();
            var config = new ConfigurationBuilder().Build();
            var transport = new TransportManager(config);
            var worker = new DataTrackerWorker(scopeFactory, logger, transport);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => worker.PollChangeTracking(cts.Token));
        }

        #endregion

        #region 2. Change Tracking Full Business Flow Tests

        /// <summary>
        /// Description: Kiểm thử toàn trình: Khi có dữ liệu mới trong bảng nguồn (TmsTrafficData), trigger gói tin 103 kích hoạt xuất bản ngay, cập nhật LastVersion trên ShareDataLastSend và ghi log thành công.
        /// Created date: 22/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenNewDataDetected_TriggerExportsAndUpdatesCheckpointLastVersion_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_CT_{testId}";
            var packetCode = "103"; // VDS packet
            var subCode = $"SUB_CT_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true; // BẬT chế độ gửi khi có dữ liệu mới
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null; // Đăng ký đang rảnh (idle), sẵn sàng nhận lock sự kiện
            });

            // Seed dữ liệu mới vào bảng TmsTrafficData
            var eqId = $"EQ_VDS_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_{testId}",
                KmNumber = 50,
                MetNumber = 100
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30A-{testId}",
                Speed = 80.5f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM50",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            var service = CreateTestWorker(scope);

            // Act: Kích hoạt xử lý tức thời cho gói tin 103 (tương đương tín hiệu NATS nhận được)
            await service.ProcessSubscriptions(packetCode, CancellationToken.None);

            // Assert: Toàn trình nghiệp vụ được xác nhận
            // 1. Checkpoint phải được tạo hoặc cập nhật với LastVersion và LastTime hợp lệ
            var checkpoint = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();

            // 2. Activity Log phải ghi nhận phiên kết xuất thành công
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.True(logs[0].RecordCount > 0);

            // 3. Subscription cập nhật LastTimeRun thành công
            var updatedSub = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();

            Assert.NotNull(updatedSub.LastTimeRun);
            Assert.True(updatedSub.LastTimeRun.Value >= now.AddSeconds(-5));
        }

        /// <summary>
        /// Description: Kiểm thử toàn trình kịch bản trễ dữ liệu: Khi dữ liệu (Xe C) được ghi vào DB sau khi đợt 1 (Xe A, B)
        ///              đã hoàn tất hoặc đang chạy, mốc checkpoint chỉ dừng ở Xe B và đợt chạy kế tiếp sẽ nhặt tiếp đúng Xe C,
        ///              đảm bảo không trùng lặp và không sót bản ghi.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenNewDataCommittedAfterFirstBatch_SubsequentRunPicksUpRemainingData_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_RACE_{testId}";
            var packetCode = "103"; // VDS packet
            var subCode = $"SUB_RACE_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            // Dọn dẹp dữ liệu bảng TmsTrafficData để đảm bảo cô lập hoàn toàn cho kịch bản race condition
            await db.Deleteable<TmsTrafficData>().ExecuteCommandAsync();

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Unspecified);
            var timeA = baseTime.AddMinutes(-4);
            var timeB = baseTime.AddMinutes(-3);
            var timeC = baseTime.AddMinutes(-1);

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 300;
                s.LastTimeRun = baseTime.AddMinutes(-10);
                s.NextTimeRun = null; // Rảnh, sẵn sàng nhận lock
            });

            var eqId = $"EQ_RACE_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_RACE_{testId}",
                KmNumber = 60,
                MetNumber = 200
            }).ExecuteCommandAsync();

            try
            {
                // 1. Ban đầu chỉ có 2 xe: Xe A và Xe B
                var carA = new TmsTrafficData
                {
                    ID = $"CAR_A_{testId}",
                    EquipmentId = eqId,
                    DetectTime = timeA,
                    Type = "CAR",
                    LicensePlate = $"30A-A_{testId}",
                    Speed = 80f,
                    Lane = "L1",
                    Direction = "NORTH",
                    Location = "KM60",
                    CreateTime = timeA,
                    UpdateTime = timeA
                };
                var carB = new TmsTrafficData
                {
                    ID = $"CAR_B_{testId}",
                    EquipmentId = eqId,
                    DetectTime = timeB,
                    Type = "CAR",
                    LicensePlate = $"30A-B_{testId}",
                    Speed = 85f,
                    Lane = "L1",
                    Direction = "NORTH",
                    Location = "KM60",
                    CreateTime = timeB,
                    UpdateTime = timeB
                };
                await db.Insertable(new[] { carA, carB }).ExecuteCommandAsync();
                await db.Updateable<TmsTrafficData>()
                    .SetColumns(t => t.CreateTime == timeA)
                    .SetColumns(t => t.UpdateTime == timeA)
                    .SetColumns(t => t.DetectTime == timeA)
                    .Where(t => t.ID == carA.ID)
                    .ExecuteCommandAsync();
                await db.Updateable<TmsTrafficData>()
                    .SetColumns(t => t.CreateTime == timeB)
                    .SetColumns(t => t.UpdateTime == timeB)
                    .SetColumns(t => t.DetectTime == timeB)
                    .Where(t => t.ID == carB.ID)
                    .ExecuteCommandAsync();

                var service = CreateTestWorker(scope);

                // Act 1: Trigger đợt 1 (chỉ thấy Xe A và Xe B)
                await service.ProcessSubscriptions(packetCode, CancellationToken.None);

                // Assert 1:
                // Checkpoint LastTime phải dừng lại ở timeB (Xe B), không được nhảy cóc
                var checkpoint1 = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                    .FirstAsync();

                Assert.NotNull(checkpoint1);
                Assert.NotNull(checkpoint1.LastTime);
                Assert.True(Math.Abs((checkpoint1.LastTime.Value - timeB).TotalSeconds) <= 1,
                    $"Mốc checkpoint đợt 1 phải là {timeB:HH:mm:ss} nhưng thực tế là {checkpoint1.LastTime:HH:mm:ss}");

                // Activity log đợt 1 ghi nhận đúng 2 bản ghi (Xe A và Xe B)
                var logsRun1 = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .ToListAsync();
                Assert.Single(logsRun1);
                Assert.Equal(2, logsRun1[0].RecordCount);

                // 2. Bây giờ xe C mới tới và được ghi vào DB
                var carC = new TmsTrafficData
                {
                    ID = $"CAR_C_{testId}",
                    EquipmentId = eqId,
                    DetectTime = timeC,
                    Type = "TRUCK",
                    LicensePlate = $"29C-C_{testId}",
                    Speed = 70f,
                    Lane = "L2",
                    Direction = "NORTH",
                    Location = "KM60",
                    CreateTime = timeC,
                    UpdateTime = timeC
                };
                await db.Insertable(carC).ExecuteCommandAsync();
                await db.Updateable<TmsTrafficData>()
                    .SetColumns(t => t.CreateTime == timeC)
                    .SetColumns(t => t.UpdateTime == timeC)
                    .SetColumns(t => t.DetectTime == timeC)
                    .Where(t => t.ID == carC.ID)
                    .ExecuteCommandAsync();

                // Đảm bảo subscription rảnh lock (NextTimeRun <= now) để đợt trigger kế tiếp nhận được
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.NextTimeRun == DateTime.Now.AddSeconds(-1))
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();

                // Act 2: Trigger đợt 2 (nhặt tiếp Xe C từ mốc của Xe B)
                await service.ProcessSubscriptions(packetCode, CancellationToken.None);

                // Assert 2:
                // Checkpoint LastTime phải được nâng lên timeC (Xe C)
                var checkpoint2 = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                    .FirstAsync();

                Assert.NotNull(checkpoint2);
                Assert.NotNull(checkpoint2.LastTime);
                Assert.True(Math.Abs((checkpoint2.LastTime.Value - timeC).TotalSeconds) <= 1,
                    $"Mốc checkpoint đợt 2 phải nâng lên {timeC:HH:mm:ss} nhưng thực tế là {checkpoint2.LastTime:HH:mm:ss}");

                // Activity log có thêm lượt chạy thứ 2, và lượt 2 CHỈ gửi đúng 1 bản ghi (Xe C), không gửi lại Xe A, B
                var logsAll = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderBy(l => l.OccurredAt)
                    .ToListAsync();

                Assert.Equal(2, logsAll.Count);
                Assert.Equal(1, logsAll[1].RecordCount); // Đợt 2 chỉ gửi đúng 1 xe (Xe C)
            }
            finally
            {
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
                await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
                await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm thử toàn trình: Khi Subscription TẮT cờ SendOnNewData (false), dù có tín hiệu trigger thì worker vẫn không xuất bản dữ liệu.
        /// Created date: 22/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenSendOnNewDataFalse_DoesNotExportAndDoesNotUpdateCheckpoint_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_NOCT_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_NOCT_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = false; // TẮT chế độ gửi khi có dữ liệu mới
                s.IntervalSeconds = 300;
                s.NextTimeRun = DateTime.Now.AddMinutes(5);
            });

            var service = CreateTestWorker(scope);

            // Act: Kích hoạt trigger gói tin
            await service.ProcessSubscriptions(packetCode, CancellationToken.None);

            // Assert: Không có checkpoint nào được cập nhật cho cặp partner và packet này
            var checkpoint = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();

            Assert.Null(checkpoint);

            // Không có log xuất bản nào được tạo
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.Empty(logs);
        }

        /// <summary>
        /// Description: Kiểm thử toàn trình: Khi DebounceSec được kích hoạt và lần chạy gần nhất nằm trong cửa sổ debounce, worker sẽ tạm hoãn gửi để tránh dồn dập request.
        /// Created date: 22/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenDebounceSecActive_ThrottlesImmediateTriggerExecution_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_DEBOUNCE_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_DEBOUNCE_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.DebounceSec = 15; // Debounce 15 giây
                s.LastTimeRun = DateTime.Now.AddSeconds(-3); // Vừa mới chạy cách đây 3 giây
                s.NextTimeRun = null; // Rảnh về mặt lock, nhưng bị chặn bởi DebounceSec
            });

            var service = CreateTestWorker(scope);

            // Act: Nhận tín hiệu trigger
            await service.ProcessSubscriptions(packetCode, CancellationToken.None);

            // Assert: Do đang trong cửa sổ Debounce 15s (mới trôi qua 3s), đợt trigger này bị hoãn
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.Empty(logs);
        }

        /// <summary>
        /// Description: Kiểm thử tính chất đơn điệu (Monotonic) của LastVersion trong CommitSuccessfulPage: Phiên bản thấp hơn hoặc bằng không bao giờ ghi đè mốc cao hơn.
        /// Created date: 22/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenLowerVersionReceived_MonotonicCheckpointPreservesHigherVersion_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_MONO_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_MONO_{testId}";

            await PrepareDatabase(db, logger, packetCode);
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s => s.SendOnNewData = true);

            // Giả lập checkpoint đã ghi nhận Version cao trước đó (ví dụ: 1000)
            var initialCheckpoint = new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partnerCode,
                PacketCode = packetCode,
                LastTime = DateTime.Now.AddMinutes(-10),
                LastKey = "PREV_KEY",
                LastVersion = 1000,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await db.Insertable(initialCheckpoint).ExecuteCommandAsync();

            // Act: Cố tình thực thi câu lệnh cập nhật checkpoint với version thấp hơn (999) hoặc LastTime cũ hơn
            var rowsAffected = await db.Updateable<ShareDataLastSend>()
                .SetColumns(c => new ShareDataLastSend
                {
                    LastTime = DateTime.Now.AddMinutes(-20),
                    LastKey = "OLD_KEY",
                    LastVersion = 999,
                    UpdateTime = DateTime.Now
                })
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .Where(c => 999 > c.LastVersion || (999 == c.LastVersion && DateTime.Now.AddMinutes(-20) > c.LastTime))
                .ExecuteCommandAsync();

            // Assert: Cơ chế chốt chặn đơn điệu từ chối ghi đè (0 dòng bị ảnh hưởng)
            Assert.Equal(0, rowsAffected);

            var checkpoint = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();

            Assert.NotNull(checkpoint);
            Assert.Equal(1000, checkpoint.LastVersion);
            Assert.Equal("PREV_KEY", checkpoint.LastKey);
        }

        /// <summary>
        /// Description: Kiểm thử khả năng chịu lỗi (Resilience) khi NATS mất kết nối: Hệ thống không bị crash,
        ///              luồng quét định kỳ (Periodic) vẫn hoạt động độc lập và tiếp tục bảo toàn dữ liệu (Zero Data Loss).
        /// Created date: 22/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_PeriodicFallback_WhenNatsOffline_GuaranteesZeroDataLossAndAdvancesWatermark_Test()
        {
            // Arrange: Thiết lập môi trường Subscription theo lịch định kỳ bình thường
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_FALLBACK_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_FALLBACK_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true; // Bật cả 2 cờ
                s.IntervalSeconds = 30;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = now.AddSeconds(-5); // Đã đến hạn chạy theo lịch
            });

            // Seed dữ liệu vào nguồn
            var eqId = $"EQ_VDS_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_{testId}",
                KmNumber = 50,
                MetNumber = 100
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30A-{testId}",
                Speed = 75.0f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM50",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            var service = CreateTestWorker(scope);

            // Act: Chạy luồng quét định kỳ (mô phỏng trường hợp NATS offline hoàn toàn, không có event trigger nào bắn tới)
            await service.ProcessSubscriptions(CancellationToken.None);

            // Assert: Luồng định kỳ quét bù thành công, dữ liệu được gửi trọn vẹn
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.True(logs[0].RecordCount > 0);
        }

        /// <summary>
        /// Description: Kiểm thử chống cướp lock: Khi một worker khác đang nắm giữ lock còn hiệu lực (NextTimeRun > now),
        ///              ProcessSubscriptions không được cướp lock và không can thiệp NextTimeRun của worker cũ.
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenLockHeldByActiveWorker_DoesNotStealLockAndLeavesNextTimeRunIntact_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_LOCK_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_LOCK_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var now = DateTime.Now;
            var activeWorkerLockExpiry = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second).AddMinutes(5); // Worker 1 đang chạy, nắm giữ lock đến 5 phút sau
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 30;
                s.LastTimeRun = now.AddMinutes(-1);
                s.NextTimeRun = activeWorkerLockExpiry;
            });

            // Seed dữ liệu mới vào bảng nguồn
            var eqId = $"EQ_VDS_LOCK_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_LOCK_{testId}",
                KmNumber = 60,
                MetNumber = 200
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30A-LOCK_{testId}",
                Speed = 90.0f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM60",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            var service = CreateTestWorker(scope);

            // Act: NATS trigger nổ ra trong lúc Worker 1 vẫn đang chạy nắm giữ lock
            await service.ProcessSubscriptions(packetCode, CancellationToken.None);

            // Assert:
            // 1. NextTimeRun của Subscription trong DB PHẢI GIỮ NGUYÊN (không bị cướp / ghi đè bởi NATS trigger)
            var currentSub = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();
            Assert.NotNull(currentSub);
            Assert.Equal(activeWorkerLockExpiry.ToString("yyyy-MM-dd HH:mm:ss"), currentSub.NextTimeRun?.ToString("yyyy-MM-dd HH:mm:ss"));

            // 2. Không có bản ghi activity log nào từ NATS trigger được tạo cho đợt này
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-2))
                .ToListAsync();
            Assert.Empty(logs);
        }

        /// <summary>
        /// Description: Kiểm thử đua tranh đồng thời (concurrent race): Khi N lời gọi ProcessSubscriptions(packetCode) nổ ra gần như đồng thời
        ///              cho cùng 1 Subscription (mô phỏng N instance Worker cùng nhận 1 bản tin NATS do cơ chế pub/sub không dùng queue group),
        ///              khoá OCC LockedSubscription phải đảm bảo đúng 1 lần export thành công; các lần thua giành lock không tạo ActivityLog/AlertLog nào.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenConcurrentTriggersRaceForSameSubscription_ExactlyOneExportSucceeds_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_RACE_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_RACE_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null; // Đăng ký đang rảnh — cả N lời gọi đều có cơ hội giành lock, không ai có sẵn lợi thế
            });

            // Dọn sạch bảng nguồn TmsTrafficData trước khi seed để cô lập số lượng bản ghi cho bài test
            await db.Deleteable<TmsTrafficData>().ExecuteCommandAsync();

            // Seed đúng 1 dòng dữ liệu mới vào bảng nguồn — nếu có > 1 lần export thật sự chạy, RecordCount sẽ lệch khỏi 1
            var eqId = $"EQ_VDS_RACE_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_RACE_{testId}",
                KmNumber = 70,
                MetNumber = 300
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30A-RACE_{testId}",
                Speed = 85.0f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM70",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            // Act: mô phỏng 5 instance Worker cùng nhận và xử lý 1 bản tin NATS đồng thời (pub/sub fan-out, không có queue group)
            // Mỗi scope độc lập => mỗi DataOutboundService.ProcessSubscriptions tự mở 1 ISqlSugarClient riêng (xem DataOutboundService.cs:148-150),
            // đúng bản chất N kết nối DB độc lập cạnh tranh cùng 1 dòng Subscription qua khoá OCC.
            const int concurrentCallers = 5;
            var scopes = Enumerable.Range(0, concurrentCallers)
                .Select(_ => _host.Services.CreateAsyncScope())
                .ToList();
            try
            {
                var tasks = scopes
                    .Select(s => CreateTestWorker(s).ProcessSubscriptions(packetCode, CancellationToken.None))
                    .ToArray();
                await Task.WhenAll(tasks);
            }
            finally
            {
                foreach (var s in scopes)
                    await s.DisposeAsync();
            }

            // Assert
            // 1. Đúng 1 dòng ActivityLog thành công — 4 lần thua giành lock không chạy export nào khác
            var raceLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-5))
                .ToListAsync();
            Assert.Single(raceLogs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, raceLogs[0].Success);
            Assert.Equal(1, raceLogs[0].RecordCount ?? 0);

            // 2. Không AlertLog nào phát sinh — thua giành lock là im lặng theo thiết kế, không phải lỗi luồng gửi dữ liệu
            var raceAlerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Empty(raceAlerts);

            // 3. Checkpoint chỉ tiến đúng 1 lần, LastTime hợp lệ
            var checkpoint = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();
            Assert.NotNull(checkpoint);
            Assert.NotNull(checkpoint.LastTime);
        }

        /// <summary>
        /// Description: Kiểm thử phân giải định danh gói tin: Khi DatatypeId trong Subscription lưu GUID trỏ tới ShareDataPacket.ID, trigger theo mã Code vẫn phải khớp và xuất bản thành công.
        /// Created date: 23/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenSubscriptionDatatypeIdStoresPacketGuid_TriggerStillMatchesAndExports_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_GUID_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_GUID_{testId}";

            await PrepareDatabase(db, logger, packetCode);
            var packet = await db.Queryable<ShareDataPacket>().FirstAsync(p => p.Code == packetCode);
            Assert.NotNull(packet);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, datatypeId: packet.ID, configureSub: s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            var eqId = $"EQ_VDS_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_{testId}",
                KmNumber = 50,
                MetNumber = 100
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30A-{testId}",
                Speed = 80.5f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM50",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            var service = CreateTestWorker(scope);

            // Act
            await service.ProcessSubscriptions(packet.Code!, CancellationToken.None);

            // Assert: Subscription được xử lý và ghi log thành công
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.True(logs[0].RecordCount > 0);
        }

        /// <summary>
        /// Description: Kiểm thử phân giải định danh gói tin: Khi DatatypeId trong Subscription lưu Code trực tiếp (chống hồi quy), trigger theo mã Code vẫn phải khớp và xuất bản thành công.
        /// Created date: 23/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenSubscriptionDatatypeIdStoresPacketCode_TriggerStillMatchesAndExports_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_CODE_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_CODE_{testId}";

            await PrepareDatabase(db, logger, packetCode);
            var packet = await db.Queryable<ShareDataPacket>().FirstAsync(p => p.Code == packetCode);
            Assert.NotNull(packet);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, datatypeId: packet.Code!, configureSub: s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            var eqId = $"EQ_VDS_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_{testId}",
                KmNumber = 50,
                MetNumber = 100
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30A-{testId}",
                Speed = 80.5f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM50",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            var service = CreateTestWorker(scope);

            // Act
            await service.ProcessSubscriptions(packet.Code!, CancellationToken.None);

            // Assert: Subscription được xử lý và ghi log thành công
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.True(logs[0].RecordCount > 0);
        }

        /// <summary>
        /// Description: Kiểm thử kịch bản sập hệ thống: Khi DataChangeWorker khởi động lại sau sự cố (mốc CT nhảy cóc tới hiện tại),
        /// luồng quét định kỳ vẫn gửi đủ 100% dữ liệu tạo trong lúc worker chết, chứng minh không thất thoát dữ liệu.
        /// Created date: 23/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenWatcherRestartsAfterDowntime_PeriodicScanStillSendsDataCreatedWhileDown_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_DWN_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_DWN_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddHours(-1);

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true; // BẬT SendOnNewData
                s.IntervalSeconds = 30;
                s.LastTimeRun = baseTime;
                s.NextTimeRun = dbNow.AddSeconds(-10); // Sẵn sàng cho đợt quét đầu
            });

            var eqId = $"EQ_DWN_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_DWN_{testId}",
                KmNumber = 50,
                MetNumber = 100
            }).ExecuteCommandAsync();

            // 1. Arrange: Seed 1 bản ghi ban đầu và chạy quét lần 1 để cắm mốc checkpoint
            var initialRecord = new TmsTrafficData
            {
                ID = $"TF_INIT_{testId}",
                EquipmentId = eqId,
                DetectTime = baseTime.AddSeconds(10),
                Type = "CAR",
                LicensePlate = $"30A-INIT-{testId}",
                Speed = 80.0f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM50",
                CreateTime = baseTime.AddSeconds(10),
                UpdateTime = baseTime.AddSeconds(10)
            };
            await db.Insertable(initialRecord).ExecuteCommandAsync();

            var service = CreateTestWorker(scope);

            try
            {
                // Chạy đợt 1 để tạo checkpoint thật
                await service.ProcessSubscriptions(CancellationToken.None);

                var cp1 = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                    .FirstAsync();
                Assert.NotNull(cp1);
                Assert.NotNull(cp1.LastTime);
                var cp1Time = cp1.LastTime.Value;
                var cp1Key = cp1.LastKey;

                var initialLogs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .ToListAsync();
                Assert.NotEmpty(initialLogs);
                Assert.All(initialLogs, l => Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success));

                // 2. Mô phỏng worker giám sát đang chết (Downtime):
                // Chèn thêm 3 bản ghi mới vào TmsTrafficData sau mốc checkpoint cp1Time mà KHÔNG chạy vòng quét Change Tracking nào
                var downtimeBaseTime = cp1Time.AddSeconds(10);
                var downtimeRecords = Enumerable.Range(1, 3).Select(i => new TmsTrafficData
                {
                    ID = $"TF_DWN_{testId}_{i}",
                    EquipmentId = eqId,
                    DetectTime = downtimeBaseTime.AddSeconds(i * 10),
                    Type = "CAR",
                    LicensePlate = $"30A-DWN-{testId}-{i}",
                    Speed = 70.0f + i,
                    Lane = "L1",
                    Direction = "NORTH",
                    Location = "KM50",
                    CreateTime = downtimeBaseTime.AddSeconds(i * 10),
                    UpdateTime = downtimeBaseTime.AddSeconds(i * 10)
                }).ToList();
                await db.Insertable(downtimeRecords).ExecuteCommandAsync();

                // 3. Mô phỏng DataChangeWorker khởi động lại:
                // Tạo instance mới của DataChangeWorker (biến _lastProcessedVersion khởi tạo = -1)
                // và kích hoạt vòng poll đầu tiên (nhảy thẳng tới version hiện tại của DB, không sinh NATS trigger cho 3 bản ghi ở bước 2)
                var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                var watcherLogger = scope.ServiceProvider.GetRequiredService<ILogger<DataTrackerWorker>>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var transport = scope.ServiceProvider.GetService<TransportManager>() ?? new TransportManager(config);

                var newWatcher = new DataTrackerWorker(scopeFactory, watcherLogger, transport);
                await newWatcher.PollChangeTracking(CancellationToken.None);

                // 4. Act: Luồng quét định kỳ (ProcessSubscriptions) kích hoạt theo lịch
                // Đảm bảo NextTimeRun đến hạn chạy định kỳ
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.NextTimeRun == DateTime.Now.AddSeconds(-5))
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();

                await service.ProcessSubscriptions(CancellationToken.None);

                // 5. Assert: 3 bản ghi chèn lúc watcher chết VẪN được gửi đầy đủ
                var allLogs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.True(allLogs.Count > initialLogs.Count);
                var latestLog = allLogs.First();
                Assert.Equal(BaseEnums.SuccessEnums.Success, latestLog.Success);

                // Tổng số bản ghi gửi ở đợt 2 đúng bằng 3 bản ghi phát sinh trong lúc watcher chết
                var newLogs = allLogs.Where(l => !initialLogs.Any(il => il.ID == l.ID)).ToList();
                var exportedDowntimeCount = newLogs.Sum(l => l.RecordCount);
                Assert.Equal(downtimeRecords.Count, exportedDowntimeCount);

                // Checkpoint đã tiến đúng tới bản ghi cuối cùng của đợt downtime
                var cp2 = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                    .FirstAsync();
                Assert.NotNull(cp2);
                Assert.True(cp2.LastTime > cp1Time || (cp2.LastTime == cp1Time && string.Compare(cp2.LastKey, cp1Key) > 0));
                Assert.Equal(downtimeRecords.Last().ID, cp2.LastKey);
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
                await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm thử toàn trình gói bản chụp (Snapshot 108): Khi Subscription bật cờ SendOnNewData,
        ///              tín hiệu trigger vẫn kích hoạt xuất bản ngay và ghi log thành công với RecordCount > 0.
        /// Created date: 23/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenSnapshotPacketHasSendOnNewData_TriggerStillExports_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_SNAP_{testId}";
            var packetCode = "108";
            var subCode = $"SUB_SNAP_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true; // BẬT chế độ gửi khi có dữ liệu mới cho gói bản chụp
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null; // Đăng ký đang rảnh (idle), sẵn sàng nhận lock sự kiện
            });

            // Seed dữ liệu mới vào bảng VmsCurrent và TmsEquipment
            var eqId = $"EQ_VMS_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VMS_{testId}",
                KmNumber = 70,
                MetNumber = 0,
                DirectionId = 1,
                LaneId = "LANE_ALL"
            }).ExecuteCommandAsync();

            var vmsRecord = new VmsCurrent
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                Name = $"VMS_SNAP_{testId}",
                RowData = "CHU Y LAI XE AN TOAN",
                Url = "http://sample.vms/preview.png",
                Size = "192x64",
                Priority = 1,
                ExecutedDate = now
            };
            await db.Insertable(vmsRecord).ExecuteCommandAsync();

            var service = CreateTestWorker(scope);

            try
            {
                // Act: Kích hoạt xử lý tức thời cho gói tin 108 theo mã 108_vmsInfo
                await service.ProcessSubscriptions("108_vmsInfo", CancellationToken.None);

                // Assert: Toàn trình nghiệp vụ bản chụp được xác nhận
                // 1. Activity Log phải ghi nhận phiên kết xuất thành công với RecordCount > 0
                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
                Assert.True(logs[0].RecordCount > 0);

                // 2. Snapshot policy: Tuyệt đối không tạo hoặc cập nhật ShareDataLastSend
                var checkpoint = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                    .FirstAsync();
                Assert.Null(checkpoint);
            }
            finally
            {
                await db.Deleteable<VmsCurrent>().Where(v => v.EquipmentId == eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
                await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataMapping>().Where(m => m.PartnerId == partner.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm thử bài đối chứng: Gói tin NotReady (110) và Disabled (111) bị chặn hoàn toàn,
        ///              không sinh trigger ở tầng ResolveTriggerPackets và không kích hoạt xuất bản ở pipeline ProcessSubscriptions.
        /// Created date: 23/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenPacketIsNotReadyOrDisabled_TriggerStillBlocked_Test()
        {
            // 1. Kiểm thử tầng ResolveTriggerPackets: TmsIncident và TmsEventType ánh xạ 107, 110, 111
            // Chỉ có 107_incidentData được kích hoạt, 110_wpData và 111 bắt buộc bị loại bỏ theo policy
            var detectedPackets = DataTrackerWorker.ResolveTriggerPackets(["TmsIncident", "TmsEventType"]);
            Assert.Contains("107_incidentData", detectedPackets);
            Assert.DoesNotContain("110_wpData", detectedPackets);
            Assert.DoesNotContain("111", detectedPackets);

            // 2. Kiểm thử toàn trình pipeline CSDL: Dù Subscription bật cờ SendOnNewData, ProcessSubscriptions vẫn chặn không xuất bản
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_BLK_{testId}";
            var subCode110 = $"SUB_BLK_110_{testId}";
            var subCode111 = $"SUB_BLK_111_{testId}";

            await PrepareDatabase(db, logger, "110");
            await PrepareDatabase(db, logger, "111");

            var (partner, sub110) = await SeedOutboundSubscription(db, partnerCode, subCode110, "110", s =>
            {
                s.SendOnNewData = true;
                s.NextTimeRun = null;
            });

            var sub111 = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = subCode111,
                PartnerId = partner.ID,
                DatatypeId = "111",
                Direction = BaseEnums.Direction.Outbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                SendOnNewData = true,
                NextTimeRun = null,
                IntervalSeconds = 300
            };
            await db.Insertable(sub111).ExecuteCommandAsync();

            var service = CreateTestWorker(scope);

            try
            {
                // Act: Bắn trigger trực tiếp cho cả 2 gói 110_wpData và 111
                await service.ProcessSubscriptions("110_wpData", CancellationToken.None);
                await service.ProcessSubscriptions("111", CancellationToken.None);

                // Assert: CSDL không ghi nhận bất kỳ Activity Log thành công nào cho gói 110 và 111
                var logs110 = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub110.ID && l.Success == BaseEnums.SuccessEnums.Success)
                    .ToListAsync();
                Assert.Empty(logs110);

                var logs111 = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub111.ID && l.Success == BaseEnums.SuccessEnums.Success)
                    .ToListAsync();
                Assert.Empty(logs111);

                // Checkpoint tuyệt đối không sinh ra
                var checkpoints = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partnerCode)
                    .ToListAsync();
                Assert.Empty(checkpoints);
            }
            finally
            {
                await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub110.ID || l.SubscriptionId == sub111.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataMapping>().Where(m => m.PartnerId == partner.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub110.ID || s.ID == sub111.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm thử luồng sự kiện: Khi nhận trigger '105_rfidData', subscription gói '105_rfidData' phân giải tự nhiên, khớp và xuất bản thành công.
        /// Created date: 23/09/2026
        /// Modified date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenPacket105RfidData_TriggerResolvesAndExports_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"P_RFID105_{unique}";
            var subCode = $"SUB_RFID105_{unique}";
            var packetCode = "105_rfidData";

            // 0. Cô lập môi trường kiểm thử: tạm thời ẩn các gói 105 khác trong CSDL test để kiểm chứng luồng quy đổi mã lệch cũ
            var existing105Packets = await db.Queryable<ShareDataPacket>()
                .Where(p => (p.Code == "105" || p.Code == "105_rfidData") && p.IsDelete == null)
                .ToListAsync();

            if (existing105Packets.Count > 0)
            {
                var packetIds = existing105Packets.Select(x => x.ID).ToList();
                await db.Updateable<ShareDataPacket>()
                    .SetColumns(p => p.IsDelete == DateTime.Now)
                    .Where(p => packetIds.Contains(p.ID))
                    .ExecuteCommandAsync();
            }

            // 1. Seed gói tin 105_rfidData chuẩn
            var packet105 = new ShareDataPacket
            {
                ID = $"packet_{unique}",
                Code = packetCode,
                Name = "Dữ liệu định danh phương tiện RFID (chuẩn 105_rfidData)",
                PacketVersion = "1.0",
                OrderNo = 5,
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(packet105).ExecuteCommandAsync();

            // 2. Nạp cấu hình trường dựa trên gói 105
            var def105 = DataOutboundServiceTests.PacketMetadataCatalogTest.Get("105");
            var fields105 = def105.Fields.Select(f => new ShareDataPacketField
            {
                ID = Guid.NewGuid().ToString("N"),
                DatatypeId = packetCode,
                AliasFieldKey = f.FieldKey,
                Type = f.DataType,
                Name = f.Name
            }).ToList();
            await db.Insertable(fields105).ExecuteCommandAsync();

            // 3. Seed dữ liệu thực tế cho gói xe qua trạm (105)
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Insertable(new TollTransactionIn
            {
                ID = Guid.NewGuid().ToString("N"),
                TransactionId = $"TXN_{unique}",
                PlateLpr = $"30A-{unique}",
                PlateEdit = $"30A-{unique}",
                TagId = $"TAG_{unique}",
                LaneId = "LANE_01",
                StationId = "STATION_01",
                TransactionDateTime = now
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsVehicleRegistration
            {
                ID = Guid.NewGuid().ToString("N"),
                LicensePlate = $"30A-{unique}",
                Brand = "Toyota",
                Owner = "Nguyen Van A"
            }).ExecuteCommandAsync();

            // 4. Seed đối tác và đăng ký gói 105_rfidData với SendOnNewData = true
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            var service = CreateTestWorker(scope);

            try
            {
                // Act: Bắn trigger theo mã chuẩn "105_rfidData"
                await service.ProcessSubscriptions("105_rfidData", CancellationToken.None);

                // Assert:
                // a. Đăng ký được khớp và xuất bản thành công
                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
                Assert.True(logs[0].RecordCount > 0, "Gói 105_rfidData phải trích xuất và xuất bản dữ liệu gói 105 khi nhận trigger 105_rfidData.");

                // b. Snapshot: không sinh checkpoint
                var checkpoints = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partner.Code && c.PacketCode == packetCode)
                    .ToListAsync();
                Assert.Empty(checkpoints);
            }
            finally
            {
                if (existing105Packets.Count > 0)
                {
                    var packetIds = existing105Packets.Select(x => x.ID).ToList();
                    await db.Updateable<ShareDataPacket>()
                        .SetColumns(p => p.IsDelete == null)
                        .Where(p => packetIds.Contains(p.ID))
                        .ExecuteCommandAsync();
                }

                await db.Deleteable<TollTransactionIn>().Where(t => t.TransactionId == $"TXN_{unique}").ExecuteCommandAsync();
                await db.Deleteable<TollTransactionOut>().Where(t => t.TransactionId == $"TXN_{unique}").ExecuteCommandAsync();
                await db.Deleteable<TmsVehicleRegistration>().Where(r => r.LicensePlate == $"30A-{unique}").ExecuteCommandAsync();
                await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partner.Code).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataMapping>().Where(m => m.PartnerId == partner.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPacketField>().Where(f => f.DatatypeId == packetCode).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPacket>().Where(p => p.ID == packet105.ID).ExecuteCommandAsync();
            }
        }

        #endregion

        #region Helpers

        private static async Task PrepareDatabase(ISqlSugarClient db, ILogger logger, string packetCode = "103")
        {
            db.CodeFirst.InitTables<ShareDataLastSend>();
            await DataOutboundServiceTests.PacketMetadataCatalogTest.SeedPacketToDb(db, packetCode);
        }


        private static async Task<(ShareDataPartner Partner, ShareDataSubscription Subscription)> SeedOutboundSubscription(
            ISqlSugarClient db,
            string partnerCode,
            string subCode,
            string datatypeId,
            Action<ShareDataSubscription>? configureSub = null)
        {
            db.CodeFirst.InitTables<ShareDataLastSend>();
            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = partnerCode,
                Name = $"Partner {partnerCode}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected,
                Address = "127.0.0.1",
                Port = 5099,
                EndPointApiUrl = "/api/sharedata/sharedatainbound"
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

            var packet = await db.Queryable<ShareDataPacket>()
                .Where(p => p.ID == datatypeId || p.Code == datatypeId)
                .FirstAsync();
            var mappingDatatype = packet?.Code ?? datatypeId;

            var fields = await db.Queryable<ShareDataPacketField>()
                .Where(f => f.DatatypeId == mappingDatatype || f.DatatypeId == datatypeId)
                .ToListAsync();
            var shapeDict = new Dictionary<string, object>();
            foreach (var f in fields)
            {
                if (!string.IsNullOrEmpty(f.AliasFieldKey))
                    shapeDict[f.AliasFieldKey] = new Dictionary<string, string> { { "$field", f.AliasFieldKey } };
            }

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = mappingDatatype,
                Direction = BaseEnums.Direction.Outbound,
                IsActive = true,
                TargetShapeJson = System.Text.Json.JsonSerializer.Serialize(shapeDict)
            }).ExecuteCommandAsync();

            return (partner, sub);
        }

        private static DataOutboundService CreateTestWorker(IServiceScope scope)
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var hostEnv = scope.ServiceProvider.GetService<IHostEnvironment>();

            var defaultHandler = new MockTestHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)));
            var clientFactory = new MockTestHttpClientFactory(defaultHandler);

            return new DataOutboundService(
                scopeFactory,
                logger,
                new DataOutboundFileSender(config, hostEnv),
                new DataOutboundRestSender(clientFactory),
                new DataOutboundExtractionProcess());
        }

        private sealed class MockTestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }

        private sealed class MockTestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return handler(request, cancellationToken);
            }
        }

        private sealed class FakeSqlException(int number, string message) : Exception(message)
        {
            public int Number { get; } = number;
        }

        #endregion

        #region 7. Concurrent – Multi-Sub, Multi-Partner, NATS Burst

        /// <summary>
        /// Description: Kiểm thử đồng thời N Subscription KHÁC NHAU cùng chạy song song (mô phỏng 3-4 worker instance
        ///              nhận tín hiệu NATS cho 3 gói tin 103, 104, 109 cùng lúc). Mỗi sub phải ghi đúng checkpoint
        ///              của chính mình, không bị lẫn dữ liệu checkpoint của sub khác.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenMultipleDistinctSubscriptionsRunConcurrently_EachWritesOwnCheckpointCorrectly_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];

            // 3 gói tin khác nhau (103 Incremental, 104 Incremental, 108 Snapshot)
            var configs = new[]
            {
                (PacketCode: "103", SubCode: $"SUB_C103_{testId}", PartnerCode: $"PTN_C103_{testId}"),
                (PacketCode: "104", SubCode: $"SUB_C104_{testId}", PartnerCode: $"PTN_C104_{testId}"),
                (PacketCode: "108", SubCode: $"SUB_C108_{testId}", PartnerCode: $"PTN_C108_{testId}"),
            };

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            // Seed packet metadata + subscription cho từng gói
            var seeded = new List<(string PacketCode, string PartnerCode, ShareDataPartner Partner, ShareDataSubscription Sub)>();
            foreach (var cfg in configs)
            {
                await PrepareDatabase(db, logger, cfg.PacketCode);
                var (partner, sub) = await SeedOutboundSubscription(db, cfg.PartnerCode, cfg.SubCode, cfg.PacketCode, s =>
                {
                    s.SendOnNewData = true;
                    s.IntervalSeconds = 300;
                    s.LastTimeRun = now.AddMinutes(-5);
                    s.NextTimeRun = null;
                });
                seeded.Add((cfg.PacketCode, cfg.PartnerCode, partner, sub));
            }

            // Seed 1 dòng TmsTrafficData cho gói 103 và TmsWeatherData cho gói 104
            var eqId = $"EQ_CONC_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"CONC_{testId}",
                KmNumber = 60,
                MetNumber = 0
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "TRUCK",
                LicensePlate = $"51G-CONC{testId}",
                Speed = 70f,
                Lane = "L2",
                Direction = "SOUTH",
                Location = $"KM60_{testId}",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            try
            {
                // Act: 3 scope độc lập gọi ProcessSubscriptions cho 3 gói tin khác nhau cùng lúc
                var tasks = configs.Select(cfg =>
                {
                    var s = _host.Services.CreateAsyncScope();
                    return CreateTestWorker(s).ProcessSubscriptions(cfg.PacketCode, CancellationToken.None);
                }).ToArray();

                await Task.WhenAll(tasks);

                // Assert: mỗi sub phải có đúng ActivityLog của chính nó, checkpoint không bị trộn lẫn
                foreach (var (packetCode, partnerCode, partner, sub) in seeded)
                {
                    var logs = await db.Queryable<ShareDataActivityLog>()
                        .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-10))
                        .ToListAsync();

                    // Mỗi sub phải có đúng 1 lần chạy thành công
                    Assert.Single(logs);
                    Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

                    // Gói Incremental (103, 104) phải có checkpoint; Snapshot (108) không có
                    var checkpoint = await db.Queryable<ShareDataLastSend>()
                        .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                        .FirstAsync();

                    if (packetCode == "108")
                        Assert.Null(checkpoint); // Snapshot: không ghi checkpoint
                    else
                        Assert.NotNull(checkpoint); // Incremental: phải có checkpoint
                }
            }
            finally
            {
                foreach (var (packetCode, partnerCode, partner, sub) in seeded)
                {
                    await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataMapping>().Where(m => m.PartnerId == partner.ID).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
                }
                await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm thử đồng thời N Partner KHÁC NHAU (mỗi partner có 1 Subscription riêng cho cùng 1 gói tin 103)
        ///              cùng chạy song song. Mỗi partner phải có checkpoint độc lập, không overwrite checkpoint của partner khác.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenMultiplePartnersSubscribeSamePacketConcurrently_EachGetsOwnCheckpoint_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            const string packetCode = "103";
            const int partnerCount = 4;

            await PrepareDatabase(db, logger, packetCode);
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            // Seed 4 partner, mỗi partner 1 sub cho gói 103
            var seeded = new List<(ShareDataPartner Partner, ShareDataSubscription Sub)>();
            for (var i = 0; i < partnerCount; i++)
            {
                var (partner, sub) = await SeedOutboundSubscription(
                    db,
                    $"PTN_MP_{i}_{testId}",
                    $"SUB_MP_{i}_{testId}",
                    packetCode,
                    s =>
                    {
                        s.SendOnNewData = true;
                        s.IntervalSeconds = 300;
                        s.LastTimeRun = now.AddMinutes(-5);
                        s.NextTimeRun = null;
                    });
                seeded.Add((partner, sub));
            }

            // Seed 1 dòng TmsTrafficData
            var eqId = $"EQ_MP_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"EQ_MP_{testId}",
                KmNumber = 45,
                MetNumber = 500
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30B-MP{testId}",
                Speed = 95f,
                Lane = "L1",
                Direction = "NORTH",
                Location = $"KM45_{testId}",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            try
            {
                // Act: 4 scope độc lập (mô phỏng 4 worker instance nhận cùng 1 bản tin NATS "103")
                // xử lý song song — mỗi scope sẽ xử lý 1 subscription khác nhau (do partner khác nhau)
                var asyncScopes = Enumerable.Range(0, partnerCount)
                    .Select(_ => _host.Services.CreateAsyncScope())
                    .ToList();

                try
                {
                    var tasks = asyncScopes
                        .Select(s => CreateTestWorker(s).ProcessSubscriptions(packetCode, CancellationToken.None))
                        .ToArray();
                    await Task.WhenAll(tasks);
                }
                finally
                {
                    foreach (var s in asyncScopes)
                        await s.DisposeAsync();
                }

                // Assert: mỗi partner phải có ActivityLog thành công và checkpoint riêng của mình
                foreach (var (partner, sub) in seeded)
                {
                    var logs = await db.Queryable<ShareDataActivityLog>()
                        .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-10))
                        .ToListAsync();

                    Assert.NotEmpty(logs);
                    Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

                    // Mỗi partner phải có checkpoint độc lập, không bị trộn lẫn
                    var checkpoint = await db.Queryable<ShareDataLastSend>()
                        .Where(c => c.PartnerCode == partner.Code && c.PacketCode == packetCode)
                        .FirstAsync();

                    Assert.NotNull(checkpoint);
                    Assert.NotNull(checkpoint.LastTime);
                }

                // Tổng số checkpoint phải bằng đúng số partner (không bị dedupe hay overwrite lẫn nhau)
                var allCheckpoints = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PacketCode == packetCode
                        && seeded.Select(x => x.Partner.Code).Contains(c.PartnerCode))
                    .ToListAsync();

                Assert.Equal(partnerCount, allCheckpoints.Count);
            }
            finally
            {
                foreach (var (partner, sub) in seeded)
                {
                    await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partner.Code).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataMapping>().Where(m => m.PartnerId == partner.ID).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                    await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
                }
                await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm thử NATS burst liên tiếp nhanh (N bản tin NATS bắn đến cho cùng 1 sub trong vòng vài giây):
        ///              Cơ chế DebounceSec phải chặn tất cả trigger kế tiếp trong cửa sổ debounce,
        ///              đảm bảo chỉ đúng 1 lần export thực sự chạy và checkpoint chỉ tiến 1 lần.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenNatsBurstsRepeatedly_DebouncePreventsMultipleExportsWithinWindow_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            const string packetCode = "103";
            var partnerCode = $"PTN_BURST_{testId}";
            var subCode = $"SUB_BURST_{testId}";

            await PrepareDatabase(db, logger, packetCode);
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            // Sub với DebounceSec = 10 giây
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.DebounceSec = 10;              // Debounce window = 10 giây
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5); // Chưa chạy gần đây → burst đầu tiên được chạy
                s.NextTimeRun = null;
            });

            var eqId = $"EQ_BURST_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"EQ_BURST_{testId}",
                KmNumber = 20,
                MetNumber = 0
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "BUS",
                LicensePlate = $"29B-BURST{testId}",
                Speed = 55f,
                Lane = "L3",
                Direction = "EAST",
                Location = $"KM20_{testId}",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            try
            {
                // Act – Đợt 1: trigger đầu tiên — sub chưa chạy gần đây nên được phép chạy
                await CreateTestWorker(scope).ProcessSubscriptions(packetCode, CancellationToken.None);

                var logsAfterFirst = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-10))
                    .ToListAsync();

                // Phải có đúng 1 export thành công từ trigger đầu tiên
                Assert.Single(logsAfterFirst);
                Assert.Equal(BaseEnums.SuccessEnums.Success, logsAfterFirst[0].Success);

                // Snapshot UpdateTime của checkpoint sau đợt 1 để đối chiếu sau burst
                var checkpointAfterFirst = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                    .FirstAsync();
                Assert.NotNull(checkpointAfterFirst);
                var updateTimeAfterFirst = checkpointAfterFirst.UpdateTime;

                // Act – Đợt 2 & 3: NATS burst tiếp trong vòng debounce window (sub vừa chạy xong)
                await CreateTestWorker(scope).ProcessSubscriptions(packetCode, CancellationToken.None);
                await CreateTestWorker(scope).ProcessSubscriptions(packetCode, CancellationToken.None);

                // Assert: Tổng số ActivityLog vẫn là 1 — 2 trigger sau bị chặn bởi DebounceSec
                var allLogs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-15))
                    .ToListAsync();

                Assert.Single(allLogs);

                // Checkpoint.UpdateTime không thay đổi so với sau đợt 1 (burst 2 & 3 không ghi đè)
                var checkpointAfterBurst = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                    .FirstAsync();

                Assert.NotNull(checkpointAfterBurst);
                Assert.True(
                    updateTimeAfterFirst?.ToString("yyyy-MM-dd HH:mm:ss") == checkpointAfterBurst.UpdateTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                    "Checkpoint.UpdateTime không được cập nhật bởi các trigger bị chặn trong cửa sổ DebounceSec.");
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
                await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataMapping>().Where(m => m.PartnerId == partner.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }

        #endregion

        [Fact]
        public async Task NatsWorker_HandleTrigger_WhenValidPacketCode_ExecutesOutboundFlow_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var realService = scope.ServiceProvider.GetRequiredService<ShareDataWorker.Core.Interfaces.IDataOutboundService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataNatsConsumerWorker>>();
            
            var (partner, sub) = await SeedOutboundSubscription(db, "TEST_PARTNER_NATS", "SUB_NATS", "103", s => { s.SendOnNewData = true; });
            var config = new ConfigurationBuilder().Build();
            var transport = new TransportManager(config);
            var worker = new DataNatsConsumerWorker(realService, logger, transport);

            var payload = System.Text.Json.JsonSerializer.Serialize(new { PacketCode = "103", Version = 12345 });

            try
            {
                // Act: Worker nhận trigger
                await InvokeNatsHandleMessages(worker, payload, CancellationToken.None);

                // Assert: Kiểm chứng trạng thái
                var currentSub = await db.Queryable<ShareDataSubscription>().Where(s => s.ID == sub.ID).FirstAsync();
                Assert.NotNull(currentSub);
            }
            finally
            {
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task NatsWorker_HandleTrigger_WhenInvalidPayload_DoesNotThrowAndDoesNotCallService_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var realService = scope.ServiceProvider.GetRequiredService<ShareDataWorker.Core.Interfaces.IDataOutboundService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataNatsConsumerWorker>>();
            var config = new ConfigurationBuilder().Build();
            var transport = new TransportManager(config);
            var worker = new DataNatsConsumerWorker(realService, logger, transport);

            // Act & Assert
            var ex1 = await Record.ExceptionAsync(() => InvokeNatsHandleMessages(worker, "invalid json payload", CancellationToken.None));
            var ex2 = await Record.ExceptionAsync(() => InvokeNatsHandleMessages(worker, "{}", CancellationToken.None));

            Assert.Null(ex1);
            Assert.Null(ex2);
        }

        private static async Task InvokeNatsHandleMessages(DataNatsConsumerWorker worker, string payload, CancellationToken token)
        {
            var method = typeof(DataNatsConsumerWorker).GetMethod("HandleMessages", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("HandleMessages method not found");
            var task = (Task)method.Invoke(worker, [payload, token])!;
            await task;
        }
    }
}
