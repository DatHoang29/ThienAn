using Module.ShareData.Core.Entities;
using Newtonsoft.Json;
using SqlSugar;

namespace Module.ShareData.Core.Dto.Packet
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 gói tin
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPacketOutput : ShareDataPacket
    {
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 gói tin (phân trang / dropdown).      
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPagePacketOutput : ShareDataPacketOutput
    {
        /// <summary>
        /// Chỉ có giá trị khi lọc theo đối tác: true = gói đang dùng SQL riêng của đối tác, false = dùng mẫu chung.
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public bool? IsPartnerCustomSql { get; set; }

        /// <summary>Số câu ghi chiều nhận (ShareDataSql.WriteSql) của gói; 0 = gói chỉ gửi.</summary>
        [SugarColumn(IsIgnore = true)]
        public int WriteSqlCount { get; set; }

        /// <summary>Số lượng đối tác đang sử dụng gói tin.</summary>
        [SugarColumn(IsIgnore = true)]
        public int PartnerCount { get; set; }

        /// <summary>Danh sách tóm tắt đối tác đang dùng gói (mã đối tác và trạng thái riêng / mẫu) để hiển thị tooltip.</summary>
        [SugarColumn(IsIgnore = true)]
        public List<string>? PartnerNames { get; set; }
    }
}
