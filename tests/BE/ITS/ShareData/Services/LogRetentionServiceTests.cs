using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Services.ShareDataLogRetentionService;
using Shared.DTO.Enums;
using SqlSugar;

namespace Tests.ShareData.Services;

/// <summary>
/// Description: Bộ kiểm thử tích hợp cho LogRetentionService (SV-13 mở rộng).
///              Kiểm chứng job dọn dẹp nhật ký nghiệp vụ ShareData (giữ tối đa 7 ngày).
/// Created date: 06/10/2026
/// </summary>
[Collection("api")]
public class LogRetentionServiceTests(Host host)
{
    private readonly Host _host = host;
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();

    #region 1. Kiểm thử xoá cả cây khi dòng cha quá hạn

    /// <summary>
    /// SV-13: Dòng cha Transfer quá hạn 10 ngày + 2 dòng con (1 con 10 ngày, 1 con mép hạn 6 ngày).
    /// Vì dòng cha đã quá hạn, toàn bộ cây cha–con phải bị xoá hard cùng nhau.
    /// </summary>
    [Fact]
    public async Task ScanAndClean_ExpiredParent_DeletesWholeTree_Test()
    {
        // Arrange
        var parentId = Guid.NewGuid().ToString("N");
        var child1Id = Guid.NewGuid().ToString("N");
        var child2Id = Guid.NewGuid().ToString("N");

        await _db.Insertable(new ShareDataActivityLog
        {
            ID = parentId,
            ParentId = null,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Description = "Dòng cha quá hạn 10 ngày",
            OccurredAt = DateTime.Now.AddDays(-10)
        }).ExecuteCommandAsync();

        await _db.Insertable(new ShareDataActivityLog
        {
            ID = child1Id,
            ParentId = parentId,
            StepNbr = 1,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Description = "Dòng con 1 quá hạn 10 ngày",
            OccurredAt = DateTime.Now.AddDays(-10)
        }).ExecuteCommandAsync();

        await _db.Insertable(new ShareDataActivityLog
        {
            ID = child2Id,
            ParentId = parentId,
            StepNbr = 2,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Description = "Dòng con 2 mép hạn 6 ngày (thuộc cha 10 ngày)",
            OccurredAt = DateTime.Now.AddDays(-6)
        }).ExecuteCommandAsync();

        // Cập nhật CreateTime lùi về quá khứ để vượt qua AOP DataExecuting của EntityTenant
        await _db.Ado.ExecuteCommandAsync(
            "UPDATE ShareDataActivityLog SET CreateTime = DATEADD(day, -10, GETDATE()) WHERE ID IN (@pId, @c1Id)",
            new { pId = parentId, c1Id = child1Id });
        await _db.Ado.ExecuteCommandAsync(
            "UPDATE ShareDataActivityLog SET CreateTime = DATEADD(day, -6, GETDATE()) WHERE ID = @c2Id",
            new { c2Id = child2Id });

        // Act
        using var scope = _host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<LogRetentionService>();
        await service.ScanAndClean(CancellationToken.None);

        // Assert: Cả 3 dòng đều bị xoá
        Assert.Null(await _db.Queryable<ShareDataActivityLog>().Where(x => x.ID == parentId).FirstAsync());
        Assert.Null(await _db.Queryable<ShareDataActivityLog>().Where(x => x.ID == child1Id).FirstAsync());
        Assert.Null(await _db.Queryable<ShareDataActivityLog>().Where(x => x.ID == child2Id).FirstAsync());
    }

    #endregion

    #region 2. Kiểm thử giữ lại cây mới và dòng CONFIG kiểm toán

    /// <summary>
    /// SV-13: Dòng cha + con Transfer mới 3 ngày; 1 dòng CONFIG 30 ngày (kiểm toán).
    /// Toàn bộ 3 dòng này phải được giữ lại nguyên vẹn.
    /// </summary>
    [Fact]
    public async Task ScanAndClean_RecentTreeAndConfigLog_AreKept_Test()
    {
        // Arrange
        var parentId = Guid.NewGuid().ToString("N");
        var childId = Guid.NewGuid().ToString("N");
        var configId = Guid.NewGuid().ToString("N");

        await _db.Insertable(new ShareDataActivityLog
        {
            ID = parentId,
            ParentId = null,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Description = "Dòng cha mới 3 ngày",
            OccurredAt = DateTime.Now.AddDays(-3)
        }).ExecuteCommandAsync();

        await _db.Insertable(new ShareDataActivityLog
        {
            ID = childId,
            ParentId = parentId,
            StepNbr = 1,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Description = "Dòng con mới 3 ngày",
            OccurredAt = DateTime.Now.AddDays(-3)
        }).ExecuteCommandAsync();

        await _db.Insertable(new ShareDataActivityLog
        {
            ID = configId,
            ParentId = null,
            LogType = BaseEnums.LogTypeEnum.CONFIG,
            Description = "Dòng kiểm toán cấu hình 30 ngày",
            OccurredAt = DateTime.Now.AddDays(-30)
        }).ExecuteCommandAsync();

        await _db.Ado.ExecuteCommandAsync(
            "UPDATE ShareDataActivityLog SET CreateTime = DATEADD(day, -3, GETDATE()) WHERE ID IN (@pId, @cId)",
            new { pId = parentId, cId = childId });
        await _db.Ado.ExecuteCommandAsync(
            "UPDATE ShareDataActivityLog SET CreateTime = DATEADD(day, -30, GETDATE()) WHERE ID = @cfgId",
            new { cfgId = configId });

        // Act
        using var scope = _host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<LogRetentionService>();
        await service.ScanAndClean(CancellationToken.None);

        // Assert: Cả 3 dòng còn nguyên
        Assert.NotNull(await _db.Queryable<ShareDataActivityLog>().Where(x => x.ID == parentId).FirstAsync());
        Assert.NotNull(await _db.Queryable<ShareDataActivityLog>().Where(x => x.ID == childId).FirstAsync());
        Assert.NotNull(await _db.Queryable<ShareDataActivityLog>().Where(x => x.ID == configId).FirstAsync());
    }

    #endregion

    #region 3. Kiểm thử vét dòng con mồ côi

    /// <summary>
    /// SV-13: Dòng con Transfer quá hạn 10 ngày có ParentId trỏ tới ID không tồn tại (con mồ côi).
    /// Bước quét vét ở cuối phải xoá sạch dòng con này.
    /// </summary>
    [Fact]
    public async Task ScanAndClean_OrphanChild_IsDeleted_Test()
    {
        // Arrange
        var orphanId = Guid.NewGuid().ToString("N");
        var nonExistentParentId = "non_existent_" + Guid.NewGuid().ToString("N");

        await _db.Insertable(new ShareDataActivityLog
        {
            ID = orphanId,
            ParentId = nonExistentParentId,
            StepNbr = 1,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Description = "Dòng con mồ côi quá hạn 10 ngày",
            OccurredAt = DateTime.Now.AddDays(-10)
        }).ExecuteCommandAsync();

        await _db.Ado.ExecuteCommandAsync(
            "UPDATE ShareDataActivityLog SET CreateTime = DATEADD(day, -10, GETDATE()) WHERE ID = @id",
            new { id = orphanId });

        // Act
        using var scope = _host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<LogRetentionService>();
        await service.ScanAndClean(CancellationToken.None);

        // Assert: Dòng con mồ côi bị xoá
        Assert.Null(await _db.Queryable<ShareDataActivityLog>().Where(x => x.ID == orphanId).FirstAsync());
    }

    #endregion

    #region 4. Kiểm thử dọn dẹp ShareDataAlertLog

    /// <summary>
    /// SV-13: Cảnh báo 10 ngày chưa xác nhận (Acknowledged=false), 10 ngày đã xác nhận (Acknowledged=true),
    /// và 3 ngày chưa xác nhận.
    /// 2 dòng 10 ngày bị xoá sạch, dòng 3 ngày còn nguyên vẹn.
    /// </summary>
    [Fact]
    public async Task ScanAndClean_AlertLog_DeletesOnlyExpired_Test()
    {
        // Arrange
        var alert1Id = Guid.NewGuid().ToString("N");
        var alert2Id = Guid.NewGuid().ToString("N");
        var alert3Id = Guid.NewGuid().ToString("N");

        await _db.Insertable(new ShareDataAlertLog
        {
            ID = alert1Id,
            AlertCode = "ESH-TEST-1",
            Acknowledged = false,
            OccurredAt = DateTime.Now.AddDays(-10),
            Message = "Cảnh báo 10 ngày chưa xác nhận"
        }).ExecuteCommandAsync();

        await _db.Insertable(new ShareDataAlertLog
        {
            ID = alert2Id,
            AlertCode = "ESH-TEST-2",
            Acknowledged = true,
            OccurredAt = DateTime.Now.AddDays(-10),
            Message = "Cảnh báo 10 ngày đã xác nhận"
        }).ExecuteCommandAsync();

        await _db.Insertable(new ShareDataAlertLog
        {
            ID = alert3Id,
            AlertCode = "ESH-TEST-3",
            Acknowledged = false,
            OccurredAt = DateTime.Now.AddDays(-3),
            Message = "Cảnh báo mới 3 ngày"
        }).ExecuteCommandAsync();

        await _db.Ado.ExecuteCommandAsync(
            "UPDATE ShareDataAlertLog SET CreateTime = DATEADD(day, -10, GETDATE()) WHERE ID IN (@id1, @id2)",
            new { id1 = alert1Id, id2 = alert2Id });
        await _db.Ado.ExecuteCommandAsync(
            "UPDATE ShareDataAlertLog SET CreateTime = DATEADD(day, -3, GETDATE()) WHERE ID = @id3",
            new { id3 = alert3Id });

        // Act
        using var scope = _host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<LogRetentionService>();
        await service.ScanAndClean(CancellationToken.None);

        // Assert: 2 dòng 10 ngày bị xoá, dòng 3 ngày còn
        Assert.Null(await _db.Queryable<ShareDataAlertLog>().Where(x => x.ID == alert1Id).FirstAsync());
        Assert.Null(await _db.Queryable<ShareDataAlertLog>().Where(x => x.ID == alert2Id).FirstAsync());
        Assert.NotNull(await _db.Queryable<ShareDataAlertLog>().Where(x => x.ID == alert3Id).FirstAsync());
    }

    #endregion
}
