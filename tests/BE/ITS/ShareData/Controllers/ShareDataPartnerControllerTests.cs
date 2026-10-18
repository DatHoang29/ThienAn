using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums;
using Shared.DTO.Enums;
using SqlSugar;
using Xunit;

namespace Tests.ShareData.Controllers
{
    /// <summary>
    /// Description: Kiểm thử cho ShareDataPartnerController và PartnerValidator qua CallApiAsync.
    ///              Xác nhận các lỗi cấu trúc ConnectionConfig bị chặn với thông báo i18n tương ứng,
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
        public async Task AddPartner_WhenConnectionConfigHasOutputPath_SavesSuccessfully_Test()
        {
            // Arrange
            var sp = host.Services;
            var db = sp.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..20];
            var input = new ShareDataAddPartnerInput
            {
                Code = $"PARTNER_OP_{unique}",
                Name = $"Partner OutputPath {unique}",
                Address = "127.0.0.1",
                Port = 8080,
                ConnectionConfig = "{\"OutputPath\": \"/var/log/sharedata\"}"
            };

            // Act: Gọi CallApiAsync mô phỏng luồng API thật (chạy validator DI rồi gọi bus)
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

