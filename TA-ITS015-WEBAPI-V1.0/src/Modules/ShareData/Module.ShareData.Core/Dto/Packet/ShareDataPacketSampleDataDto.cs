namespace Module.ShareData.Core.Dto.Packet
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy dữ liệu mẫu THẬT của một gói tin — dùng cho màn Ánh xạ dữ liệu,
    ///              tab Gửi thử. Dữ liệu chạy đúng câu truy vấn nguồn mà dịch vụ nền dùng khi
    ///              gửi cho đối tác, chỉ giới hạn số dòng.
    /// Created date: 2026-09-22
    /// Modified date: 03/11/2026
    /// </summary>
    public class ShareDataPacketSampleDataInput
    {
        /// <summary>
        /// ID gói tin (ShareDataPacket.ID). Bỏ trống thì tra theo Code.
        /// </summary>
        public string? DatatypeId { get; set; }

        /// <summary>
        /// Mã gói tin (ShareDataPacket.Code) — dùng khi không truyền DatatypeId.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Số bản ghi cần lấy. Bỏ trống = 5, tối đa 50.
        /// </summary>
        public int? Top { get; set; }

        /// <summary>
        /// Mã đối tác (ShareDataPartner.Code) — tuỳ chọn, để lấy ExtractSql riêng của đối tác nếu có.
        /// </summary>
        public string? PartnerCode { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về dữ liệu mẫu THÔ của một gói tin.
    ///              Cố ý KHÔNG ánh xạ sang khuôn đối tác: việc quy đổi bộ mã, ép kiểu và dựng
    ///              payload do giao diện tự làm, để người dùng so được dữ liệu trước và sau khi
    ///              qua hồ sơ ánh xạ.
    /// Created date: 2026-09-22
    /// </summary>
    public class ShareDataPacketSampleDataOutput
    {
        /// <summary>ID gói tin đã lấy dữ liệu.</summary>
        public string? DatatypeId { get; set; }

        /// <summary>Mã gói tin đã lấy dữ liệu.</summary>
        public string? PacketCode { get; set; }

        /// <summary>Tên cột theo đúng thứ tự trong câu truy vấn — trùng với AliasFieldKey của field gói tin.</summary>
        public List<string> Columns { get; set; } = [];

        /// <summary>Các dòng dữ liệu thô, khoá tra là tên cột.</summary>
        public List<Dictionary<string, object?>> Rows { get; set; } = [];

        /// <summary>Số dòng thực tế lấy được.</summary>
        public int RowCount { get; set; }

        /// <summary>Câu truy vấn đã chạy — để đối chiếu khi dữ liệu trông không như mong đợi.</summary>
        public string? Sql { get; set; }
    }
}
