using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;
using ITS.VideoWall.Services.ISAPIDevice;
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
        /// Description: C5..C6 - Trả về thông tin đăng nhập Account và PassWord (kể cả plaintext) từ DB controller.
        /// Created date: 08/09/2026
        /// </summary>
        [Theory]
        [InlineData("user1", "pass1")]
        [InlineData("admin", "PlaintextPassword123!")]
        public void ResolveCredential_ReturnsControllerCredentials_Test(string inputAccount, string inputPassword)
        {
            // Arrange
            var options = Options.Create(new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions()
            });
            var resolver = new VwISAPICredentialResolver(options);
            var controller = new VwController
            {
                Account = inputAccount,
                PassWord = inputPassword
            };

            // Act
            var (account, password) = resolver.Resolve(controller);

            // Assert
            Assert.Equal(inputAccount, account);
            Assert.Equal(inputPassword, password);
        }
    }
}
