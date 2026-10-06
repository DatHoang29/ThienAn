using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShareDataWorker.Core.Constants;
using ShareDataWorker.Extensions;
using ShareDataWorker.Infrastructure.Workers;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using ShareDataWorker.Core.Models.DataOutbound;
using Module.ShareData.Core.Entities;

namespace Tests.ShareData.Workers
{
    /// <summary>
    /// Description: Bộ kiểm thử tích hợp vai trò instance ShareDataWorker và định danh SelfPartnerCode (SV-4).
    /// Created date: 06/10/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataWorkerRoleTests
    {
        private readonly Host _host;

        public ShareDataWorkerRoleTests(Host host)
        {
            _host = host;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Both")]
        [InlineData("both")]
        public void AddWorkerInfrastructure_WhenRoleBothOrEmpty_RegistersAllFourWorkers_Test(string? role)
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=dev_test;",
                ["ShareData:Role"] = role
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
            var services = new ServiceCollection();

            // Act
            services.AddWorkerInfrastructure(config);

            // Assert
            var hostedServices = services.Where(s => s.ServiceType == typeof(IHostedService)).Select(s => s.ImplementationType).ToList();
            Assert.Contains(typeof(DataOutboundWorker), hostedServices);
            Assert.Contains(typeof(DataChangeTrackingWorker), hostedServices);
            Assert.Contains(typeof(DataNatsWorker), hostedServices);
            Assert.Contains(typeof(DataInboundWorker), hostedServices);
            Assert.Equal(4, hostedServices.Count);
        }

        [Fact]
        public void AddWorkerInfrastructure_WhenRoleSendOnly_RegistersOnlySendWorkers_Test()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=dev_test;",
                ["ShareData:Role"] = ShareDataRole.SendOnly
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
            var services = new ServiceCollection();

            // Act
            services.AddWorkerInfrastructure(config);

            // Assert
            var hostedServices = services.Where(s => s.ServiceType == typeof(IHostedService)).Select(s => s.ImplementationType).ToList();
            Assert.Contains(typeof(DataOutboundWorker), hostedServices);
            Assert.Contains(typeof(DataChangeTrackingWorker), hostedServices);
            Assert.Contains(typeof(DataNatsWorker), hostedServices);
            Assert.DoesNotContain(typeof(DataInboundWorker), hostedServices);
            Assert.Equal(3, hostedServices.Count);
        }

        [Fact]
        public void AddWorkerInfrastructure_WhenRoleReceiveOnly_RegistersOnlyInboundWorker_Test()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=dev_test;",
                ["ShareData:Role"] = ShareDataRole.ReceiveOnly
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
            var services = new ServiceCollection();

            // Act
            services.AddWorkerInfrastructure(config);

            // Assert
            var hostedServices = services.Where(s => s.ServiceType == typeof(IHostedService)).Select(s => s.ImplementationType).ToList();
            Assert.Contains(typeof(DataInboundWorker), hostedServices);
            Assert.DoesNotContain(typeof(DataOutboundWorker), hostedServices);
            Assert.DoesNotContain(typeof(DataChangeTrackingWorker), hostedServices);
            Assert.DoesNotContain(typeof(DataNatsWorker), hostedServices);
            Assert.Single(hostedServices);
        }

        [Fact]
        public void AddWorkerInfrastructure_WhenRoleInvalid_ThrowsInvalidOperationException_Test()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=dev_test;",
                ["ShareData:Role"] = "UnsupportedRole123"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
            var services = new ServiceCollection();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => services.AddWorkerInfrastructure(config));
            Assert.Contains("không hợp lệ", ex.Message);
        }

        [Fact]
        public async Task DataOutboundRestSender_WhenSelfPartnerCodeConfigured_UsesItInHeader_Test()
        {
            // Arrange
            _host.PartnerServer.ResetDefaults();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareData:SelfPartnerCode"] = "A101"
            }).Build();

            var sender = new DataOutboundRestSender(configuration: config);

            // Giả lập gửi tới Mock Server đối tác sẵn có trong test host
            var partner = new ShareDataPartner
            {
                Code = "DEST_PARTNER",
                Address = "127.0.0.1",
                Port = 18090,
                EndPointApiUrl = "/api/receive"
            };
            var sub = new ShareDataSubscription { PartnerId = partner.ID, SerialNbr = 1 };
            var ctx = new DataOutboundContext(sub, partner, new ShareDataPacket { Code = "101" }, new ShareDataMapping(), DateTime.Now);

            // Bắt request tại Mock Server
            _host.PartnerServer.SetResponse(18090, 200, "{\"success\":true}");
            var mappingResult = new DataMappingResult(true, "[]"u8.ToArray(), 0);

            // Act
            await sender.Send(mappingResult, ctx, CancellationToken.None);

            // Assert
            var requests = _host.PartnerServer.GetReceivedRequests(18090);
            Assert.NotEmpty(requests);
            var lastReq = requests.Last();
            Assert.Equal("A101", lastReq.Headers["PartnerCode"]);
        }

        [Fact]
        public async Task DataOutboundRestSender_WhenSelfPartnerCodeNotConfigured_UsesPartnerCodeFromContext_Test()
        {
            // Arrange
            _host.PartnerServer.ResetDefaults();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareData:SelfPartnerCode"] = ""
            }).Build();

            var sender = new DataOutboundRestSender(configuration: config);

            var partner = new ShareDataPartner
            {
                Code = "DEST_PARTNER",
                Address = "127.0.0.1",
                Port = 18090,
                EndPointApiUrl = "/api/receive"
            };
            var sub = new ShareDataSubscription { PartnerId = partner.ID, SerialNbr = 1 };
            var ctx = new DataOutboundContext(sub, partner, new ShareDataPacket { Code = "101" }, new ShareDataMapping(), DateTime.Now);

            _host.PartnerServer.SetResponse(18090, 200, "{\"success\":true}");
            var mappingResult = new DataMappingResult(true, "[]"u8.ToArray(), 0);

            // Act
            await sender.Send(mappingResult, ctx, CancellationToken.None);

            // Assert
            var requests = _host.PartnerServer.GetReceivedRequests(18090);
            Assert.NotEmpty(requests);
            var lastReq = requests.Last();
            Assert.Equal("DEST_PARTNER", lastReq.Headers["PartnerCode"]);
        }
    }
}
