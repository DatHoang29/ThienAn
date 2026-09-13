using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using System.Net;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Kiểm thử đơn vị cơ chế retry có giới hạn (bounded exponential retry) của IVwISAPIDeviceClient
    /// Created date: 12/09/2026
    /// </summary>
    [Collection("api")]
    public class VwISAPIDeviceClientRetryTests(Host host)
    {
        private const string TestPrefix = "TEST_RETRY_";
        private readonly IVwISAPIDeviceClient _client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();
        private readonly VwISAPIMockServerHikvision _mock = host.MockServer;

        /// <summary>
        /// Description: Lệnh GET gặp lỗi 503 tạm thời 2 lần rồi thành công — Client tự retry đúng 2 lần và trả về kết quả thành công
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task SendCoreAsync_When503TransientErrorThenSuccess_RetriesAndSucceeds_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _client.ResetAllCircuitBreakers();
            _mock.TransientErrorCount = 2;
            _mock.TransientStatusCode = HttpStatusCode.ServiceUnavailable;

            var controller = new VwController
            {
                ID = $"{TestPrefix}CTRL_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Role = "center"
            };

            // Act
            var result = await _client.GetCapabilitiesAsync(controller);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, _mock.TransientErrorCalls);
            Assert.True(_mock.GetCapabilitiesCallCount >= 1);
        }

        /// <summary>
        /// Description: Lỗi 401 Unauthorized do sai thông tin xác thực là lỗi phi tạm thời — Client KHÔNG retry mù
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task SendCoreAsync_When401Unauthorized_DoesNotRetry_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _client.ResetAllCircuitBreakers();
            var controller = new VwController
            {
                ID = $"{TestPrefix}CTRL_UNAUTH_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = "wrong_user",
                PassWord = "wrong_password",
                Role = "center"
            };

            var startReqCount = _mock.TotalReceivedRequests;

            // Act
            var result = await _client.UserCheckAsync(controller);

            // Assert
            Assert.False(result.Success);
            // Với Digest authentication, request 1 trả 401 challenge, request 2 gửi credentials sai và nhận 401 cuối cùng (không retry thêm vòng nào)
            var attempts = _mock.TotalReceivedRequests - startReqCount;
            Assert.InRange(attempts, 1, 2);
        }

        /// <summary>
        /// Description: Lệnh POST tạo mới tài nguyên gặp lỗi 503 tạm thời KHÔNG được tự động retry để tránh nhân đôi cửa sổ trên phần cứng
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task SendCoreAsync_WhenPostMethodFails_DoesNotRetry_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _client.ResetAllCircuitBreakers();
            _mock.TransientErrorCount = 5; // Luôn lỗi
            _mock.TransientStatusCode = HttpStatusCode.ServiceUnavailable;

            var controller = new VwController
            {
                ID = $"{TestPrefix}CTRL_POST_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Role = "center"
            };

            var startReqCount = _mock.TotalReceivedRequests;

            // Act: Gọi POST AddWindow
            var req = new ITS.VideoWall.Core.Dto.ISAPI.VwISAPIWindowRequest
            {
                Id = 999,
                Rect = new Module.VideoWall.Core.Dto.ISAPI.VwISAPIRect
                {
                    Coordinate = new Module.VideoWall.Core.Dto.ISAPI.VwISAPICoordinate { X = 0, Y = 0 },
                    Width = 1920,
                    Height = 1080
                }
            };
            var result = await _client.AddWindowAsync(controller, req, wallNo: 1);

            // Assert
            Assert.False(result.Success);
            // POST method: IsMethodRetriable trả về false -> maxAttempts = 1, chỉ gọi đúng 1 lần
            var attempts = _mock.TotalReceivedRequests - startReqCount;
            Assert.Equal(1, attempts);
        }

        /// <summary>
        /// Description: Controller A bị Circuit Breaker ngắt mạch không gây ảnh hưởng hay làm nghẽn Controller B
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task SendCoreAsync_CircuitBreakerOfControllerA_DoesNotAffectControllerB_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _client.ResetAllCircuitBreakers();

            var portA = VwISAPIMockServerHikvision.DefaultPorts[2];
            var portB = VwISAPIMockServerHikvision.DefaultPorts[3];

            var ctrlA = new VwController
            {
                ID = $"{TestPrefix}CTRL_CB_A_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{portA}",
                Account = "wrong_user",
                PassWord = "wrong_password",
                Role = "center"
            };
            var ctrlB = new VwController
            {
                ID = $"{TestPrefix}CTRL_CB_B_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{portB}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Role = "center"
            };

            // Gọi lặp trên ctrlA với sai thông tin xác thực để kích hoạt Circuit Breaker (lỗi 401 tích luỹ)
            for (var i = 0; i < 3; i++)
            {
                await _client.GetCapabilitiesAsync(ctrlA);
            }

            // Assert: ctrlA bị ngắt mạch
            Assert.True(_client.IsCircuitBreakerBlocked(ctrlA.IP, out _));

            // Act: ctrlB vẫn gọi bình thường
            var resultB = await _client.GetCapabilitiesAsync(ctrlB);

            // Assert: ctrlB không bị ảnh hưởng và gọi thành công
            Assert.True(resultB.Success);
            Assert.False(_client.IsCircuitBreakerBlocked(ctrlB.IP, out _));

            // Cleanup
            _client.ResetAllCircuitBreakers();
        }
    }
}
