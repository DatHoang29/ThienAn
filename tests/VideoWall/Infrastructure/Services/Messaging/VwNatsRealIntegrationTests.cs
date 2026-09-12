using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Device;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using SqlSugar;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services.Messaging
{
    /// <summary>
    /// Description: Kiểm thử luồng thực tế qua NATS Broker thật kết nối giữa WebAPI (IVwNatsRequestClient) và Worker (VwCommandConsumer) với MockServer Hikvision
    /// Created date: 11/09/2026
    /// </summary>
    [Collection("api")]
    public class VwNatsRealIntegrationTests(Host host)
    {
        private const string TestPrefix = "TEST_NATS_REAL_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly IVwNatsRequestClient _client = host.Services.GetRequiredService<IVwNatsRequestClient>();
        private readonly VwISAPIMockServerHikvision _mock = host.MockServer;

        /// <summary>
        /// Description: WebAPI gửi lệnh DeviceSetupPing qua NATS thật, Worker xử lý gọi ISAPI MockServer thật và trả kết quả thành công về WebAPI
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task RequestAsync_DeviceSetupPing_OverRealNats_ReturnsSuccess_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = await EnsureCenterController();

            // Act: Gửi qua NATS thật
            var result = await _client.RequestAsync<VwSetupSceneStep>(
                VwCommandActions.DeviceSetupPing,
                center.ID);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Xác thực Digest", result.Name);
            Assert.Equal(200, result.HttpStatus);
        }

        /// <summary>
        /// Description: WebAPI gửi truy vấn InputChannels qua NATS thật, Worker proxy sang MockServer thật và trả DTO về cho WebAPI
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task RequestAsync_DeviceProxyInputChannels_OverRealNats_ReturnsInputChannelsData_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = await EnsureCenterController();

            // Act: Gửi qua NATS thật
            var result = await _client.RequestAsync<VwDeviceGenericOutput<VwISAPIInputChannelsResponse>>(
                VwCommandActions.DeviceProxyInputChannels,
                new VwDeviceInputChannelsInput { ControllerId = center.ID });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.True(_mock.GetInputChannelsCallCount >= 1);
        }

        private async Task<VwController> EnsureCenterController(CancellationToken ct = default)
        {
            var center = await _db.Queryable<VwController>()
                .FirstAsync(u => u.IsDelete == null && u.Role == "center", ct);

            if (center == null)
            {
                center = new VwController
                {
                    ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                    Role = "center",
                    IntegrationMode = "active",
                    IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                    Account = VwISAPIMockServerHikvision.DefaultUser,
                    PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                    Status = BaseEnums.StatusEnum.Enable
                };
                await _db.Insertable(center).ExecuteCommandAsync();
            }
            else
            {
                center.IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}";
                center.Account = VwISAPIMockServerHikvision.DefaultUser;
                center.PassWord = VwISAPIMockServerHikvision.DefaultPassword;
                center.Status = BaseEnums.StatusEnum.Enable;
                center.IntegrationMode = "active";
                await _db.Updateable(center).ExecuteCommandAsync();
            }

            return center;
        }
    }
}
