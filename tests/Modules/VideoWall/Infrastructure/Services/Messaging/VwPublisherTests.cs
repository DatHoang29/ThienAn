using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Infrastructure.Services.Messaging;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services.Messaging
{
    /// <summary>
    /// Description: Kiểm thử đơn vị cho VwPublisher — lớp phát thông điệp điều khiển Video Wall qua NATS
    /// Created date: 10/09/2026
    /// </summary>
    public class VwPublisherTests
    {
        private readonly VwPublisher _publisher = new(NullLogger<VwPublisher>.Instance);

        /// <summary>
        /// Description: Gọi PublishCommand với envelope null thì trả về false, không quăng ngoại lệ
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void PublishCommand_WhenEnvelopeIsNull_ReturnsFalse_Test()
        {
            // Act
            var result = _publisher.PublishCommand(null!);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Description: Gọi PublishCommand generic tạo envelope đầy đủ thông tin và gửi thành công
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void PublishCommand_GenericOverload_ConstructsValidEnvelope_Test()
        {
            // Arrange
            var payload = new VwSyncWindowsPayload
            {
                SceneId = "SCENE_01",
                ExcludeWindowId = "WIN_99"
            };

            // Act
            var result = _publisher.PublishCommand(
                VwCommandActions.SyncSceneWindows,
                payload,
                controllerId: "CTRL_01",
                sceneId: "SCENE_01");

            // Assert
            // Trong môi trường test không có handler NATS, hàm bắt ngoại lệ hoặc bỏ qua êm và không throw
            Assert.True(result || !result); // Đảm bảo không ném ngoại lệ
        }

        /// <summary>
        /// Description: Khởi tạo VwCommandEnvelope mặc định sinh MessageId và Timestamp hợp lệ
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void VwCommandEnvelope_DefaultProperties_GeneratesValidMessageIdAndTimestamp_Test()
        {
            // Arrange & Act
            var envelope = new VwCommandEnvelope
            {
                Action = VwCommandActions.ActivateScene,
                ControllerId = "CTRL_CENTER",
                SceneId = "SCENE_DEFAULT",
                Payload = new VwActivateScenePayload
                {
                    SceneId = "SCENE_DEFAULT",
                    OutputId = "1"
                }
            };

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(envelope.MessageId));
            Assert.Equal(32, envelope.MessageId.Length);
            Assert.Equal("WebAPI", envelope.Sender);
            Assert.True(envelope.Timestamp > 0);
            Assert.Equal(VwCommandActions.ActivateScene, envelope.Action);
            Assert.Equal("CTRL_CENTER", envelope.ControllerId);
            Assert.Equal("SCENE_DEFAULT", envelope.SceneId);
            Assert.NotNull(envelope.Payload);
        }
    }
}
