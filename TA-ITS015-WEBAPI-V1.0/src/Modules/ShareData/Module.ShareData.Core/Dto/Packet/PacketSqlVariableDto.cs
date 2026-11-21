namespace Module.ShareData.Core.Dto.Packet
{
    /// <summary>
    /// Author: DatHQ
    /// Description: DTO lấy bảng tham số câu truy vấn của gói tin. PartnerId trống là bộ biến của mẫu chung; có PartnerId thì
    ///              trả bộ biến của SQL riêng đối tác (đối tác chưa có SQL riêng và chưa có dòng biến nào thì trả bộ của mẫu chung).
    /// Created date: 24/10/2026
    /// </summary>
    public class GetPacketSqlVariablesInput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string? PartnerId { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Một dòng tham số câu truy vấn. ValueKind = Const thì Value là hằng số; ValueKind = Builtin thì Value là mã nguồn hệ thống.
    ///              Scope: Common (bộ biến của mẫu chung) | Partner (bộ biến của SQL riêng đối tác).
    /// Created date: 24/10/2026
    /// </summary>
    public class PacketSqlVariableItem
    {
        public string Code { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string DataType { get; set; } = string.Empty;

        public string ValueKind { get; set; } = string.Empty;

        public string? Value { get; set; }

        public string? DefaultValue { get; set; }

        public string? SavedValue { get; set; }

        public string? Remark { get; set; }

        public int? OrderNo { get; set; }

        public string Scope { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Bảng tham số câu truy vấn của gói tin (theo mẫu chung hoặc theo đối tác).
    /// Created date: 24/10/2026
    /// </summary>
    public class PacketSqlVariablesOutput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string? PartnerId { get; set; }

        public List<PacketSqlVariableItem> Items { get; set; } = [];
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Một dòng tham số gửi lên khi lưu.
    /// Created date: 24/10/2026
    /// </summary>
    public class PacketSqlVariableItemInput
    {
        public string? Code { get; set; }

        public string? Name { get; set; }

        public string? DataType { get; set; }

        public string? ValueKind { get; set; }

        public string? Value { get; set; }

        public string? DefaultValue { get; set; }

        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO lưu toàn bộ bộ biến của một câu SQL (mẫu chung, hoặc SQL riêng khi có PartnerId); dòng không gửi lên là xóa.
    /// Created date: 24/10/2026
    /// </summary>
    public class UpdatePacketSqlVariablesInput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string? PartnerId { get; set; }

        public List<PacketSqlVariableItemInput> Items { get; set; } = [];
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO đặt lại mốc đã gửi (ShareDataLastSend) của gói tin cho đối tác về trạng thái chưa có (sẽ dùng mốc mặc định ở lần chạy kế tiếp).
    /// Created date: 27/10/2026
    /// </summary>
    public class ResetLastSendInput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string PartnerId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO đặt mốc nối đuôi (ShareDataLastSend) của gói tin cho một đối tác: lượt gửi kế tiếp lấy dữ liệu sau mốc này.
    ///              Dùng khi đối tác chỉ muốn nhận dữ liệu từ một thời điểm mà không phải tách cấu hình riêng chỉ để đổi mốc khởi tạo.
    /// Created date: 29/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    public class SetLastSendInput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string PartnerId { get; set; } = string.Empty;

        /// <summary>Mốc khóa, bắt buộc (bỏ mốc thì dùng Đặt lại).</summary>
        public string? LastKey { get; set; }

        /// <summary>Nhóm mốc (hậu tố của cột __lastKey_&lt;nhóm&gt;); trống là nhóm mặc định __lastKey.</summary>
        public string? CursorKey { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Một biến @tên của câu SQL, lưu dạng mảng JSON ở cột ShareDataSql.VariablesJson (như SqlFieldItem của FieldsJson).
    /// Created date: 27/10/2026
    /// </summary>
    public class SqlVariableItem
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public string? ValueKind { get; set; }
        public string? Value { get; set; }
        public string? DefaultValue { get; set; }
        public string? Remark { get; set; }
        public int? OrderNo { get; set; }
    }
}
