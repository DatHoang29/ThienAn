using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure.Services.Messaging;
using Newtonsoft.Json;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services.Messaging
{
    /// <summary>
    /// Description: Kiểm thử đơn vị cho VwTelemetryConsumer — lắng nghe telemetry và phát broadcast trạng thái Scene
    /// Created date: 11/09/2026
    /// </summary>
    public class VwTelemetryConsumerTests
    {
        private readonly VwTelemetryConsumer _consumer;
        private readonly VwDeviceOptions _options;

        public VwTelemetryConsumerTests()
        {
            _options = new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions
                {
                    DeviceAckTimeoutSeconds = 2
                }
            };
            _consumer = new VwTelemetryConsumer(
                Microsoft.Extensions.Options.Options.Create(_options),
                NullLogger<VwTelemetryConsumer>.Instance);
        }

        /// <summary>
        /// Description: Gọi ProcessTelemetryAsync với dữ liệu null thì hoàn thành êm không lỗi
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessTelemetryAsync_WhenRawIsNull_CompletesSafely_Test()
        {
            var exception = await Record.ExceptionAsync(() => _consumer.ProcessTelemetryAsync(null!));
            Assert.Null(exception);
        }

        /// <summary>
        /// Description: Nhận telemetry thành công từ Worker thì broadcast sự kiện với unconfirmed = false
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessTelemetryAsync_WhenSuccessTrue_BroadcastsUnconfirmedFalse_Test()
        {
            // Arrange
            var messageId = "MSG_TEST_001";
            var sceneId = "SCENE_TEST_001";
            var activeValue = new { SceneId = sceneId, Name = "Scene 1" };
            var controllers = new List<string> { "CTRL_01" };

            _consumer.RegisterPendingActivation(sceneId, activeValue, controllers, messageId);

            object? capturedActiveValue = null;
            List<string>? capturedControllers = null;
            bool? capturedUnconfirmed = null;

            _consumer.OnBroadcastSceneActivated = (val, ctrls, unconf) =>
            {
                capturedActiveValue = val;
                capturedControllers = ctrls;
                capturedUnconfirmed = unconf;
            };

            var telemetryPayload = new
            {
                MessageId = messageId,
                Action = VwCommandActions.ActivateScene,
                Success = true,
                Data = new { SceneId = sceneId }
            };

            // Act
            await _consumer.ProcessTelemetryAsync(JsonConvert.SerializeObject(telemetryPayload));

            // Assert
            Assert.NotNull(capturedActiveValue);
            Assert.NotNull(capturedControllers);
            Assert.False(capturedUnconfirmed);
        }

        /// <summary>
        /// Description: Nhận telemetry thất bại từ Worker thì broadcast sự kiện với unconfirmed = true
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessTelemetryAsync_WhenSuccessFalse_BroadcastsUnconfirmedTrue_Test()
        {
            // Arrange
            var messageId = "MSG_TEST_002";
            var sceneId = "SCENE_TEST_002";
            var activeValue = new { SceneId = sceneId };
            var controllers = new List<string> { "CTRL_01" };

            _consumer.RegisterPendingActivation(sceneId, activeValue, controllers, messageId);

            bool? capturedUnconfirmed = null;
            _consumer.OnBroadcastSceneActivated = (val, ctrls, unconf) =>
            {
                capturedUnconfirmed = unconf;
            };

            var telemetryPayload = new
            {
                MessageId = messageId,
                Action = VwCommandActions.ActivateScene,
                Success = false,
                ErrorMessage = "Device unreachable",
                Data = new { SceneId = sceneId }
            };

            // Act
            await _consumer.ProcessTelemetryAsync(JsonConvert.SerializeObject(telemetryPayload));

            // Assert
            Assert.True(capturedUnconfirmed);
        }

        /// <summary>
        /// Description: Quá thời gian timeout chờ xác nhận thì CheckTimeouts tự động kích hoạt broadcast unconfirmed = true
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task CheckTimeouts_WhenTimeoutExceeded_BroadcastsUnconfirmedTrue_Test()
        {
            // Arrange
            _options.Device.DeviceAckTimeoutSeconds = 0; // Timeout ngay lập tức
            var messageId = "MSG_TEST_TIMEOUT";
            var sceneId = "SCENE_TEST_TIMEOUT";

            _consumer.RegisterPendingActivation(sceneId, new { }, ["CTRL_01"], messageId);

            bool? capturedUnconfirmed = null;
            _consumer.OnBroadcastSceneActivated = (val, ctrls, unconf) =>
            {
                capturedUnconfirmed = unconf;
            };

            // Đợi 10ms để đảm bảo now >= registeredAt + timeout
            await Task.Delay(10);

            // Act
            _consumer.CheckTimeouts();

            // Assert
            Assert.True(capturedUnconfirmed);
        }

        /// <summary>
        /// Description: Nhận VwCommandResponseEnvelope có kiểu với PackageType ControlResponse hoàn tất xác nhận thành công
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessTelemetryAsync_WhenTypedVwCommandResponseEnvelope_BroadcastsAndCompletesReplySink_Test()
        {
            // Arrange
            var messageId = "MSG_TYPED_TEST_001";
            var sceneId = "SCENE_TYPED_TEST_001";
            var activeValue = new { SceneId = sceneId, Name = "Typed Scene" };
            var controllers = new List<string> { "CTRL_01" };

            _consumer.RegisterPendingActivation(sceneId, activeValue, controllers, messageId);

            bool? capturedUnconfirmed = null;
            object? capturedActiveValue = null;

            _consumer.OnBroadcastSceneActivated = (val, ctrls, unconf) =>
            {
                capturedActiveValue = val;
                capturedUnconfirmed = unconf;
            };

            var typedEnvelope = new VwCommandResponseEnvelope
            {
                MessageId = messageId,
                Action = VwCommandActions.ActivateScene,
                Success = true,
                PackageType = VwPackageType.ControlResponse,
                Data = new { SceneId = sceneId }
            };

            // Act
            await _consumer.ProcessTelemetryAsync(typedEnvelope);

            // Assert
            Assert.NotNull(capturedActiveValue);
            Assert.False(capturedUnconfirmed);
        }

        /// <summary>
        /// Description: Thông điệp lỗi thiếu Action hoặc rỗng được bỏ qua an toàn không gây exception
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessTelemetryAsync_WhenInvalidMessage_MissingAction_SafelyIgnored_Test()
        {
            // Arrange
            var invalidEnvelope = new VwCommandResponseEnvelope
            {
                MessageId = "MSG_INVALID_NO_ACTION",
                Action = "",
                Success = true
            };

            bool broadcastInvoked = false;
            _consumer.OnBroadcastSceneActivated = (_, _, _) => broadcastInvoked = true;

            // Act
            var ex = await Record.ExceptionAsync(() => _consumer.ProcessTelemetryAsync(invalidEnvelope));

            // Assert
            Assert.Null(ex);
            Assert.False(broadcastInvoked);
        }

        /// <summary>
        /// Description: Payload cũ không có trường PackageType vẫn được phân tích tương thích ngược an toàn
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessTelemetryAsync_WhenLegacyPayloadWithoutPackageType_BackwardCompatible_Test()
        {
            // Arrange
            var messageId = "MSG_LEGACY_001";
            var sceneId = "SCENE_LEGACY_001";
            _consumer.RegisterPendingActivation(sceneId, new { SceneId = sceneId }, ["CTRL_01"], messageId);

            bool? capturedUnconfirmed = null;
            _consumer.OnBroadcastSceneActivated = (_, _, unconf) => capturedUnconfirmed = unconf;

            var legacyJson = JsonConvert.SerializeObject(new
            {
                MessageId = messageId,
                Action = VwCommandActions.ActivateScene,
                Success = true,
                Data = new { SceneId = sceneId }
            });

            // Act
            await _consumer.ProcessTelemetryAsync(legacyJson);

            // Assert
            Assert.False(capturedUnconfirmed);
        }
    }
}
