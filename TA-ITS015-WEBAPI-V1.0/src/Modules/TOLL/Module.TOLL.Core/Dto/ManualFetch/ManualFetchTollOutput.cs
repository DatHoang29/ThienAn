using System.Collections.Generic;

namespace Module.TOLL.Core.Dto.ManualFetch
{
    /// <summary>
    /// Kết quả trả về ngay cho FE sau khi BE đã phát lệnh lên NATS (fire-and-forget).
    /// FE dùng <see cref="RequestId"/> để đối chiếu với gói tin phản hồi (Type = "result") nhận trực tiếp qua NATS.
    /// Author: ChuongLP
    /// Created date: 14/09/2026
    /// </summary>
    public class ManualFetchTollOutput
    {
        /// <summary>Mã truy vết của yêu cầu — FE lắng nghe kết quả trên NATS theo mã này.</summary>
        public string RequestId { get; set; } = string.Empty;

        /// <summary>Các loại dữ liệu đã yêu cầu lấy (để FE hiển thị/đối chiếu).</summary>
        public List<string> DataTypes { get; set; } = new();

        /// <summary>Thông báo ngắn xác nhận đã tiếp nhận yêu cầu.</summary>
        public string Message { get; set; } = string.Empty;
    }
}
