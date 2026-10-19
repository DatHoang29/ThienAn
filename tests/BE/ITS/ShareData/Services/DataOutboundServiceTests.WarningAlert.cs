using Module.ShareData.Core.Entities;
using ShareDataWorker.Infrastructure.Logging;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    public partial class DataOutboundServiceTests
    {
        [Fact]
        public async Task ProcessSubscriptions_WhenConnectionConfigHasIgnoredValues_LogsEsh1403Warning_DeduplicatesAlert_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var invalidConfig1 = "{\"TimeoutSeconds\": 10, \"AuthType\": \"None\", \"Token\": \"stray_token\", \"OutputPath\": \"/deprecated/path\"}";

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_WARN_{unique}",
                SubCode = $"SUB_WARN_{unique}",
                DatatypeId = "101",
                ConfigurePartner = p =>
                {
                    p.Address = ShareDataPartnerServerMock.DefaultHost;
                    p.Port = ShareDataPartnerServerMock.DefaultPort;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                    p.ConnectionConfig = invalidConfig1;
                }
            });

            var worker = CreateWorker(scope);

            // Run 1: Must succeed in delivery AND log ESH-1403 alert
            await worker.ProcessSubscriptions(CancellationToken.None);

            var logs1 = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs1);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs1[0].Success);

            var alerts1 = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.ConfigValueIgnored)
                .ToListAsync();
            Assert.Single(alerts1);
            Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts1[0].Severity);
            Assert.Equal(BaseEnums.AlertSource.Protocol, alerts1[0].AlertSource);
            Assert.Contains("OutputPath", alerts1[0].Message);
            Assert.Contains("xác thực", alerts1[0].Message);

            // Run 2 (same config): Must succeed in delivery, but NOT add duplicate ESH-1403 alert
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await worker.ProcessSubscriptions(CancellationToken.None);

            var alerts2 = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.ConfigValueIgnored)
                .ToListAsync();
            Assert.Single(alerts2);

            // Run 3 (changed config): Change config to new invalid config, update partner
            var invalidConfig2 = "{\"TimeoutSeconds\": 10, \"AuthType\": \"UnsupportedMethodXYZ\", \"OutputPath\": \"/another/path\"}";
            await db.Updateable<ShareDataPartner>()
                .SetColumns(p => p.ConnectionConfig == invalidConfig2)
                .Where(p => p.ID == partner.ID)
                .ExecuteCommandAsync();

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await worker.ProcessSubscriptions(CancellationToken.None);

            var alerts3 = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.ConfigValueIgnored)
                .ToListAsync();
            Assert.Equal(2, alerts3.Count);
        }
    }
}
