using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Core.Interfaces;
using ShareDataWorker.Infrastructure.Logging;
using Shared.DTO.Enums;
using SqlSugar;
using Xunit;

namespace Tests.ShareData.Services;

/// <summary>
/// Description: Bộ kiểm thử đơn vị và tích hợp cho DataCleanupService (dọn dẹp nhật ký Change Tracking ESH-16xx).
/// Author: DatHQ
/// Created date: 18/10/2026
/// </summary>
[Collection("api")]
public class DataCleanupServiceTests(Host host)
{
    private readonly Host _host = host;

    /// <summary>
    /// Description: Kiểm thử dọn nhật ký hạ tầng Change Tracking: chỉ xoá các dòng bắt đầu bằng mã ESH-16xx quá hạn,
    ///              giữ nguyên các dòng nghiệp vụ (Remark null hoặc khác ESH-16xx).
    /// </summary>
    [Fact]
    public async Task CleanTrackingLogs_OnlyDeletesEsh16Rows_KeepsBusinessTransferRows_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();

        var oldTime = (await db.Ado.GetDateTimeAsync("SELECT GETDATE()")).AddDays(-30);
        var trackingId = Guid.NewGuid().ToString("N");
        var businessId = Guid.NewGuid().ToString("N");

        await db.Insertable(new ShareDataActivityLog
        {
            ID = trackingId,
            LogType = LogTypeEnum.Transfer,
            Remark = ShareDataAlertCode.Tracking.SelfHeal,
            Description = "Dòng hạ tầng quá hạn dùng cho test dọn nhật ký.",
            CreateTime = oldTime,
            UpdateTime = oldTime
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @oldTime WHERE ID = @id", new { oldTime, id = trackingId });

        await db.Insertable(new ShareDataActivityLog
        {
            ID = businessId,
            LogType = LogTypeEnum.Transfer,
            Remark = null,
            Description = "Dòng nghiệp vụ quá hạn — PHẢI còn nguyên sau khi dọn.",
            CreateTime = oldTime,
            UpdateTime = oldTime
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @oldTime WHERE ID = @id", new { oldTime, id = businessId });

        // Act
        await cleanupService.CleanTrackingLogs(CancellationToken.None);

        // Assert
        Assert.Null(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == trackingId).FirstAsync());
        Assert.NotNull(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == businessId).FirstAsync());
    }

}
