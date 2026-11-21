using System.Collections.Generic;

namespace Module.TOLL.Core.Dto.ManualFetch
{
    /// <summary>
    /// Gói kết quả (Type = "result") do dịch vụ ITS.Toll.Worker phát trên kênh ta.its.control.toll
    /// sau khi đã kéo dữ liệu từ TCS và lưu DB. BE chỉ đọc các trường cần cho việc xoá cache.
    /// Cấu trúc khớp ITS.Toll.Core.Messaging.TollControlResult (Newtonsoft không phân biệt hoa/thường).
    /// Author: ChuongLP
    /// Created date: 28/09/2026
    /// </summary>
    public class TollControlResultMessage
    {
        /// <summary>Loại gói tin: "request" (BE phát) hoặc "result" (dịch vụ phát).</summary>
        public string? Type { get; set; }

        /// <summary>Mã truy vết yêu cầu.</summary>
        public string? RequestId { get; set; }

        /// <summary>Loại dữ liệu trong gói "request" (BE tự phát, được bỏ qua).</summary>
        public List<string>? DataTypes { get; set; }

        /// <summary>Kết quả theo từng loại dữ liệu.</summary>
        public List<TollControlResultMessageItem>? Items { get; set; }
    }

    /// <summary>Kết quả của một loại dữ liệu trong gói "result".</summary>
    public class TollControlResultMessageItem
    {
        public string? DataType { get; set; }

        public bool Ok { get; set; }
    }
}
