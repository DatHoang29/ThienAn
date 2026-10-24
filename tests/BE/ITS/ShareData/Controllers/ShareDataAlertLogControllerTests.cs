using Module.ShareData.Core.Dto.AlertLog;
using Module.ShareData.Core.Entities;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Description: Kiểm thử tích hợp cho ShareDataAlertLog Controller và CommandHandler (xác nhận cảnh báo)
/// Created date: 24/10/2026
/// </summary>
[Collection("api")]
public class ShareDataAlertLogControllerTests(Host host)
{
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();

    /// <summary>
    /// Description: FE gửi ToDate theo UTC (toISOString). Xác nhận tất cả phải đổi ToDate về giờ server như bên Query,
    ///              nên cảnh báo phát sinh trong vòng 7 giờ gần nhất vẫn được xác nhận, cảnh báo sau ToDate thì không.
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task AcknowledgeAll_WhenToDateIsUtc_AcknowledgesRecentAlertsInServerTime_Test()
    {
        // Arrange
        var partnerId = Guid.NewGuid().ToString("N");
        var now = DateTime.Now;
        var recent = new ShareDataAlertLog
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerId,
            OccurredAt = now.AddHours(-1),
            Severity = BaseEnums.AlertSeverity.Error,
            AlertCode = "ESH-1401",
            Message = "Cảnh báo trong 7 giờ gần nhất"
        };
        var future = new ShareDataAlertLog
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerId,
            OccurredAt = now.AddHours(1),
            Severity = BaseEnums.AlertSeverity.Error,
            AlertCode = "ESH-1401",
            Message = "Cảnh báo sau mốc ToDate"
        };
        await _db.Insertable(new List<ShareDataAlertLog> { recent, future }).ExecuteCommandAsync();

        // Act
        var affected = await host.Services.CallApiAsync<int>(new ShareDataAcknowledgeAllAlertInput
        {
            PartnerId = partnerId,
            ToDate = DateTime.UtcNow
        });

        // Assert
        var rows = await _db.Queryable<ShareDataAlertLog>()
            .Where(a => a.PartnerId == partnerId)
            .ToListAsync();
        Assert.Equal(1, affected);
        Assert.True(rows.Single(a => a.ID == recent.ID).Acknowledged,
            "Cảnh báo 1 giờ trước phải được xác nhận: ToDate UTC phải đổi về giờ server trước khi so với OccurredAt.");
        Assert.NotEqual(true, rows.Single(a => a.ID == future.ID).Acknowledged);
    }
}
