using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums;
using Modules.Integration.Delivery.Base.Helpers;
using Xunit;

namespace Tests.ShareData.Connect
{
    /// <summary>
    /// Description: Bộ kiểm thử đơn vị cho ConnectConfigValidator.
    ///              Kiểm chứng 12 trường hợp mã lỗi ConnectConfigIssue và tính hợp lệ của cấu hình.
    /// Created date: 16/10/2026
    /// </summary>
    public class ConnectConfigValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Check_NullOrWhitespace_ReturnsNone_Test(string? config)
        {
            // Act
            var issue = ConnectConfigValidator.Check(config, out var detail);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
            Assert.Null(detail);
        }

        [Theory]
        [InlineData("[1, 2, 3]")]
        [InlineData("\"plain string\"")]
        [InlineData("12345")]
        public void Check_NotJsonObject_ReturnsNotJsonObject_Test(string config)
        {
            // Act
            var issue = ConnectConfigValidator.Check(config, out _);

            // Assert
            Assert.Equal(ConnectConfigIssue.NotJsonObject, issue);
        }

        [Fact]
        public void Check_SyntaxError_ReturnsSyntaxError_Test()
        {
            // Arrange
            var invalidJson = "{ invalid_json: 123 }";

            // Act
            var issue = ConnectConfigValidator.Check(invalidJson, out var detail);

            // Assert
            Assert.Equal(ConnectConfigIssue.SyntaxError, issue);
            Assert.NotNull(detail);
        }

        [Fact]
        public void Check_EmptyKey_ReturnsEmptyKey_Test()
        {
            // Arrange
            var json = "{\"\": \"some_value\"}";

            // Act
            var issue = ConnectConfigValidator.Check(json, out _);

            // Assert
            Assert.Equal(ConnectConfigIssue.EmptyKey, issue);
        }

        [Theory]
        [InlineData("{\"BaseUrl\": \"https://example.com\"}")]
        [InlineData("{\"ip\": \"192.168.1.1\"}")]
        [InlineData("{\"Port\": 8080}")]
        [InlineData("{\"Host\": \"localhost\"}")]
        public void Check_ReservedKey_ReturnsReservedKey_Test(string json)
        {
            // Act
            var issue = ConnectConfigValidator.Check(json, out var detail);

            // Assert
            Assert.Equal(ConnectConfigIssue.ReservedKey, issue);
            Assert.NotNull(detail);
        }

        [Fact]
        public void Check_ObsoleteKey_ReturnsObsoleteKey_Test()
        {
            // Arrange
            var json = "{\"OutputPath\": \"/var/log/sharedata\"}";

            // Act
            var issue = ConnectConfigValidator.Check(json, out var detail);

            // Assert
            Assert.Equal(ConnectConfigIssue.ObsoleteKey, issue);
            Assert.Equal("OutputPath", detail);
        }

        [Fact]
        public void Check_DuplicateKey_ReturnsDuplicateKey_Test()
        {
            // Arrange (Case-insensitive duplication)
            var json = "{\"TimeoutSeconds\": 30, \"timeoutseconds\": 60}";

            // Act
            var issue = ConnectConfigValidator.Check(json, out var detail);

            // Assert
            Assert.Equal(ConnectConfigIssue.DuplicateKey, issue);
            Assert.Equal("timeoutseconds", detail);
        }

        [Fact]
        public void Check_NestedObjectOrArray_ReturnsNestedValue_Test()
        {
            // Arrange
            var jsonObject = "{\"Options\": {\"Key\": \"Value\"}}";
            var jsonArray = "{\"Items\": [1, 2, 3]}";

            // Act & Assert
            Assert.Equal(ConnectConfigIssue.NestedValue, ConnectConfigValidator.Check(jsonObject, out _));
            Assert.Equal(ConnectConfigIssue.NestedValue, ConnectConfigValidator.Check(jsonArray, out _));
        }

        [Theory]
        [InlineData("{\"TimeoutSeconds\": -1}")]
        [InlineData("{\"TimeoutSeconds\": 0}")]
        [InlineData("{\"TimeoutSeconds\": \"abc\"}")]
        [InlineData("{\"HttpTimeoutSec\": -5}")]
        [InlineData("{\"TimeoutSec\": 0}")]
        public void Check_InvalidTimeout_ReturnsInvalidTimeout_Test(string json)
        {
            // Act
            var issue = ConnectConfigValidator.Check(json, out _);

            // Assert
            Assert.Equal(ConnectConfigIssue.InvalidTimeout, issue);
        }

        [Theory]
        [InlineData("{\"BypassSsl\": \"not_a_bool\"}")]
        [InlineData("{\"IgnoreSsl\": 123}")]
        [InlineData("{\"Insecure\": \"invalid\"}")]
        public void Check_InvalidBoolean_ReturnsInvalidBoolean_Test(string json)
        {
            // Act
            var issue = ConnectConfigValidator.Check(json, out _);

            // Assert
            Assert.Equal(ConnectConfigIssue.InvalidBoolean, issue);
        }

        [Fact]
        public void Check_UnsupportedAuthType_ReturnsUnsupportedAuthType_Test()
        {
            // Arrange
            var json = "{\"AuthType\": \"UnsupportedAuthMethod\"}";

            // Act
            var issue = ConnectConfigValidator.Check(json, out var detail);

            // Assert
            Assert.Equal(ConnectConfigIssue.UnsupportedAuthType, issue);
            Assert.Equal("UnsupportedAuthMethod", detail);
        }

        [Fact]
        public void Check_UnsupportedEncryptor_ReturnsUnsupportedEncryptor_Test()
        {
            // Arrange
            var json = $"{{\"{ConnectKeys.Encryptor}\": \"Bzip2Unsupported\"}}";

            // Act
            var issue = ConnectConfigValidator.Check(json, out _);

            // Assert
            Assert.Equal(ConnectConfigIssue.UnsupportedEncryptor, issue);
        }

        [Theory]
        [InlineData("{\"FileNamePattern\": \"fixed_file_name.json\"}")]
        [InlineData("{\"FileNamePattern\": \"report_2026.csv\"}")]
        public void Check_FileNamePatternNoToken_ReturnsFileNamePatternNoToken_Test(string json)
        {
            // Act
            var issue = ConnectConfigValidator.Check(json, out _);

            // Assert
            Assert.Equal(ConnectConfigIssue.FileNamePatternNoToken, issue);
        }

        [Fact]
        public void Check_ValidConfig_ReturnsNone_Test()
        {
            // Arrange
            var validJson = $@"
            {{
                ""TimeoutSeconds"": 60,
                ""BypassSsl"": true,
                ""AuthType"": ""Bearer"",
                ""{ConnectKeys.Encryptor}"": ""None"",
                ""FileNamePattern"": ""data_{{timestamp}}_{{serial}}.json"",
                ""Username"": ""admin""
            }}";

            // Act
            var issue = ConnectConfigValidator.Check(validJson, out var detail);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
            Assert.Null(detail);
        }

        #region CheckStructure Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CheckStructure_NullOrWhitespace_ReturnsNone_Test(string? config)
        {
            // Act
            var issue = ConnectConfigValidator.CheckStructure(config);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Theory]
        [InlineData("[1, 2, 3]")]
        [InlineData("\"plain string\"")]
        [InlineData("12345")]
        public void CheckStructure_NotJsonObject_ReturnsNotJsonObject_Test(string config)
        {
            // Act
            var issue = ConnectConfigValidator.CheckStructure(config);

            // Assert
            Assert.Equal(ConnectConfigIssue.NotJsonObject, issue);
        }

        [Fact]
        public void CheckStructure_SyntaxError_ReturnsSyntaxError_Test()
        {
            // Arrange
            var invalidJson = "{ invalid_json: 123 }";

            // Act
            var issue = ConnectConfigValidator.CheckStructure(invalidJson);

            // Assert
            Assert.Equal(ConnectConfigIssue.SyntaxError, issue);
        }

        [Fact]
        public void CheckStructure_EmptyKey_ReturnsEmptyKey_Test()
        {
            // Arrange
            var json = "{\"\": \"some_value\"}";

            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.EmptyKey, issue);
        }

        [Theory]
        [InlineData("{\"BaseUrl\": \"https://example.com\"}")]
        [InlineData("{\"ip\": \"192.168.1.1\"}")]
        [InlineData("{\"Port\": 8080}")]
        [InlineData("{\"Host\": \"localhost\"}")]
        public void CheckStructure_ReservedColumnKey_ReturnsReservedKey_Test(string json)
        {
            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.ReservedKey, issue);
        }

        [Fact]
        public void CheckStructure_DuplicateKeyCaseInsensitive_ReturnsDuplicateKey_Test()
        {
            // Arrange
            var json = "{\"TimeoutSeconds\": 30, \"timeoutseconds\": 60}";

            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.DuplicateKey, issue);
        }

        [Fact]
        public void CheckStructure_NestedObjectOrArray_ReturnsNestedValue_Test()
        {
            // Arrange
            var jsonObject = "{\"Options\": {\"Key\": \"Value\"}}";
            var jsonArray = "{\"Items\": [1, 2, 3]}";

            // Act & Assert
            Assert.Equal(ConnectConfigIssue.NestedValue, ConnectConfigValidator.CheckStructure(jsonObject));
            Assert.Equal(ConnectConfigIssue.NestedValue, ConnectConfigValidator.CheckStructure(jsonArray));
        }

        [Theory]
        [InlineData("{\"TimeoutSeconds\": -1}")]
        [InlineData("{\"TimeoutSeconds\": 0}")]
        [InlineData("{\"TimeoutSeconds\": \"abc\"}")]
        [InlineData("{\"HttpTimeoutSec\": -5}")]
        [InlineData("{\"TimeoutSec\": 0}")]
        public void CheckStructure_InvalidTimeoutValue_DoesNotRejectAndReturnsNone_Test(string json)
        {
            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Theory]
        [InlineData("{\"BypassSsl\": \"not_a_bool\"}")]
        [InlineData("{\"IgnoreSsl\": 123}")]
        [InlineData("{\"Insecure\": \"invalid\"}")]
        public void CheckStructure_InvalidBooleanValue_DoesNotRejectAndReturnsNone_Test(string json)
        {
            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Fact]
        public void CheckStructure_UnsupportedAuthType_DoesNotRejectAndReturnsNone_Test()
        {
            // Arrange
            var json = "{\"AuthType\": \"UnsupportedAuthMethod\"}";

            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Fact]
        public void CheckStructure_UnsupportedEncryptor_DoesNotRejectAndReturnsNone_Test()
        {
            // Arrange
            var json = $"{{\"{ConnectKeys.Encryptor}\": \"Bzip2Unsupported\"}}";

            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Theory]
        [InlineData("{\"FileNamePattern\": \"fixed_file_name.json\"}")]
        [InlineData("{\"FileNamePattern\": \"report_2026.csv\"}")]
        public void CheckStructure_FileNamePatternWithoutTokens_DoesNotRejectAndReturnsNone_Test(string json)
        {
            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Fact]
        public void CheckStructure_ObsoleteOutputPath_DoesNotRejectAndReturnsNone_Test()
        {
            // Arrange
            var json = "{\"OutputPath\": \"/var/log/sharedata\"}";

            // Act
            var issue = ConnectConfigValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Fact]
        public void CheckStructure_ValidFlatJsonObject_ReturnsNone_Test()
        {
            // Arrange
            var validJson = $@"
            {{
                ""TimeoutSeconds"": 60,
                ""BypassSsl"": true,
                ""AuthType"": ""Bearer"",
                ""{ConnectKeys.Encryptor}"": ""None"",
                ""FileNamePattern"": ""data_{{timestamp}}_{{serial}}.json"",
                ""Username"": ""admin""
            }}";

            // Act
            var issue = ConnectConfigValidator.CheckStructure(validJson);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        #endregion

    }
}
