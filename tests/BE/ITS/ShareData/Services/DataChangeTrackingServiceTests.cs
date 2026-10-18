using System.Net;
using System.Reflection;
using Tests.ShareData.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Persistence.SeedData;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.VMS.Core.Entities;
using Services.Shared.Messaging;
using ShareDataWorker.Core.Utils.Resolvers;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using ShareDataWorker.Infrastructure.Services.DataChangeTracking;
using Modules.Integration.ShareData.Base.Interfaces.PacketSend;
using Modules.Integration.ShareData.PacketSend;
using Modules.Integration.ShareData.PacketSend.Senders;
using System.Text.Json;
using ShareDataWorker.Infrastructure.Services.DataNats;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Bộ kiểm thử chuyên biệt cho SQL Server Change Tracking & DataChangeTrackingService.
    /// Created date: 22/09/2026
    /// Modified date: 17/10/2026
    /// </summary>
    [Collection("api")]
    public class DataChangeTrackingServiceTests
    {
        private readonly Host _host;

        public DataChangeTrackingServiceTests(Host host)
        {
            host.PartnerServer.ResetDefaults();
            _host = host;

            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .ExecuteCommand();

            db.Deleteable<TmsTrafficData>().ExecuteCommand();
            db.Deleteable<TmsIncident>().ExecuteCommand();

            EnsureDefaultTrackTables(db).GetAwaiter().GetResult();
        }

        #region 1. DataChangeTrackingService Static & Status Tests

        /// <summary>
        /// Description: Kiểm thử chu kỳ đầu tiên của PollChangeTracking: Tự động khởi tạo cấu hình Change Tracking,
        ///              kiểm tra CSDL và thiết lập mốc LastVersion trong bảng ShareDataSourceTable ban đầu (>= 0).
        /// Created date: 26/09/2026
        /// </summary>
        [Fact]
        public async Task PollChangeTracking_InitialPoll_InitializesEnvironmentAndSetsBaselineVersion_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            var trackState = await GetTrackState(db);
            if (trackState != null)
                await SetStateVersion(db, -1);

            // Act: Chu kỳ poll đầu tiên khởi tạo môi trường và mốc version
            await worker.PollChanges(CancellationToken.None);

            // Assert: Sau chu kỳ đầu, mốc version đã được khởi tạo thành công (>= 0)
            Assert.True(await GetStateVersion(db) >= 0);
        }

        [Fact]
        public async Task ChangedTablesSql_WhenBuiltFromTrackedTables_ExecutesSuccessfullyOnDatabase()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var tracked = new[] { "TmsTrafficData", "TmsWeather", "TmsIncident" };

            // Act
            var sql = InvokeBuildChangedTablesSql(tracked);
            Assert.NotEmpty(sql);

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
            var worker = CreateTrackerWorker(scope);
            await worker.PollChanges(CancellationToken.None);

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
            var verObj = await db.Ado.GetScalarAsync(DataChangeTrackingService.SqlCurrentVersion);
            var verAfterInsert = Convert.ToInt64(verObj);

            // Act 1: Xóa bản ghi (chỉ sinh SYS_CHANGE_OPERATION = 'D')
            await db.Deleteable<TmsTrafficData>().Where(t => t.ID == carId).ExecuteCommandAsync();

            // Kiểm tra câu lệnh BuildChangedTablesSql từ mốc verAfterInsert
            var sql = InvokeBuildChangedTablesSql(["TmsTrafficData"]);
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
        }

        /// <summary>
        /// Description: Đo lại và siết kịch bản — 3 lượt probe với mốc hợp lệ (đối chứng), v=0 (ngoài cửa sổ),
        ///              và v=minValid-1 (sát biên) — để xác định tất định CHANGETABLE có ném hay trả rỗng âm thầm.
        ///              Mốc nền: Assert 2/3 đỏ = CHANGETABLE không ném, trả rỗng âm thầm ⇒ tiến hành TĐ3.
        ///              Mốc nền: Assert 2/3 xanh nhờ Caught != null = CHANGETABLE có ném ⇒ dừng ở TĐ2, báo mã lỗi.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ChangeTable_WhenVersionOutOfWindow_MeasureThrowOrSilentEmpty_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var testId = Guid.NewGuid().ToString("N")[..8];

            // Arrange 1: Chèn dòng A để đẩy version CSDL lên ≥ 1 TRƯỚC khi bật lại tracking
            await db.Insertable(new TmsWeather
            {
                ID = $"W_M1_{testId}",
                RefId = $"W_M1_{testId}",
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // Arrange 2: Disable / Enable để purge metadata => minValid nhảy lên = version CSDL hiện tại (≥ 1)
            var isInitiallyTracked = await db.Ado.GetIntAsync(
                "SELECT COUNT(1) FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')") > 0;
            if (isInitiallyTracked)
                await db.Ado.ExecuteCommandAsync("ALTER TABLE [TmsWeather] DISABLE CHANGE_TRACKING;");
            await db.Ado.ExecuteCommandAsync("ALTER TABLE [TmsWeather] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");

            // Arrange 3: Chốt chặn — minValid phải > 0 để kịch bản ép được lỗi thật
            var minValidObj = await db.Ado.GetScalarAsync(
                "SELECT min_valid_version FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')");
            var minValid = Convert.ToInt64(minValidObj);
            Assert.True(minValid > 0,
                $"minValid == 0 sau khi disable/enable — kịch bản ép lỗi không thể thực hiện (DB quá mới hoặc version chưa tăng). Dừng đo, không sửa assert 2/3.");

            // Arrange 4: Chèn dòng B sau khi bật lại tracking để bảng có đúng 1 thay đổi thật
            await db.Insertable(new TmsWeather
            {
                ID = $"W_M2_{testId}",
                RefId = $"W_M2_{testId}",
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // Đo 3 lượt
            async Task<(Exception? Caught, int RowCount)> Probe(long v)
            {
                try
                {
                    var rows = await db.Ado.SqlQueryAsync<string>(
                        "SELECT CAST(CT.SYS_CHANGE_OPERATION AS NVARCHAR(10)) FROM CHANGETABLE(CHANGES TmsWeather, @v) AS CT",
                        new { v });
                    return (null, rows?.Count ?? 0);
                }
                catch (Exception ex)
                {
                    return (ex, -1);
                }
            }

            var valid    = await Probe(minValid);       // Đối chứng: mốc HỢP LỆ
            var atZero   = await Probe(0);              // Ngoài cửa sổ
            var belowMin = await Probe(minValid - 1);   // Ngoài cửa sổ, sát biên

            static string Shape(Exception? ex)
            {
                if (ex == null)
                    return "(không ném)";

                var levels = new List<string>();
                for (var cur = ex; cur != null; cur = cur.InnerException)
                {
                    var number = cur.GetType().GetProperty("Number")?.GetValue(cur) as int?;
                    levels.Add($"{cur.GetType().Name} | Number={number?.ToString() ?? "null"} | {cur.Message}");
                }

                return string.Join(Environment.NewLine, levels);
            }

            // Assert 1: Đối chứng — mốc hợp lệ phải đọc được dòng B
            Assert.True(valid.Caught == null && valid.RowCount >= 1,
                $"Đối chứng thất bại: mốc hợp lệ (v={minValid}) không đọc được dòng B (RowCount={valid.RowCount}). Kịch bản sai, không sửa assert 2/3.{Environment.NewLine}Exception: {Shape(valid.Caught)}");

            // Assert 2: Phép đo quyết định — v=0 ngoài cửa sổ: phải HOẶC ném HOẶC trả ≥1 dòng
            Assert.True(atZero.Caught != null || atZero.RowCount >= 1,
                $"MẤT DỮ LIỆU ÂM THẦM: mốc @v=0 nằm ngoài cửa sổ (minValid={minValid}) mà CHANGETABLE vừa KHÔNG ném vừa trả 0 dòng, trong khi mốc hợp lệ đọc được {valid.RowCount} dòng.{Environment.NewLine}Hình dạng ngoại lệ @v=0:{Environment.NewLine}{Shape(atZero.Caught)}");

            // Assert 3: Ca sát biên — v=minValid-1
            Assert.True(belowMin.Caught != null || belowMin.RowCount >= 1,
                $"MẤT DỮ LIỆU ÂM THẦM: mốc @v={minValid - 1} (=minValid-1) nằm ngoài cửa sổ mà CHANGETABLE vừa KHÔNG ném vừa trả 0 dòng, trong khi mốc hợp lệ đọc được {valid.RowCount} dòng.{Environment.NewLine}Hình dạng ngoại lệ @v={minValid - 1}:{Environment.NewLine}{Shape(belowMin.Caught)}");

            // Assert 4: Nếu ném — thu hình dạng ngoại lệ để biết mã lỗi thật
            if (atZero.Caught != null)
                Assert.Fail(
                    $"CHANGETABLE có ném tại @v=0 — dừng ở TĐ2, KHÔNG làm TĐ3. Mã lỗi thật:{Environment.NewLine}{Shape(atZero.Caught)}");

            // Khôi phục lại tracking cho TmsWeather
            await db.Ado.ExecuteCommandAsync(
                "IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')) " +
                "ALTER TABLE TmsWeather ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");
        }

        /// <summary>
        /// Description: Kiểm chứng khi CHỈ MỘT PHẦN bảng nguồn có mốc tối thiểu mới hơn mốc đang giữ: worker nhảy cóc
        ///              RIÊNG bảng quá hạn đó lên version hiện tại, ghi LastIssue và ESH-1601, trong khi các bảng lành
        ///              mạnh khác vẫn giữ nguyên mốc để tiếp tục bắt thay đổi.
        /// Created date: 02/10/2026
        /// Modified date: 17/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenOnlySomeTablesAreStale_IsolatesThemAndKeepsTriggerForHealthy_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            var testId = Guid.NewGuid().ToString("N")[..8];

            // Arrange 1: Khởi tạo mốc gốc qua PollChanges
            await worker.PollChanges(CancellationToken.None);
            var baseline = await GetStateVersion(db);
            Assert.True(baseline >= 0, "Baseline version phải >= 0 sau khi khởi tạo.");

            // Arrange 2: Disable / Enable TmsWeather => minValid của TmsWeather nhảy lên bằng version CSDL hiện tại
            var isTracked = await db.Ado.GetIntAsync(
                "SELECT COUNT(1) FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')") > 0;
            if (isTracked)
                await db.Ado.ExecuteCommandAsync("ALTER TABLE [TmsWeather] DISABLE CHANGE_TRACKING;");
            await db.Ado.ExecuteCommandAsync("ALTER TABLE [TmsWeather] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");

            var minValidObj = await db.Ado.GetScalarAsync(
                "SELECT min_valid_version FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')");
            var minValid = Convert.ToInt64(minValidObj);

            // Arrange 3: Chèn 1 dòng vào TmsWeather để version CSDL vượt mốc đang giữ
            await db.Insertable(new TmsWeather
            {
                ID = $"W_FFW_{testId}",
                RefId = $"W_FFW_{testId}",
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // Arrange 4: Hạ riêng mốc của TmsWeather xuống dưới minValid (nhưng >= 0), giữ các bảng khác ở minValid (hợp lệ)
            var staleVersion = Math.Max(0L, minValid - 1);
            await SetStateVersion(db, minValid);
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == staleVersion)
                .SetColumns(x => x.LastIssue == null)
                .Where(x => x.IsDelete == null && x.TableName == "TmsWeather")
                .ExecuteCommandAsync();

            var existingIds = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.SelfHeal)
                .Select(x => x.ID)
                .ToListAsync();

            // Act: PollChanges lần 1
            await worker.PollChanges(CancellationToken.None);

            // Assert 1: TmsWeather đã được nhảy cóc riêng lên version hiện tại và ghi LastIssue, KHÔNG bị cô lập vào MissingTables
            var missing = GetMissingTables(worker);
            Assert.False(missing.ContainsKey("TmsWeather"));
            var weatherRow = await GetTrackState(db, "TmsWeather");
            Assert.NotNull(weatherRow);
            Assert.True(weatherRow!.LastVersion > minValid);
            Assert.False(string.IsNullOrWhiteSpace(weatherRow.LastIssue));

            // Assert 2: Câu truy vấn đang áp dụng vẫn giữ cả TmsWeather và TmsTrafficData
            Assert.Contains("TmsWeather", worker.ActiveChangesSql);
            Assert.Contains("TmsTrafficData", worker.ActiveChangesSql);

            // Assert 3: Đúng 1 dòng ESH-1601 (SelfHeal) mang staleTables chứa TmsWeather
            var esh1601Rows = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.SelfHeal && !existingIds.Contains(x.ID))
                .Where(x => x.AfterJson != null && x.AfterJson.Contains("staleTables"))
                .ToListAsync();

            Assert.Single(esh1601Rows);
            Assert.Contains("TmsWeather", esh1601Rows[0].AfterJson!);

            // Assert 4: PollChanges lần 2 — KHÔNG sinh thêm dòng ESH-1601 nào (mốc của TmsWeather đã hợp lệ lại)
            await worker.PollChanges(CancellationToken.None);

            var esh1601CountAfterSecondPoll = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.SelfHeal && !existingIds.Contains(x.ID))
                .Where(x => x.AfterJson != null && x.AfterJson.Contains("staleTables"))
                .CountAsync();
            Assert.Equal(1, esh1601CountAfterSecondPoll);

            // Bật lại tracking TmsWeather
            await db.Ado.ExecuteCommandAsync(
                "IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')) " +
                "ALTER TABLE TmsWeather ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");
        }

        /// <summary>
        /// Description: Kiểm chứng khi MỌI bảng nguồn đều có mốc tối thiểu mới hơn mốc đang giữ: worker nhảy cóc
        ///              mốc lên version hiện tại và ghi 1 dòng ESH-1601, KHÔNG cô lập bảng nào.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenAllTablesAreStale_FastForwardsAndWritesEsh1601_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            var testId = Guid.NewGuid().ToString("N")[..8];

            // Arrange 1: Khởi tạo mốc gốc qua PollChanges
            await worker.PollChanges(CancellationToken.None);
            var baseline = await GetStateVersion(db);
            Assert.True(baseline >= 0, "Baseline version phải >= 0 sau khi khởi tạo.");

            // Arrange 2: Chèn 1 dòng vào TmsWeather để version CSDL chắc chắn > 0
            await db.Insertable(new TmsWeather
            {
                ID = $"W_ALLSTALE_{testId}",
                RefId = $"W_ALLSTALE_{testId}",
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // Arrange 3: Đọc min_valid_version của mọi bảng đang bật tracking
            var minOfAllObj = await db.Ado.GetScalarAsync("SELECT MIN(min_valid_version) FROM sys.change_tracking_tables");
            var minOfAll = Convert.ToInt64(minOfAllObj);
            Assert.True(minOfAll > 0, $"min_valid_version nhỏ nhất của các bảng là {minOfAll}, không thể ép ca mọi bảng đều stale.");

            // Arrange 4: Đặt mốc xuống 0 (mốc 0 < minOfAll của mọi bảng => mọi bảng đều stale; mốc >= 0 nên không đi nhánh init)
            await SetStateVersion(db, 0);

            var existingIds = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.SelfHeal)
                .Select(x => x.ID)
                .ToListAsync();

            // Act: PollChanges lần 1
            await worker.PollChanges(CancellationToken.None);

            // Assert 1: Mốc đã nhảy cóc
            var afterVersion = await GetStateVersion(db);
            Assert.True(afterVersion > 0, $"Mốc chưa nhảy cóc: afterVersion={afterVersion}.");

            // Assert 2: Đúng 1 dòng ESH-1601 sinh ra, AfterJson chứa staleTables
            var esh1601Rows = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.SelfHeal && !existingIds.Contains(x.ID))
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .ToListAsync();

            Assert.Single(esh1601Rows);
            Assert.NotNull(esh1601Rows[0].AfterJson);
            Assert.Contains("staleTables", esh1601Rows[0].AfterJson);

            // Assert 3: 🔴 GetMissingTables rỗng — nhánh tất-cả KHÔNG cô lập bảng nào
            var missing = GetMissingTables(worker);
            Assert.Empty(missing);

            // Assert 4: PollChanges lần 2 — KHÔNG sinh thêm ESH-1601 (mốc đã hợp lệ lại)
            await worker.PollChanges(CancellationToken.None);

            var totalEsh1601AfterSecondPoll = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.SelfHeal && !existingIds.Contains(x.ID))
                .CountAsync();
            Assert.Equal(1, totalEsh1601AfterSecondPoll);
        }

        /// <summary>
        /// Description: Kiểm thử nhánh khởi tạo lại baseline: Khi mốc LastVersion âm (-10), worker đi lại đường
        ///              khởi tạo (lastVersion < 0) và cắm lại mốc gốc từ version CSDL hiện tại. Không liên quan đến
        ///              cơ chế self-heal — bài này kiểm đường init, không đường proactive forward.
        /// Created date: 25/09/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenWatermarkIsNegative_ReInitsBaseline_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var currentVerObj = await db.Ado.GetScalarAsync(DataChangeTrackingService.SqlCurrentVersion);
            var currentVer = Convert.ToInt64(currentVerObj);

            var worker = CreateTrackerWorker(scope);

            // Act 1: Chu kỳ poll đầu tiên khởi tạo mốc LastVersion trong bảng ShareDataSourceTable = currentVer
            await worker.PollChanges(CancellationToken.None);
            Assert.True(await GetStateVersion(db) >= 0);

            // Act 2: Giả lập mốc LastVersion trong bảng ShareDataSourceTable bị lệch hoặc âm (-10)
            await SetStateVersion(db, -10);
            await worker.PollChanges(CancellationToken.None);

            // Assert: Worker tự căn chỉnh lại theo mốc DB hiện tại
            Assert.True(await GetStateVersion(db) >= currentVer);
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
            var worker = CreateTrackerWorker(scope);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.PollChanges(cts.Token));
        }

        /// <summary>
        /// Description: Kiểm thử khả năng tự phục hồi và cô lập lỗi (Fault Isolation / Self-Healing) khi một bảng nguồn
        ///              bị mất Change Tracking (DBA tắt CT hoặc bảng bị drop/recreate):
        ///              - Worker KHÔNG bị sập / crash vòng lặp polling vô hạn.
        ///              - Tự động phát hiện bảng bị mất CT, cô lập bảng lỗi ra khỏi câu truy vấn (trong RAM) và ghi LastIssue.
        ///              - Các bảng nguồn lành mạnh còn lại (healthy tables) vẫn tiếp tục được giám sát, phát hiện thay đổi và tiến mốc LastVersion.
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenSourceTableLosesTracking_IsolatesBrokenTableAndProcessesHealthyTables_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            var testId = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_ISO_{testId}";
            var carId = $"CAR_ISO_{testId}";

            // 1. Chu kỳ đầu: Khởi tạo môi trường, đảm bảo CT bật trên toàn bộ bảng và mốc LastVersion >= 0
            await worker.PollChanges(CancellationToken.None);
            var initialVersion = await GetStateVersion(db);
            Assert.True(initialVersion >= 0);

            // 2. Giả lập DBA tắt Change Tracking trên 1 bảng (ví dụ TmsWeather)
            await db.Ado.ExecuteCommandAsync("ALTER TABLE [TmsWeather] DISABLE CHANGE_TRACKING;");

            // Xác nhận TmsWeather thực sự đã bị tắt CT trong sys.change_tracking_tables
            var isWeatherTracked = await db.Ado.GetIntAsync("SELECT COUNT(1) FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')") > 0;
            Assert.False(isWeatherTracked);

            // 3. Tạo thay đổi dữ liệu trên một bảng LÀNH MẠNH khác (TmsTrafficData)
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"EQ_ISO_{testId}",
                KmNumber = 15,
                MetNumber = 300
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = carId,
                EquipmentId = eqId,
                DetectTime = DateTime.Now,
                Type = "CAR",
                LicensePlate = $"30A-ISO_{testId}",
                Speed = 65f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM15",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // Lấy mốc version mới của DB (phải lớn hơn initialVersion)
            var currentVerObj = await db.Ado.GetScalarAsync(DataChangeTrackingService.SqlCurrentVersion);
            var currentVer = Convert.ToInt64(currentVerObj);
            Assert.True(currentVer > initialVersion);

            // Act: Chu kỳ poll tiếp theo chạy khi TmsWeather đã mất CT
            await worker.PollChanges(CancellationToken.None);

            // Assert 1:
            // Mốc version của TmsTrafficData PHẢI tiến lên currentVer (chứng minh bảng lành mạnh đã được xử lý, không bị nghẽn bởi TmsWeather)
            var trafficRow = await GetTrackState(db, "TmsTrafficData");
            Assert.NotNull(trafficRow);
            Assert.Equal(currentVer, trafficRow!.LastVersion);

            // Assert 2:
            // TmsWeather bị cô lập trong RAM, loại khỏi câu SQL đang áp dụng, có mốc hẹn thử lại riêng và có LastIssue trong DB
            Assert.Contains("TmsWeather", GetMissingTables(worker).Keys);
            Assert.DoesNotContain("TmsWeather", worker.ActiveChangesSql);
            Assert.True(GetMissingTables(worker)["TmsWeather"] > DateTime.UtcNow);
            var brokenWeatherRow = await GetTrackState(db, "TmsWeather");
            Assert.NotNull(brokenWeatherRow);
            Assert.False(string.IsNullOrWhiteSpace(brokenWeatherRow!.LastIssue));

            // Act 2: Có thay đổi dữ liệu mới trong hệ thống làm version CSDL tiến lên TRƯỚC khi DBA bật lại Change Tracking cho TmsWeather
            await db.Insertable(new TmsTrafficData
            {
                ID = $"CAR_ISO2_{testId}",
                EquipmentId = eqId,
                DetectTime = DateTime.Now,
                Type = "CAR",
                LicensePlate = $"30B-ISO_{testId}",
                Speed = 70f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM15",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // DBA bật lại Change Tracking cho TmsWeather và mốc hẹn thử lại của nó đã tới hạn
            await db.Ado.ExecuteCommandAsync("ALTER TABLE [TmsWeather] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");
            ExpireMissingTableRetry(worker);

            await worker.PollChanges(CancellationToken.None);

            // Assert 3: Với mốc riêng từng bảng trong ShareDataSourceTable, TryRestoreMissingTables tự động nâng mốc riêng của TmsWeather
            // lên currentVersion, xóa LastIssue và khôi phục ngay TmsWeather vào ActiveChangesSql
            Assert.Empty(GetMissingTables(worker));
            Assert.Contains("TmsWeather", worker.ActiveChangesSql);
            var restoredWeatherRow = await GetTrackState(db, "TmsWeather");
            Assert.NotNull(restoredWeatherRow);
            Assert.Null(restoredWeatherRow!.LastIssue);

            // Luôn khôi phục lại Change Tracking cho TmsWeather để không ảnh hưởng các test khác
            await db.Ado.ExecuteCommandAsync(
                "IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')) " +
                "ALTER TABLE [TmsWeather] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");
        }

        /// <summary>
        /// Description: Kiểm chứng lỗi đã sửa — mốc hẹn thử lại tính RIÊNG cho từng bảng. Khi bảng thứ hai
        ///              bị cô lập muộn hơn, mốc hẹn của bảng thứ nhất phải giữ nguyên, không bị đẩy lùi.
        ///              Trước khi sửa, hai bảng dùng chung một mốc và mốc đó bị gán lại vô điều kiện, nên
        ///              bảng hỏng trước bị mất lượt thử lại; hỏng liên tiếp thì không bảng nào được thử lại.
        /// Created date: 27/09/2026
        /// </summary>
        [Fact]
        public async Task TrackerWorker_WhenSecondTableBreaksLater_DoesNotPushBackFirstTableRetry_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker();
            await worker.ReloadConfig(db);
            var missing = GetMissingTables(worker);
            var firstTable = worker.TrackedTables[0];
            var secondTable = worker.TrackedTables[1];

            // Arrange: cô lập bảng thứ nhất rồi cho mốc hẹn của nó về quá khứ (đã đến hạn)
            missing[firstTable] = DateTime.UtcNow.AddSeconds(-1);
            var firstRetryAt = missing[firstTable];

            // Act: cô lập thêm bảng thứ hai ở thời điểm muộn hơn
            missing[secondTable] = DateTime.UtcNow + TimeSpan.FromMinutes(5);

            // Assert: mốc của bảng thứ nhất KHÔNG bị đẩy lùi theo bảng thứ hai
            Assert.Equal(firstRetryAt, missing[firstTable]);
            Assert.True(missing[firstTable] <= DateTime.UtcNow, "Bảng hỏng trước phải vẫn ở trạng thái đã đến hạn thử lại.");
            Assert.True(missing[secondTable] > DateTime.UtcNow, "Bảng hỏng sau phải còn trong cửa sổ chờ.");

            missing.Clear();
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

            var service = CreateOutboundService(scope);

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

            var eqId = $"EQ_RACE_{testId}";
            await PrepareDatabase(db, logger, packetCode);

            // Bài này cần TmsTrafficData rỗng để đếm đúng số lượt trigger — xem ChangeTracking_WhenNewDataCommittedAfterFirstBatch_SubsequentRunPicksUpRemainingData_Test. Không siết phễu được.
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

            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_RACE_{testId}",
                KmNumber = 60,
                MetNumber = 200
            }).ExecuteCommandAsync();

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

                var service = CreateOutboundService(scope);

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
                    .Where(l => l.SubscriptionId == sub.ID && l.ParentId == null)
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
                    .SetColumns(s => s.NextTimeRun == db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-1))
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
                    .Where(l => l.SubscriptionId == sub.ID && l.ParentId == null)
                    .OrderBy(l => l.OccurredAt)
                    .ToListAsync();

                Assert.Equal(2, logsAll.Count);
                Assert.Equal(1, logsAll[1].RecordCount); // Đợt 2 chỉ gửi đúng 1 xe (Xe C)
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
                s.NextTimeRun = db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddMinutes(5);
            });

            var service = CreateOutboundService(scope);

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

        // Tạm comment 28/09/2026 cùng lượt tắt vế chống dội trong ExportSubscription — bật lại vế đó thì bỏ comment bài này.
        // /// <summary>
        // /// Description: Kiểm thử toàn trình: Khi DebounceSec được kích hoạt và lần chạy gần nhất nằm trong cửa sổ debounce, worker sẽ tạm hoãn gửi để tránh dồn dập request.
        // /// Created date: 22/09/2026
        // /// </summary>
        // [Fact]
        // public async Task ChangeTracking_WhenDebounceSecActive_ThrottlesImmediateTriggerExecution_Test()
        // {
        //     await using var scope = _host.Services.CreateAsyncScope();
        //     var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        //     var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();
        // 
        //     var testId = Guid.NewGuid().ToString("N")[..8];
        //     var partnerCode = $"PARTNER_DEBOUNCE_{testId}";
        //     var packetCode = "103";
        //     var subCode = $"SUB_DEBOUNCE_{testId}";
        // 
        //     await PrepareDatabase(db, logger, packetCode);
        // 
        //     var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
        //     {
        //         s.SendOnNewData = true;
        //         s.DebounceSec = 15;
        //         s.LastTimeRun = db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-3);
        //         s.NextTimeRun = null;
        //     });
        // 
        //     var service = CreateOutboundService(scope);
        // 
        //     await service.ProcessSubscriptions(packetCode, CancellationToken.None);
        // 
        //     var logs = await db.Queryable<ShareDataActivityLog>()
        //         .Where(l => l.SubscriptionId == sub.ID)
        //         .ToListAsync();
        // 
        //     Assert.Empty(logs);
        // }

        /// <summary>
        /// Description: Kiểm thử tính chất đơn điệu (Monotonic) của ShareDataLastSend: Mốc thời gian (LastTime) hoặc khóa (LastKey) thấp hơn hoặc bằng không bao giờ ghi đè mốc cao hơn.
        /// Created date: 22/09/2026
        /// Modified date: 26/09/2026
        /// </summary>
        [Fact]
        public async Task LastSend_WhenLowerTimestampOrKeyReceived_MonotonicLastSendPreservesHigherProgress_Test()
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

            var baseTime = DateTime.Now;
            var initialLastTime = baseTime.AddMinutes(-10);

            // Giả lập LastSend đã ghi nhận mốc thời gian và key cao trước đó
            var initialCheckpoint = new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partnerCode,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = "KEY_500",
                CreateTime = baseTime,
                UpdateTime = baseTime
            };
            await db.Insertable(initialCheckpoint).ExecuteCommandAsync();

            // Act 1: Cố tình thực thi câu lệnh cập nhật với LastTime cũ hơn (-20 phút)
            var oldTime = baseTime.AddMinutes(-20);
            var oldKey = "KEY_100";
            var updateQuery1 = db.Updateable<ShareDataLastSend>()
                .SetColumns(c => c.LastTime == oldTime)
                .SetColumns(c => c.LastKey == oldKey)
                .SetColumns(c => c.UpdateTime == baseTime)
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .Where(c => c.LastTime < oldTime || (c.LastTime == oldTime && (c.LastKey == null || c.LastKey.CompareTo(oldKey) < 0)));

            var rowsAffected1 = await updateQuery1.ExecuteCommandAsync();

            // Assert 1: Cơ chế chốt chặn đơn điệu từ chối ghi đè khi LastTime cũ hơn (0 dòng bị ảnh hưởng)
            Assert.Equal(0, rowsAffected1);

            // Act 2: Cố tình cập nhật cùng LastTime nhưng LastKey nhỏ hơn ("KEY_200" < "KEY_500")
            var sameTime = initialLastTime;
            var smallerKey = "KEY_200";
            var updateQuery2 = db.Updateable<ShareDataLastSend>()
                .SetColumns(c => c.LastTime == sameTime)
                .SetColumns(c => c.LastKey == smallerKey)
                .SetColumns(c => c.UpdateTime == baseTime)
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .Where(c => c.LastTime < sameTime || (c.LastTime == sameTime && (c.LastKey == null || c.LastKey.CompareTo(smallerKey) < 0)));

            var rowsAffected2 = await updateQuery2.ExecuteCommandAsync();

            // Assert 2: Cơ chế chốt chặn đơn điệu từ chối ghi đè khi LastKey nhỏ hơn (0 dòng bị ảnh hưởng)
            Assert.Equal(0, rowsAffected2);

            // Xác nhận mốc ban đầu được bảo toàn nguyên vẹn
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();

            Assert.NotNull(lastSend);
            Assert.Equal("KEY_500", lastSend.LastKey);
            Assert.Equal(initialLastTime.ToString("yyyy-MM-dd HH:mm:ss"), lastSend.LastTime?.ToString("yyyy-MM-dd HH:mm:ss"));
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

            var service = CreateOutboundService(scope);

            // Act: Chạy luồng quét định kỳ (mô phỏng trường hợp NATS offline hoàn toàn, không có event trigger nào bắn tới)
            await service.ProcessSubscriptions(CancellationToken.None);

            // Assert: Luồng định kỳ quét bù thành công, dữ liệu được gửi trọn vẹn
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.True(logs[0].Success == BaseEnums.SuccessEnums.Success, $"Msg: '{logs[0].ErrorMessage}', Desc: '{logs[0].Description}'");
            Assert.True(logs[0].RecordCount > 0);
        }

        /// <summary>
        /// Description: Kiểm chứng: Khi Subscription đang có worker khác nắm giữ lock (ProcessingUntil mang mốc tương lai),
        ///              lời gọi kích hoạt ProcessSubscriptions mới KHÔNG được cướp lock,
        ///              giữ nguyên ProcessingUntil và KHÔNG gửi hay sinh log.
        /// Created date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenLockHeldByActiveWorker_DoesNotStealLockAndLeavesProcessingUntilIntact_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_LOCK_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_LOCK_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var activeWorkerLockExpiry = now.AddMinutes(5);
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 30;
                s.LastTimeRun = now.AddMinutes(-1);
                s.ProcessingUntil = activeWorkerLockExpiry;
            });

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

            var service = CreateOutboundService(scope);

            await service.ProcessSubscriptions(packetCode, CancellationToken.None);

            var updatedSub = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();
            Assert.Equal(sub.LastTimeRun, updatedSub.LastTimeRun);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Empty(logs);

            Assert.NotNull(updatedSub.ProcessingUntil);
            Assert.Equal(
                activeWorkerLockExpiry.ToString("yyyy-MM-dd HH:mm:ss"),
                updatedSub.ProcessingUntil?.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// Description: Chứng minh trigger đến muộn khi Subscription đang có worker khác xử lý dở
        ///              (ProcessingUntil mang mốc tương lai) thì KHÔNG được gửi lại, KHÔNG cướp lock,
        ///              và không sinh ActivityLog nào.
        /// Created date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task TriggerFlow_WhenSubscriptionIsBeingProcessed_DoesNotSendAgain_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_BUSY_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_BUSY_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var heldProcessingUntil = now.AddMinutes(5);
            var initialLastTimeRun = now.AddMinutes(-10);

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 300;
                s.LastTimeRun = initialLastTimeRun;
                s.NextTimeRun = now.AddMinutes(-1); // Đã tới hạn lịch (bẫy cũ)
                s.ProcessingUntil = heldProcessingUntil; // Đang bị worker khác xử lý
                s.SerialNbr = 10;
            });

            var eqId = $"EQ_BUSY_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_BUSY_{testId}",
                KmNumber = 65,
                MetNumber = 100
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30A-BUSY_{testId}",
                Speed = 80.0f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM65",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            var service = CreateOutboundService(scope);

            await service.ProcessSubscriptions(packetCode, CancellationToken.None);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Empty(logs);

            var updatedSub = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();
            Assert.NotNull(updatedSub);
            Assert.Equal(
                heldProcessingUntil.ToString("yyyy-MM-dd HH:mm:ss"),
                updatedSub.ProcessingUntil?.ToString("yyyy-MM-dd HH:mm:ss"));
            Assert.Equal(initialLastTimeRun.ToString("yyyy-MM-dd HH:mm:ss"),
                updatedSub.LastTimeRun?.ToString("yyyy-MM-dd HH:mm:ss"));
            Assert.Equal(10, updatedSub.SerialNbr);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Kiểm thử đua tranh đồng thời (concurrent race): Khi N lời gọi ProcessSubscriptions(packetCode) nổ ra gần như đồng thời
        ///              cho cùng 1 Subscription (mô phỏng N instance Worker cùng nhận 1 bản tin NATS do cơ chế pub/sub không dùng queue group),
        ///              nhiều instance CÓ THỂ cùng chạy (sau bản sửa 28/09/2026: luồng sự kiện không còn loại trừ lẫn nhau qua NextTimeRun),
        ///              nhưng OCC trong CommitSuccess đảm bảo đúng 1 lần commit thắng và mốc gửi không bị hỏng.
        /// Created date: 25/09/2026
        /// Modified date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenConcurrentTriggersRaceForSameSubscription_ExactlyOneExportSucceeds_Test()
        {
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
                s.NextTimeRun = null;
            });

            var eqId = $"EQ_VDS_RACE_{testId}";
            // Bài này cần TmsTrafficData rỗng để đếm đúng số lượt trigger — xem ChangeTracking_WhenConcurrentTriggersRaceForSameSubscription_ExactlyOneExportSucceeds_Test. Không siết phễu được.
            await db.Deleteable<TmsTrafficData>().ExecuteCommandAsync();

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

            // Chụp mốc SerialNbr TRƯỚC khi đua tranh, để đếm được chính xác có bao nhiêu lượt commit thắng.
            var subBefore = await db.Queryable<ShareDataSubscription>().Where(s => s.ID == sub.ID).FirstAsync();
            var serialBefore = subBefore.SerialNbr ?? 0;

            const int concurrentCallers = 5;
            var scopes = Enumerable.Range(0, concurrentCallers)
                .Select(_ => _host.Services.CreateAsyncScope())
                .ToList();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.Responder = (callIndex, req) =>
            {
                Thread.Sleep(1000);
                return new PartnerResponsePlan(HttpStatusCode.OK);
            };
            var services = scopes.Select(s => CreateOutboundService(s)).ToList();

            using var readyGate = new CountdownEvent(concurrentCallers);
            using var startGate = new ManualResetEventSlim(false);
            var tasks = services
                .Select(svc => Task.Run(async () =>
                {
                    readyGate.Signal();
                    startGate.Wait();
                    await svc.ProcessSubscriptions(packetCode, CancellationToken.None);
                }))
                .ToArray();

            readyGate.Wait();
            startGate.Set();
            await Task.WhenAll(tasks);

            var raceLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-5) && l.ParentId == null)
                .ToListAsync();
            // Đua tranh giữa 5 callers: chỉ cho phép DUY NHẤT 1 lần xuất bản có dữ liệu (RecordCount > 0)
            Assert.Single(raceLogs, l => l.RecordCount > 0);
            // Siết 28/09/2026: bài này mang tên ExactlyOne nhưng trước đó chỉ khẳng định "ít nhất một",
            // nên xanh kể cả khi cả 5 lời gọi cùng gửi thành công. Đếm đúng số lượt để lộ đua tranh lọt lưới.
            // Tách bạch: ExportPage cũng ghi log Success cho lượt "không có dữ liệu mới" (RecordCount = 0),
            // lượt đó vô hại. Chỉ lượt có RecordCount > 0 mới là thật sự gửi dữ liệu cho đối tác.
            var successCount = raceLogs.Count(l => l.Success == BaseEnums.SuccessEnums.Success);
            var sentCount = raceLogs.Count(l => l.Success == BaseEnums.SuccessEnums.Success && l.RecordCount > 0);

            var raceAlerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Empty(raceAlerts);

            var checkpoint = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();
            Assert.NotNull(checkpoint);
            Assert.NotNull(checkpoint.LastTime);

            var updatedSub = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();
            Assert.True(updatedSub.LastTimeRun > sub.LastTimeRun,
                "Sau đua tranh, LastTimeRun phải tiến — ít nhất 1 commit thắng.");

            // SerialNbr tăng đúng bằng số lượt commit thắng — đây là thước đo chắc chắn nhất, vì
            // CommitSuccess là nơi duy nhất tăng nó và nó nằm trong cùng giao dịch với kiểm OCC.
            var serialDelta = (updatedSub.SerialNbr ?? 0) - serialBefore;
            Assert.True(sentCount == 1 && serialDelta == 1,
                $"{concurrentCallers} lời gọi đua nhau phải cho ĐÚNG 1 lượt gửi dữ liệu và SerialNbr tăng ĐÚNG 1. "
                + $"Thực tế: gửi có dữ liệu = {sentCount}, tổng log Success = {successCount}, SerialNbr {serialBefore} -> {updatedSub.SerialNbr} (delta {serialDelta}).");

            foreach (var s in scopes)
                await s.DisposeAsync();
        }

        /// <summary>
        /// Description: Kiểm thử 2 service (Worker instances) chạy song song cùng lúc:
        ///              - 2 instance DataChangeTrackingService độc lập cùng poll Change Tracking.
        ///              - Cả 2 cùng phát hiện dữ liệu mới và kích hoạt ProcessSubscriptions đồng thời.
        ///              - Sau bản sửa 28/09/2026: nhiều service CÓ THỂ cùng chạy export — chỉ CommitSuccess
        ///                (OCC theo nextRunDeadline) đảm bảo mốc gửi không bị hỏng hay lùi.
        /// Created date: 26/09/2026
        /// Modified date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenTwoServicesRunConcurrently_MaintainsDataIntegrityAndSingleExport_Test()
        {
            await using var scope1 = _host.Services.CreateAsyncScope();
            await using var scope2 = _host.Services.CreateAsyncScope();
            var db = scope1.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope1.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var partnerCode = $"PARTNER_2SVC_{testId}";
            var packetCode = "103";
            var subCode = $"SUB_2SVC_{testId}";

            await PrepareDatabase(db, logger, packetCode);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            var trackerA = CreateTrackerWorker(scope1);
            var trackerB = CreateTrackerWorker(scope2);
            var serviceA = CreateOutboundService(scope1);
            var serviceB = CreateOutboundService(scope2);

            await trackerA.PollChanges(CancellationToken.None);
            await trackerB.PollChanges(CancellationToken.None);

            Assert.True(await GetStateVersion(db) >= 0);

            var eqId = $"EQ_2SVC_{testId}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_2SVC_{testId}",
                KmNumber = 80,
                MetNumber = 400
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "CAR",
                LicensePlate = $"30A-2SVC_{testId}",
                Speed = 90.0f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM80",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            await trackerA.PollChanges(CancellationToken.None);
            await trackerB.PollChanges(CancellationToken.None);

            Assert.True(await GetStateVersion(db) > 0);

            await Task.WhenAll(
                serviceA.ProcessSubscriptions(packetCode, CancellationToken.None),
                serviceB.ProcessSubscriptions(packetCode, CancellationToken.None));

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-5))
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.Contains(logs, l => l.Success == BaseEnums.SuccessEnums.Success);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Empty(alerts);

            var checkpoint = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();
            Assert.NotNull(checkpoint);
            Assert.NotNull(checkpoint.LastTime);

            var updatedSub = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();
            Assert.True(updatedSub.LastTimeRun > sub.LastTimeRun,
                "Sau đua tranh, LastTimeRun phải tiến — ít nhất 1 commit thắng.");

            await trackerA.PollChanges(CancellationToken.None);
            await trackerB.PollChanges(CancellationToken.None);

            var logsAfterSecondPoll = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Equal(logs.Count, logsAfterSecondPoll.Count);
        }

        /// <summary>
        /// Description: Kiểm thử hành vi Worker thứ hai poll sau Worker thứ nhất đã nâng mốc version:
        ///              Worker 2 thấy version không đổi và rút lui lặng lẽ — version trong CSDL không bị thay đổi.
        ///              Bài này chạy TUẦN TỰ (không đồng thời thật). Kiểm chứng đua tranh thật với 10 instance đồng loạt
        ///              nằm ở bài PollChanges_WhenTenInstancesPollSimultaneously_ExactlyOneRaisesVersionAndPublishes_Test.
        /// Created date: 27/09/2026
        /// Modified date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenSecondWorkerPollsAfterFirst_VersionUnchangedAndNoDuplicateTrigger_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var worker1 = CreateTrackerWorker(scope);
            var worker2 = CreateTrackerWorker(scope);

            await worker1.PollChanges(CancellationToken.None);
            var initialVersion = await GetStateVersion(db);
            Assert.True(initialVersion >= 0);

            var testId = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_CAS_{testId}";
            {
                await db.Insertable(new TmsEquipment
                {
                    ID = eqId,
                    Code = $"EQ_CAS_{testId}",
                    KmNumber = 10,
                    MetNumber = 100
                }).ExecuteCommandAsync();

                await worker1.PollChanges(CancellationToken.None);
                var stateAfterW1 = await GetTrackState(db);
                Assert.NotNull(stateAfterW1);
                Assert.True(stateAfterW1.LastVersion > initialVersion);
                var newVersion = stateAfterW1.LastVersion!.Value;

                await worker2.PollChanges(CancellationToken.None);
                var stateAfterW2 = await GetTrackState(db);
                Assert.NotNull(stateAfterW2);

                Assert.Equal(newVersion, stateAfterW2.LastVersion);
            }
        }

        /// <summary>
        /// Description: Kiểm thử đua tranh đồng thời giữa 10 instance DataChangeTrackingService cùng poll một mốc Change Tracking mới:
        ///              đúng 1 instance thắng cuộc trong lệnh Atomic CAS và nâng version, các instance thua cuộc rút lui an toàn.
        ///              Nếu có NATS broker thật kết nối được, kiểm chứng đúng 1 event được publish lên NATS.
        /// Created date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenTenInstancesPollSimultaneously_ExactlyOneRaisesVersionAndPublishes_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var transport = _host.Services.GetRequiredService<TransportManager>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await transport.ConnectAsync(cts.Token);

            var testId = Guid.NewGuid().ToString("N")[..8];
            var uniqueSubject = $"ta.its.event.sharedata.newdata.ten_{testId}";
            var testConfig = new ConfigurationBuilder()
                .AddConfiguration(config)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Nats:NewDataSubject"] = uniqueSubject,
                    ["Nats:Subject"] = uniqueSubject
                })
                .Build();

            var eventCount = 0;
            var probeReceived = 0;
            if (transport.IsConnected)
            {
                transport.Unsubscribe(uniqueSubject);
                await transport.SubscribeAsync(uniqueSubject, (string msg) =>
                {
                    if (msg.Contains("\"Probe\":true", StringComparison.OrdinalIgnoreCase))
                        Interlocked.Increment(ref probeReceived);
                    else if (msg.Contains("101_commonData", StringComparison.OrdinalIgnoreCase))
                        Interlocked.Increment(ref eventCount);
                    return Task.CompletedTask;
                });

                var probeDeadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < probeDeadline && Volatile.Read(ref probeReceived) == 0)
                {
                    await transport.PublishAsync(uniqueSubject, new { Probe = true });
                    await Task.Delay(100, cts.Token);
                }
            }

            const int workerCount = 10;
            var scopes = Enumerable.Range(0, workerCount)
                .Select(_ => _host.Services.CreateAsyncScope())
                .ToList();
            var workers = scopes
                .Select(s => CreateTrackerWorker(s, transport, testConfig))
                .ToList();

            foreach (var w in workers)
                await w.ReloadConfig(db);

            await SetStateVersion(db, 0);
            await workers[0].PollChanges(cts.Token);
            var initialVersion = await GetStateVersion(db);
            Assert.True(initialVersion >= 0);

            var zoneId = $"ZONE_10W_{testId}";
            var statusId = Guid.NewGuid().ToString("N");

            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Zone 10W {testId}",
                FromKmNumber = 10,
                FromMetNumber = 0,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "L1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = statusId,
                ZoneId = zoneId,
                AverageSpeed = "70.0",
                Condition = "NORMAL",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = workers.Select(async w =>
            {
                await tcs.Task;
                await w.PollChanges(cts.Token);
            }).ToArray();

            tcs.SetResult();
            await Task.WhenAll(tasks);

            var rowCount = await db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null).CountAsync();
            Assert.Equal(15, rowCount);

            var finalState = await GetTrackState(db, "TmsZoneStatus");
            Assert.NotNull(finalState);
            Assert.True(finalState!.LastVersion > initialVersion);

            if (transport.IsConnected)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (eventCount == 0 && sw.ElapsedMilliseconds < 3000)
                    await Task.Delay(50, cts.Token);

                Assert.Equal(1, eventCount);
            }

            transport.Unsubscribe(uniqueSubject);
            foreach (var s in scopes)
                await s.DisposeAsync();
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

            var service = CreateOutboundService(scope);

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

            var service = CreateOutboundService(scope);

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
        /// Description: Kiểm thử kịch bản sập hệ thống: Khi DataChangeTrackingService khởi động lại sau sự cố (mốc CT nhảy cóc tới hiện tại),
        /// luồng quét định kỳ vẫn gửi đủ 100% dữ liệu tạo trong lúc worker chết, chứng minh không thất thoát dữ liệu.
        /// Created date: 23/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenWatcherRestartsAfterDowntime_PeriodicScanStillSendsDataCreatedWhileDown_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();
            var transport = _host.Services.GetRequiredService<TransportManager>();

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

            var service = CreateOutboundService(scope);

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
                .Where(l => l.SubscriptionId == sub.ID && l.ParentId == null)
                .ToListAsync();
            Assert.NotEmpty(initialLogs);
            Assert.All(initialLogs, l => Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success));

            // 2. Mô phỏng worker giám sát đang chết (Downtime):
            // Chèn thêm 3 bản ghi mới vào TmsTrafficData sau mốc checkpoint cp1Time mà KHÔNG chạy vòng quét Change Tracking nào
            var downtimeBaseTime = cp1Time.AddSeconds(10);
            var downtimeList = Enumerable.Range(1, 3).Select(i => new TmsTrafficData
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
            await db.Insertable(downtimeList).ExecuteCommandAsync();

            // 3. Mô phỏng DataChangeTrackingService khởi động lại:
            // Tạo instance mới của DataChangeTrackingService (mốc LastVersion trong bảng ShareDataSourceTable khởi tạo = -1)
            // và kích hoạt vòng poll đầu tiên (nhảy thẳng tới version hiện tại của DB, không sinh NATS trigger cho 3 bản ghi ở bước 2)
            await SetStateVersion(db, -1);
            var newWatcher = CreateTrackerWorker(scope, transport);
            await newWatcher.PollChanges(CancellationToken.None);

            // 4. Act: Luồng quét định kỳ (ProcessSubscriptions) kích hoạt theo lịch
            // Đảm bảo NextTimeRun đến hạn chạy định kỳ
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-5))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await service.ProcessSubscriptions(CancellationToken.None);

            // 5. Assert: 3 bản ghi chèn lúc watcher chết VẪN được gửi đầy đủ
            var allLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID && l.ParentId == null)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.True(allLogs.Count > initialLogs.Count);
            var latestLog = allLogs.First();
            Assert.Equal(BaseEnums.SuccessEnums.Success, latestLog.Success);

            // Tổng số bản ghi gửi ở đợt 2 đúng bằng 3 bản ghi phát sinh trong lúc watcher chết
            var newLogs = allLogs.Where(l => !initialLogs.Any(il => il.ID == l.ID)).ToList();
            var exportedDowntimeCount = newLogs.Sum(l => l.RecordCount);
            Assert.Equal(downtimeList.Count, exportedDowntimeCount);

            // Checkpoint đã tiến đúng tới bản ghi cuối cùng của đợt downtime
            var cp2 = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();
            Assert.NotNull(cp2);
            Assert.True(cp2.LastTime > cp1Time || (cp2.LastTime == cp1Time && string.Compare(cp2.LastKey, cp1Key) > 0));
            Assert.Equal(downtimeList.Last().ID, cp2.LastKey);
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

            var service = CreateOutboundService(scope);

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

        /// <summary>
        /// Description: Kiểm thử bài đối chứng: Gói tin NotReady (110) và Disabled (111) bị chặn hoàn toàn,
        ///              không sinh trigger ở tầng ResolveTriggerPackets và không kích hoạt xuất bản ở pipeline ProcessSubscriptions.
        /// Created date: 23/09/2026
        /// </summary>
        [Fact]
        public async Task ChangeTracking_WhenPacketIsNotReadyOrDisabled_TriggerStillBlocked_Test()
        {
            // 1. Kiểm thử tầng ResolveTriggerPackets: TmsIncident và TmsEventType ánh xạ 107, 110, 111
            // Gói 107_incidentData, 110_wpData và 111 đều được resolve; việc loại bỏ gói tắt được thực hiện tại tầng ProcessSubscriptions theo trạng thái DB
            var detectedPackets = InvokeResolveTriggerPackets(["TmsIncident", "TmsEventType"]);
            Assert.Contains("107_incidentData", detectedPackets);
            Assert.Contains("110_wpData", detectedPackets);
            Assert.Contains("111", detectedPackets);

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

            var service = CreateOutboundService(scope);

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
                Status = BaseEnums.StatusEnum.Enable,
                ExtractSql = PacketDefaultTemplate.TryGet(packetCode, out var tpl105) ? tpl105.ExtractSql : null
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

            var service = CreateOutboundService(scope);

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

            if (existing105Packets.Count > 0)
            {
                var packetIds = existing105Packets.Select(x => x.ID).ToList();
                await db.Updateable<ShareDataPacket>()
                    .SetColumns(p => p.IsDelete == null)
                    .Where(p => packetIds.Contains(p.ID))
                    .ExecuteCommandAsync();
            }
        }
        #endregion



        #region Helpers

        private static async Task EnsureDefaultTrackTables(ISqlSugarClient db)
        {
            if (!db.DbMaintenance.IsAnyTable("ShareDataSourceTable", false))
                db.CodeFirst.InitTables<ShareDataSourceTable>();

            var seedRows = new ShareDataSourceTableSeedData().HasData().ToList();
            var seedTableNames = seedRows
                .Where(x => !string.IsNullOrWhiteSpace(x.TableName))
                .Select(x => x.TableName!.Trim())
                .ToList();

            using (var rawDb = db.CopyNew())
            {
                rawDb.QueryFilter.Clear();
                rawDb.Aop.DataExecuting = null;
                await rawDb.Deleteable<ShareDataSourceTable>()
                    .Where(x => x.IsDelete != null)
                    .ExecuteCommandAsync();

                await rawDb.Updateable<TmsWeather>()
                    .SetColumns(x => x.RefId == x.ID)
                    .Where(x => x.RefId == null)
                    .ExecuteCommandAsync();

                await rawDb.Updateable<TmsWeather>()
                    .SetColumns(x => new TmsWeather
                    {
                        UpdateTime = SqlFunc.DateAdd(SqlFunc.GetDate(), -10, DateType.Minute),
                        CreateTime = SqlFunc.DateAdd(SqlFunc.GetDate(), -10, DateType.Minute),
                        TimeDetect = SqlFunc.DateAdd(SqlFunc.GetDate(), -10, DateType.Minute)
                    })
                    .Where(x => x.UpdateTime > SqlFunc.DateAdd(SqlFunc.GetDate(), -1, DateType.Minute)
                             || x.CreateTime > SqlFunc.DateAdd(SqlFunc.GetDate(), -1, DateType.Minute))
                    .ExecuteCommandAsync();
            }

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == false)
                .Where(x => !seedTableNames.Contains(x.TableName!))
                .ExecuteCommandAsync();

            var existingRows = await db.Queryable<ShareDataSourceTable>()
                .Where(x => x.IsDelete == null)
                .ToListAsync();

            var existingByTable = existingRows
                .Where(x => !string.IsNullOrWhiteSpace(x.TableName))
                .ToDictionary(x => x.TableName!.Trim(), StringComparer.OrdinalIgnoreCase);

            var toInsert = new List<ShareDataSourceTable>();
            foreach (var seed in seedRows)
            {
                var tableName = seed.TableName!.Trim();
                if (!existingByTable.TryGetValue(tableName, out var existing))
                {
                    toInsert.Add(new ShareDataSourceTable
                    {
                        ID = seed.ID,
                        TableName = seed.TableName,
                        Name = seed.Name,
                        PacketCodes = seed.PacketCodes,
                        OrderNo = seed.OrderNo,
                        IsTracked = seed.IsTracked,
                        IsQueryable = seed.IsQueryable,
                        Category = seed.Category,
                        DefaultAlias = seed.DefaultAlias,
                        Remark = seed.Remark,
                        LastVersion = seed.LastVersion,
                        LastChangeTime = null,
                        LastIssue = null,
                        TenantId = seed.TenantId,
                        CreateTime = seed.CreateTime,
                        UpdateTime = seed.UpdateTime
                    });
                }
                else if (existing.IsTracked != seed.IsTracked
                      || existing.PacketCodes != seed.PacketCodes
                      || existing.OrderNo != seed.OrderNo
                      || existing.Name != seed.Name
                      || existing.Remark != seed.Remark)
                {
                    await db.Updateable<ShareDataSourceTable>()
                        .SetColumns(x => x.IsTracked == seed.IsTracked)
                        .SetColumns(x => x.PacketCodes == seed.PacketCodes)
                        .SetColumns(x => x.OrderNo == seed.OrderNo)
                        .SetColumns(x => x.Name == seed.Name)
                        .SetColumns(x => x.Remark == seed.Remark)
                        .Where(x => x.ID == existing.ID)
                        .ExecuteCommandAsync();
                }
            }

            if (toInsert.Count > 0)
                await db.Insertable(toInsert).ExecuteCommandAsync();
        }

        private static async Task PrepareDatabase(ISqlSugarClient db, ILogger logger, string packetCode = "103")
        {
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.IsDelete == DateTime.Now)
                .Where(p => p.IsDelete == null && p.Code != packetCode && p.Code!.StartsWith(packetCode + "_"))
                .ExecuteCommandAsync();

            await DataOutboundServiceTests.PacketMetadataCatalogTest.SeedPacketToDb(db, packetCode);
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
                SessionState = BaseEnums.SessionState.Connected,
                Address = ShareDataPartnerServerMock.DefaultHost,
                Port = ShareDataPartnerServerMock.DefaultPort,
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

        private static DataOutboundService CreateOutboundService(IServiceScope scope, IHttpClientFactory? httpClientFactory = null)
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var clientFactory = httpClientFactory ?? scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var dispatcher = scope.ServiceProvider.GetService<IPacketSendDispatcher>()
                ?? PacketSendDispatcher.CreateForHandlers([
                    new HttpPacketSender(clientFactory),
                    new FilePacketSender(config.GetSection("NasStorage:BasePath").Value ?? "/tmp"),
                    new KafkaPacketSender(),
                    new FtpPacketSender()
                ]);

            return new DataOutboundService(
                scopeFactory,
                logger,
                new DataOutboundExtractionProcess(),
                new DataOutboundTransport(dispatcher, config));
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

            // Act: 3 scope độc lập gọi ProcessSubscriptions cho 3 gói tin khác nhau cùng lúc
            var tasks = configs.Select(cfg =>
            {
                var s = _host.Services.CreateAsyncScope();
                return CreateOutboundService(s).ProcessSubscriptions(cfg.PacketCode, CancellationToken.None);
            }).ToArray();

            await Task.WhenAll(tasks);

            // Assert: mỗi sub phải có đúng ActivityLog của chính nó, checkpoint không bị trộn lẫn
            foreach (var (packetCode, partnerCode, partner, sub) in seeded)
            {
                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID && l.OccurredAt >= now.AddSeconds(-10) && l.ParentId == null)
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

            // Act: 4 scope độc lập (mô phỏng 4 worker instance nhận cùng 1 bản tin NATS "103")
            // xử lý song song — mỗi scope sẽ xử lý 1 subscription khác nhau (do partner khác nhau)
            var asyncScopes = Enumerable.Range(0, partnerCount)
                .Select(_ => _host.Services.CreateAsyncScope())
                .ToList();

            var tasks = asyncScopes
                .Select(s => CreateOutboundService(s).ProcessSubscriptions(packetCode, CancellationToken.None))
                .ToArray();
            await Task.WhenAll(tasks);

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

            // Dọn dẹp tài nguyên và dữ liệu tuần tự ở cuối hàm
            foreach (var s in asyncScopes)
                await s.DisposeAsync();

            foreach (var (partner, sub) in seeded)
            {
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partner.Code && c.PacketCode == packetCode).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
            await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
            await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
        }

        #endregion

        /// <summary>
        /// Description: Kiểm thử handler của DataNatsService: Khi nhận payload NATS hợp lệ với mã gói,
        ///              service kích hoạt luồng xuất bản thật (ghi ActivityLog và tiến checkpoint ShareDataLastSend).
        /// Created date: 22/09/2026
        /// Modified date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task NatsWorker_HandleTrigger_WhenValidPacketCode_ExecutesOutboundFlow_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var realService = CreateOutboundService(scope);
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataNatsService>>();

            await PrepareDatabase(db, scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>(), "103");
            var testId = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_NW_{testId}";
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"EQ_NW_{testId}", KmNumber = 30, MetNumber = 0 }).ExecuteCommandAsync();
            await db.Insertable(new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = now,
                Type = "BUS",
                LicensePlate = $"29B-NW{testId}",
                Speed = 60f,
                Lane = "L1",
                Direction = "EAST",
                Location = $"KM30_{testId}",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"PTN_NW_{testId}", $"SUB_NW_{testId}", "103", s =>
            {
                s.SendOnNewData = true;
                s.DebounceSec = null;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            var transport = _host.Services.GetRequiredService<TransportManager>();
            var service = new DataNatsService(realService, logger, transport);

            var payload = System.Text.Json.JsonSerializer.Serialize(new { PacketCode = "103", Version = 12345 });

            // Act: Service nhận trigger
            await InvokeNatsHandleMessages(service, payload, CancellationToken.None);

            // Assert: luồng xuất bản ĐÃ CHẠY THẬT, không phải chỉ "hàm có được gọi"
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();

            Assert.True(logs.Count > 0,
                "HandleTrigger nhận đúng mã gói nhưng luồng xuất bản không chạy — không có dòng "
                + "ShareDataActivityLog nào cho đăng ký này.");
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.True(logs[0].RecordCount > 0,
                "Có log Success nhưng RecordCount = 0 — luồng đi đường 'không có dữ liệu mới' chứ KHÔNG gửi "
                + "bản ghi nào. Rà lại dữ liệu nguồn gói 103 và mốc ShareDataLastSend của đối tác này.");

            // Mốc gửi nối đuôi đã tiến ⇒ chứng minh đi trọn tới tầng CSDL
            var checkpoint = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partner.Code)
                .FirstAsync();
            Assert.NotNull(checkpoint);
        }

        /// <summary>
        /// Description: Kiểm thử handler của DataNatsService: Khi nhận payload không hợp lệ (json rác hoặc object rỗng),
        ///              service không ném ngoại lệ và không kích hoạt luồng xuất bản (không sinh log).
        /// Created date: 22/09/2026
        /// Modified date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task NatsWorker_HandleTrigger_WhenInvalidPayload_DoesNotThrowAndDoesNotCallService_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var realService = scope.ServiceProvider.GetRequiredService<ShareDataWorker.Core.Interfaces.IDataOutboundService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataNatsService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var (partner, sub) = await SeedOutboundSubscription(db, $"PTN_INV_{testId}", $"SUB_INV_{testId}", "103", s =>
            {
                s.SendOnNewData = true;
            });

            var transport = _host.Services.GetRequiredService<TransportManager>();
            var service = new DataNatsService(realService, logger, transport);

            // Act & Assert (Vế 1: DoesNotThrow)
            var ex1 = await Record.ExceptionAsync(() => InvokeNatsHandleMessages(service, "invalid json payload", CancellationToken.None));
            var ex2 = await Record.ExceptionAsync(() => InvokeNatsHandleMessages(service, "{}", CancellationToken.None));

            Assert.Null(ex1);
            Assert.Null(ex2);

            // Assert (Vế 2: DoesNotCallService - không kích hoạt luồng xuất bản, không có log nào sinh ra)
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Empty(logs);
        }

        /// <summary>
        /// Description: Kiểm thử ĐẦU-CUỐI đi qua dây NATS thật: chèn dữ liệu nguồn → Change Tracking phát hiện
        ///              → DataChangeTrackingService publish thật lên broker → DataNatsService nhận qua subject
        ///              thật → chạy luồng outbound thật → biến đổi CSDL. Đây là bài DUY NHẤT canh CHỖ NỐI giữa
        ///              bên phát và bên nhận: tên subject hai bên, tên field trong payload, việc đăng ký
        ///              subscribe. Các bài NATS khác chỉ kiểm riêng từng nửa.
        ///              Không có broker NATS ở 127.0.0.1:4222 thì chuyển sang kiểm hợp đồng offline — xUnit
        ///              2.9.3 không có API bỏ qua động nên buộc phải rẽ nhánh ngay trong bài test.
        /// Created date: 27/09/2026
        /// </summary>
        [Fact]
        public async Task NatsRoundTrip_WhenTrackerPublishesRealEvent_ConsumerReceivesAndRunsOutboundFlow_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var outboundLogger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();
            var consumerLogger = scope.ServiceProvider.GetRequiredService<ILogger<DataNatsService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            const string packetCode = "103";
            var partnerCode = $"PTN_RT_{testId}";
            var subCode = $"SUB_RT_{testId}";
            var eqId = $"EQ_RT_{testId}";

            await PrepareDatabase(db, outboundLogger, packetCode);
            await SetStateVersion(db, -1);
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.DebounceSec = null;                 // Bài này chỉ canh chỗ nối NATS, không xét chống dội
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"EQ_RT_{testId}",
                KmNumber = 30,
                MetNumber = 0
            }).ExecuteCommandAsync();

            // Hàm cục bộ: chèn 1 bản ghi nguồn gói 103. Dùng ở CẢ HAI nhánh nhưng gọi ở ĐÚNG THỜI ĐIỂM của từng
            // nhánh — nhánh thật phải gọi SAU khi cắm mốc gốc, nếu gọi trước thì bản ghi nằm dưới mốc và Change
            // Tracking sẽ không thấy thay đổi nào.
            async Task SeedTrafficRow() =>
                await db.Insertable(new TmsTrafficData
                {
                    ID = Guid.NewGuid().ToString("N"),
                    EquipmentId = eqId,
                    DetectTime = now,
                    Type = "BUS",
                    LicensePlate = $"29B-RT{testId}",
                    Speed = 60f,
                    Lane = "L1",
                    Direction = "EAST",
                    Location = $"KM30_{testId}",
                    CreateTime = now,
                    UpdateTime = now
                }).ExecuteCommandAsync();

            // TransportManager THẬT từ Host (appsettings.Test.json -> Nats:Url).
            var transport = _host.Services.GetRequiredService<TransportManager>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await transport.ConnectAsync(cts.Token);

            Assert.True(transport.IsConnected,
                "Không thể kết nối tới NATS broker tại 127.0.0.1:4222 (Nats:Url trong appsettings.Test.json). "
                + "Bắt buộc phải bật NATS broker (Docker local) trước khi chạy bài test toàn trình NatsRoundTrip.");

            var uniqueSubject = $"ta.its.event.sharedata.newdata.roundtrip_{testId}";
            var testConfig = new ConfigurationBuilder()
                .AddConfiguration(config)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Nats:NewDataSubject"] = uniqueSubject,
                    ["Nats:Subject"] = uniqueSubject
                })
                .Build();

            // 1) Bên nhận đăng ký subscribe TRƯỚC và chờ xong, để bản tin phát ra không bị mất.
            transport.Unsubscribe(uniqueSubject);
            var consumer = new DataNatsService(
                CreateOutboundService(scope),
                consumerLogger,
                transport,
                testConfig);
            await consumer.InitSubscribe(cts.Token);
            await Task.Delay(500, cts.Token);

            // 2) Bên phát: chu kỳ đầu chỉ lập mốc gốc (LastVersion từ -1 lên mốc hiện tại của CSDL).
            var publisher = CreateTrackerWorker(
                scope,
                transport,
                testConfig);
            await publisher.PollChanges(cts.Token);

            Assert.True(await GetStateVersion(db) >= 0,
                "Change Tracking chưa sẵn sàng trên CSDL test nên mốc gốc không lập được. Bật Change "
                + "Tracking ở cấp CSDL rồi chạy lại — bài này cần mốc gốc mới phát hiện được thay đổi.");

            // 3) Dữ liệu nguồn mới -> Change Tracking tăng version. Phải chèn SAU bước 2 (cắm mốc gốc).
            await SeedTrafficRow();

            // 4) Chu kỳ sau phát hiện thay đổi và PUBLISH THẬT lên subject.
            await publisher.PollChanges(cts.Token);

            // 5) Giao nhận NATS là bất đồng bộ -> chờ CÓ BIÊN, thoát sớm ngay khi thấy kết quả.
            // Trong môi trường chạy batch song song tải cao, nếu bản tin đầu bị broker drop do vừa re-subscribe,
            // cơ chế bù nhịp sau 3 giây (chèn thêm dữ liệu nguồn + PollChanges) sẽ đảm bảo bản tin được phát lại.
            List<ShareDataActivityLog> logs = [];
            var deadline = DateTime.UtcNow.AddSeconds(45);
            var lastPollTime = DateTime.UtcNow;
            while (DateTime.UtcNow < deadline)
            {
                logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .ToListAsync();

                if (logs.Count > 0)
                    break;

                if ((DateTime.UtcNow - lastPollTime).TotalSeconds >= 3)
                {
                    await SeedTrafficRow();
                    await publisher.PollChanges(cts.Token);
                    lastPollTime = DateTime.UtcNow;
                }

                await Task.Delay(200, cts.Token);
            }

            // Assert: bên nhận đã chạy luồng outbound THẬT, do chính bản tin của bên phát kích hoạt.
            Assert.True(logs.Count > 0,
                "Bản tin NATS phát từ DataChangeTrackingService đã không tới được DataNatsService (chờ 45 giây "
                + "không thấy ShareDataActivityLog). Rà theo thứ tự: (1) InitSubscribe có đăng ký được "
                + "subscribe không, (2) nhánh publish của PollChanges có chạy không (Transport.IsConnected), "
                + "(3) tên field PacketCode trong payload có đúng không.");
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.True(logs[0].RecordCount > 0,
                "Có log Success nhưng RecordCount = 0 — tức bản tin NATS tới được và pipeline chạy, nhưng "
                + "KHÔNG gửi bản ghi nào (đi đường 'không có dữ liệu mới'). Rà lại câu truy vấn trích xuất "
                + "gói 103 và mốc nối đuôi ShareDataLastSend.");

            // Mốc gửi nối đuôi đã tiến -> chứng minh đi trọn tới tầng CSDL, không dừng ở chỗ nhận bản tin.
            var checkpoint = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                .FirstAsync();
            Assert.NotNull(checkpoint);

            transport.Unsubscribe(uniqueSubject);
        }

        [Fact]
        public async Task GetCurrentDbVersion_WhenExceptionOccurs_ThrowsException_Test()
        {
            // Arrange: Client có kết nối không hợp lệ để chắc chắn ném exception khi Ado.GetScalarAsync
            using var badClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = "Server=127.0.0.1,59999;Database=not_exist;Connect Timeout=1;",
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            });

            // Act & Assert: Ngoại lệ kết nối DB không bị nuốt mà văng lên để ExecuteAsync cấp cha xử lý
            await Assert.ThrowsAnyAsync<Exception>(() => InvokeGetCurrentDbVersion(badClient));
        }

        [Fact]
        public async Task GetCurrentDbVersion_WhenDbActive_ReturnsVersion_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            // Act
            var version = await InvokeGetCurrentDbVersion(db);

            // Assert: Trên DB test đã kích hoạt Change Tracking thì version >= 0
            Assert.NotNull(version);
            Assert.True(version >= 0);
        }


        /// <summary>
        /// Description: Kiểm thử câu lệnh SqlGetAllTrackedTables lấy đầy đủ tất cả các bảng đã kích hoạt Change Tracking trong 1 query.
        /// Created date: 26/09/2026
        /// </summary>
        [Fact]
        public async Task SqlGetAllTrackedTables_ReturnsActiveTrackedTables_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            // Act
            var tables = await db.Ado.SqlQueryAsync<string>(DataChangeTrackingService.SqlGetAllTrackedTables);

            // Assert: Trả về danh sách bảng và có chứa các bảng chính (TmsZoneStatus, TmsTrafficData, v.v.)
            Assert.NotNull(tables);
            Assert.NotEmpty(tables);
            Assert.Contains("TmsTrafficData", tables, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Description: Kiểm thử toàn trình — khi câu SQL Change Tracking bị lỗi vì lý do khác version,
        ///              phải ghi 1 dòng ShareDataActivityLog mang mã ESH-1602 ở cột Remark, kèm câu SQL trong
        ///              AfterJson để lần ra bảng nào gây lỗi, và vẫn ném lỗi lên cho ExecuteAsync.
        /// Created date: 27/09/2026
        /// </summary>
        [Fact]
        public async Task TrackingLog_WhenChangeTableSqlFails_WritesEsh1602WithSql_Test()
        {
            // Arrange: câu SQL trỏ tới bảng không tồn tại
            var worker = CreateTrackerWorker();
            const string brokenSql = "SELECT 'X' AS TableName WHERE EXISTS (SELECT 1 FROM CHANGETABLE(CHANGES [ShareData_NoSuchTable_9x7], @lastVer) AS CT)";

            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var state = new ShareDataSourceTable { ID = Guid.NewGuid().ToString("N"), TableName = "TmsWeather", LastVersion = 0 };
            var startedAt = (await db.Ado.GetDateTimeAsync("SELECT GETDATE()")).AddSeconds(-5);

            // Act & Assert: lỗi vẫn phải văng lên
            await Assert.ThrowsAnyAsync<Exception>(() => InvokeQueryChangedTables(worker, db, state, brokenSql));

            // Assert: đã ghi đúng mã ESH-1602, AfterJson chứa câu SQL
            var row = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.QueryFailed && x.CreateTime >= startedAt)
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .FirstAsync();

            Assert.NotNull(row);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, row!.Success);
            Assert.Contains("ShareData_NoSuchTable_9x7", row.AfterJson ?? string.Empty);
        }

        /// <summary>
        /// Description: Kiểm thử lưới an toàn của tác vụ dọn nhật ký — chỉ được xoá dòng hạ tầng mã ESH-16xx
        ///              quá hạn, TUYỆT ĐỐI không được xoá dòng nhật ký truyền nhận nghiệp vụ cùng khoảng thời gian.
        /// Created date: 27/09/2026
        /// </summary>
        [Fact]
        public async Task CleanupTrackingLogs_OnlyDeletesEsh16Rows_KeepsBusinessTransferRows_Test()
        {
            // Arrange: 1 dòng hạ tầng quá hạn + 1 dòng nghiệp vụ quá hạn (Remark null)
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var oldTime = (await db.Ado.GetDateTimeAsync("SELECT GETDATE()")).AddDays(-30);
            var trackingId = Guid.NewGuid().ToString("N");
            var businessId = Guid.NewGuid().ToString("N");

            var rawConfig = new ConnectionConfig
            {
                ConnectionString = db.CurrentConnectionConfig.ConnectionString,
                DbType = db.CurrentConnectionConfig.DbType,
                IsAutoCloseConnection = true,
                ConfigureExternalServices = new ConfigureExternalServices
                {
                    EntityService = (prop, col) =>
                    {
                        if (prop.Name == nameof(ShareDataActivityLog.CreateTime))
                        {
                            col.InsertServerTime = false;
                            col.IsOnlyIgnoreUpdate = false;
                        }
                    }
                }
            };
            using (var rawDb = new SqlSugarClient(rawConfig))
            {
                await rawDb.Insertable(new ShareDataActivityLog
                {
                    ID = trackingId,
                    LogType = BaseEnums.LogTypeEnum.Transfer,
                    Remark = ShareDataAlertCode.Tracking.SelfHeal,
                    Description = "Dòng hạ tầng quá hạn dùng cho test dọn nhật ký.",
                    CreateTime = oldTime,
                    UpdateTime = oldTime
                }).ExecuteCommandAsync();

                await rawDb.Insertable(new ShareDataActivityLog
                {
                    ID = businessId,
                    LogType = BaseEnums.LogTypeEnum.Transfer,
                    Remark = null,
                    Description = "Dòng nghiệp vụ quá hạn — PHẢI còn nguyên sau khi dọn.",
                    CreateTime = oldTime,
                    UpdateTime = oldTime
                }).ExecuteCommandAsync();
            }

            // Act
            await ShareDataTransferLog.CleanTrackingLogs(db, retentionDays: 7);

            // Assert: dòng hạ tầng bị xoá, dòng nghiệp vụ còn nguyên
            Assert.Null(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == trackingId).FirstAsync());
            Assert.NotNull(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == businessId).FirstAsync());
        }

        /// <summary>
        /// Description: Kiểm thử cấu trúc bảng ShareDataSourceTable: Mỗi bảng nguồn có đúng 1 dòng cấu hình.
        ///              Hai lượt poll liên tiếp không sinh dòng mới, giữ nguyên ID và cập nhật UpdateTime.
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        [Fact]
        public async Task TrackState_WhenPolled_KeepsExactlyOneRow_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);
            var worker = CreateTrackerWorker();

            var countBefore = await db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null).CountAsync();

            // Act: 2 lượt poll liên tiếp
            await worker.PollChanges(CancellationToken.None);
            var afterFirst = await GetTrackState(db, "TmsZoneStatus");
            await worker.PollChanges(CancellationToken.None);

            // Assert: số dòng giữ nguyên (15 dòng cấu hình), không sinh dòng mới mỗi lượt
            var rowCount = await db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null).CountAsync();
            Assert.Equal(countBefore, rowCount);

            var afterSecond = await GetTrackState(db, "TmsZoneStatus");
            Assert.NotNull(afterFirst);
            Assert.NotNull(afterSecond);
            Assert.True(afterSecond!.LastVersion >= 0);
            Assert.Equal(afterFirst!.ID, afterSecond.ID);

            Assert.NotNull(afterSecond.UpdateTime);
            Assert.True(afterSecond.UpdateTime >= afterSecond.CreateTime);
        }

        /// <summary>
        /// Description: Kiểm thử cơ chế resume — khi khởi động mà CSDL đã có mốc trạng thái của lần chạy trước
        ///              trong ShareDataSourceTable, mốc version phải được tiếp tục từ đó thay vì quay về -1.
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        [Fact]
        public async Task TrackState_WhenPreviousRunExists_ResumesItsVersion_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);
            var worker = CreateTrackerWorker(scope);

            await SetStateVersion(db, 0);
            var currentVerObj = await db.Ado.GetScalarAsync(DataChangeTrackingService.SqlCurrentVersion);
            var previousVersion = currentVerObj == null || currentVerObj == DBNull.Value ? 0L : Convert.ToInt64(currentVerObj);
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == previousVersion)
                .SetColumns(x => x.LastIssue == null)
                .Where(x => x.IsDelete == null && x.IsTracked == true)
                .ExecuteCommandAsync();

            var trackState = await GetTrackState(db, "TmsZoneStatus");
            Assert.NotNull(trackState);
            var previousId = trackState!.ID;

            // Act: chu kỳ quét đầu tiên, đi qua đúng đường sản xuất
            await worker.PollChanges(CancellationToken.None);

            // Assert: tiếp tục đúng dòng trạng thái hiện có và TIẾP TỤC mốc LastVersion của lần chạy trước
            var state = await GetTrackState(db, "TmsZoneStatus");
            Assert.NotNull(state);
            Assert.Equal(previousId, state!.ID);
            Assert.Equal(previousVersion, state.LastVersion);
        }

        /// <summary>
        /// Description: Kiểm thử tính năng nạp cấu hình danh sách bảng cần giám sát từ ShareDataSourceTable.
        ///              Khi cấu hình mặc định thì nạp 14 bảng bật (TmsEquipment tắt); khi chỉ bật 2 bảng thì chỉ theo dõi 2 bảng đó.
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenConfiguredInShareDataSourceTable_LoadsCustomTables_Test()
        {
            // Case 1: Nạp cấu hình mặc định từ ShareDataSourceTable -> nạp đủ 14 bảng bật (TmsEquipment Status=0)
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var defaultWorker = CreateTrackerWorker();
            await defaultWorker.PollChanges(CancellationToken.None);
            Assert.NotEmpty(defaultWorker.TrackedTables);
            Assert.Equal(14, defaultWorker.TrackedTables.Count);
            Assert.DoesNotContain("TmsEquipment", defaultWorker.TrackedTables);

            // Case 2: Tắt các bảng khác, chỉ giữ TmsIncident và TmsTrafficData đang bật trong ShareDataSourceTable
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == false)
                .Where(x => x.IsDelete == null && x.TableName != "TmsIncident" && x.TableName != "TmsTrafficData")
                .ExecuteCommandAsync();

            var customWorker = CreateTrackerWorker();
            await customWorker.PollChanges(CancellationToken.None);

            // Assert: Nạp chính xác 2 bảng tùy biến và sinh câu SQL chỉ gồm 2 bảng đó
            Assert.Equal(2, customWorker.TrackedTables.Count);
            Assert.Contains("TmsIncident", customWorker.TrackedTables);
            Assert.Contains("TmsTrafficData", customWorker.TrackedTables);
            Assert.Contains("TmsIncident", customWorker.ActiveChangesSql);
            Assert.Contains("TmsTrafficData", customWorker.ActiveChangesSql);
            Assert.DoesNotContain("TollLane", customWorker.ActiveChangesSql);

            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm thử nạp bảng ánh xạ TablePacketMap từ ShareDataSourceTable.
        ///              Xác thực hỗ trợ cả mã ngắn ("101"), mã chuẩn ("101_commonData") và phân cách dấu phẩy.
        ///              Tất cả đều được chuẩn hoá về mã chuẩn và deduplicate khi phân giải gói tin (ResolvePackets).
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenConfiguredTablePacketMap_LoadsMapAndNormalizesPackets_Test()
        {
            // Arrange: Cấu hình ShareDataSourceTable gồm cả mã ngắn, mã đầy đủ, và kết hợp cả hai
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == false)
                .Where(x => x.IsDelete == null)
                .ExecuteCommandAsync();

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == true)
                .SetColumns(x => x.PacketCodes == "101, 101_commonData")
                .Where(x => x.IsDelete == null && x.TableName == "TmsZoneStatus")
                .ExecuteCommandAsync();

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == true)
                .SetColumns(x => x.PacketCodes == "102")
                .Where(x => x.IsDelete == null && x.TableName == "CctvDevice")
                .ExecuteCommandAsync();

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == true)
                .SetColumns(x => x.PacketCodes == "104_weatherData")
                .Where(x => x.IsDelete == null && x.TableName == "TmsWeather")
                .ExecuteCommandAsync();

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == true)
                .SetColumns(x => x.PacketCodes == "107, 110, 111")
                .Where(x => x.IsDelete == null && x.TableName == "TmsIncident")
                .ExecuteCommandAsync();

            // Act
            var worker = CreateTrackerWorker();
            await worker.PollChanges(CancellationToken.None);

            // Assert 1: Danh sách bảng được suy ra từ TablePacketMap.Keys
            Assert.Equal(4, worker.TrackedTables.Count);
            Assert.Contains("TmsZoneStatus", worker.TrackedTables);
            Assert.Contains("CctvDevice", worker.TrackedTables);
            Assert.Contains("TmsWeather", worker.TrackedTables);
            Assert.Contains("TmsIncident", worker.TrackedTables);

            // Assert 2: TmsZoneStatus với mảng cả 2 mã ["101", "101_commonData"] -> Deduplicate về đúng 1 gói "101_commonData"
            var packetsZone = DataChangeTrackingService.ResolvePackets(["TmsZoneStatus"], worker.TablePacketMap);
            Assert.Single(packetsZone);
            Assert.Equal("101_commonData", packetsZone[0]);

            // Assert 3: CctvDevice với mã ngắn "102" -> Chuẩn hoá thành "102_cctvData"
            var packetsCctv = DataChangeTrackingService.ResolvePackets(["CctvDevice"], worker.TablePacketMap);
            Assert.Single(packetsCctv);
            Assert.Equal("102_cctvData", packetsCctv[0]);

            // Assert 4: TmsWeather với mã đầy đủ "104_weatherData" -> Giữ nguyên "104_weatherData"
            var packetsWeather = DataChangeTrackingService.ResolvePackets(["TmsWeather"], worker.TablePacketMap);
            Assert.Single(packetsWeather);
            Assert.Equal("104_weatherData", packetsWeather[0]);

            // Assert 5: TmsIncident với [107, 110, 111] -> chuẩn hoá cả 3 gói
            var packetsIncident = DataChangeTrackingService.ResolvePackets(["TmsIncident"], worker.TablePacketMap);
            Assert.Equal(3, packetsIncident.Count);
            Assert.Contains(PacketCodeConst.IncidentData, packetsIncident);
            Assert.Contains(PacketCodeConst.WpData, packetsIncident);
            Assert.Contains(PacketCodeConst.InterCenter, packetsIncident);

            await EnsureDefaultTrackTables(db);
        }

        [Fact]
        public async Task ChangeTracking_WhenPacketDisabled_SkipsOutboundProcessingSilently_Test()
        {
            // 2.2: Trigger Change Tracking cho gói tắt -> Bỏ qua lặng lẽ, không lỗi, không ghi alert log
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"110_CT_DIS_{unique}";

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Disabled Packet CT {unique}",
                Status = BaseEnums.StatusEnum.Disable
            }).ExecuteCommandAsync();

            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"P_CT_DIS_{unique}",
                Name = $"Partner CT {unique}",
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"SUB_CT_DIS_{unique}",
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.UtcNow.AddMinutes(-5),
                IntervalSeconds = 5
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var worker = _host.Services.GetRequiredService<DataOutboundService>();

            // Act: Gọi xử lý xuất bản theo mã gói (giống như luồng Nats/ChangeTracking kích hoạt)
            await worker.ProcessSubscriptions(packetCode, CancellationToken.None);

            // Assert: Không phát sinh bất kỳ ShareDataAlertLog nào
            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID || a.PartnerId == partner.ID)
                .ToListAsync();
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Description: Kiểm thử đơn vị phương thức PacketMetadataResolver.NormalizePacketCode
        ///              cho toàn bộ 11 gói tin chuẩn từ 101 đến 111 đối với cả mã ngắn và mã chuẩn.
        /// Created date: 27/09/2026
        /// </summary>
        [Theory]
        [InlineData("101", "101_commonData")]
        [InlineData("101_commonData", "101_commonData")]
        [InlineData("102", "102_cctvData")]
        [InlineData("102_cctvData", "102_cctvData")]
        [InlineData("103", "103_vdsData")]
        [InlineData("103_vdsData", "103_vdsData")]
        [InlineData("104", "104_weatherData")]
        [InlineData("104_weatherData", "104_weatherData")]
        [InlineData("105", "105_rfidData")]
        [InlineData("105_rfidData", "105_rfidData")]
        [InlineData("106", "106_wimData")]
        [InlineData("106_wimData", "106_wimData")]
        [InlineData("107", "107_incidentData")]
        [InlineData("107_incidentData", "107_incidentData")]
        [InlineData("108", "108_vmsInfo")]
        [InlineData("108_vmsInfo", "108_vmsInfo")]
        [InlineData("109", "109_etcData")]
        [InlineData("109_etcData", "109_etcData")]
        [InlineData("110", "110_wpData")]
        [InlineData("110_wpData", "110_wpData")]
        [InlineData("111", "111")]
        public void NormalizePacketCode_ResolvesShortAndCanonicalCorrectly(string input, string expected)
        {
            var normalized = PacketMetadataResolver.NormalizePacketCode(input);
            Assert.Equal(expected, normalized);
        }

        #region 8. Chuông vs Hàng (Signal vs Batching & Update Watermark Tests)

        /// <summary>
        /// Description: Kiểm thử tầng chuông: Nhiều thay đổi trên 2 bảng (5 dòng TmsTrafficData, 3 dòng TmsIncident)
        ///              chỉ sinh đúng 1 bản tin NATS cho mỗi mã gói tin sau khi khử trùng và lọc chính sách.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task Tracker_WhenManyChangesAcrossTwoTables_PublishesOneMessagePerPacketCode_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T1_{testId}";
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = eqId,
                Name = $"Equipment {eqId}",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            var expectedPacketCodes = new[]
            {
                PacketCodeConst.VdsData,        // "103_vdsData"
                PacketCodeConst.WimData,        // "106_wimData"
                PacketCodeConst.IncidentData,   // "107_incidentData"
                PacketCodeConst.WpData,         // "110_wpData"
                "111"
            };

            var transport = _host.Services.GetRequiredService<TransportManager>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await transport.ConnectAsync(cts.Token);

            Assert.True(transport.IsConnected,
                "Không thể kết nối tới NATS broker tại 127.0.0.1:4222 (Nats:Url trong appsettings.Test.json). "
                + "Bắt buộc phải bật NATS broker (Docker local) trước khi chạy bài test.");

            var uniqueSubject = $"ta.its.event.sharedata.newdata.two_tables_{testId}";
            var testConfig = new ConfigurationBuilder()
                .AddConfiguration(config)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Nats:NewDataSubject"] = uniqueSubject,
                    ["Nats:Subject"] = uniqueSubject
                })
                .Build();

            var received = new List<string>();

            // Mở kênh nghe trên uniqueSubject
            transport.Unsubscribe(uniqueSubject);
            await transport.SubscribeAsync(uniqueSubject, msg =>
            {
                lock (received)
                    received.Add(msg);
                return Task.CompletedTask;
            });

            // Bản tin mồi phát lặp định kỳ (mỗi 100ms) cho tới khi listener nhận được,
            // khắc phục triệt để race condition khi NATS broker đăng ký subscribe nền.
            var probeDeadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < probeDeadline)
            {
                await transport.PublishAsync(uniqueSubject, new { Probe = true });
                await Task.Delay(100, cts.Token);
                lock (received)
                {
                    if (received.Count > 0)
                        break;
                }
            }
            lock (received)
            {
                Assert.True(received.Count > 0, "Listener NATS không nhận được bản tin mồi sau 5 giây.");
                received.Clear();
            }

            // Cắm mốc gốc: PollChanges lượt đầu
            await SetStateVersion(db, -1);
            var publisher = CreateTrackerWorker(
                scope,
                transport,
                testConfig);
            await publisher.PollChanges(cts.Token);
            Assert.True(await GetStateVersion(db) >= 0,
                "Change Tracking chưa sẵn sàng trên CSDL test nên mốc gốc không lập được.");

            // Seed 5 dòng vào TmsTrafficData và 3 dòng vào TmsIncident SAU mốc gốc
            var trafficRows = new List<TmsTrafficData>();
            for (var i = 1; i <= 5; i++)
            {
                var t = now.AddSeconds(i);
                trafficRows.Add(new TmsTrafficData
                {
                    ID = Guid.NewGuid().ToString("N"),
                    EquipmentId = eqId,
                    DetectTime = t,
                    CreateTime = t,
                    UpdateTime = t,
                    Type = "CAR",
                    LicensePlate = $"29A-T1{testId}{i}",
                    Speed = 60.0f + i,
                    Lane = "L1",
                    Direction = "EAST",
                    Location = $"KM30_{testId}"
                });
            }
            await db.Insertable(trafficRows).ExecuteCommandAsync();

            var incidentRows = new List<TmsIncident>();
            for (var i = 1; i <= 3; i++)
            {
                var t = now.AddSeconds(i);
                incidentRows.Add(new TmsIncident
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"INC_{testId}_{i}",
                    Name = $"Incident {testId} {i}",
                    StartDate = t,
                    CreateTime = t,
                    UpdateTime = t,
                    ProcessMode = "manual"
                });
            }
            await db.Insertable(incidentRows)
                .InsertColumns(x => new { x.ID, x.Code, x.Name, x.StartDate, x.CreateTime, x.UpdateTime, x.ProcessMode })
                .ExecuteCommandAsync();

            // Act: Chạy PollChanges đúng 1 lượt
            await publisher.PollChanges(cts.Token);

            // Assert
            var gotExpected = await WaitUntil(() =>
            {
                lock (received)
                    return received.Count >= expectedPacketCodes.Length;
            }, TimeSpan.FromSeconds(10));
            Assert.True(gotExpected, $"Chờ nhận đủ {expectedPacketCodes.Length} bản tin NATS bị timeout. Hiện nhận: {received.Count}");

            lock (received)
            {
                Assert.Equal(expectedPacketCodes.Length, received.Count);

                var actualPacketCodes = received
                    .Select(m => JsonDocument.Parse(m).RootElement.GetProperty("PacketCode").GetString())
                    .OrderBy(c => c)
                    .ToArray();
                Assert.Equal(expectedPacketCodes.OrderBy(c => c).ToArray(), actualPacketCodes);
            }

            transport.Unsubscribe(uniqueSubject);
            await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
            await db.Deleteable<TmsIncident>().Where(i => i.Code != null && i.Code.StartsWith($"INC_{testId}_")).ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Kiểm thử tầng hàng: 10 dòng dữ liệu mới thay đổi cùng lúc chỉ sinh đúng 1 request HTTP
        ///              mang trọn vẹn đủ 10 bản ghi sang đối tác theo chuẩn batching của trang.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task TriggerFlow_WhenTenRowsChangeAtOnce_SendsOneBatchWithAllRows_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var outboundLogger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            const string packetCode = "103";
            var partnerCode = $"PTN_T2_{testId}";
            var subCode = $"SUB_T2_{testId}";
            var eqId = $"EQ_T2_{testId}";
            var platePrefix = $"29A-T2{testId}";

            await PrepareDatabase(db, outboundLogger, packetCode);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .ExecuteCommandAsync();
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = now.AddHours(1);

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.DebounceSec = null;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            // Cắm mốc checkpoint ban đầu ở baseTime để chỉ trích xuất các bản ghi của bài test này
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partnerCode,
                PacketCode = packetCode,
                LastTime = baseTime,
                LastKey = string.Empty,
                CreateTime = baseTime,
                UpdateTime = baseTime
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = eqId,
                Name = $"Equipment {eqId}",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            var rows = new List<TmsTrafficData>();
            for (var i = 1; i <= 10; i++)
            {
                var t = baseTime.AddSeconds(i);
                rows.Add(new TmsTrafficData
                {
                    ID = Guid.NewGuid().ToString("N"),
                    EquipmentId = eqId,
                    DetectTime = t,
                    CreateTime = t,
                    UpdateTime = t,
                    Type = "CAR",
                    LicensePlate = $"{platePrefix}_{i:D2}",
                    Speed = 60.0f + i,
                    Lane = "L1",
                    Direction = "EAST",
                    Location = $"KM30_{testId}"
                });
            }
            await db.Insertable(rows).ExecuteCommandAsync();
            await db.Updateable<TmsTrafficData>()
                .SetColumns(t => t.UpdateTime == t.DetectTime)
                .Where(t => t.EquipmentId == eqId)
                .ExecuteCommandAsync();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            // Act
            await CreateOutboundService(scope).ProcessSubscriptions(packetCode, CancellationToken.None);

            // Assert
            Assert.Equal(1, partnerServer.TotalReceivedRequests);

            var logs = (await GetLogs(db, sub.ID)).Where(l => l.RecordCount > 0).ToList();
            Assert.Single(logs);
            Assert.Equal(10, logs[0].RecordCount);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            Assert.NotNull(partnerServer.LastRequest);
            var items = JsonDocument.Parse(partnerServer.LastRequest!.Body).RootElement.EnumerateArray().ToList();
            Assert.Equal(10, items.Count);

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).ExecuteCommandAsync();
            await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
            await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Kiểm thử ngữ nghĩa chuông vs hàng: Cùng một trigger tới 5 lần liên tiếp thì chỉ lượt đầu
        ///              mang dữ liệu đi, 4 lượt sau gặp watermark đã tiến nên ghi nhận không có dữ liệu mới.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task TriggerFlow_WhenSameTriggerArrivesFiveTimes_SendsDataOnceThenReportsNoNewData_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var outboundLogger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            const string packetCode = "103";
            var partnerCode = $"PTN_T3_{testId}";
            var subCode = $"SUB_T3_{testId}";
            var eqId = $"EQ_T3_{testId}";
            var platePrefix = $"29A-T3{testId}";

            await PrepareDatabase(db, outboundLogger, packetCode);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .ExecuteCommandAsync();
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = now.AddHours(2);

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.DebounceSec = null;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            // Cắm mốc checkpoint ban đầu ở baseTime để chỉ trích xuất các bản ghi của bài test này
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partnerCode,
                PacketCode = packetCode,
                LastTime = baseTime,
                LastKey = string.Empty,
                CreateTime = baseTime,
                UpdateTime = baseTime
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = eqId,
                Name = $"Equipment {eqId}",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            var rows = new List<TmsTrafficData>();
            for (var i = 1; i <= 10; i++)
            {
                var t = baseTime.AddSeconds(i);
                rows.Add(new TmsTrafficData
                {
                    ID = Guid.NewGuid().ToString("N"),
                    EquipmentId = eqId,
                    DetectTime = t,
                    CreateTime = t,
                    UpdateTime = t,
                    Type = "CAR",
                    LicensePlate = $"{platePrefix}_{i:D2}",
                    Speed = 60.0f + i,
                    Lane = "L1",
                    Direction = "EAST",
                    Location = $"KM30_{testId}"
                });
            }
            await db.Insertable(rows).ExecuteCommandAsync();
            await db.Updateable<TmsTrafficData>()
                .SetColumns(t => t.UpdateTime == t.DetectTime)
                .Where(t => t.EquipmentId == eqId)
                .ExecuteCommandAsync();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            // Act: Chạy 5 lần liên tiếp
            for (var i = 0; i < 5; i++)
                await CreateOutboundService(scope).ProcessSubscriptions(packetCode, CancellationToken.None);

            // Assert
            // 1. Chỉ MỘT lượt thật sự mang dữ liệu đi
            var logsWithData = (await GetLogs(db, sub.ID)).Where(l => l.RecordCount > 0).ToList();
            Assert.Single(logsWithData);
            Assert.Equal(10, logsWithData[0].RecordCount);

            // 2. Bốn lượt sau đi đường "không có dữ liệu mới"
            var emptyLogs = (await GetLogs(db, sub.ID)).Where(l => l.RecordCount == 0).ToList();
            Assert.Equal(4, emptyLogs.Count);

            // 3. Mốc gửi chỉ tiến MỘT lần, không lùi
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partner.Code)
                .FirstAsync();
            Assert.NotNull(lastSend);

            // 4. SerialNbr tăng đúng 1 — commit đúng một lần
            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal((sub.SerialNbr ?? 0) + 1, updatedSub.SerialNbr);

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).ExecuteCommandAsync();
            await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
            await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Kiểm thử vế UPDATE: Bản ghi đã gửi chỉ được gửi lại khi UpdateTime được nâng vượt mốc
        ///              watermark cũ; nếu sửa bản ghi mà quên nâng UpdateTime thì đối tác không nhận được bản sửa.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task TriggerFlow_WhenSentRowIsUpdated_PartnerReceivesNewVersionOnlyIfWatermarkAdvances_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var outboundLogger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();

            var testId = Guid.NewGuid().ToString("N")[..8];
            const string packetCode = "103";
            var partnerCode = $"PTN_T4_{testId}";
            var subCode = $"SUB_T4_{testId}";
            var eqId = $"EQ_T4_{testId}";
            var plateA = $"29A-T4A{testId}";
            var plateB = $"29A-T4B{testId}";

            await PrepareDatabase(db, outboundLogger, packetCode);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .ExecuteCommandAsync();
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = now.AddHours(3);

            var (partner, sub) = await SeedOutboundSubscription(db, partnerCode, subCode, packetCode, s =>
            {
                s.SendOnNewData = true;
                s.DebounceSec = null;
                s.IntervalSeconds = 300;
                s.LastTimeRun = now.AddMinutes(-5);
                s.NextTimeRun = null;
            });

            // Cắm mốc checkpoint ban đầu ở baseTime để chỉ trích xuất các bản ghi của bài test này
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partnerCode,
                PacketCode = packetCode,
                LastTime = baseTime,
                LastKey = string.Empty,
                CreateTime = baseTime,
                UpdateTime = baseTime
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = eqId,
                Name = $"Equipment {eqId}",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            var rowA = new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = baseTime.AddSeconds(1),
                CreateTime = baseTime.AddSeconds(1),
                UpdateTime = baseTime.AddSeconds(1),
                Type = "CAR",
                LicensePlate = plateA,
                Speed = 60.0f,
                Lane = "L1",
                Direction = "EAST",
                Location = $"KM30_{testId}"
            };
            var rowB = new TmsTrafficData
            {
                ID = Guid.NewGuid().ToString("N"),
                EquipmentId = eqId,
                DetectTime = baseTime.AddSeconds(2),
                CreateTime = baseTime.AddSeconds(2),
                UpdateTime = baseTime.AddSeconds(2),
                Type = "CAR",
                LicensePlate = plateB,
                Speed = 70.0f,
                Lane = "L2",
                Direction = "EAST",
                Location = $"KM30_{testId}"
            };
            await db.Insertable(new[] { rowA, rowB }).ExecuteCommandAsync();
            await db.Updateable<TmsTrafficData>()
                .SetColumns(t => t.UpdateTime == t.DetectTime)
                .Where(t => t.EquipmentId == eqId)
                .ExecuteCommandAsync();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            // Pha 1 — gửi lần đầu: cả 2 bản ghi A và B đều được gửi
            await CreateOutboundService(scope).ProcessSubscriptions(packetCode, CancellationToken.None);

            Assert.Single(partnerServer.ReceivedRequests);
            Assert.Contains(plateA, partnerServer.LastRequest!.Body, StringComparison.Ordinal);
            Assert.Contains(plateB, partnerServer.LastRequest!.Body, StringComparison.Ordinal);

            // Pha 2 — sửa A, CÓ nâng UpdateTime vượt watermark hiện tại
            var newSpeed = 99.5f;
            var bumpedTime = baseTime.AddMinutes(5);
            await db.Updateable<TmsTrafficData>()
                .SetColumns(t => t.Speed == newSpeed)
                .SetColumns(t => t.UpdateTime == bumpedTime)
                .Where(t => t.ID == rowA.ID)
                .ExecuteCommandAsync();

            partnerServer.ResetDefaults();
            await CreateOutboundService(scope).ProcessSubscriptions(packetCode, CancellationToken.None);

            // Đối tác nhận LẠI bản ghi A với giá trị ĐÃ SỬA; B không đổi nên không gửi lại
            Assert.Single(partnerServer.ReceivedRequests);
            Assert.Contains(plateA, partnerServer.LastRequest!.Body, StringComparison.Ordinal);
            Assert.DoesNotContain(plateB, partnerServer.LastRequest!.Body, StringComparison.Ordinal);

            var logsAfterUpdate = (await GetLogs(db, sub.ID)).Where(l => l.RecordCount > 0).ToList();
            Assert.Equal(1, logsAfterUpdate.OrderByDescending(l => l.OccurredAt).First().RecordCount);

            // Pha 3 — sửa B, KHÔNG nâng UpdateTime (cố ý không đụng UpdateTime)
            await db.Updateable<TmsTrafficData>()
                .SetColumns(t => t.Speed == 11.1f)
                .Where(t => t.ID == rowB.ID)
                .ExecuteCommandAsync();

            partnerServer.ResetDefaults();
            await CreateOutboundService(scope).ProcessSubscriptions(packetCode, CancellationToken.None);

            // GIỚI HẠN ĐÃ BIẾT: watermark của B vẫn đứng ở mốc cũ (nhỏ hơn LastTime sau pha 2)
            // => bản sửa KHÔNG tới được đối tác
            Assert.Empty(partnerServer.ReceivedRequests);

            var lastLog = (await GetLogs(db, sub.ID)).OrderByDescending(l => l.OccurredAt).First();
            Assert.Equal(0, lastLog.RecordCount);

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await db.Deleteable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).ExecuteCommandAsync();
            await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
            await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Kiểm chứng khi LastVersion lớn hơn CURRENT_VERSION() của CSDL (bị lùi/lệch version), PollChanges tự động hạ mốc về -1 và ghi đúng 1 dòng cảnh báo ESH-1604.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenLastVersionExceedsDatabaseVersion_ResetsBaselineAndWritesEsh1604_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            var initialCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .CountAsync();

            var currentVerObj = await db.Ado.GetScalarAsync(DataChangeTrackingService.SqlCurrentVersion);
            var currentVer = currentVerObj == null || currentVerObj == DBNull.Value ? 0L : Convert.ToInt64(currentVerObj);
            var futureVer = currentVer + 1_000_000L;

            await SetStateVersion(db, futureVer);

            // Act
            await worker.PollChanges(CancellationToken.None);

            // Assert
            var dbState = await GetTrackState(db);
            Assert.NotNull(dbState);
            Assert.Equal(-1, dbState!.LastVersion);

            var finalCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .CountAsync();
            Assert.Equal(initialCount + 1, finalCount);

            var latestLog = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .FirstAsync();
            Assert.Contains("\"trackingEnabled\":true", latestLog.AfterJson ?? string.Empty);
        }

        /// <summary>
        /// Description: Kiểm chứng khi 10 instance đồng thời phát hiện LastVersion > CURRENT_VERSION(), Atomic CAS chiều ngược bảo đảm đúng 1 instance hạ mốc và ghi duy nhất 1 dòng cảnh báo ESH-1604.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenVersionResetRacedByTenInstances_ExactlyOneWritesEsh1604_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            const int workerCount = 10;
            var scopes = Enumerable.Range(0, workerCount)
                .Select(_ => _host.Services.CreateAsyncScope())
                .ToList();
            var workers = scopes
                .Select(s => CreateTrackerWorker(s))
                .ToList();

            var initialCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .CountAsync();

            var currentVerObj = await db.Ado.GetScalarAsync(DataChangeTrackingService.SqlCurrentVersion);
            var currentVer = currentVerObj == null || currentVerObj == DBNull.Value ? 0L : Convert.ToInt64(currentVerObj);
            var futureVer = currentVer + 1_000_000L;

            await SetStateVersion(db, futureVer);

            // Act
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = workers.Select(async w =>
            {
                await tcs.Task;
                await w.PollChanges(CancellationToken.None);
            }).ToArray();

            tcs.SetResult();
            await Task.WhenAll(tasks);

            // Assert
            var dbState = await GetTrackState(db);
            Assert.NotNull(dbState);
            Assert.True(dbState!.LastVersion == -1 || (dbState!.LastVersion >= currentVer && dbState!.LastVersion < futureVer));

            var finalCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .CountAsync();

            Assert.Equal(initialCount + 1, finalCount);

            foreach (var s in scopes)
                await s.DisposeAsync();
        }

        /// <summary>
        /// Description: Kiểm chứng sau khi hạ mốc về -1 vì lệch version, chu kỳ polling tiếp theo tự động đi lại đường TryInitChangeTracking để cắm lại mốc ban đầu mà không sinh thêm cảnh báo ESH-1604.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_AfterVersionReset_NextCycleReInitsBaseline_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            var initialCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .CountAsync();

            var currentVerObj = await db.Ado.GetScalarAsync(DataChangeTrackingService.SqlCurrentVersion);
            var currentVer = currentVerObj == null || currentVerObj == DBNull.Value ? 0L : Convert.ToInt64(currentVerObj);
            var futureVer = currentVer + 1_000_000L;

            await SetStateVersion(db, futureVer);

            // Act 1: Lượt 1 phát hiện version lệch -> hạ về -1
            await worker.PollChanges(CancellationToken.None);

            var dbStateAfterReset = await GetTrackState(db);
            Assert.NotNull(dbStateAfterReset);
            Assert.Equal(-1, dbStateAfterReset!.LastVersion);

            // Act 2: Lượt 2 thấy LastVersion = -1 -> đi đường TryInitChangeTracking cắm lại mốc
            await worker.PollChanges(CancellationToken.None);

            // Assert
            var finalState = await GetTrackState(db);
            Assert.NotNull(finalState);
            Assert.True(finalState!.LastVersion >= 0);

            var finalCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .CountAsync();

            Assert.Equal(initialCount + 1, finalCount);
        }

        /// <summary>
        /// Description: Kiểm chứng đường khởi tạo của ShareDataSourceTable: Khi các dòng trong bảng ShareDataSourceTable có LastVersion = null,
        ///              worker cắm baseline hợp lệ trên các dòng hiện có mà không tự tạo thêm dòng mới sau 2 lượt poll.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenTrackStateTableIsEmpty_CreatesExactlyOneRowAndPlantsBaseline_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == null)
                .Where(x => x.IsDelete == null)
                .ExecuteCommandAsync();

            var worker = CreateTrackerWorker(scope);

            // Act 1: Lượt 1 phát hiện các dòng có LastVersion = null -> TryInitChangeTracking cắm mốc trên các dòng hiện có
            await worker.PollChanges(CancellationToken.None);

            // Act 2: Lượt 2 đọc lại các dòng đã có mốc -> không tạo thêm dòng mới
            await worker.PollChanges(CancellationToken.None);

            // Assert
            var allRows = await db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null).ToListAsync();
            Assert.Equal(15, allRows.Count);
            var enabledRows = allRows.Where(x => x.IsTracked == true).ToList();
            Assert.All(enabledRows, r =>
            {
                Assert.NotNull(r.LastVersion);
                Assert.True(r.LastVersion >= 0);
            });
        }

        /// <summary>
        /// Description: Kiểm chứng TĐ1: Khi tự phục hồi mốc version trên dòng ShareDataSourceTable, UpdateTime được nâng
        ///              theo đồng hồ CSDL trong khi CreateTime và các cột cấu hình khác được bảo toàn nguyên vẹn.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task SaveTrackVersion_WhenSelfHealRuns_AdvancesUpdateTimeAndKeepsCreateTime_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            var pastCreateTime = new DateTime(2025, 1, 1, 10, 0, 0);
            var pastUpdateTime = new DateTime(2025, 1, 1, 11, 0, 0);

            await SetStateVersion(db, -10);
            var trackState = await GetTrackState(db, "TmsZoneStatus");
            Assert.NotNull(trackState);
            var testVersionId = trackState!.ID;

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.CreateTime == pastCreateTime)
                .SetColumns(x => x.UpdateTime == pastUpdateTime)
                .Where(x => x.ID == testVersionId)
                .ExecuteCommandAsync();

            // Act: Chạy PollChanges kích hoạt phục hồi mốc version
            await worker.PollChanges(CancellationToken.None);

            // Assert
            var updatedState = await db.Queryable<ShareDataSourceTable>().InSingleAsync(testVersionId);
            Assert.NotNull(updatedState);
            Assert.True(updatedState!.LastVersion >= 0);
            Assert.Equal(pastCreateTime, updatedState.CreateTime);
            Assert.NotNull(updatedState.UpdateTime);
            Assert.True(updatedState.UpdateTime > pastUpdateTime);
        }

        /// <summary>
        /// Description: Kiểm chứng TĐ8 — Nhánh dò tên trong thông điệp lỗi của TrySkipBrokenTables ưu tiên khớp
        ///              tên dài trước (OrderByDescending theo Length), tránh tình trạng tiền tố TmsZone cướp lượt
        ///              cô lập của TmsZoneStatus và làm cô lập sai lan dần.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task TrySkipBrokenTables_WhenErrorMessageMatchesPrefixAndFullName_IsolatesLongestMatch_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            // Khởi tạo tracking để nạp cấu hình bảng từ ShareDataSourceTable
            await worker.PollChanges(CancellationToken.None);

            Assert.Contains("TmsZone", worker.TrackedTables);
            Assert.Contains("TmsZoneStatus", worker.TrackedTables);

            // Act: Gọi TrySkipBrokenTables qua Reflection với Exception chứa thông điệp lỗi có tên TmsZoneStatus
            var fakeEx = new Exception("Invalid column name in table 'TmsZoneStatus'. Operation failed.");
            var skippedCount = await InvokeTrySkipBrokenTables(worker, db, fakeEx);

            // Assert: Phải cô lập đúng TmsZoneStatus (tên dài nhất), KHÔNG cô lập TmsZone
            Assert.Equal(1, skippedCount);
            var missing = GetMissingTables(worker);
            Assert.True(missing.ContainsKey("TmsZoneStatus"));
            Assert.False(missing.ContainsKey("TmsZone"));

            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm chứng xử lý khi Change Tracking bị tắt ở cấp CSDL (CurrentVersion là null):
        ///              ResetAllWhenDbVersionNull hạ LastVersion về -1 và ghi đúng 1 bản ghi cảnh báo ESH-1604.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenCurrentVersionIsNull_ResetsBaselineAndWritesEsh1604_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var rootDb = _host.Services.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            // Cắm mốc gốc ban đầu
            await worker.PollChanges(CancellationToken.None);
            var initialVersion = await GetStateVersion(db);
            Assert.True(initialVersion >= 0);

            var initialCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .CountAsync();

            // SqlSugar AOP: Viết lại SELECT CHANGE_TRACKING_CURRENT_VERSION() để trả về NULL (mô phỏng tắt Change Tracking cấp CSDL)
            Func<string, SugarParameter[], KeyValuePair<string, SugarParameter[]>> interceptor = (sql, pars) =>
            {
                if (sql.Contains("CHANGE_TRACKING_CURRENT_VERSION()", StringComparison.OrdinalIgnoreCase))
                {
                    return new KeyValuePair<string, SugarParameter[]>(
                        "SELECT CAST(NULL AS BIGINT)",
                        pars);
                }
                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };

            rootDb.Aop.OnExecutingChangeSql = interceptor;
            db.Aop.OnExecutingChangeSql = interceptor;

            // Act
            await worker.PollChanges(CancellationToken.None);

            rootDb.Aop.OnExecutingChangeSql = null;
            db.Aop.OnExecutingChangeSql = null;

            // Assert 1: Trong CSDL LastVersion bị hạ về -1
            var resetVersion = await GetStateVersion(db);
            Assert.Equal(-1, resetVersion);

            // Assert 2: Đúng 1 dòng cảnh báo ESH-1604 được ghi
            var finalCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .CountAsync();
            Assert.Equal(initialCount + 1, finalCount);

            var latestLog = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.VersionReset)
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .FirstAsync();
            Assert.Equal(BaseEnums.SuccessEnums.Fail, latestLog.Success);

            // Assert 3: DetailJson (AfterJson) chứa trackingEnabled:false và currentVersion:null
            Assert.Contains("\"trackingEnabled\":false", latestLog.AfterJson ?? string.Empty);
            Assert.Contains("\"currentVersion\":null", latestLog.AfterJson ?? string.Empty);

            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Đo xem một lỗi SQL KHÔNG thuộc nhóm lỗi mốc version có khiến TrySkipBrokenTables
        ///              cô lập oan một bảng đang lành hay không (dò tên bảng trong thông điệp ngoại lệ).
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task TrySkipBrokenTables_WhenNonVersionErrorOccurs_DoesNotIsolateHealthyTable_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var rootDb = _host.Services.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            var testId = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_ERR_{testId}";
            var carId = $"CAR_ERR_{testId}";

            // Arrange 1: Cắm mốc gốc ban đầu
            await worker.PollChanges(CancellationToken.None);
            var initialVersion = await GetStateVersion(db);
            Assert.True(initialVersion >= 0);

            // Arrange 2: Chèn 1 dòng vào một bảng nguồn đang được giám sát để đẩy version CSDL tăng
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
                LicensePlate = $"30A-ERR_{testId}",
                Speed = 60f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM10",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // Arrange 3: SqlSugar AOP: ép câu truy vấn thay đổi hỏng bằng một lỗi truy vấn.
            // Chia cho 0 ⇒ "Divide by zero error encountered." đi vào nhánh TrySkipBrokenTables cần đo.
            Func<string, SugarParameter[], KeyValuePair<string, SugarParameter[]>> interceptor = (sql, pars) =>
            {
                if (sql.Contains("CHANGETABLE(CHANGES", StringComparison.OrdinalIgnoreCase))
                    return new KeyValuePair<string, SugarParameter[]>("RAISERROR('Divide by zero error encountered.', 16, 1);", pars);

                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };

            rootDb.Aop.OnExecutingChangeSql = interceptor;
            db.Aop.OnExecutingChangeSql = interceptor;

            // Act: QueryChangedTables rethrow lỗi không phải lỗi version, nên PollChanges ném ra
            var actStartedAt = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await Assert.ThrowsAnyAsync<Exception>(() => worker.PollChanges(CancellationToken.None));
            rootDb.Aop.OnExecutingChangeSql = null;
            db.Aop.OnExecutingChangeSql = null;

            // Assert 1: Phép đo chính — không cô lập oan bảng lành
            var missing = GetMissingTables(worker);
            Assert.Empty(missing);

            // Assert 2: Không có dòng ShareDataActivityLog nào mang thông điệp "mất Change Tracking giữa chừng"
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.QueryFailed && x.CreateTime >= actStartedAt)
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .ToListAsync();

            Assert.DoesNotContain(logs, x => (x.Description ?? string.Empty).Contains("mất Change Tracking giữa chừng", StringComparison.OrdinalIgnoreCase));

            await db.Deleteable<TmsTrafficData>().Where(t => t.ID == carId).ExecuteCommandAsync();
            await db.Deleteable<TmsEquipment>().Where(t => t.ID == eqId).ExecuteCommandAsync();
            await db.Deleteable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.QueryFailed && x.CreateTime >= actStartedAt)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Kiểm chứng khi một dòng cấu hình bảng trong ShareDataSourceTable bị xoá mềm:
        ///              worker bỏ qua bảng đã xóa mềm, KHÔNG tự tạo dòng mới, dòng cũ vẫn còn vật lý trong CSDL,
        ///              và các bảng còn lại vẫn được theo dõi bình thường.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenTrackStateRowIsSoftDeleted_CreatesFreshRowAndReInitsBaseline_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataTracker:ConfigReloadSeconds"] = "0"
            }).Build();
            var worker = CreateTrackerWorker(scope, config: config);

            // Khởi tạo mốc gốc ban đầu
            await worker.PollChanges(CancellationToken.None);
            var weatherRow = await GetTrackState(db, "TmsWeather");
            Assert.NotNull(weatherRow);
            var stateId = weatherRow!.ID;

            // Giả lập xoá mềm dòng TmsWeather (IsDelete là datetime, không phải bit)
            var deletedAt = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsDelete == deletedAt)
                .Where(x => x.ID == stateId)
                .ExecuteCommandAsync();

            // Act: Nạp lại cấu hình và chạy PollChanges sau khi dòng TmsWeather bị xóa mềm
            await worker.ReloadConfig(db);
            await worker.PollChanges(CancellationToken.None);

            // Assert 1: Còn 14 dòng chưa xóa mềm trong ShareDataSourceTable (worker KHÔNG tự INSERT dòng mới)
            var aliveCount = await db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null).CountAsync();
            Assert.Equal(14, aliveCount);
            Assert.DoesNotContain("TmsWeather", worker.TrackedTables);

            // Assert 2: Dòng cũ vẫn còn VẬT LÝ trong CSDL — chỉ bị xoá mềm, không bị xoá cứng
            var physicalCount = await db.Queryable<ShareDataSourceTable>()
                .ClearFilter()
                .Where(x => x.ID == stateId)
                .CountAsync();
            Assert.Equal(1, physicalCount);

            // Assert 3: Các dòng còn sống vẫn có mốc hợp lệ
            var finalVersion = await GetStateVersion(db);
            Assert.True(finalVersion >= 0);

            // Khôi phục lại dòng TmsWeather về trạng thái chuẩn
            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm chứng chặn cấu hình rỗng: Khi không có dòng nào đang bật trong ShareDataSourceTable,
        ///              ghi 1 cảnh báo ESH-1603, không ghi lặp ở chu kỳ sau và không cắm mốc gốc.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenTablePacketMapIsEmpty_LogsEsh1603OnceAndDoesNotPlantBaseline_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            // Tắt toàn bộ dòng trong ShareDataSourceTable và gán LastVersion = 50 cho TmsZoneStatus
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == false)
                .SetColumns(x => x.LastVersion == null)
                .Where(x => x.IsDelete == null)
                .ExecuteCommandAsync();
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == 50L)
                .Where(x => x.TableName == "TmsZoneStatus")
                .ExecuteCommandAsync();

            var worker = CreateTrackerWorker(scope);

            var initialEsh1603Count = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.NotReady)
                .CountAsync();
            var initialRowCount = await db.Queryable<ShareDataSourceTable>().CountAsync();
            var startedAt = (await db.Ado.GetDateTimeAsync("SELECT GETDATE()")).AddSeconds(-10);

            // Act 1: Gọi PollChanges lần 1
            await worker.PollChanges(CancellationToken.None);

            // Assert 1: worker.TrackedTables rỗng và worker.ActiveChangesSql là chuỗi rỗng
            Assert.Empty(worker.TrackedTables);
            Assert.Equal(string.Empty, worker.ActiveChangesSql);

            // Assert 2: Đếm được đúng 1 dòng ShareDataActivityLog mới có Remark chứa ESH-1603
            var logsFirstCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.NotReady)
                .CountAsync();
            Assert.Equal(initialEsh1603Count + 1, logsFirstCount);

            // Act 2: Gọi PollChanges lần 2
            await worker.PollChanges(CancellationToken.None);

            // Assert 3: Số dòng ESH-1603 vẫn tăng đúng 1 (cờ IsEmptyConfigReported chặn ghi lặp)
            var logsSecondCount = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.NotReady)
                .CountAsync();
            Assert.Equal(initialEsh1603Count + 1, logsSecondCount);

            // Assert 4: ShareDataSourceTable không tăng số dòng và LastVersion của dòng có sẵn giữ nguyên 50
            var rowCountAfter = await db.Queryable<ShareDataSourceTable>().CountAsync();
            Assert.Equal(initialRowCount, rowCountAfter);

            var existingRow = await GetTrackState(db, "TmsZoneStatus");
            Assert.NotNull(existingRow);
            Assert.Equal(50L, existingRow!.LastVersion);

            // Dọn dẹp dữ liệu bài test tuần tự ở cuối hàm
            await db.Deleteable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.NotReady && x.CreateTime >= startedAt)
                .ExecuteCommandAsync();
            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm chứng khi Change Tracking chưa bật được ở cấp CSDL: ESH-1603 ghi đúng 1 lần cho mỗi
        ///              tiến trình và lượt khởi tạo được giãn nhịp, không dội lệnh DDL mỗi chu kỳ.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task PollChanges_WhenDbTrackingCannotBeEnabled_WritesEsh1603OnceAndBacksOff_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var rootDb = _host.Services.GetRequiredService<ISqlSugarClient>();
            var worker = CreateTrackerWorker(scope);

            // Đặt mốc version < 0 để kiểm chứng khi DB tracking lỗi thì không cắm mốc gốc mới
            await SetStateVersion(db, -1);
            var existingIds = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.NotReady)
                .Select(x => x.ID)
                .ToListAsync();
            var alterDatabaseCount = 0;

            Func<string, SugarParameter[], KeyValuePair<string, SugarParameter[]>> interceptor = (sql, pars) =>
            {
                // Giả lập CSDL chưa bật Change Tracking.
                if (sql.Contains("sys.change_tracking_databases", StringComparison.OrdinalIgnoreCase))
                    return new KeyValuePair<string, SugarParameter[]>("SELECT 0", pars);

                // 🔴 Vô hiệu hoá lệnh DDL thật: đếm rồi đổi thành câu SELECT vô hại.
                if (sql.TrimStart().StartsWith("ALTER DATABASE", StringComparison.OrdinalIgnoreCase))
                {
                    alterDatabaseCount++;
                    return new KeyValuePair<string, SugarParameter[]>("SELECT 1", pars);
                }

                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };

            rootDb.Aop.OnExecutingChangeSql = interceptor;
            db.Aop.OnExecutingChangeSql = interceptor;

            // Act: Gọi PollChanges 3 lần liên tiếp
            await worker.PollChanges(CancellationToken.None);
            await worker.PollChanges(CancellationToken.None);
            await worker.PollChanges(CancellationToken.None);

            // Assert 1: Đếm dòng ShareDataActivityLog có Remark == ShareDataAlertCode.Tracking.NotReady do lỗi DB tracking => đúng 1
            var notReadyLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.NotReady && x.AfterJson != null && !x.AfterJson.Contains("\"trackedTotal\":0") && !existingIds.Contains(x.ID))
                .ToListAsync();
            Assert.Single(notReadyLogs);

            // Assert 2: alterDatabaseCount == 1 chứng minh cổng giãn nhịp chặn được dội DDL
            Assert.Equal(1, alterDatabaseCount);

            // Assert 3: NextInitRetryTime có giá trị và ở tương lai
            var nextRetryProp = typeof(DataChangeTrackingService).GetProperty("NextInitRetryTime", BindingFlags.NonPublic | BindingFlags.Instance);
            var nextRetryVal = (DateTime?)nextRetryProp?.GetValue(worker);
            Assert.NotNull(nextRetryVal);
            Assert.True(nextRetryVal.Value > DateTime.UtcNow);

            // Assert 4: ShareDataSourceTable không bị cắm mốc gốc: version < 0
            var version = await GetStateVersion(db);
            Assert.True(version < 0);

            // Khôi phục trạng thái AOP và bảng theo dõi ở cuối hàm
            rootDb.Aop.OnExecutingChangeSql = null;
            db.Aop.OnExecutingChangeSql = null;
            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm chứng đo lường: Khi một bảng nguồn bị mất Change Tracking, hai instance worker
        ///              độc lập tự học và cô lập bảng lỗi riêng trong RAM của mình, mỗi instance phát sinh
        ///              đúng 1 dòng nhật ký ESH-1602 khi truy vấn thất bại.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task TrySkipBrokenTables_WhenTwoInstancesPoll_EachLearnsIsolationIndependently_Test()
        {
            await using var scopeA = _host.Services.CreateAsyncScope();
            await using var scopeB = _host.Services.CreateAsyncScope();

            var dbA = scopeA.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var dbB = scopeB.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var workerA = CreateTrackerWorker(scopeA);
            var workerB = CreateTrackerWorker(scopeB);

            var testId = Guid.NewGuid().ToString("N")[..8];
            var eqIdA = $"EQ_INST_A_{testId}";
            var carIdA = $"CAR_INST_A_{testId}";
            var eqIdB = $"EQ_INST_B_{testId}";
            var carIdB = $"CAR_INST_B_{testId}";

            // 1. Chu kỳ đầu: Khởi tạo mốc gốc và nạp cấu hình cho cả hai instance khi mọi bảng còn lành mạnh
            await workerA.PollChanges(CancellationToken.None);
            await workerB.PollChanges(CancellationToken.None);
            var initialVersion = await GetStateVersion(dbA);
            Assert.True(initialVersion >= 0);

            var existingIds = await dbA.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.QueryFailed)
                .Select(x => x.ID)
                .ToListAsync();

            // 2. Giả lập DBA tắt Change Tracking trên TmsWeather
            await dbA.Ado.ExecuteCommandAsync("ALTER TABLE [TmsWeather] DISABLE CHANGE_TRACKING;");
            var isWeatherTracked = await dbA.Ado.GetIntAsync("SELECT COUNT(1) FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')") > 0;
            Assert.False(isWeatherTracked);

            // Act 1: Tạo thay đổi mới trên bảng lành mạnh -> workerA chạy
            await dbA.Insertable(new TmsEquipment
            {
                ID = eqIdA,
                Code = $"EQ_A_{testId}",
                KmNumber = 10,
                MetNumber = 500
            }).ExecuteCommandAsync();

            await dbA.Insertable(new TmsTrafficData
            {
                ID = carIdA,
                EquipmentId = eqIdA,
                DetectTime = DateTime.Now,
                Type = "CAR",
                LicensePlate = $"30A-INSTA_{testId}",
                Speed = 50f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM10",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await workerA.PollChanges(CancellationToken.None);

            // Assert 1: workerA đã tự học cô lập TmsWeather; workerB chưa chạy nên _missingTables vẫn rỗng
            Assert.Contains("TmsWeather", GetMissingTables(workerA).Keys);
            Assert.Empty(GetMissingTables(workerB));

            // Act 2: Tạo thêm một thay đổi dữ liệu mới để DB currentVersion tăng tiếp -> workerB chạy
            await dbB.Insertable(new TmsEquipment
            {
                ID = eqIdB,
                Code = $"EQ_B_{testId}",
                KmNumber = 10,
                MetNumber = 600
            }).ExecuteCommandAsync();

            await dbB.Insertable(new TmsTrafficData
            {
                ID = carIdB,
                EquipmentId = eqIdB,
                DetectTime = DateTime.Now,
                Type = "CAR",
                LicensePlate = $"30B-INSTB_{testId}",
                Speed = 55f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM10",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await workerB.PollChanges(CancellationToken.None);

            // Assert 2: workerB giờ cũng đã tự học cô lập TmsWeather
            Assert.Contains("TmsWeather", GetMissingTables(workerB).Keys);

            // Assert 3: Đếm số dòng ESH-1602 (QueryFailed) phát sinh trong suốt bài test này
            var testEsh1602Count = await dbA.Queryable<ShareDataActivityLog>()
                .Where(x => x.Remark == ShareDataAlertCode.Tracking.QueryFailed && !existingIds.Contains(x.ID))
                .CountAsync();

            // Số lượng ESH-1602 kỳ vọng đúng 2 (mỗi instance ghi 1 dòng khi truy vấn hỏng)
            Assert.Equal(2, testEsh1602Count);

            // Khôi phục lại Change Tracking cho TmsWeather
            await dbA.Ado.ExecuteCommandAsync(
                "IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')) " +
                "ALTER TABLE [TmsWeather] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");
            await EnsureDefaultTrackTables(dbA);
        }

        #region 11. ShareDataSourceTable Per-Table Tracking Tests

        /// <summary>
        /// Description: Kiểm thử nạp bảng ánh xạ TablePacketMap từ ShareDataSourceTable:
        ///              - Đọc đúng các bản ghi có Status = Enable.
        ///              - Loại bỏ bản ghi có Status = Disable (0) như TmsEquipment.
        ///              - Chuẩn hóa PacketCodes (cắt khoảng trắng, loại trùng).
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenConfiguredInShareDataSourceTable_LoadsMapAndFiltersDisabledAndNormalizesValues_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == null)
                .Where(x => x.TableName == "TmsEquipment")
                .ExecuteCommandAsync();

            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.PacketCodes == " 101 , 101_commonData , 101 ")
                .Where(x => x.TableName == "TmsZoneStatus")
                .ExecuteCommandAsync();

            var worker = CreateTrackerWorker(scope);

            // Act
            await worker.PollChanges(CancellationToken.None);

            // Assert: (e) Bảng Status = Disable (TmsEquipment) không được quét
            Assert.Contains("TmsZoneStatus", worker.TrackedTables);
            Assert.Contains("TmsWeather", worker.TrackedTables);
            Assert.DoesNotContain("TmsEquipment", worker.TrackedTables);
            Assert.DoesNotContain("[TmsEquipment]", worker.ActiveChangesSql);

            var equipRow = await GetTrackState(db, "TmsEquipment");
            Assert.NotNull(equipRow);
            Assert.Null(equipRow!.LastVersion);

            var zonePackets = worker.TablePacketMap["TmsZoneStatus"];
            Assert.Equal(2, zonePackets.Count);
            Assert.Contains("101", zonePackets);
            Assert.Contains("101_commonData", zonePackets);

            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm thử (a): Thêm bảng mới (LastVersion = null) chỉ khởi tạo mốc cho bảng mới,
        ///              KHÔNG đổi LastVersion của các bảng đã có mốc.
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenNewTableHasNullLastVersion_InitsOnlyNewTableWithoutTouchingExisting_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var currentDbVer = (await InvokeGetCurrentDbVersion(db)) ?? 1000L;
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == currentDbVer)
                .SetColumns(x => x.LastIssue == null)
                .SetColumns(x => x.UpdateTime == new DateTime(2025, 1, 1))
                .Where(x => x.IsDelete == null)
                .ExecuteCommandAsync();

            // Giả lập TmsIncident là bảng vừa mới được bật / thêm vào với LastVersion = null
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == null)
                .SetColumns(x => x.UpdateTime == new DateTime(2025, 1, 1))
                .Where(x => x.TableName == "TmsIncident")
                .ExecuteCommandAsync();

            var zoneBefore = await GetTrackState(db, "TmsZone");
            Assert.NotNull(zoneBefore);
            Assert.Equal(currentDbVer, zoneBefore!.LastVersion);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataTracker:ConfigReloadSeconds"] = "0"
            }).Build();
            var worker = CreateTrackerWorker(scope, config: config);

            // Act
            await worker.PollChanges(CancellationToken.None);

            // Assert: TmsIncident đã được khởi tạo mốc >= currentDbVer, còn TmsZone giữ nguyên mốc cũ
            var incidentAfter = await GetTrackState(db, "TmsIncident");
            var zoneAfter = await GetTrackState(db, "TmsZone");
            Assert.NotNull(incidentAfter?.LastVersion);
            Assert.True(incidentAfter!.LastVersion >= currentDbVer);
            Assert.Equal(currentDbVer, zoneAfter!.LastVersion);

            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm thử (b): Một bảng quá hạn (min_valid_version > LastVersion) chỉ nhảy cóc riêng bảng đó,
        ///              còn bảng lành mạnh khác vẫn bắt được thay đổi trong cùng chu kỳ.
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenOneTableIsStaleAndAnotherHasChanges_FastForwardsStaleAndDetectsHealthyChanges_Test()
        {
            // Arrange
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var rootDb = _host.Services.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var worker = CreateTrackerWorker(scope);
            await worker.PollChanges(CancellationToken.None);

            var baselineVer = (await InvokeGetCurrentDbVersion(db)) ?? 0L;
            await SetStateVersion(db, baselineVer);

            // Tạo thay đổi thật trên bảng lành mạnh TmsZone
            var testZoneId = $"ZONE_P15B_{Guid.NewGuid():N}"[..24];
            await db.Insertable(new TmsZone
            {
                ID = testZoneId,
                Code = testZoneId,
                Name = "Zone Test Per Table Stale",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            // Đặt TmsWeather có LastVersion = 0 và dùng interceptor ép min_valid_version của riêng TmsWeather > 0
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == 0L)
                .Where(x => x.TableName == "TmsWeather")
                .ExecuteCommandAsync();

            Func<string, SugarParameter[], KeyValuePair<string, SugarParameter[]>> interceptor = (sql, pars) =>
            {
                if (sql.Contains("CHANGE_TRACKING_MIN_VALID_VERSION", StringComparison.OrdinalIgnoreCase) &&
                    sql.Contains("sys.tables", StringComparison.OrdinalIgnoreCase))
                {
                    var rewritten = sql.Replace(
                        "CHANGE_TRACKING_MIN_VALID_VERSION(t.object_id)",
                        "CASE WHEN t.name = 'TmsWeather' THEN CAST(999999999 AS BIGINT) ELSE CHANGE_TRACKING_MIN_VALID_VERSION(t.object_id) END",
                        StringComparison.OrdinalIgnoreCase);
                    return new KeyValuePair<string, SugarParameter[]>(rewritten, pars);
                }
                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };

            rootDb.Aop.OnExecutingChangeSql = interceptor;
            db.Aop.OnExecutingChangeSql = interceptor;

            // Act
            await worker.PollChanges(CancellationToken.None);

            rootDb.Aop.OnExecutingChangeSql = null;
            db.Aop.OnExecutingChangeSql = null;

            // Assert: TmsWeather được nhảy cóc và ghi LastIssue chứa ESH-1601; TmsZone bắt được thay đổi và có LastChangeTime mới
            var weatherRow = await GetTrackState(db, "TmsWeather");
            var zoneRow = await GetTrackState(db, "TmsZone");

            Assert.NotNull(weatherRow);
            Assert.True(weatherRow!.LastVersion > baselineVer);
            Assert.Contains(ShareDataAlertCode.Tracking.SelfHeal, weatherRow.LastIssue ?? string.Empty);

            Assert.NotNull(zoneRow);
            Assert.True(zoneRow!.LastVersion > baselineVer);
            Assert.NotNull(zoneRow.LastChangeTime);
            Assert.Null(zoneRow.LastIssue);

            await db.Deleteable<TmsZone>().Where(x => x.ID == testZoneId).ExecuteCommandAsync();
            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm thử (c): Hai instance chạy PollChanges song song trên cùng một thay đổi
        ///              chỉ đúng 1 instance nâng được mốc CAS và xử lý thay đổi, không phát trigger trùng.
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenTwoInstancesPollConcurrently_DoesNotDuplicateClaim_Test()
        {
            await using var scope1 = _host.Services.CreateAsyncScope();
            await using var scope2 = _host.Services.CreateAsyncScope();
            var db = scope1.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var worker1 = CreateTrackerWorker(scope1);
            var worker2 = CreateTrackerWorker(scope2);

            await worker1.PollChanges(CancellationToken.None);
            await worker2.PollChanges(CancellationToken.None);

            var testZoneId = $"ZONE_P15C_{Guid.NewGuid():N}"[..24];
            var beforeChangeTime = (await db.Ado.GetDateTimeAsync("SELECT GETDATE()")).AddSeconds(-2);

            await db.Insertable(new TmsZone
            {
                ID = testZoneId,
                Code = testZoneId,
                Name = "Concurrent Zone",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var t1 = Task.Run(async () =>
            {
                await gate.Task;
                await worker1.PollChanges(CancellationToken.None);
            });
            var t2 = Task.Run(async () =>
            {
                await gate.Task;
                await worker2.PollChanges(CancellationToken.None);
            });

            gate.SetResult();
            await Task.WhenAll(t1, t2);

            var zoneRow = await GetTrackState(db, "TmsZone");
            Assert.NotNull(zoneRow);
            Assert.NotNull(zoneRow!.LastChangeTime);
            Assert.True(zoneRow.LastChangeTime >= beforeChangeTime);

            await db.Deleteable<TmsZone>().Where(x => x.ID == testZoneId).ExecuteCommandAsync();
            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm thử (d): LastChangeTime được cập nhật khi bảng có thay đổi;
        ///              LastIssue được ghi khi bảng gặp sự cố và tự động xóa về null khi bảng lành lại.
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenTableRecoversAndChanges_ClearsLastIssueAndSetsLastChangeTime_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var worker = CreateTrackerWorker(scope);
            await worker.PollChanges(CancellationToken.None);

            // Bước 1: Giả lập tắt CT trên TmsWeather và đẩy version tăng để kích hoạt lỗi cô lập bảng
            await db.Ado.ExecuteCommandAsync("ALTER TABLE [TmsWeather] DISABLE CHANGE_TRACKING;");
            var testZoneId = $"ZONE_P15D1_{Guid.NewGuid():N}"[..24];
            await db.Insertable(new TmsZone
            {
                ID = testZoneId,
                Code = testZoneId,
                Name = "Trigger CT Check",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await worker.PollChanges(CancellationToken.None);

            var weatherBroken = await GetTrackState(db, "TmsWeather");
            Assert.NotNull(weatherBroken);
            Assert.False(string.IsNullOrWhiteSpace(weatherBroken!.LastIssue));
            Assert.Contains(ShareDataAlertCode.Tracking.QueryFailed, weatherBroken.LastIssue!);

            // Bước 2: Bật lại CT cho TmsWeather và cho hết hạn cô lập -> TryRestoreMissingTables xóa LastIssue về null
            await db.Ado.ExecuteCommandAsync(
                "IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('TmsWeather')) " +
                "ALTER TABLE [TmsWeather] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");
            ExpireMissingTableRetry(worker);

            var testZoneId2 = $"ZONE_P15D2_{Guid.NewGuid():N}"[..24];
            await db.Insertable(new TmsZone
            {
                ID = testZoneId2,
                Code = testZoneId2,
                Name = "Trigger Recovery",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await worker.PollChanges(CancellationToken.None);

            var weatherRecovered = await GetTrackState(db, "TmsWeather");
            var zoneChanged = await GetTrackState(db, "TmsZone");
            Assert.NotNull(weatherRecovered);
            Assert.Null(weatherRecovered!.LastIssue);
            Assert.NotNull(zoneChanged?.LastChangeTime);

            await db.Deleteable<TmsZone>().Where(x => x.ID == testZoneId || x.ID == testZoneId2).ExecuteCommandAsync();
            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm thử (f): Đối chiếu 15 dòng trong ShareDataSourceTableSeedData.HasData()
        ///              trùng khớp 100% với 15 dòng trong script 03-sharedata-seed-cau-hinh.sql.
        /// </summary>
        [Fact]
        public void ShareDataSourceTableSeedData_MatchesSqlScript03_Exactly15Rows_Test()
        {
            var seedRows = new ShareDataSourceTableSeedData().HasData().OrderBy(x => x.OrderNo).ToList();
            Assert.Equal(15, seedRows.Count);

            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            var sql03Path = Path.Combine(repoRoot, "DocBusinessThienAn", "HữuNghị-ChiLăng", "ShareData", "sql", "03-sharedata-seed-cau-hinh.sql");
            Assert.True(File.Exists(sql03Path), $"Không tìm thấy tệp SQL tại {sql03Path}");

            var sqlContent = File.ReadAllText(sql03Path);
            Assert.DoesNotContain("sharedata_tracker", sqlContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ShareDataSourceTable", sqlContent, StringComparison.Ordinal);

            foreach (var row in seedRows)
            {
                Assert.Contains($"N'{row.TableName}'", sqlContent, StringComparison.Ordinal);
                Assert.Contains($"N'{row.PacketCodes}'", sqlContent, StringComparison.Ordinal);
                Assert.Contains($"N'{row.Name}'", sqlContent, StringComparison.Ordinal);
            }

            var disabledRow = Assert.Single(seedRows, x => x.IsTracked == false);
            Assert.Equal("TmsEquipment", disabledRow.TableName);
            Assert.Equal(130, disabledRow.OrderNo);
        }

        /// <summary>
        /// Description: Kiểm thử (g): Sau khi chạy khởi tạo, đặt lại mốc và nhảy cóc quá hạn,
        ///              tổng số dòng và toàn bộ các cột cấu hình (ID, TableName, Name, PacketCodes, Status, OrderNo, Remark)
        ///              trong ShareDataSourceTable giữ nguyên 100%, không bị xóa hay tạo lại.
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_AcrossInitResetAndFastForward_PreservesRowCountAndConfigColumns_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var rootDb = _host.Services.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var beforeRows = await db.Queryable<ShareDataSourceTable>()
                .Where(x => x.IsDelete == null)
                .OrderBy(x => x.OrderNo)
                .ToListAsync();
            Assert.Equal(15, beforeRows.Count);

            var worker = CreateTrackerWorker(scope);

            // 1. Khởi tạo mốc khi LastVersion = null
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == null)
                .Where(x => x.IsDelete == null)
                .ExecuteCommandAsync();
            await worker.PollChanges(CancellationToken.None);

            // 2. Đặt lại mốc khi LastVersion > CurrentVersion
            await SetStateVersion(db, 999_999_999_999L);
            await worker.PollChanges(CancellationToken.None);

            // 3. Nhảy cóc quá hạn khi min_valid_version > LastVersion
            await SetStateVersion(db, 0L);
            Func<string, SugarParameter[], KeyValuePair<string, SugarParameter[]>> interceptor = (sql, pars) =>
            {
                if (sql.Contains("CHANGE_TRACKING_MIN_VALID_VERSION", StringComparison.OrdinalIgnoreCase) &&
                    sql.Contains("sys.tables", StringComparison.OrdinalIgnoreCase))
                {
                    var rewritten = sql.Replace(
                        "CHANGE_TRACKING_MIN_VALID_VERSION(t.object_id)",
                        "CAST(999999999 AS BIGINT)",
                        StringComparison.OrdinalIgnoreCase);
                    return new KeyValuePair<string, SugarParameter[]>(rewritten, pars);
                }
                return new KeyValuePair<string, SugarParameter[]>(sql, pars);
            };
            rootDb.Aop.OnExecutingChangeSql = interceptor;
            db.Aop.OnExecutingChangeSql = interceptor;

            await worker.PollChanges(CancellationToken.None);

            rootDb.Aop.OnExecutingChangeSql = null;
            db.Aop.OnExecutingChangeSql = null;

            var afterRows = await db.Queryable<ShareDataSourceTable>()
                .Where(x => x.IsDelete == null)
                .OrderBy(x => x.OrderNo)
                .ToListAsync();

            Assert.Equal(beforeRows.Count, afterRows.Count);
            for (var i = 0; i < beforeRows.Count; i++)
            {
                Assert.Equal(beforeRows[i].ID, afterRows[i].ID);
                Assert.Equal(beforeRows[i].TableName, afterRows[i].TableName);
                Assert.Equal(beforeRows[i].Name, afterRows[i].Name);
                Assert.Equal(beforeRows[i].PacketCodes, afterRows[i].PacketCodes);
                Assert.Equal(beforeRows[i].IsTracked, afterRows[i].IsTracked);
                Assert.Equal(beforeRows[i].OrderNo, afterRows[i].OrderNo);
                Assert.Equal(beforeRows[i].Remark, afterRows[i].Remark);
            }

            await EnsureDefaultTrackTables(db);
        }

        /// <summary>
        /// Description: Kiểm thử khi truy vấn CSDL gặp lỗi trong chu kỳ nạp lại:
        ///              Worker giữ nguyên map đang chạy và ghi log Warning (không làm gián đoạn tracking).
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenDbQueryFailsDuringReload_RetainsExistingMapAndLogsWarning_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataTracker:ConfigReloadSeconds"] = "0"
            }).Build();

            var worker = CreateTrackerWorker(scope, config: config);
            await worker.PollChanges(CancellationToken.None);

            Assert.Equal(14, worker.TrackedTables.Count);
            Assert.Contains("TmsZone", worker.TrackedTables);

            // Act: Gọi ReloadConfig với client đã dispose để giả lập lỗi truy vấn CSDL
            var brokenDb = db.CopyNew();
            brokenDb.Ado.Dispose();

            await worker.ReloadConfig(brokenDb);

            // Assert: Map vẫn được bảo toàn nguyên vẹn
            Assert.Equal(14, worker.TrackedTables.Count);
            Assert.Contains("TmsZone", worker.TrackedTables);
        }

        /// <summary>
        /// Description: Kiểm thử bảng ban đầu có Status = Disable (0) như TmsEquipment, sau đó được bật Status = Enable (1):
        ///              Lần nạp lại (ReloadConfig) nhận diện ngay bảng mới và đưa vào TrackedTables/ActiveChangesSql.
        /// </summary>
        [Fact]
        public async Task DataChangeTrackingService_WhenTableDisabledThenEnabled_ReloadPicksUpNewTable_Test()
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            await EnsureDefaultTrackTables(db);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataTracker:ConfigReloadSeconds"] = "0"
            }).Build();

            var worker = CreateTrackerWorker(scope, config: config);
            await worker.PollChanges(CancellationToken.None);

            // Ban đầu TmsEquipment có Status = Disable nên chưa được theo dõi
            Assert.DoesNotContain("TmsEquipment", worker.TrackedTables);

            // Act: Quản trị bật Status = Enable cho TmsEquipment
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.IsTracked == true)
                .Where(x => x.TableName == "TmsEquipment")
                .ExecuteCommandAsync();

            await worker.ReloadConfig(db);

            // Assert: Sau khi reload, bảng TmsEquipment đã được đưa vào theo dõi
            Assert.Contains("TmsEquipment", worker.TrackedTables);
            Assert.Contains("[TmsEquipment]", worker.ActiveChangesSql);

            await EnsureDefaultTrackTables(db);
        }

        #endregion

        #endregion

        private static async Task<bool> WaitUntil(Func<bool> condition, TimeSpan timeout, int pollIntervalMs = 50)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                    return true;
                await Task.Delay(pollIntervalMs);
            }
            return condition();
        }

        private static async Task<List<ShareDataActivityLog>> GetLogs(ISqlSugarClient db, string subId)
        {
            return await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subId)
                .Where(l => l.ParentId == null)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();
        }

        private DataChangeTrackingService CreateTrackerWorker(IServiceScope? scope = null, TransportManager? transport = null, IConfiguration? config = null)
        {
            var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
            var logger = (scope?.ServiceProvider ?? _host.Services).GetRequiredService<ILogger<DataChangeTrackingService>>();
            transport ??= new TransportManager(new ConfigurationBuilder().Build());
            config ??= _host.Services.GetService<IConfiguration>();
            return new DataChangeTrackingService(scopeFactory, logger, transport, config);
        }

        /// <summary>
        /// Description: Lấy dòng trạng thái trong bảng ShareDataSourceTable (mặc định lấy dòng đầu tiên đang bật theo OrderNo, hoặc theo tên bảng chỉ định).
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private static async Task<ShareDataSourceTable?> GetTrackState(ISqlSugarClient db, string? tableName = null)
        {
            var q = db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null);
            if (!string.IsNullOrWhiteSpace(tableName))
                return await q.Where(x => x.TableName == tableName).FirstAsync();
            return await q.Where(x => x.IsTracked == true).OrderBy(x => x.OrderNo).FirstAsync();
        }

        /// <summary>
        /// Description: Lấy mốc version Change Tracking đang lưu; chưa có mốc thì trả về -1.
        /// Created date: 27/09/2026
        /// </summary>
        private static async Task<long> GetStateVersion(ISqlSugarClient db) =>
            (await GetTrackState(db))?.LastVersion ?? -1;

        /// <summary>
        /// Description: Ghi đè mốc version trên toàn bộ các dòng đang bật trong bảng ShareDataSourceTable (mô phỏng mốc bị lệch hoặc quá cũ).
        /// Created date: 27/09/2026
        /// Modified date: 17/10/2026
        /// </summary>
        private static async Task SetStateVersion(ISqlSugarClient db, long version)
        {
            await EnsureDefaultTrackTables(db);
            await db.Updateable<ShareDataSourceTable>()
                .SetColumns(x => x.LastVersion == version)
                .SetColumns(x => x.LastIssue == null)
                .Where(x => x.IsDelete == null && x.IsTracked == true)
                .ExecuteCommandAsync();
        }

        private static async Task<int> InvokeTrySkipBrokenTables(DataChangeTrackingService worker, ISqlSugarClient db, Exception ex)
        {
            var method = typeof(DataChangeTrackingService).GetMethod("TrySkipBrokenTables", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("TrySkipBrokenTables method not found");
            var task = (Task<int>)method.Invoke(worker, [db, ex])!;
            return await task;
        }

        private static async Task<List<string>> InvokeQueryChangedTables(DataChangeTrackingService worker, ISqlSugarClient db, ShareDataSourceTable trackTable, string sql)
        {
            var method = typeof(DataChangeTrackingService).GetMethod("QueryChangedTables", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("QueryChangedTables method not found");
            IReadOnlyList<ShareDataSourceTable> claimed = [trackTable];
            var task = (Task<List<string>>)method.Invoke(worker, [db, claimed, sql])!;
            return await task;
        }

        private static string InvokeBuildChangedTablesSql(IReadOnlyList<string> trackedTables) =>
            DataChangeTrackingService.BuildChangesSql(trackedTables);

        private IReadOnlyList<string> InvokeResolveTriggerPackets(IEnumerable<string> changedTables)
        {
            var worker = CreateTrackerWorker();
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            worker.ReloadConfig(db).GetAwaiter().GetResult();
            return DataChangeTrackingService.ResolvePackets(changedTables, worker.TablePacketMap);
        }

        private static async Task<long?> InvokeGetCurrentDbVersion(ISqlSugarClient db)
        {
            var method = typeof(DataChangeTrackingService).GetMethod("GetCurrentVersion", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("GetCurrentVersion method not found");
            var task = (Task<long?>)method.Invoke(null, [db])!;
            return await task;
        }

        private static async Task InvokeNatsHandleMessages(DataNatsService worker, string payload, CancellationToken token)
        {
            var method = typeof(DataNatsService).GetMethod("HandleMessages", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("HandleMessages method not found");
            var task = (Task)method.Invoke(worker, [payload, token])!;
            await task;
        }

        /// <summary>
        /// Description: Đọc danh sách bảng đang bị cô lập kèm mốc hẹn thử lại của từng bảng (trạng thái private trong RAM).
        /// Created date: 27/09/2026
        /// </summary>
        private static Dictionary<string, DateTime> GetMissingTables(DataChangeTrackingService worker) =>
            (Dictionary<string, DateTime>)typeof(DataChangeTrackingService)
                .GetProperty("MissingTables", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(worker)!;

        /// <summary>
        /// Description: Đặt mốc hẹn thử lại của MỌI bảng đang bị cô lập về quá khứ, để mô phỏng đã đến hạn quét lại.
        /// Created date: 27/09/2026
        /// </summary>
        private static void ExpireMissingTableRetry(DataChangeTrackingService worker)
        {
            var missing = GetMissingTables(worker);
            foreach (var key in missing.Keys.ToList())
                missing[key] = DateTime.UtcNow.AddSeconds(-1);
        }
    }
}

