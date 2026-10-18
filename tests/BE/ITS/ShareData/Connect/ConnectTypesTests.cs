using Modules.Integration.ShareData.Base.Constants;
using Xunit;

namespace Tests.ShareData.Connect
{
    public class ConnectTypesTests
    {
        [Theory]
        [InlineData(null, ConnectTypes.Api)]
        [InlineData("", ConnectTypes.Api)]
        [InlineData("   ", ConnectTypes.Api)]
        [InlineData("API", ConnectTypes.Api)]
        [InlineData("api", ConnectTypes.Api)]
        [InlineData(" REST ", ConnectTypes.Api)]
        [InlineData("http", ConnectTypes.Api)]
        [InlineData("WebApi", ConnectTypes.Api)]
        [InlineData("WEBAPI", ConnectTypes.Api)]
        [InlineData("File", ConnectTypes.File)]
        [InlineData("file", ConnectTypes.File)]
        [InlineData("DISK", ConnectTypes.File)]
        [InlineData("disk", ConnectTypes.File)]
        [InlineData("NAS", ConnectTypes.File)]
        [InlineData("nas", ConnectTypes.File)]
        [InlineData("Ftp", ConnectTypes.Ftp)]
        [InlineData("ftp", ConnectTypes.Ftp)]
        [InlineData("FTP", ConnectTypes.Ftp)]
        [InlineData("FTPS", ConnectTypes.Ftp)]
        [InlineData("ftps", ConnectTypes.Ftp)]
        [InlineData("Kafka", ConnectTypes.Kafka)]
        [InlineData("kafka", ConnectTypes.Kafka)]
        public void Normalize_WithSupportedAliasesAndWhitespace_ReturnsCanonicalType_Test(string? input, string expected)
        {
            // Act
            var result = ConnectTypes.Normalize(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("UNKNOWN")]
        [InlineData("ABC")]
        [InlineData("TCP")]
        [InlineData("UDP")]
        public void Normalize_WithUnknownType_ReturnsNull_Test(string input)
        {
            // Act
            var result = ConnectTypes.Normalize(input);

            // Assert
            Assert.Null(result);
            Assert.False(ConnectTypes.IsSupported(input));
        }

        [Theory]
        [InlineData("API")]
        [InlineData("REST")]
        [InlineData("HTTP")]
        [InlineData("WEBAPI")]
        [InlineData("File")]
        [InlineData("DISK")]
        [InlineData("NAS")]
        [InlineData("Ftp")]
        [InlineData("FTPS")]
        [InlineData("Kafka")]
        public void IsSupported_WithValidValues_ReturnsTrue_Test(string input)
        {
            // Act
            var result = ConnectTypes.IsSupported(input);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("File", true)]
        [InlineData("DISK", true)]
        [InlineData("disk", true)]
        [InlineData("NAS", true)]
        [InlineData("nas", true)]
        [InlineData("API", false)]
        [InlineData("Kafka", false)]
        [InlineData("Ftp", false)]
        [InlineData(null, false)]
        public void IsFileChannel_VerifiesFileChannelsCorrectly_Test(string? input, bool expected)
        {
            // Act
            var result = ConnectTypes.IsFileChannel(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("FTPS", true)]
        [InlineData("ftps", true)]
        [InlineData(" FTPS ", true)]
        [InlineData("Ftp", false)]
        [InlineData("ftp", false)]
        [InlineData("API", false)]
        [InlineData(null, false)]
        public void IsFtps_VerifiesFtpsCorrectly_Test(string? input, bool expected)
        {
            // Act
            var result = ConnectTypes.IsFtps(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
