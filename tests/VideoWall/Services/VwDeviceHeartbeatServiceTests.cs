using ITS.VideoWall.Services.Heartbeat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using Shared.DTO.Enums;
using SqlSugar;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Services
{
    /// <summary>
    /// Description: Kiểm thử VwDeviceHeartbeatService — poll thiết bị định kỳ, cập nhật ScreenState /
    ///              SignalStatus vào CSDL và phát NATS telemetry (happy path + offline path).
    /// Created date: 12/09/2026
    /// </summary>
    [Collection("api")]
    public class VwDeviceHeartbeatServiceTests(Host host)
    {
        private const string TestPrefix = "TEST_VWHB_";
        private readonly IServiceScopeFactory _scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly VwISAPIMockServerHikvision _mock = host.MockServer;

        /// <summary>
        /// Description: PollOneController khi MockServer trả về channels hợp lệ thì cập nhật
        ///              ScreenState = Online, SignalStatus = Enable vào CSDL và phát NATS telemetry.
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task PollOneController_WhenChannelsAvailable_UpdatesDbAndPublishesDeviceTelemetry_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = false;

            var publisher = new FakeNatsPublisherTest();
            var options = Options.Create(new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions { HeartbeatIntervalSeconds = 20 }
            });

            var controller = await EnsureController();

            // Tạo VwScreen có OutputId = "17235971" (khớp với kênh cổng ra HDMI 1 của Hikvision MockServer)
            var screen = new VwScreen
            {
                ID = $"{TestPrefix}SCR_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCR_{Guid.NewGuid():N}",
                Name = "Test Heartbeat Screen",
                ControllerId = controller.ID,
                OutputId = "17235971",
                ScreenState = null,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();

            // Tạo VwSource có SignalNo = 16842753 (khớp với Id đầu vào của MockServer non-cascade)
            var source = new VwSource
            {
                ID = $"{TestPrefix}SRC_{Guid.NewGuid():N}",
                Name = "Test Heartbeat Source",
                ControllerId = controller.ID,
                SignalNo = 16842753,
                SignalStatus = null,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(source).ExecuteCommandAsync();

            var service = new VwDeviceHeartbeatService(
                _scopeFactory,
                options,
                NullLogger<VwDeviceHeartbeatService>.Instance,
                publisher);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Act — gọi trực tiếp PollOneController qua reflection để kiểm thử logic xử lý
            var pollOneMethod = typeof(VwDeviceHeartbeatService)
                .GetMethod("PollOneController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(pollOneMethod);
            using (var actScope = _scopeFactory.CreateScope())
            {
                await (Task)pollOneMethod.Invoke(service, [controller, actScope.ServiceProvider, cts.Token])!;
            }

            // Assert DB — ScreenState đã được cập nhật Online và có LastCheckedAt
            var updatedScreen = await _db.Queryable<VwScreen>().FirstAsync(s => s.ID == screen.ID);
            Assert.NotNull(updatedScreen);
            Assert.Equal(BaseEnums.ScreenState.Online, updatedScreen.ScreenState);
            Assert.NotNull(updatedScreen.LastCheckedAt);

            // Assert DB — SignalStatus đã được cập nhật Enable và có LastCheckedAt
            var updatedSource = await _db.Queryable<VwSource>().FirstAsync(s => s.ID == source.ID);
            Assert.NotNull(updatedSource);
            Assert.Equal(BaseEnums.StatusEnum.Enable, updatedSource.SignalStatus);
            Assert.NotNull(updatedSource.LastCheckedAt);

            // Assert NATS — publisher đã được gọi ít nhất 1 lần
            Assert.True(publisher.PublishedCount >= 1);
            Assert.Equal(VwSubjects.Device, publisher.LastSubject);

            // Assert MockServer — đã nhận được request thăm dò
            Assert.True(_mock.GetInputChannelsCallCount >= 1);
            Assert.True(_mock.GetOutputChannelsCallCount >= 1);
        }

        /// <summary>
        /// Description: PollOneController khi Controller không thể kết nối (SimulateDeviceFailure = true)
        ///              thì MarkControllerScreensOffline đánh toàn bộ VwScreen thuộc controller = Offline.
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task PollOneController_WhenControllerUnreachable_MarksAllScreensOffline_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.SimulateDeviceFailure = true; // MockServer sẽ trả về lỗi cho mọi request

            var publisher = new FakeNatsPublisherTest();
            var options = Options.Create(new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions { HeartbeatIntervalSeconds = 20 }
            });

            var controller = await EnsureController();

            // Tạo 2 VwScreen — cả hai phải chuyển Offline khi controller mất mạng
            var screenA = new VwScreen
            {
                ID = $"{TestPrefix}SCR_A_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCR_A",
                Name = "Screen Offline A",
                ControllerId = controller.ID,
                OutputId = "1",
                ScreenState = BaseEnums.ScreenState.Online,  // hiện đang Online
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var screenB = new VwScreen
            {
                ID = $"{TestPrefix}SCR_B_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCR_B",
                Name = "Screen Offline B",
                ControllerId = controller.ID,
                OutputId = "2",
                ScreenState = BaseEnums.ScreenState.Online,  // hiện đang Online
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screenA).ExecuteCommandAsync();
            await _db.Insertable(screenB).ExecuteCommandAsync();

            var service = new VwDeviceHeartbeatService(
                _scopeFactory,
                options,
                NullLogger<VwDeviceHeartbeatService>.Instance,
                publisher);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Act
            var pollMethod = typeof(VwDeviceHeartbeatService)
                .GetMethod("PollAllControllers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(pollMethod);
            await (Task)pollMethod.Invoke(service, [cts.Token])!;

            // Assert DB — cả 2 màn hình chuyển Offline
            var updatedA = await _db.Queryable<VwScreen>().FirstAsync(s => s.ID == screenA.ID);
            var updatedB = await _db.Queryable<VwScreen>().FirstAsync(s => s.ID == screenB.ID);
            Assert.Equal(BaseEnums.ScreenState.Offline, updatedA?.ScreenState);
            Assert.Equal(BaseEnums.ScreenState.Offline, updatedB?.ScreenState);

            // Assert NATS — telemetry offline được phát
            Assert.True(publisher.PublishedCount >= 1);
            Assert.Equal(VwSubjects.Device, publisher.LastSubject);
        }

        // ─── Helper: đảm bảo controller tồn tại trong DB trỏ về MockServer ───

        /// <summary>
        /// Description: Tạo hoặc lấy VwController test trỏ về MockServer để heartbeat poll.
        /// Created date: 12/09/2026
        /// </summary>
        private async Task<VwController> EnsureController(CancellationToken ct = default)
        {
            var ctrl = await _db.Queryable<VwController>()
                .FirstAsync(c => c.IsDelete == null && c.IP == $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}"
                              && c.Status == BaseEnums.StatusEnum.Enable, ct);

            if (ctrl == null)
            {
                ctrl = new VwController
                {
                    ID = $"{TestPrefix}CTRL_{Guid.NewGuid():N}",
                    IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                    Account = VwISAPIMockServerHikvision.DefaultUser,
                    PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                    Status = BaseEnums.StatusEnum.Enable,
                    CreateTime = DateTime.Now
                };
                await _db.Insertable(ctrl).ExecuteCommandAsync();
            }

            return ctrl;
        }

        // ─── Fake Publisher for test isolation ───

        /// <summary>
        /// Description: Publisher giả lập để bắt lệnh phát NATS trong unit test.
        /// Created date: 12/09/2026
        /// </summary>
        private sealed class FakeNatsPublisherTest : IVwNatsPublisher
        {
            public int PublishedCount { get; private set; }
            public string? LastSubject { get; private set; }
            public object? LastPayload { get; private set; }

            /// <summary>
            /// Description: Ghi nhận lệnh publish để assert trong test.
            /// Created date: 12/09/2026
            /// </summary>
            public Task<bool> PublishAsync(string subject, object payload, CancellationToken ct = default)
            {
                PublishedCount++;
                LastSubject = subject;
                LastPayload = payload;
                return Task.FromResult(true);
            }
        }
    }
}
