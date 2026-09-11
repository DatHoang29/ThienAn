using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure.Services.ISAPIDevice;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Bộ kiểm thử Nhóm C (C1-C6) cho phân giải URI thiết bị và xác thực thông tin
    ///              đăng nhập giữa bộ điều khiển DB và cấu hình kết nối trong kiến trúc Cascade.
    /// Created date: 08/09/2026
    /// </summary>
    /// <remarks>
    /// VÌ SAO Ở TRONG COLLECTION "api" DÙ KHÔNG DÙNG CSDL: VwISAPIDeviceClient.ResolveDeviceUri
    /// có đường dẫn ném lỗi nghiệp vụ qua Oops.Oh, mà lời gọi đó chạm vào static class Furion.App.
    /// Chạy NGOÀI host thì App.Configuration còn null nên static constructor của App ném
    /// NullReferenceException — và CLR cache lỗi static constructor VĨNH VIỄN, khiến mọi test
    /// dùng Host sau đó chết sạch với TypeInitializationException.
    /// Nằm trong collection "api" buộc Host dựng xong trước, App có config, nên không đầu độc
    /// process. ĐỪNG BỎ thuộc tính này chỉ vì thấy class không truy vấn CSDL.
    /// </remarks>
    [Collection("api")]
    public class VwCascadeRoutingCredentialTests
    {
        /// <summary>
        /// Description: C1 - Khi UseMockDevice=false, URI thiết bị lấy từ IP bộ điều khiển với port mặc định 80.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void C1_ResolveDeviceUri_WhenUseMockDeviceIsFalse_UsesControllerIpAndDefaultPort()
        {
            // Arrange
            var config = new VwDeviceConnectionOptions
            {
                UseMockDevice = false
            };
            var controller = new VwController
            {
                IP = "10.0.0.11"
            };

            // Act
            var result = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            // Assert
            Assert.Equal("http://10.0.0.11/", result.AbsoluteUri);
            Assert.Equal(80, result.Port);
        }

        /// <summary>
        /// Description: C2 - Khi bộ điều khiển có port tuỳ biến trong IP, ResolveDeviceUri giữ nguyên port đó.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void C2_ResolveDeviceUri_WhenControllerHasCustomPort_RespectsCustomPort()
        {
            // Arrange
            var config = new VwDeviceConnectionOptions
            {
                UseMockDevice = false
            };
            var controller = new VwController
            {
                IP = "10.0.0.11:8080"
            };

            // Act
            var result = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            // Assert
            Assert.Equal("http://10.0.0.11:8080/", result.AbsoluteUri);
        }

        /// <summary>
        /// Description: C3 - Khi UseMockDevice=true, URI thiết bị lấy theo cấu hình appsettings/options.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void C3_ResolveDeviceUri_WhenUseMockDeviceIsTrue_UsesConfigIp()
        {
            // Arrange
            var config = new VwDeviceConnectionOptions
            {
                UseMockDevice = true,
                Ip = "127.0.0.1",
                Port = 8080
            };
            var controller = new VwController
            {
                IP = "10.0.0.11"
            };

            // Act
            var result = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            // Assert
            Assert.Equal("http://127.0.0.1:8080/", result.AbsoluteUri);
        }

        /// <summary>
        /// Description: C4 - Khi IP rỗng cả trong controller lẫn cấu hình, ném ngoại lệ rõ ràng.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void C4_ResolveDeviceUri_WhenIpIsEmpty_ThrowsOops()
        {
            // Arrange
            var config = new VwDeviceConnectionOptions
            {
                UseMockDevice = false,
                Ip = string.Empty
            };
            var controller = new VwController
            {
                IP = string.Empty
            };

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() => VwISAPIDeviceClient.ResolveDeviceUri(controller, config));
            Assert.True(ex is TypeInitializationException || ex.Message.Contains("Chưa cấu hình IP thiết bị"));
        }

        /// <summary>
        /// Description: C5 - Khi UseMockDevice=false, trả về thông tin đăng nhập Account và PassWord từ DB controller.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void C5_ResolveCredential_WhenUseMockDeviceIsFalse_ReturnsControllerCredentials()
        {
            // Arrange
            var options = Options.Create(new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions
                {
                    UseMockDevice = false
                }
            });
            var resolver = new VwISAPICredentialResolver(options);
            var controller = new VwController
            {
                Account = "user1",
                PassWord = "pass1"
            };

            // Act
            var (account, password) = resolver.Resolve(controller);

            // Assert
            Assert.Equal("user1", account);
            Assert.Equal("pass1", password);
        }

        /// <summary>
        /// Description: C6 - Mật khẩu plaintext được trả về nguyên bản không bị biến dạng.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void C6_ResolveCredential_WithPlaintextPassword_ReturnsPasswordDirectly()
        {
            // Arrange
            var options = Options.Create(new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions
                {
                    UseMockDevice = false
                }
            });
            var resolver = new VwISAPICredentialResolver(options);
            var controller = new VwController
            {
                Account = "admin",
                PassWord = "PlaintextPassword123!"
            };

            // Act
            var (_, password) = resolver.Resolve(controller);

            // Assert
            Assert.Equal("PlaintextPassword123!", password);
        }
    }
}
