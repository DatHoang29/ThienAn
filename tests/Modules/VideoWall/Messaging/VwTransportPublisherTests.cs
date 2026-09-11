using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Services.Shared.Messaging;
using Xunit;

namespace Tests.Modules.VideoWall.Messaging
{
    /// <summary>
    /// Description: Kiểm thử đơn vị cho VwTransportPublisher — lớp phát thông điệp NATS sử dụng TransportManager
    /// Created date: 11/09/2026
    /// </summary>
    public class VwTransportPublisherTests
    {
        /// <summary>
        /// Description: VwTransportPublisher khi chưa kết nối NATS thì bỏ qua êm và trả về false (chính sách mỏng)
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwTransportPublisher_WhenNotConnected_ReturnsFalseGracefully_Test()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            var transport = new TransportManager(config);
            var publisher = new VwTransportPublisher(transport, NullLogger<VwTransportPublisher>.Instance);

            // Act
            var result = await publisher.PublishAsync(VwSubjects.Response, new { Test = "Payload" });

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Description: VwTransportPublisher trả về false khi truyền subject rỗng hoặc payload null
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwTransportPublisher_WhenPayloadOrSubjectInvalid_ReturnsFalse_Test()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            var transport = new TransportManager(config);
            var publisher = new VwTransportPublisher(transport, NullLogger<VwTransportPublisher>.Instance);

            // Act
            var result1 = await publisher.PublishAsync(string.Empty, new { Test = 1 });
            var result2 = await publisher.PublishAsync(VwSubjects.Response, null!);

            // Assert
            Assert.False(result1);
            Assert.False(result2);
        }
    }
}
