using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Infrastructure.Services.Cleanup;
using Shared.DTO.Enums;
using SqlSugar;
using Tests.ShareData.Mocks;

namespace Tests.ShareData.Infrastructure.Services.Cleanup
{
    /// <summary>
    /// Description: Bộ kiểm thử cho dịch vụ dọn dẹp định kỳ (Job Cleanup - #22).
    ///              Kiểm chứng dọn dẹp thư mục tệp kết xuất cũ (cấu trúc mới và legacy),
    ///              xóa nhật ký truyền nhận theo lô (batch) và BẢO VỆ TUYỆT ĐỐI audit cấu hình (LogType = CONFIG).
    /// Created date: 14/10/2026
    /// </summary>
    [Collection("api")]
    public class CleanupServiceTests(Host host)
    {
        [Fact]
        public void CleanOldExportFiles_DeletesOnlyFoldersOlderThanRetentionDays_KeepsRecentFolders_Test()
        {
            // Arrange
            var tempRoot = Path.Combine(Path.GetTempPath(), "sharedata_cleanup_test_" + Guid.NewGuid().ToString("N"));
            var outRoot = Path.Combine(tempRoot, "Out", "PARTNER_TEST");

            // 1. Cấu trúc mới: 2026/08/15 (cũ hơn 30 ngày) -> phải bị xóa
            var oldDayDir = Path.Combine(outRoot, "2026", "08", "15", "101");
            Directory.CreateDirectory(oldDayDir);
            File.WriteAllText(Path.Combine(oldDayDir, "export_old.json"), "{\"data\":1}");

            // 2. Cấu trúc mới: Ngày hiện tại -> phải được giữ nguyên
            var today = DateTime.Now;
            var recentDayDir = Path.Combine(outRoot, today.Year.ToString(), today.Month.ToString("D2"), today.Day.ToString("D2"), "101");
            Directory.CreateDirectory(recentDayDir);
            File.WriteAllText(Path.Combine(recentDayDir, "export_today.json"), "{\"data\":2}");

            // 3. Cấu trúc legacy: 202608/1510 (cũ hơn 30 ngày) -> phải bị xóa
            var legacyOldDir = Path.Combine(outRoot, "202608", "1510");
            Directory.CreateDirectory(legacyOldDir);
            File.WriteAllText(Path.Combine(legacyOldDir, "legacy_old.json"), "{\"data\":3}");

            var service = new CleanupService();

            // Act
            var deletedCount = service.CleanOldExportFiles(tempRoot, retentionDays: 30);

            // Assert
            Assert.True(deletedCount >= 2, $"Kỳ vọng xóa ít nhất 2 thư mục ngày cũ, thực tế xóa: {deletedCount}");
            Assert.False(Directory.Exists(oldDayDir), "Thư mục ngày cũ 2026/08/15 phải bị xóa");
            Assert.False(Directory.Exists(legacyOldDir), "Thư mục legacy cũ 202608/1510 phải bị xóa");
            Assert.True(Directory.Exists(recentDayDir), "Thư mục ngày hôm nay phải được giữ nguyên");
            Assert.True(File.Exists(Path.Combine(recentDayDir, "export_today.json")), "Tệp kết xuất ngày hôm nay phải còn nguyên");

            // Cleanup
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public async Task CleanOldTransferLogsAsync_DeletesOnlyTransferActivityLogs_PreservesConfigAuditLogs_Test()
        {
            // Arrange
            var db = host.Services.GetRequiredService<ISqlSugarClient>();

            var testId = Guid.NewGuid().ToString("N");
            var oldTransferId = "transfer_old_" + testId;
            var recentTransferId = "transfer_recent_" + testId;
            var oldConfigId = "config_old_" + testId;

            var oldTransferLog = new ShareDataActivityLog
            {
                ID = oldTransferId,
                LogType = BaseEnums.LogTypeEnum.Transfer,
                Action = BaseEnums.ActivityAction.Export,
                Description = "Transfer Log Old " + testId
            };

            var recentTransferLog = new ShareDataActivityLog
            {
                ID = recentTransferId,
                LogType = BaseEnums.LogTypeEnum.Transfer,
                Action = BaseEnums.ActivityAction.Export,
                Description = "Transfer Log Recent " + testId
            };

            var oldConfigAuditLog = new ShareDataActivityLog
            {
                ID = oldConfigId,
                LogType = BaseEnums.LogTypeEnum.CONFIG,
                Action = BaseEnums.ActivityAction.Update,
                Description = "Config Audit Old " + testId
            };

            await db.Insertable(new[] { oldTransferLog, recentTransferLog, oldConfigAuditLog }).ExecuteCommandAsync();

            // SqlSugar EntityTenant tự ép CreateTime = GETDATE() khi insert, cập nhật lại ngày cũ cho bản ghi cần test xóa
            await db.Ado.ExecuteCommandAsync(
                "UPDATE dbo.ShareDataActivityLog SET CreateTime = DATEADD(day, -45, GETDATE()) WHERE ID IN (@oldTransferId, @oldConfigId)",
                new { oldTransferId, oldConfigId });

            var service = new CleanupService();

            // Act
            var deletedTotal = await service.CleanOldTransferLogsAsync(db, retentionDays: 30, batchSize: 500);

            // Assert
            Assert.True(deletedTotal >= 1, "Phải xóa ít nhất 1 bản ghi cũ");

            var checkOldTransfer = await db.Queryable<ShareDataActivityLog>().InSingleAsync(oldTransferId);
            Assert.Null(checkOldTransfer);

            var checkRecentTransfer = await db.Queryable<ShareDataActivityLog>().InSingleAsync(recentTransferId);
            Assert.NotNull(checkRecentTransfer);

            var checkOldConfig = await db.Queryable<ShareDataActivityLog>().InSingleAsync(oldConfigId);
            Assert.NotNull(checkOldConfig);
            Assert.Equal(BaseEnums.LogTypeEnum.CONFIG, checkOldConfig.LogType);
        }

        [Fact]
        public async Task CleanOldTransferLogsAsync_DeletesOldAlertLogs_PreservesRecentAlertLogs_Test()
        {
            // Arrange
            var db = host.Services.GetRequiredService<ISqlSugarClient>();

            var testId = Guid.NewGuid().ToString("N");
            var oldAlertId = "alert_old_" + testId;
            var recentAlertId = "alert_recent_" + testId;

            var oldAlert = new ShareDataAlertLog
            {
                ID = oldAlertId,
                AlertCode = "TEST_OLD_ALERT",
                Message = "Alert Old " + testId
            };

            var recentAlert = new ShareDataAlertLog
            {
                ID = recentAlertId,
                AlertCode = "TEST_RECENT_ALERT",
                Message = "Alert Recent " + testId
            };

            await db.Insertable(new[] { oldAlert, recentAlert }).ExecuteCommandAsync();

            // Cập nhật ngày cũ cho bản ghi alert cũ
            await db.Ado.ExecuteCommandAsync(
                "UPDATE dbo.ShareDataAlertLog SET CreateTime = DATEADD(day, -50, GETDATE()) WHERE ID = @oldAlertId",
                new { oldAlertId });

            var service = new CleanupService();

            // Act
            await service.CleanOldTransferLogsAsync(db, retentionDays: 30, batchSize: 500);

            // Assert
            var checkOldAlert = await db.Queryable<ShareDataAlertLog>().InSingleAsync(oldAlertId);
            Assert.Null(checkOldAlert);

            var checkRecentAlert = await db.Queryable<ShareDataAlertLog>().InSingleAsync(recentAlertId);
            Assert.NotNull(checkRecentAlert);
        }

        [Fact]
        public async Task RecordCleanupActivityAsync_InsertsConfigAuditLog_WithSummaryDescription_Test()
        {
            // Arrange
            var db = host.Services.GetRequiredService<ISqlSugarClient>();
            var service = new CleanupService();

            // Act
            await service.RecordCleanupActivityAsync(db, filesCleaned: 12, logsCleaned: 450, retentionDays: 30);

            // Assert
            var log = await db.Queryable<ShareDataActivityLog>()
                .Where(x => x.OperatorName == "SYSTEM_CLEANUP_JOB")
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .FirstAsync();

            Assert.NotNull(log);
            Assert.Equal(BaseEnums.LogTypeEnum.CONFIG, log.LogType);
            Assert.Contains("[Job Cleanup]", log.Description);
            Assert.Contains("12 thư mục", log.Description);
            Assert.Contains("450 bản ghi", log.Description);
        }

        [Fact]
        public async Task CleanOldInboundPacketsAsync_WhenDisabledByPolicy_ReturnsZeroAndPreservesDonePackets_Test()
        {
            // Arrange
            var db = host.Services.GetRequiredService<ISqlSugarClient>();
            var inboundDb = db.AsTenant().IsAnyConnection("ShareDataDB")
                ? db.AsTenant().GetConnectionScope("ShareDataDB")
                : db;

            var testId = Guid.NewGuid().ToString("N");
            var packetId = "inbound_old_" + testId;

            var packet = new ShareDataInboundPacket
            {
                ID = packetId,
                PartnerCode = "PARTNER_TEST",
                PacketCode = "101",
                ProcessState = BaseEnums.InboundProcessState.Done,
                RawContent = "{\"test\":true}"
            };

            await inboundDb.Insertable(packet).ExecuteCommandAsync();

            await inboundDb.Ado.ExecuteCommandAsync(
                "UPDATE dbo.ShareDataInboundPacket SET CreateTime = DATEADD(day, -45, GETDATE()) WHERE ID = @packetId",
                new { packetId });

            var service = new CleanupService();

            // Act
            var deletedCount = await service.CleanOldInboundPacketsAsync(inboundDb, retentionDays: 30, batchSize: 500);

            // Assert
            Assert.Equal(0, deletedCount);

            var checkPacket = await inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetId);
            Assert.NotNull(checkPacket);
            Assert.Equal(BaseEnums.InboundProcessState.Done, checkPacket.ProcessState);
        }
    }
}
