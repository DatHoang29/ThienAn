using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services.Messaging
{
    /// <summary>
    /// Description: Kiểm thử đơn vị cho VwItsNatsPublisher — lớp phát thông điệp NATS qua ItsDataTransporter
    /// Created date: 11/09/2026
    /// </summary>
    public class VwItsNatsPublisherTests
    {
        /// <summary>
        /// Description: VwItsNatsPublisher trả về false khi truyền subject rỗng hoặc payload null
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwItsNatsPublisher_WhenPayloadOrSubjectInvalid_ReturnsFalse_Test()
        {
            // Arrange
            var publisher = new VwItsNatsPublisher();

            // Act
            var result1 = await publisher.PublishAsync(string.Empty, new { Test = 1 });
            var result2 = await publisher.PublishAsync(VwSubjects.Request, null!);

            // Assert
            Assert.False(result1);
            Assert.False(result2);
        }

        /// <summary>
        /// Description: VwItsNatsPublisher hoàn thành thành công và trả về true với payload hợp lệ
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwItsNatsPublisher_WhenValid_ReturnsTrue_Test()
        {
            // Arrange
            var publisher = new VwItsNatsPublisher();

            // Act
            var result = await publisher.PublishAsync(VwSubjects.SceneData, new { Alert = "Test" });

            // Assert
            Assert.True(result);
        }
    }
}
