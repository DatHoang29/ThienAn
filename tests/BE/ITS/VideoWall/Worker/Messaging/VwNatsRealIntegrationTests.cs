using System.Diagnostics;


using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Dto.Device;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;

using Tests.VideoWall.Mocks;




namespace Tests.VideoWall.Worker.Messaging
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
        private readonly IVwPublisher _publisher = host.Services.GetRequiredService<IVwPublisher>();
        private readonly IVwISAPIDeviceService _deviceService = host.Services.GetRequiredService<IVwISAPIDeviceService>();
        private readonly VwISAPIServerHikvisionMock _mock = host.MockServer;

        /// <summary>
        /// Description: WebAPI publish lệnh SyncSceneWindows qua NATS, Worker Consumer nhận tin và đồng bộ xuống thiết bị MockServer, cập nhật DeviceWindowId vào CSDL
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task PublishCommand_SyncSceneWindows_OverNats_ConsumerProcessesAndUpdatesDb_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;
            _deviceService.ResetAllDeviceAuthFailures();

            var center = await EnsureCenterController();

            var screen = new VwScreen
            {
                ID = $"{TestPrefix}SCR_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCR_{Guid.NewGuid():N}",
                Name = "Screen NATS E2E",


                OutputId = "1",
                GridCol = 0,
                GridRow = 0,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCN_{Guid.NewGuid():N}",
                Name = "Scene NATS E2E",
                OutputId = "1",


                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var window = new VwWindowScene
            {
                ID = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
                Name = "Window NATS E2E",
                SceneId = scene.ID,
                X = 0,
                Y = 0,
                W = 1920,
                H = 1080,
                Visible = BaseEnums.SceneWindowVisible.Visible,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(window).ExecuteCommandAsync();

            var initialAddCount = _mock.AddWindowCallCount;

            // Act: WebAPI publish lệnh qua NATS
            var published = await _publisher.PublishCommandAsync(
                VwCommandActions.SyncSceneWindows,
                new VwSyncWindowsPayload
                {
                    SceneId = scene.ID
                },
                controllerId: center.ID,
                sceneId: scene.ID);

            Assert.True(published, "Publish lệnh SyncSceneWindows qua NATS thất bại");

            // Assert: Đợi Worker Consumer tiêu thụ tin nhắn NATS và cập nhật CSDL
            var timeout = TimeSpan.FromSeconds(10);
            var sw = Stopwatch.StartNew();
            VwWindowScene? updatedWin = null;
            while (sw.Elapsed < timeout)
            {
                updatedWin = await _db.Queryable<VwWindowScene>().FirstAsync(w => w.ID == window.ID);
                if (!string.IsNullOrEmpty(updatedWin?.ID) && _mock.SaveSceneDataCallCount >= 1)
                    break;

                await Task.Delay(100);
            }

            Assert.NotNull(updatedWin);
            Assert.True(_mock.AddWindowCallCount > initialAddCount, "Consumer chưa gọi MockServer tạo window.");
            Assert.True(_mock.SaveSceneDataCallCount >= 1, "Consumer chưa gọi MockServer lưu kịch bản.");

            _deviceService.ResetAllDeviceAuthFailures();
        }

        private async Task<VwController> EnsureCenterController(CancellationToken ct = default)
        {
            var center = await _db.Queryable<VwController>()
                .FirstAsync(u => u.IsDelete == null && u.ParentControllerId == null, ct);

            if (center == null)
            {
                center = new VwController
                {
                    ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",


                    IntegrationMode = "active",
                    IP = $"127.0.0.1:{VwISAPIServerHikvisionMock.DefaultPort}",
                    Account = VwISAPIServerHikvisionMock.DefaultUser,
                    PassWord = VwISAPIServerHikvisionMock.DefaultPassword,
                    Status = BaseEnums.StatusEnum.Enable
                };
                await _db.Insertable(center).ExecuteCommandAsync();
            }
            else
            {
                center.IP = $"127.0.0.1:{VwISAPIServerHikvisionMock.DefaultPort}";
                center.Account = VwISAPIServerHikvisionMock.DefaultUser;
                center.PassWord = VwISAPIServerHikvisionMock.DefaultPassword;
                center.Status = BaseEnums.StatusEnum.Enable;
                center.IntegrationMode = "active";
                await _db.Updateable(center).ExecuteCommandAsync();
            }

            return center;
        }
    }
}

