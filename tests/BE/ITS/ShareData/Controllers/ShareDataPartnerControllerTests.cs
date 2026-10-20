using FluentValidation;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;

namespace Tests.ShareData.Controllers
{
    /// <summary>
    /// Description: Kiểm thử cho ShareDataPartnerController và PartnerValidator qua CallApiAsync.
    ///              Xác nhận các lỗi cấu trúc và giá trị ConnectionConfig bị chặn với thông báo i18n tương ứng,
    ///              cấu hình hợp lệ lưu thành công, và kiểm tra tính hợp lệ của ConnectionType.
    /// Created date: 17/10/2026
    /// Modified date: 19/10/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataPartnerControllerTests(Host host)
    {
        #region 1. Validation qua AddPartnerValidator & Lưu qua IMessageBus

        [Theory]
        [InlineData("[1, 2, 3]")]
        [InlineData("\"plain string\"")]
        [InlineData("12345")]
        [InlineData("true")]
        public async Task AddPartner_WhenConnectionConfigNotJsonObject_ValidatorRejectsWithI18n_Test(string config)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = config
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigNotJsonObject]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigSyntaxError_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{ invalid_json: 123 }"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigSyntaxError]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigEmptyKey_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{\"\": \"some_value\"}"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigEmptyKey]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Theory]
        [InlineData("{\"BaseUrl\": \"https://example.com\"}")]
        [InlineData("{\"ip\": \"192.168.1.1\"}")]
        [InlineData("{\"Port\": 8080}")]
        [InlineData("{\"Host\": \"localhost\"}")]
        public async Task AddPartner_WhenConnectionConfigReservedKey_ValidatorRejectsWithI18n_Test(string json)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = json
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigReservedKey]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigDuplicateKey_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{\"TimeoutSeconds\": 30, \"timeoutseconds\": 60}"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigDuplicateKey]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigNestedValue_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{\"Options\": {\"Key\": \"Value\"}}"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigNestedValue]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigObsoleteKey_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_OBS_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner ObsoleteKey {unique}",
                Address = "127.0.0.1",
                Port = 8080,
                ConnectionConfig = "{\"OutputPath\": \"/var/log/sharedata\"}"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigObsoleteKey]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Theory]
        [InlineData("{\"TimeoutSeconds\": -1}")]
        [InlineData("{\"TimeoutSeconds\": 0}")]
        [InlineData("{\"TimeoutSeconds\": \"abc\"}")]
        [InlineData("{\"HttpTimeoutSec\": -5}")]
        [InlineData("{\"TimeoutSec\": 0}")]
        public async Task AddPartner_WhenConnectionConfigInvalidTimeout_ValidatorRejectsWithI18n_Test(string json)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_TO_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner InvalidTimeout {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = json
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigInvalidTimeout]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Theory]
        [InlineData("{\"BypassSsl\": \"not_a_bool\"}")]
        [InlineData("{\"IgnoreSsl\": 123}")]
        [InlineData("{\"Insecure\": \"invalid\"}")]
        public async Task AddPartner_WhenConnectionConfigInvalidBoolean_ValidatorRejectsWithI18n_Test(string json)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_BOOL_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner InvalidBoolean {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = json
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigInvalidBoolean]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigUnsupportedAuthType_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_AUTH_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner UnsupportedAuthType {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{\"AuthType\": \"UnsupportedAuthMethod\"}"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigUnsupportedAuthType]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigUnsupportedEncryptor_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_ENC_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner UnsupportedEncryptor {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{\"Encryptor\": \"Bzip2Unsupported\"}"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigUnsupportedEncryptor]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Theory]
        [InlineData("{\"FileNamePattern\": \"fixed_file_name.json\"}")]
        [InlineData("{\"FileNamePattern\": \"report_2026.csv\"}")]
        public async Task AddPartner_WhenConnectionConfigFileNamePatternNoToken_ValidatorRejectsWithI18n_Test(string json)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var code = $"P_PAT_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner FileNamePatternNoToken {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = json
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionConfig)
                                         && e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigFileNamePatternNoToken]);

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigValid_SavesSuccessfully_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"PARTNER_OK_{unique}",
                Name = $"Partner Valid Config {unique}",
                Address = "127.0.0.1",
                Port = 8080,
                ConnectionConfig = "{\"TimeoutSeconds\": 60, \"BypassSsl\": true, \"AuthType\": \"Bearer\", \"Encryptor\": \"None\", \"FileNamePattern\": \"data_{timestamp}_{serial}.json\"}"
            };

            // Act: Gọi CallApiAsync mô phỏng luồng API thật
            await sp.CallApiAsync(input);

            // Assert
            var saved = await db.Queryable<ShareDataPartner>()
                .Where(p => p.Code == input.Code)
                .FirstAsync();
            Assert.NotNull(saved);
            Assert.Equal(input.ConnectionConfig, saved.ConnectionConfig);
        }

        #endregion

        #region 2. PartnerValidator Unit Tests

        [Theory]
        [InlineData(ConnectionType.Api)]
        [InlineData(ConnectionType.File)]
        [InlineData(ConnectionType.Ftp)]
        [InlineData(ConnectionType.Ftps)]
        [InlineData(ConnectionType.Kafka)]
        public async Task PartnerValidator_ValidConnectionType_PassesValidation_Test(ConnectionType connectionType)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_VALID_TYPE_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionType = connectionType
            };

            // Act
            await sp.CallApiAsync(input);

            // Assert
            var saved = await db.Queryable<ShareDataPartner>().FirstAsync(p => p.Code == input.Code);
            Assert.NotNull(saved);
            Assert.Equal(connectionType, saved.ConnectionType);
        }

        [Fact]
        public async Task PartnerValidator_NullConnectionType_PassesValidation_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_NULL_TYPE_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionType = null
            };

            // Act
            await sp.CallApiAsync(input);

            // Assert
            var saved = await db.Queryable<ShareDataPartner>().FirstAsync(p => p.Code == input.Code);
            Assert.NotNull(saved);
            Assert.Equal(ConnectionType.Api, saved.ConnectionType);
        }

        [Fact]
        public async Task PartnerValidator_InvalidConnectionType_FailsValidation_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var code = $"P_INVALID_TYPE_{unique}";
            var input = new ShareDataAddPartnerInput
            {
                Code = code,
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionType = (ConnectionType)999
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(input));
            Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionType));

            var inDb = await db.Queryable<ShareDataPartner>().FirstAsync(x => x.Code == code);
            Assert.Null(inDb);
        }

        #endregion
    }
}

