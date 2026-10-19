namespace Tests.ShareData.Infrastructure.Services.DataOutbound;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Xunit;

public partial class DataOutboundServiceTests
{
    #region 10. Đa tiến trình & Đa đối tác (SV-4 Concurrency & SV-5 Multi-Partner)

    /// <summary>
    /// Description: SV-4 · Khi gửi dữ liệu cho đối tác, header HTTP PartnerCode, PacketCode,
    ///              SerialNbr được truyền chính xác theo cấu hình của đối tác và lượt gửi.
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    [Trait("Feature", "SV-4")]
    public async Task ProcessSubscriptions_WhenPartnerCodeIsPresent_SendsPartnerCodeHeader_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var partnerServer = _host.PartnerServer;
        partnerServer.ResetDefaults();

        await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
        var unique = Guid.NewGuid().ToString("N")[..8];
        await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

        var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
        {
            PartnerCode = $"DEST_P_{unique}",
            SubCode = $"SUB_SV4_{unique}",
            DatatypeId = "101",
            ConfigureSub = s => s.SerialNbr = 1
        });

        // Act
        await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

        // Assert
        Assert.NotNull(partnerServer.LastRequest);
        var lastReq = partnerServer.LastRequest;

        Assert.True(lastReq.Headers.ContainsKey("PartnerCode"), "Request phải có header PartnerCode");
        Assert.Equal(partner.Code, lastReq.Headers["PartnerCode"]);
        Assert.Equal("101", lastReq.Headers["PacketCode"]);
        Assert.Equal((sub.SerialNbr ?? 1).ToString(), lastReq.Headers["SerialNbr"]);

        var logs = await GetLogs(db, sub.ID);
        var parent = logs.Single(l => l.ParentId == null);
        Assert.Equal(BaseEnums.SuccessEnums.Success, parent.Success);
    }

    /// <summary>
    /// Description: SV-4 · Chứng minh khoá phân tán CSDL (ProcessingUntil): 4 worker tiến trình cùng quét
    ///              đồng thời một subscription, chỉ duy nhất 1 worker giành được quyền gửi và không gửi đúp (0 duplicate).
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    [Trait("Feature", "SV-4")]
    public async Task ProcessSubscriptions_MultiWorkerConcurrency_FourWorkersCompeteForSameSubscription_OnlyOneProcesses_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var partnerServer = _host.PartnerServer;
        partnerServer.ResetDefaults();

        await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
        var unique = Guid.NewGuid().ToString("N")[..8];
        await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

        var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
        {
            PartnerCode = $"CONC_P_{unique}",
            SubCode = $"SUB_CONC_{unique}",
            DatatypeId = "101"
        });

        var worker1 = CreateWorker(scope);
        var worker2 = CreateWorker(scope);
        var worker3 = CreateWorker(scope);
        var worker4 = CreateWorker(scope);

        // Act: 4 worker cùng kích hoạt tranh chấp đồng thời một subscription
        await Task.WhenAll(
            worker1.ProcessSubscriptions(CancellationToken.None),
            worker2.ProcessSubscriptions(CancellationToken.None),
            worker3.ProcessSubscriptions(CancellationToken.None),
            worker4.ProcessSubscriptions(CancellationToken.None));

        // Assert: Chỉ duy nhất 1 worker được gửi, phía đối tác chỉ nhận đúng 1 request, không bị trùng lặp
        Assert.Equal(1, partnerServer.TotalReceivedRequests);

        var logs = await GetLogs(db, sub.ID);
        var parentLogs = logs.Where(l => l.ParentId == null).ToList();
        Assert.Single(parentLogs);
        Assert.Equal(BaseEnums.SuccessEnums.Success, parentLogs[0].Success);
    }

    /// <summary>
    /// Description: SV-5 · Kịch bản test đa đối tác: 4 node clone (A101..A104) cùng gửi về một Receiver,
    ///              mỗi node gửi kèm header PartnerCode định danh chính mình, dữ liệu hoàn toàn độc lập.
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    [Trait("Feature", "SV-4")]
    [Trait("Feature", "SV-5")]
    public async Task ProcessSubscriptions_FourCloneNodes_SendWithDistinctPartnerCodes_SV5_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var partnerServer = _host.PartnerServer;
        partnerServer.ResetDefaults();

        await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
        var unique = Guid.NewGuid().ToString("N")[..8];
        await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

        var expectedCloneCodes = new[] { "A101", "A102", "A103", "A104" };
        var subs = new List<ShareDataSubscription>();
        for (var i = 0; i < 4; i++)
        {
            var (_, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = expectedCloneCodes[i],
                SubCode = $"SUB_SV5_{i}_{unique}",
                DatatypeId = "101",
                ConfigureSub = s => s.NextTimeRun = DateTime.MaxValue
            });
            subs.Add(sub);
        }

        // Act: Lần lượt từng clone node xử lý subscription của mình
        for (var i = 0; i < 4; i++)
        {
            await MakeSubscriptionDue(db, subs[i].ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
        }

        // Assert: Phía server nhận nhận đủ 4 request độc lập với đúng header PartnerCode tương ứng
        Assert.Equal(4, partnerServer.TotalReceivedRequests);

        for (var i = 0; i < 4; i++)
        {
            var req = partnerServer.ReceivedRequests[i];
            Assert.True(req.Headers.ContainsKey("PartnerCode"), $"Request {i + 1} phải có header PartnerCode");
            Assert.Equal(expectedCloneCodes[i], req.Headers["PartnerCode"]);

            var logs = await GetLogs(db, subs[i].ID);
            var parent = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Success, parent.Success);
        }
    }

    #endregion
}
