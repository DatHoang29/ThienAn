using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Utility.Helpers
{
    /// <summary>
    /// Author: kien.le
    /// Description: Ticket ngắn hạn ký HMAC-SHA256 để FE truy cập luồng CCTV qua Cctv Proxy Service
    /// mà không cần cầm token/cookie NxVMS. WebAPI (sau khi đã xác thực JWT người dùng) tạo ticket,
    /// Proxy kiểm tra chữ ký + hạn + DeviceId + Kind trước khi chuyển tiếp sang NxVMS.
    /// Định dạng: base64url(json payload) + "." + base64url(hmac).
    /// Created date: 06/10/2026
    /// </summary>
    public class CctvStreamTicketHelper
    {
        /// <summary>Tên query param chứa ticket trên URL proxy.</summary>
        public const string QueryName = "ticket";

        /// <summary>Luồng WebRTC signaling (live + playback).</summary>
        public const string KindWebRtc = "webrtc";

        /// <summary>Luồng HTTP media (tải video archive).</summary>
        public const string KindMedia = "media";

        /// <summary>Ảnh snapshot (xem camera dạng ảnh định kỳ khi ở mạng public).</summary>
        public const string KindImage = "image";

        /// <summary>
        /// Thời lượng tối đa một lần tải video archive (60 phút). Dùng chung: WebAPI validate khi cấp URL,
        /// Cctv Proxy chặn <c>durationMs</c> vượt mức (ticket không ký query nên sửa tay URL vẫn bị chặn).
        /// Date: 21/10/2026
        /// </summary>
        public const int MaxMediaDownloadMinutes = 60;

        /// <summary><see cref="MaxMediaDownloadMinutes"/> quy ra ms.</summary>
        public const long MaxMediaDownloadDurationMs = MaxMediaDownloadMinutes * 60L * 1000L;

        /// <summary>
        /// Nội dung một ticket truy cập luồng CCTV qua proxy.
        /// </summary>
        public class CctvStreamTicketPayload
        {
            /// <summary>DeviceId (id camera bên VMS) mà ticket được phép truy cập.</summary>
            [JsonPropertyName("d")]
            public string DeviceId { get; set; } = string.Empty;

            /// <summary>Loại luồng: <see cref="KindWebRtc"/> | <see cref="KindMedia"/> | <see cref="KindImage"/>.</summary>
            [JsonPropertyName("k")]
            public string Kind { get; set; } = string.Empty;

            /// <summary>UserId người yêu cầu (chỉ để ghi log/truy vết).</summary>
            [JsonPropertyName("u")]
            public string? UserId { get; set; }

            /// <summary>Thời điểm hết hạn (Unix giây, UTC).</summary>
            [JsonPropertyName("e")]
            public long ExpiresAtUnix { get; set; }
        }

        /// <summary>Tạo ticket ký HMAC-SHA256 từ payload.</summary>
        /// <param name="secret">Secret dùng chung giữa WebAPI và proxy (SysOpConfig cctv_proxy_ticket_secret).</param>
        /// <param name="payload">DeviceId + loại luồng + hạn dùng.</param>
        /// <returns>Chuỗi ticket để gắn vào query <see cref="QueryName"/>.</returns>
        public static string Create(string secret, CctvStreamTicketPayload payload)
        {
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("Ticket secret is required.", nameof(secret));

            var payloadPart = Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(payload));
            var signaturePart = Base64Url.EncodeToString(Sign(secret, payloadPart));
            return $"{payloadPart}.{signaturePart}";
        }

        /// <summary>
        /// Kiểm tra chữ ký + hạn dùng. Không kiểm tra DeviceId/Kind — caller tự so khớp với route.
        /// </summary>
        /// <param name="ticket">Ticket lấy từ query.</param>
        /// <param name="secret">Secret dùng chung với WebAPI.</param>
        /// <param name="now">Thời điểm hiện tại (truyền vào để test được).</param>
        /// <param name="payload">Nội dung ticket khi hợp lệ.</param>
        /// <param name="error">Lý do không hợp lệ.</param>
        /// <returns>true nếu chữ ký đúng và chưa hết hạn.</returns>
        public static bool TryValidate(
            string? ticket,
            string secret,
            DateTimeOffset now,
            out CctvStreamTicketPayload? payload,
            out string? error)
        {
            payload = null;
            error = null;

            if (string.IsNullOrWhiteSpace(ticket))
            {
                error = "Thiếu ticket";
                return false;
            }

            if (string.IsNullOrWhiteSpace(secret))
            {
                error = "Proxy chưa cấu hình ticket secret";
                return false;
            }

            var parts = ticket.Split('.');
            if (parts.Length != 2)
            {
                error = "Ticket sai định dạng";
                return false;
            }

            byte[] signature;
            byte[] payloadBytes;
            try
            {
                signature = Base64Url.DecodeFromChars(parts[1]);
                payloadBytes = Base64Url.DecodeFromChars(parts[0]);
            }
            catch (FormatException)
            {
                error = "Ticket sai định dạng";
                return false;
            }

            if (!CryptographicOperations.FixedTimeEquals(signature, Sign(secret, parts[0])))
            {
                error = "Ticket sai chữ ký";
                return false;
            }

            try
            {
                payload = JsonSerializer.Deserialize<CctvStreamTicketPayload>(payloadBytes);
            }
            catch (JsonException)
            {
                payload = null;
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.DeviceId))
            {
                error = "Ticket sai nội dung";
                payload = null;
                return false;
            }

            if (payload.ExpiresAtUnix < now.ToUnixTimeSeconds())
            {
                error = "Ticket đã hết hạn";
                return false;
            }

            return true;
        }

        private static byte[] Sign(string secret, string payloadPart) =>
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payloadPart));
    }
}
