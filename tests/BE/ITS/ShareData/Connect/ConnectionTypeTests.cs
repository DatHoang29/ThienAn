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
            Assert.Equal(BaseEnums.ConnectionType.Api, ConnectionTypeExtensions.DefaultType);
        }

        [Theory]
        [InlineData(BaseEnums.ConnectionType.File, true)]
        [InlineData(BaseEnums.ConnectionType.Api, false)]
        [InlineData(BaseEnums.ConnectionType.Ftp, false)]
        [InlineData(BaseEnums.ConnectionType.Ftps, false)]
        [InlineData(BaseEnums.ConnectionType.Kafka, false)]
        public void IsFileChannel_WithEnumValues_ReturnsExpected_Test(BaseEnums.ConnectionType type, bool expected)
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
            BaseEnums.ConnectionType? fileType = BaseEnums.ConnectionType.File;
            BaseEnums.ConnectionType? nullType = null;
            BaseEnums.ConnectionType? apiType = BaseEnums.ConnectionType.Api;

            // Act & Assert
            Assert.True(fileType.IsFileChannel());
            Assert.False(nullType.IsFileChannel());
            Assert.False(apiType.IsFileChannel());
        }

        [Theory]
        [InlineData(BaseEnums.ConnectionType.Ftps, true)]
        [InlineData(BaseEnums.ConnectionType.Ftp, false)]
        [InlineData(BaseEnums.ConnectionType.Api, false)]
        [InlineData(BaseEnums.ConnectionType.File, false)]
        [InlineData(BaseEnums.ConnectionType.Kafka, false)]
        public void IsFtps_WithEnumValues_ReturnsExpected_Test(BaseEnums.ConnectionType type, bool expected)
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
            BaseEnums.ConnectionType? ftpsType = BaseEnums.ConnectionType.Ftps;
            BaseEnums.ConnectionType? nullType = null;
            BaseEnums.ConnectionType? ftpType = BaseEnums.ConnectionType.Ftp;

            // Act & Assert
            Assert.True(ftpsType.IsFtps());
            Assert.False(nullType.IsFtps());
            Assert.False(ftpType.IsFtps());
        }
    }
}
