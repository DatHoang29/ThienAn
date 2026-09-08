using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure.Services.ISAPIDevice;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Author: Đạt
    /// Description: Bộ kiểm thử đơn vị cho VwISAPICredentialResolver — nguồn tài khoản Digest
    /// Created date: 08/09/2026
    /// </summary>
    public class VwISAPICredentialResolverTests
    {
        private static IOptions<VwDeviceOptions> CreateOptions(bool useMockDevice, string? account, string? password)
        {
            var options = new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions
                {
                    UseMockDevice = useMockDevice,
                    Account = account,
                    Password = password
                }
            };
            return Options.Create(options);
        }

        [Fact]
        public void Resolve_WhenUseMockDeviceIsTrueAndConfigHasAccount_ReturnsConfigCredentialsIgnoringController()
        {
            // Arrange
            var options = CreateOptions(useMockDevice: true, account: "config_admin", password: "config_password");
            var resolver = new VwISAPICredentialResolver(options);
            var controller = new VwController
            {
                Account = "db_operator",
                PassWord = "db_password"
            };

            // Act
            var (account, password) = resolver.Resolve(controller);

            // Assert
            Assert.Equal("config_admin", account);
            Assert.Equal("config_password", password);
        }

        [Fact]
        public void Resolve_WhenUseMockDeviceIsFalseAndControllerHasCredentials_ReturnsControllerCredentials()
        {
            // Arrange
            var options = CreateOptions(useMockDevice: false, account: "config_admin", password: "config_password");
            var resolver = new VwISAPICredentialResolver(options);
            var controller = new VwController
            {
                Account = "db_operator",
                PassWord = "db_password"
            };

            // Act
            var (account, password) = resolver.Resolve(controller);

            // Assert
            Assert.Equal("db_operator", account);
            Assert.Equal("db_password", password);
        }

        [Fact]
        public void Resolve_WhenUseMockDeviceIsFalseAndControllerAccountIsEmpty_FallsBackToConfig()
        {
            // Arrange
            var options = CreateOptions(useMockDevice: false, account: "config_admin", password: "config_password");
            var resolver = new VwISAPICredentialResolver(options);
            var controller = new VwController
            {
                Account = string.Empty,
                PassWord = string.Empty
            };

            // Act
            var (account, password) = resolver.Resolve(controller);

            // Assert
            Assert.Equal("config_admin", account);
            Assert.Equal("config_password", password);
        }

        [Fact]
        public void Resolve_WhenControllerIsNull_ReturnsConfigWithoutThrowing()
        {
            // Arrange
            var options = CreateOptions(useMockDevice: false, account: "config_admin", password: "config_password");
            var resolver = new VwISAPICredentialResolver(options);

            // Act
            var (account, password) = resolver.Resolve(null);

            // Assert
            Assert.Equal("config_admin", account);
            Assert.Equal("config_password", password);
        }

        [Fact]
        public void HasCredential_WhenAccountOrPasswordIsEmpty_ReturnsFalse()
        {
            // Arrange
            var emptyOptions = CreateOptions(useMockDevice: false, account: string.Empty, password: string.Empty);
            var resolver = new VwISAPICredentialResolver(emptyOptions);
            var emptyController = new VwController { Account = string.Empty, PassWord = string.Empty };

            // Act & Assert
            Assert.False(resolver.HasCredential(emptyController));
            Assert.False(resolver.HasCredential(null));

            var validOptions = CreateOptions(useMockDevice: false, account: "admin", password: "123");
            var validResolver = new VwISAPICredentialResolver(validOptions);
            Assert.True(validResolver.HasCredential(null));
        }
    }
}
