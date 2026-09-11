using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Shared.DTO.Constants.Application;
using SqlSugar;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Consumer
{
    /// <summary>
    /// Description: Kiểm thử luồng xử lý gói tin lệnh NATS của VwCommandConsumer tương tác thực tế với CSDL và VwISAPIMockServerHikvision
    /// Created date: 11/09/2026
    /// </summary>
    [Collection("api")]
    public class VwCommandConsumerTests(Host host)
    {
        private const string TestPrefix = "TEST_VWCMD_";
        private readonly IServiceScopeFactory _scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly VwISAPIMockServerHikvision _mock = host.MockServer;

        /// <summary>
        /// Description: Lệnh ACTIVATE_SCENE gọi qua VwCommandConsumer kích hoạt HTTP thật vào MockServer và phát telemetry thành công
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenActivateScene_CallsMockServerAndPublishesSuccess_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = await EnsureCenterController();
            var scene = await EnsureScene("1");

            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedMessageId = null;
            string? capturedAction = null;
            bool? capturedSuccess = null;
            string? capturedError = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedMessageId = msgId;
                capturedAction = action;
                capturedSuccess = success;
                capturedError = error;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_ACTIVATE_{Guid.NewGuid():N}",
                Action = VwCommandActions.ActivateScene,
                SceneId = scene.ID,
                ControllerId = center.ID,
                Payload = new VwActivateScenePayload
                {
                    SceneId = scene.ID,
                    OutputId = "1",
                    TargetControllerIds = [center.ID]
                }
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.True(_mock.ActivateSceneCallCount >= 1);
            Assert.True(_mock.GetCapabilitiesCallCount >= 1);
            Assert.True(_mock.GetVideoWallsCallCount >= 1);
            Assert.True(capturedSuccess);
            Assert.Equal(envelope.MessageId, capturedMessageId);
            Assert.Equal(VwCommandActions.ActivateScene, capturedAction);
            Assert.Null(capturedError);
        }

        /// <summary>
        /// Description: Lệnh SYNC_SCENE_WINDOWS gửi qua VwCommandConsumer đồng bộ cửa sổ lên thiết bị MockServer qua HTTP thật và phản hồi telemetry thành công
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenSyncSceneWindows_CallsMockServerAndPublishesSuccess_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = await EnsureCenterController();
            var scene = await EnsureScene("1");
            await EnsureWindowScene(scene.ID);

            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedAction = null;
            bool? capturedSuccess = null;
            string? capturedError = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedAction = action;
                capturedSuccess = success;
                capturedError = error;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_SYNC_{Guid.NewGuid():N}",
                Action = VwCommandActions.SyncSceneWindows,
                SceneId = scene.ID,
                ControllerId = center.ID,
                Payload = new VwSyncWindowsPayload
                {
                    SceneId = scene.ID
                }
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.Null(capturedError);
            Assert.True(capturedSuccess);
            Assert.True(_mock.AddWindowCallCount >= 1);
            Assert.True(_mock.SaveSceneDataCallCount >= 1);
            Assert.Equal(VwCommandActions.SyncSceneWindows, capturedAction);
        }

        /// <summary>
        /// Description: Lệnh SET_WINDOW_LAYER với hành động Top gửi qua VwCommandConsumer kích hoạt HTTP top vào MockServer và phát telemetry thành công
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenSetWindowLayerTop_CallsMockServerAndPublishesSuccess_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = await EnsureCenterController();
            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedAction = null;
            bool? capturedSuccess = null;
            string? capturedError = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedAction = action;
                capturedSuccess = success;
                capturedError = error;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_TOP_{Guid.NewGuid():N}",
                Action = VwCommandActions.SetWindowLayer,
                ControllerId = center.ID,
                Payload = new VwSetWindowLayerPayload
                {
                    ControllerId = center.ID,
                    DeviceWindowId = "1",
                    Action = "Top"
                }
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.True(_mock.WindowTopCallCount >= 1);
            Assert.True(capturedSuccess);
            Assert.Equal(VwCommandActions.SetWindowLayer, capturedAction);
            Assert.Null(capturedError);
        }

        /// <summary>
        /// Description: Lệnh SET_WINDOW_LAYER với hành động Bottom gửi qua VwCommandConsumer kích hoạt HTTP bottom vào MockServer và phát telemetry thành công
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenSetWindowLayerBottom_CallsMockServerAndPublishesSuccess_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = await EnsureCenterController();
            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedAction = null;
            bool? capturedSuccess = null;
            string? capturedError = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedAction = action;
                capturedSuccess = success;
                capturedError = error;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_BTM_{Guid.NewGuid():N}",
                Action = VwCommandActions.SetWindowLayer,
                ControllerId = center.ID,
                Payload = new VwSetWindowLayerPayload
                {
                    ControllerId = center.ID,
                    DeviceWindowId = "1",
                    Action = "Bottom"
                }
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.True(_mock.WindowBottomCallCount >= 1);
            Assert.True(capturedSuccess);
            Assert.Equal(VwCommandActions.SetWindowLayer, capturedAction);
            Assert.Null(capturedError);
        }

        /// <summary>
        /// Description: Khi MockServer báo lỗi thiết bị thì VwCommandConsumer bắt ngoại lệ và phát telemetry thất bại
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenDeviceFails_PublishesFailedTelemetry_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;
            _mock.SimulateDeviceFailure = true;

            var center = await EnsureCenterController();
            var scene = await EnsureScene("1");

            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedMessageId = null;
            bool? capturedSuccess = null;
            string? capturedError = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedMessageId = msgId;
                capturedSuccess = success;
                capturedError = error;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_FAIL_{Guid.NewGuid():N}",
                Action = VwCommandActions.ActivateScene,
                SceneId = scene.ID,
                ControllerId = center.ID,
                Payload = new VwActivateScenePayload
                {
                    SceneId = scene.ID,
                    OutputId = "1",
                    TargetControllerIds = [center.ID]
                }
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.False(capturedSuccess);
            Assert.NotNull(capturedError);
            Assert.Equal(envelope.MessageId, capturedMessageId);
        }

        /// <summary>
        /// Description: VwCommandConsumer gọi MockServer thành công và phát telemetry qua IVwNatsPublisher lên subject VwSubjects.Response
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WithPublisher_CallsMockServerAndPublishesTelemetryViaNatsPublisher_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = await EnsureCenterController();
            var scene = await EnsureScene("1");

            var publisherStub = new FakeNatsPublisherTest();
            var consumer = new VwCommandConsumer(
                _scopeFactory,
                NullLogger<VwCommandConsumer>.Instance,
                transport: null,
                publisher: publisherStub);

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_PUB_{Guid.NewGuid():N}",
                Action = VwCommandActions.ActivateScene,
                SceneId = scene.ID,
                ControllerId = center.ID,
                Payload = new VwActivateScenePayload
                {
                    SceneId = scene.ID,
                    OutputId = "1",
                    TargetControllerIds = [center.ID]
                }
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.True(_mock.ActivateSceneCallCount >= 1);
            Assert.True(publisherStub.PublishedCount > 0);
            Assert.Equal(VwSubjects.Response, publisherStub.LastSubject);
            Assert.NotNull(publisherStub.LastPayload);
        }

        /// <summary>
        /// Description: Lệnh ResetCircuitBreaker thực thi thành công và phát telemetry phản hồi
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenResetCircuitBreaker_PublishesSuccessTelemetry_Test()
        {
            // Arrange
            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedAction = null;
            bool? capturedSuccess = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedAction = action;
                capturedSuccess = success;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_RESET_{Guid.NewGuid():N}",
                Action = VwCommandActions.ResetCircuitBreaker,
                ControllerId = "CTRL_TEST_BREAKER",
                Payload = new VwResetCircuitBreakerPayload
                {
                    TargetIpOrKey = "127.0.0.1:18080"
                }
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.Equal(VwCommandActions.ResetCircuitBreaker, capturedAction);
            Assert.True(capturedSuccess);
        }

        /// <summary>
        /// Description: Gửi lệnh có hành động không hỗ trợ thì phát telemetry báo lỗi unsupported action
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenActionUnsupported_PublishesFailedTelemetry_Test()
        {
            // Arrange
            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedMessageId = null;
            string? capturedAction = null;
            bool? capturedSuccess = null;
            string? capturedError = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedMessageId = msgId;
                capturedAction = action;
                capturedSuccess = success;
                capturedError = error;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_UNSUPPORTED_{Guid.NewGuid():N}",
                Action = "UnknownActionXYZ",
                ControllerId = "CTRL_01"
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.Equal(envelope.MessageId, capturedMessageId);
            Assert.Equal("UnknownActionXYZ", capturedAction);
            Assert.False(capturedSuccess);
            Assert.NotNull(capturedError);
            Assert.Contains("Unsupported action", capturedError);
        }

        /// <summary>
        /// Description: Gọi ProcessCommandAsync với dữ liệu null thì hoàn thành êm không gây lỗi
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenRawIsNull_CompletesSafely_Test()
        {
            // Arrange
            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            // Act
            var exception = await Record.ExceptionAsync(() => consumer.ProcessCommandAsync(null!));

            // Assert
            Assert.Null(exception);
        }

        /// <summary>
        /// Description: Lệnh DEVICE_SETUP_PING gọi qua VwCommandConsumer tới MockServer và phát telemetry thành công
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenDeviceSetupPing_CallsMockServerAndPublishesSuccess_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            var center = await EnsureCenterController();

            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedAction = null;
            bool? capturedSuccess = null;
            object? capturedData = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedAction = action;
                capturedSuccess = success;
                capturedData = data;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_PING_{Guid.NewGuid():N}",
                Action = VwCommandActions.DeviceSetupPing,
                ControllerId = center.ID,
                Payload = center.ID
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.Equal(VwCommandActions.DeviceSetupPing, capturedAction);
            Assert.True(capturedSuccess);
            Assert.NotNull(capturedData);
        }

        /// <summary>
        /// Description: Lệnh DEVICE_PROXY_CAPABILITIES gọi qua VwCommandConsumer tới MockServer và phát telemetry thành công
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessCommandAsync_WhenDeviceProxyCapabilities_CallsMockServerAndPublishesSuccess_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            var center = await EnsureCenterController();

            var consumer = new VwCommandConsumer(_scopeFactory, NullLogger<VwCommandConsumer>.Instance);

            string? capturedAction = null;
            bool? capturedSuccess = null;
            object? capturedData = null;

            consumer.OnTelemetryPublished = (msgId, action, success, error, data) =>
            {
                capturedAction = action;
                capturedSuccess = success;
                capturedData = data;
            };

            var envelope = new VwCommandEnvelope
            {
                MessageId = $"MSG_CAPS_{Guid.NewGuid():N}",
                Action = VwCommandActions.DeviceProxyCapabilities,
                ControllerId = center.ID,
                Payload = new VwDeviceCapabilitiesInput
                {
                    ControllerId = center.ID
                }
            };

            // Act
            await consumer.ProcessCommandAsync(envelope);

            // Assert
            Assert.Equal(VwCommandActions.DeviceProxyCapabilities, capturedAction);
            Assert.True(capturedSuccess);
            Assert.NotNull(capturedData);
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

        private async Task<VwScene> EnsureScene(string outputId = "1", CancellationToken ct = default)
        {
            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_{Guid.NewGuid():N}",
                Code = $"SCN_{Guid.NewGuid():N}",
                Name = "Scene Test Real Mock",
                OutputId = outputId,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();
            return scene;
        }

        private async Task<VwWindowScene> EnsureWindowScene(string sceneId, CancellationToken ct = default)
        {
            var window = new VwWindowScene
            {
                ID = $"{TestPrefix}WND_{Guid.NewGuid():N}",
                SceneId = sceneId,
                Name = "Window 1",
                DeviceWindowId = "1",
                X = 0,
                Y = 0,
                W = 1920,
                H = 1080,
                ZIndex = 1
            };
            await _db.Insertable(window).ExecuteCommandAsync();
            return window;
        }

        private sealed class FakeNatsPublisherTest : IVwNatsPublisher
        {
            public int PublishedCount { get; private set; }
            public string? LastSubject { get; private set; }
            public object? LastPayload { get; private set; }

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
