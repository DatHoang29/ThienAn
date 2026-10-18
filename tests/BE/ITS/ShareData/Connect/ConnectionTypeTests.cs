using Modules.Integration.Delivery.Base.Extensions;
using Shared.DTO.Enums;
using Xunit;

namespace Tests.ShareData.Connect
{
    public class ConnectionTypeTests
    {
        [Fact]
        public void DefaultType_ReturnsApi_Test()
        {
            // Assert
            Assert.Equal(ConnectionType.Api, ConnectionTypeExtensions.DefaultType);
        }

        [Theory]
        [InlineData(ConnectionType.File, true)]
        [InlineData(ConnectionType.Api, false)]
        [InlineData(ConnectionType.Ftp, false)]
        [InlineData(ConnectionType.Ftps, false)]
        [InlineData(ConnectionType.Kafka, false)]
        public void IsFileChannel_WithEnumValues_ReturnsExpected_Test(ConnectionType type, bool expected)
        {
            // Act
            var result = type.IsFileChannel();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsFileChannel_WithNullable_ReturnsExpected_Test()
        {
            // Arrange
            ConnectionType? fileType = ConnectionType.File;
            ConnectionType? nullType = null;
            ConnectionType? apiType = ConnectionType.Api;

            // Act & Assert
            Assert.True(fileType.IsFileChannel());
            Assert.False(nullType.IsFileChannel());
            Assert.False(apiType.IsFileChannel());
        }

        [Theory]
        [InlineData(ConnectionType.Ftps, true)]
        [InlineData(ConnectionType.Ftp, false)]
        [InlineData(ConnectionType.Api, false)]
        [InlineData(ConnectionType.File, false)]
        [InlineData(ConnectionType.Kafka, false)]
        public void IsFtps_WithEnumValues_ReturnsExpected_Test(ConnectionType type, bool expected)
        {
            // Act
            var result = type.IsFtps();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsFtps_WithNullable_ReturnsExpected_Test()
        {
            // Arrange
            ConnectionType? ftpsType = ConnectionType.Ftps;
            ConnectionType? nullType = null;
            ConnectionType? ftpType = ConnectionType.Ftp;

            // Act & Assert
            Assert.True(ftpsType.IsFtps());
            Assert.False(nullType.IsFtps());
            Assert.False(ftpType.IsFtps());
        }
    }
}
