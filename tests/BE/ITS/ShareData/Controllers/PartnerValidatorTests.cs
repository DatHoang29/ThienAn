using Module.ShareData.Controllers.Partner.Validators;
using Modules.Integration.ShareData;
using Xunit;

namespace Tests.ShareData.Controllers
{
    /// <summary>
    /// Description: Kiểm thử đơn vị cho PartnerValidator và hàm kiểm tra cấu trúc cấu hình kết nối CheckStructure.
    ///              Xác nhận 6 lỗi cấu trúc bị chặn và các lỗi giá trị cấu hình không còn bị chặn khi lưu (P13-BE).
    /// Created date: 17/10/2026
    /// </summary>
    public class PartnerValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CheckStructure_NullOrWhitespace_ReturnsNone_Test(string? config)
        {
            // Act
            var issue = PartnerValidator.CheckStructure(config);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Theory]
        [InlineData("[1, 2, 3]")]
        [InlineData("\"plain string\"")]
        [InlineData("12345")]
        [InlineData("true")]
        public void CheckStructure_NotJsonObject_ReturnsNotJsonObject_Test(string config)
        {
            // Act
            var issue = PartnerValidator.CheckStructure(config);

            // Assert
            Assert.Equal(ConnectConfigIssue.NotJsonObject, issue);
        }

        [Fact]
        public void CheckStructure_SyntaxError_ReturnsSyntaxError_Test()
        {
            // Arrange
            var invalidJson = "{ invalid_json: 123 }";

            // Act
            var issue = PartnerValidator.CheckStructure(invalidJson);

            // Assert
            Assert.Equal(ConnectConfigIssue.SyntaxError, issue);
        }

        [Fact]
        public void CheckStructure_EmptyKey_ReturnsEmptyKey_Test()
        {
            // Arrange
            var json = "{\"\": \"some_value\"}";

            // Act
            var issue = PartnerValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.EmptyKey, issue);
        }

        [Theory]
        [InlineData("{\"BaseUrl\": \"https://example.com\"}")]
        [InlineData("{\"Url\": \"https://example.com\"}")]
        [InlineData("{\"Host\": \"localhost\"}")]
        [InlineData("{\"Server\": \"10.0.0.1\"}")]
        [InlineData("{\"Address\": \"192.168.1.1\"}")]
        [InlineData("{\"IP\": \"127.0.0.1\"}")]
        [InlineData("{\"Port\": 8080}")]
        [InlineData("{\"Endpoint\": \"/api\"}")]
        [InlineData("{\"Path\": \"/data\"}")]
        [InlineData("{\"EndPointApiUrl\": \"/api/v1\"}")]
        public void CheckStructure_ReservedColumnKey_ReturnsReservedKey_Test(string json)
        {
            // Act
            var issue = PartnerValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.ReservedKey, issue);
        }

        [Fact]
        public void CheckStructure_DuplicateKeyCaseInsensitive_ReturnsDuplicateKey_Test()
        {
            // Arrange
            var json = "{\"TimeoutSeconds\": 30, \"timeoutseconds\": 60}";

            // Act
            var issue = PartnerValidator.CheckStructure(json);

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
            Assert.Equal(ConnectConfigIssue.NestedValue, PartnerValidator.CheckStructure(jsonObject));
            Assert.Equal(ConnectConfigIssue.NestedValue, PartnerValidator.CheckStructure(jsonArray));
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
            var issue = PartnerValidator.CheckStructure(json);

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
            var issue = PartnerValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Fact]
        public void CheckStructure_UnsupportedAuthType_DoesNotRejectAndReturnsNone_Test()
        {
            // Arrange
            var json = "{\"AuthType\": \"UnsupportedAuthMethod\"}";

            // Act
            var issue = PartnerValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Fact]
        public void CheckStructure_UnsupportedEncryptor_DoesNotRejectAndReturnsNone_Test()
        {
            // Arrange
            var json = $"{{\"{ConnectKeys.Encryptor}\": \"Bzip2Unsupported\"}}";

            // Act
            var issue = PartnerValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Theory]
        [InlineData("{\"FileNamePattern\": \"fixed_file_name.json\"}")]
        [InlineData("{\"FileNamePattern\": \"report_2026.csv\"}")]
        public void CheckStructure_FileNamePatternWithoutTokens_DoesNotRejectAndReturnsNone_Test(string json)
        {
            // Act
            var issue = PartnerValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Fact]
        public void CheckStructure_ObsoleteOutputPath_DoesNotRejectAndReturnsNone_Test()
        {
            // Arrange
            var json = "{\"OutputPath\": \"/var/log/sharedata\"}";

            // Act
            var issue = PartnerValidator.CheckStructure(json);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }

        [Fact]
        public void CheckStructure_ValidFlatJsonObject_ReturnsNone_Test()
        {
            // Arrange
            var validJson = """
            {
                "TimeoutSeconds": 60,
                "BypassSsl": true,
                "AuthType": "Bearer",
                "Encryptor": "None",
                "FileNamePattern": "data_{timestamp}_{serial}.json",
                "Username": "admin"
            }
            """;

            // Act
            var issue = PartnerValidator.CheckStructure(validJson);

            // Assert
            Assert.Equal(ConnectConfigIssue.None, issue);
        }
    }
}
