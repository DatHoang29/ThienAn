using Modules.Integration.ShareData.Base.Constants;
using Modules.Integration.ShareData.Base.Helpers;
using Xunit;

namespace Tests.ShareData.Connect
{
    /// <summary>
    /// Description: Bộ kiểm thử đơn vị cho bộ đọc cấu hình dùng chung ConnectConfigReader.
    ///              Kiểm chứng thứ tự ưu tiên alias, chuỗi rỗng/GetRawString, parse lỗi trả mặc định, và không phân biệt hoa thường.
    /// Created date: 15/10/2026
    /// </summary>
    public class ConnectConfigReaderTests
    {
        [Fact]
        public void GetString_CaseInsensitive_AndTrimsWhitespace_Test()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["timeoutseconds"] = "  45  "
            };
            var reader = new ConnectConfigReader(dict);

            // Act
            var val = reader.GetString("TimeoutSeconds");

            // Assert
            Assert.Equal("45", val);
        }

        [Fact]
        public void GetString_PrioritizesMainKeyOverAliases_Test()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["TimeoutSeconds"] = "60",
                ["HttpTimeoutSec"] = "30",
                ["TimeoutSec"] = "15"
            };
            var reader = new ConnectConfigReader(dict);

            // Act
            var val = reader.GetString(
                ConnectKeys.TimeoutSeconds,
                ConnectKeys.TimeoutSecondsAliases);

            // Assert
            Assert.Equal("60", val);
        }

        [Fact]
        public void GetString_WhenMainKeyEmpty_FallsBackToFirstValidAlias_Test()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["TimeoutSeconds"] = "   ",
                ["HttpTimeoutSec"] = "25",
                ["TimeoutSec"] = "10"
            };
            var reader = new ConnectConfigReader(dict);

            // Act
            var val = reader.GetString(
                ConnectKeys.TimeoutSeconds,
                ConnectKeys.TimeoutSecondsAliases);

            // Assert
            Assert.Equal("25", val);
        }

        [Fact]
        public void GetString_WhenAllKeysEmptyOrAbsent_ReturnsNull_Test()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["TimeoutSeconds"] = "   ",
                ["HttpTimeoutSec"] = ""
            };
            var reader = new ConnectConfigReader(dict);

            // Act
            var val = reader.GetString(
                ConnectKeys.TimeoutSeconds,
                ConnectKeys.TimeoutSecondsAliases);

            // Assert
            Assert.Null(val);
        }

        [Fact]
        public void GetRawString_PreservesEmptyOrWhitespace_WithoutTrimByDefault_Test()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Password"] = "  secret_pass  "
            };
            var reader = new ConnectConfigReader(dict);

            // Act
            var raw = reader.GetRawString(ConnectKeys.Password, defaultValue: "default", trim: false);
            var trimmed = reader.GetRawString(ConnectKeys.Password, defaultValue: "default", trim: true);

            // Assert
            Assert.Equal("  secret_pass  ", raw);
            Assert.Equal("secret_pass", trimmed);
        }

        [Fact]
        public void GetRawString_WhenKeyExistsWithEmptyString_ReturnsEmptyNotDefault_Test()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Username"] = ""
            };
            var reader = new ConnectConfigReader(dict);

            // Act
            var val = reader.GetRawString(ConnectKeys.Username, defaultValue: "fallback");

            // Assert
            Assert.Equal("", val);
        }

        [Fact]
        public void GetRawString_WhenKeyAbsent_ReturnsDefaultValue_Test()
        {
            // Arrange
            var dict = new Dictionary<string, string>();
            var reader = new ConnectConfigReader(dict);

            // Act
            var val = reader.GetRawString("NonExistentKey", defaultValue: "my_default");

            // Assert
            Assert.Equal("my_default", val);
        }

        [Theory]
        [InlineData("100", 0, 100)]
        [InlineData("invalid_number", 42, 42)]
        [InlineData(null, 50, 50)]
        public void GetInt_ParsesOrReturnsDefaultOnFailure_Test(string? configuredValue, int defaultValue, int expected)
        {
            // Arrange
            var dict = new Dictionary<string, string>();
            if (configuredValue != null)
            {
                dict["TimeoutSeconds"] = configuredValue;
            }
            var reader = new ConnectConfigReader(dict);

            // Act
            var result = reader.GetInt(ConnectKeys.TimeoutSeconds, defaultValue);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("true", false, true)]
        [InlineData("FALSE", true, false)]
        [InlineData("not_a_bool", true, true)]
        [InlineData(null, false, false)]
        public void GetBool_ParsesOrReturnsDefaultOnFailure_Test(string? configuredValue, bool defaultValue, bool expected)
        {
            // Arrange
            var dict = new Dictionary<string, string>();
            if (configuredValue != null)
            {
                dict["BypassSsl"] = configuredValue;
            }
            var reader = new ConnectConfigReader(dict);

            // Act
            var result = reader.GetBool(ConnectKeys.BypassSsl, defaultValue);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetOptionalEnum_ParsesOrReturnsNullOnFailure_Test()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Proto1"] = "SaslPlaintext",
                ["Proto2"] = "invalid_protocol"
            };
            var reader = new ConnectConfigReader(dict);

            // Act
            var val1 = reader.GetOptionalEnum<Confluent.Kafka.SecurityProtocol>("Proto1");
            var val2 = reader.GetOptionalEnum<Confluent.Kafka.SecurityProtocol>("Proto2");
            var val3 = reader.GetOptionalEnum<Confluent.Kafka.SecurityProtocol>("Proto3");

            // Assert
            Assert.Equal(Confluent.Kafka.SecurityProtocol.SaslPlaintext, val1);
            Assert.Null(val2);
            Assert.Null(val3);
        }
    }
}
