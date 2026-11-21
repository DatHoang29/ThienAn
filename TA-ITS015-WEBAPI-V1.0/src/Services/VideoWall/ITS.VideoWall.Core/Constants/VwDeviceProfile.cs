namespace ITS.VideoWall.Core.Constants
{
    /// <summary>
    /// Profile định danh và thông số giao tiếp của bộ điều khiển Hikvision Video Wall.
    /// Giá trị hằng số đóng vai trò giá trị mặc định, có thể override qua cấu hình nếu cần.
    /// </summary>
    public static class VwDeviceProfile
    {
        /// <summary>
        /// Model thiết bị hiện trường (DS-C30S-S11). Giá trị này dùng hiển thị mặc định,
        /// model thật đọc từ deviceInfo khi cần.
        /// </summary>
        public const string Model = "DS-C30S-S11";

        /// <summary>
        /// Giao thức HTTP mặc định của ISAPI (§3 ISAPI Framework, 09-api-reference.md).
        /// </summary>
        public const string Scheme = "http";

        /// <summary>
        /// Port mặc định của thiết bị khi IP không kèm port (§3 ISAPI Framework, 09-api-reference.md).
        /// </summary>
        public const int DefaultPort = 80;

        /// <summary>
        /// XML Namespace của ISAPI chuẩn Hikvision — xác nhận qua log thiết bị thật (LogsAPI/).
        /// </summary>
        public const string XmlNamespace = "http://www.isapi.org/ver20/XMLSchema";

        /// <summary>
        /// Chế độ toạ độ bắt buộc khi gửi WallWindow — xác nhận qua log thiết bị thật (LogsAPI/).
        /// </summary>
        public const string WndOperateMode = "uniformCoordinate";

        /// <summary>
        /// Chế độ tín hiệu sub-window — xác nhận qua log thiết bị thật (LogsAPI/).
        /// </summary>
        public const string SignalMode = "video input";

        /// <summary>
        /// Nonce count dùng cho Digest Auth (§6 Information Security, 09-api-reference.md).
        /// </summary>
        public const string DigestNonceCount = "00000001";
    }
}
