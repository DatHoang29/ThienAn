using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Module.ShareData.Controllers.Partner.Validators;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums;
using Shared.DTO.Enums;
using SqlSugar;
using Wolverine;
using Xunit;

namespace Tests.ShareData.Controllers
{
    /// <summary>
    /// Description: Kiểm thử cho ShareDataPartnerController và PartnerValidator qua Wolverine IMessageBus (Rule 19.84).
    ///              Xác nhận 6 lỗi cấu trúc ConnectionConfig bị chặn với thông báo i18n tương ứng,
    ///              khóa OutputPath vẫn lưu thành công, và kiểm tra tính hợp lệ của ConnectionType.
    /// Created date: 17/10/2026
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
        public void AddPartner_WhenConnectionConfigNotJsonObject_ValidatorRejectsWithI18n_Test(string config)
        {
            // Arrange
            var validator = new AddPartnerValidator(host.Localizer);
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = config
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigNotJsonObject]);
        }

        [Fact]
        public void AddPartner_WhenConnectionConfigSyntaxError_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var validator = new AddPartnerValidator(host.Localizer);
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{ invalid_json: 123 }"
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigSyntaxError]);
        }

        [Fact]
        public void AddPartner_WhenConnectionConfigEmptyKey_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var validator = new AddPartnerValidator(host.Localizer);
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{\"\": \"some_value\"}"
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigEmptyKey]);
        }

        [Theory]
        [InlineData("{\"BaseUrl\": \"https://example.com\"}")]
        [InlineData("{\"ip\": \"192.168.1.1\"}")]
        [InlineData("{\"Port\": 8080}")]
        [InlineData("{\"Host\": \"localhost\"}")]
        public void AddPartner_WhenConnectionConfigReservedKey_ValidatorRejectsWithI18n_Test(string json)
        {
            // Arrange
            var validator = new AddPartnerValidator(host.Localizer);
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = json
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigReservedKey]);
        }

        [Fact]
        public void AddPartner_WhenConnectionConfigDuplicateKey_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var validator = new AddPartnerValidator(host.Localizer);
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{\"TimeoutSeconds\": 30, \"timeoutseconds\": 60}"
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigDuplicateKey]);
        }

        [Fact]
        public void AddPartner_WhenConnectionConfigNestedValue_ValidatorRejectsWithI18n_Test()
        {
            // Arrange
            var validator = new AddPartnerValidator(host.Localizer);
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"P_{unique}",
                Name = $"Partner {unique}",
                Address = "127.0.0.1",
                ConnectionConfig = "{\"Options\": {\"Key\": \"Value\"}}"
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == host.Localizer[BaseMsg.ShareData.Validation.ConnectConfigNestedValue]);
        }

        [Fact]
        public async Task AddPartner_WhenConnectionConfigHasOutputPath_SavesSuccessfully_Test()
        {
            // Arrange
            var validator = new AddPartnerValidator(host.Localizer);
            var bus = host.Services.GetRequiredService<IMessageBus>();
            var db = host.Services.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"PARTNER_OP_{unique}",
                Name = $"Partner OutputPath {unique}",
                Address = "127.0.0.1",
                Port = 8080,
                ConnectionConfig = "{\"OutputPath\": \"/var/log/sharedata\"}"
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.True(result.IsValid);

            await bus.InvokeAsync(input);

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
        public void PartnerValidator_ValidConnectionType_PassesValidation_Test(ConnectionType connectionType)
        {
            // Arrange
            var validator = new PartnerValidator(new EchoLocalizer());
            var input = new ShareDataAddPartnerInput
            {
                Code = "PARTNER_01",
                Name = "Partner 01",
                Address = "127.0.0.1",
                ConnectionType = connectionType
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionType));
        }

        [Fact]
        public void PartnerValidator_NullConnectionType_PassesValidation_Test()
        {
            // Arrange
            var validator = new PartnerValidator(new EchoLocalizer());
            var input = new ShareDataAddPartnerInput
            {
                Code = "PARTNER_01",
                Name = "Partner 01",
                Address = "127.0.0.1",
                ConnectionType = null
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionType));
        }

        [Fact]
        public void PartnerValidator_InvalidConnectionType_FailsValidation_Test()
        {
            // Arrange
            var validator = new PartnerValidator(new EchoLocalizer());
            var input = new ShareDataAddPartnerInput
            {
                Code = "PARTNER_01",
                Name = "Partner 01",
                Address = "127.0.0.1",
                ConnectionType = (ConnectionType)999
            };

            // Act
            var result = validator.Validate(input);

            // Assert
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(ShareDataAddPartnerInput.ConnectionType));
        }

        #endregion

        private sealed class EchoLocalizer : IStringLocalizer
        {
            public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

            public LocalizedString this[string name, params object[] arguments] =>
                new(name, string.Format(name, arguments), resourceNotFound: false);

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        }
    }
}
