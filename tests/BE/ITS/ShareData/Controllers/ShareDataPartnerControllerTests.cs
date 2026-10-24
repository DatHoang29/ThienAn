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
    /// Modified date: 24/10/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataPartnerControllerTests(Host host)
    {
        #region 1. Validation qua AddPartnerValidator & Lưu qua IMessageBus

        /// <summary>
        /// Description: Kiểm chứng các trường hợp ConnectionConfig không hợp lệ bị validator chặn
        ///              với mã thông báo i18n tương ứng và không lưu bản ghi vào CSDL.
        /// Created date: 17/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData("[1, 2, 3]", BaseMsg.ShareData.Validation.ConnectConfigNotJsonObject)]
        [InlineData("{ invalid_json: 123 }", BaseMsg.ShareData.Validation.ConnectConfigSyntaxError)]
        [InlineData("{\"\": \"some_value\"}", BaseMsg.ShareData.Validation.ConnectConfigEmptyKey)]
        [InlineData("{\"BaseUrl\": \"https://example.com\"}", BaseMsg.ShareData.Validation.ConnectConfigReservedKey)]
        [InlineData("{\"TimeoutSeconds\": 30, \"timeoutseconds\": 60}", BaseMsg.ShareData.Validation.ConnectConfigDuplicateKey)]
        [InlineData("{\"Options\": {\"Key\": \"Value\"}}", BaseMsg.ShareData.Validation.ConnectConfigNestedValue)]
        [InlineData("{\"OutputPath\": \"/var/log/sharedata\"}", BaseMsg.ShareData.Validation.ConnectConfigObsoleteKey)]
        [InlineData("{\"TimeoutSeconds\": -1}", BaseMsg.ShareData.Validation.ConnectConfigInvalidTimeout)]
        [InlineData("{\"BypassSsl\": \"not_a_bool\"}", BaseMsg.ShareData.Validation.ConnectConfigInvalidBoolean)]
        [InlineData("{\"AuthType\": \"UnsupportedAuthMethod\"}", BaseMsg.ShareData.Validation.ConnectConfigUnsupportedAuthType)]
        [InlineData("{\"Encryptor\": \"Bzip2Unsupported\"}", BaseMsg.ShareData.Validation.ConnectConfigUnsupportedEncryptor)]
        [InlineData("{\"FileNamePattern\": \"fixed_file_name.json\"}", BaseMsg.ShareData.Validation.ConnectConfigFileNamePatternNoToken)]
        public async Task AddPartner_WhenConnectionConfigInvalid_ValidatorRejectsWithI18n_Test(string config, string expectedErrorKey)
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
                                         && e.ErrorMessage == host.Localizer[expectedErrorKey]);

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

        /// <summary>
        /// Description: Kiểm chứng ConnectionType hợp lệ (Api, Ftp) hoặc null (mặc định Api) lưu thành công vào CSDL.
        /// Created date: 17/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData(ConnectionType.Api, ConnectionType.Api)]
        [InlineData(ConnectionType.Ftp, ConnectionType.Ftp)]
        [InlineData(null, ConnectionType.Api)]
        public async Task AddPartner_WhenConnectionTypeValidOrNull_SavesExpectedType_Test(ConnectionType? inputType, ConnectionType expectedType)
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_TYPE_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionType = inputType
            };

            // Act
            await sp.CallApiAsync(input);

            // Assert
            var saved = await db.Queryable<ShareDataPartner>().FirstAsync(p => p.Code == input.Code);
            Assert.NotNull(saved);
            Assert.Equal(expectedType, saved.ConnectionType);
        }

        /// <summary>
        /// Description: Kiểm chứng ConnectionType không hợp lệ (ngoài enum) bị validator từ chối.
        /// Created date: 17/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Fact]
        public async Task AddPartner_WhenConnectionTypeInvalid_ValidatorRejects_Test()
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

