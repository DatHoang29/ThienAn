#if HAS_SHAREDATAWORKER
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Senders;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using Shared.DTO.Enums;

namespace Tests.RealServer
{
    /// <summary>
    /// Description: Bộ kiểm thử kết nối trực tiếp tới máy chủ đối tác thật (115.78.1.139 / tacorp.vn:12204).
    ///              Độc lập hoàn toàn với luồng nghiệp vụ và CSDL local, thuộc Collection "real-server".
    ///              Mọi bài kiểm thử đều gắn cờ [Fact(Skip = "...")] để không ảnh hưởng luồng CI/local test tự động.
    ///              Lưu ý an toàn: KHÔNG thực hiện gửi thành công (POST 200) lên tacorp.vn vì sẽ tạo gói tin thật bên nhận.
    ///              Chứng chỉ SSL tacorp.vn có hạn đến 31/10/2026.
    /// Created date: 15/10/2026
    /// </summary>
    [Collection("real-server")]
    public class ShareDataPartnerHttpsEndpointTests
    {
        /// <summary>
        /// Description: TC4.a - Gọi server đối tác thật 115.78.1.139 với giao thức HTTP qua cổng 12204.
        ///              Máy chủ đối tác chỉ chấp nhận TLS nên kết nối HTTP bị cắt/reset ngang -> Success == false.
        /// Created date: 15/10/2026
        /// </summary>
        [Fact(Skip = "Gọi server đối tác thật 115.78.1.139 — chỉ gỡ Skip khi cần kiểm tra kết nối")]
        public async Task HttpSender_WhenCallingRealServerWithHttp_FailsDueToDroppedConnection_Test()
        {
            // Arrange
            var sender = new HttpDeliverySender();
            var partner = new ShareDataPartner
            {
                Code = "PARTNER_REAL_HTTP",
                Address = "http://115.78.1.139",
                Port = 12204,
                EndPointApiUrl = "/api/sharedata/shareDataInbound/receive"
            };
            var packet = new ShareDataPacket { Code = "103" };
            var sub = new ShareDataSubscription { ID = "sub-real-http-1", PartnerId = partner.Code, DatatypeId = "103", SerialNbr = 1 };
            var ctx = new DataOutboundContext(sub, partner, packet, null, DateTime.UtcNow, CancellationToken.None);
            var request = new DeliveryRequest
            {
                JsonPayload = "[{\"licensePlate\":\"29A-12345\"}]",
                ConnectType = BaseEnums.ConnectionType.Api,
                ConnectConfig = new Dictionary<string, string>()
            };

            // Act
            var result = await sender.Send(request, DataOutboundTransport.BuildContext(ctx), CancellationToken.None);

            // Assert
            Assert.False(result.Success);
        }

        /// <summary>
        /// Description: TC4.b - Gọi server đối tác thật 115.78.1.139 với giao thức HTTPS qua cổng 12204.
        ///              Vì chứng chỉ TLS của đối tác chỉ cấp cho tên miền 'tacorp.vn', gọi bằng IP gây lỗi xác thực tên miền chứng chỉ (hostname mismatch) -> Success == false, ErrorMessage rơi vào nhánh "Lỗi xác thực chứng chỉ SSL/TLS".
        /// Created date: 15/10/2026
        /// </summary>
        [Fact(Skip = "Gọi server đối tác thật 115.78.1.139 — chỉ gỡ Skip khi cần kiểm tra kết nối")]
        public async Task HttpSender_WhenCallingRealServerWithHttpsIp_FailsDueToCertificateMismatch_Test()
        {
            // Arrange
            var sender = new HttpDeliverySender();
            var partner = new ShareDataPartner
            {
                Code = "PARTNER_REAL_HTTPS_IP",
                Address = "https://115.78.1.139",
                Port = 12204,
                EndPointApiUrl = "/api/sharedata/shareDataInbound/receive"
            };
            var packet = new ShareDataPacket { Code = "103" };
            var sub = new ShareDataSubscription { ID = "sub-real-https-2", PartnerId = partner.Code, DatatypeId = "103", SerialNbr = 1 };
            var ctx = new DataOutboundContext(sub, partner, packet, null, DateTime.UtcNow, CancellationToken.None);
            var request = new DeliveryRequest
            {
                JsonPayload = "[{\"licensePlate\":\"29A-12345\"}]",
                ConnectType = BaseEnums.ConnectionType.Api,
                ConnectConfig = new Dictionary<string, string>()
            };

            // Act
            var result = await sender.Send(request, DataOutboundTransport.BuildContext(ctx), CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Lỗi xác thực chứng chỉ SSL/TLS", result.ErrorMessage);
        }
    }
}
#endif

