namespace Module.ShareData.Core.Dto.Packet
{
    /// <summary>
    /// Author: DatHQ
    /// Description: DTO lấy cấu hình khối câu ghi chiều nhận (ShareDataSql.WriteSql) của gói tin.
    /// Created date: 26/10/2026
    /// </summary>
    public class GetPacketWriteSqlsInput
    {
        public string PacketCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cấu hình khối câu ghi chiều nhận của gói tin: một khối SQL (có thể gồm nhiều câu MERGE/INSERT/UPDATE),
    /// Created date: 26/10/2026
    /// </summary>
    public class PacketWriteSqlsOutput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string? WriteSql { get; set; }

        public List<string> AffectedTables { get; set; } = [];

        public List<string> DetectedTables { get; set; } = [];

        public List<string> KeyFields { get; set; } = [];

        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy danh sách đối tác có thể cấu hình câu ghi chiều nhận riêng trên một gói tin
    ///              (giống GetPartnerSqlListInput của chiều gửi; kết quả dùng chung PartnerSqlListItemOutput, IsCustom = đã có câu ghi riêng).
    /// Created date: 30/10/2026
    /// </summary>
    public class GetPartnerWriteSqlListInput
    {
        public string PacketCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO lưu khối câu ghi chiều nhận của gói tin; WriteSql trống sẽ xoá mềm cấu hình ghi của gói (chuyển về Chỉ gửi).
    /// Created date: 26/10/2026
    /// </summary>
    public class UpdatePacketWriteSqlsInput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string? WriteSql { get; set; }

        public List<string>? AffectedTables { get; set; }

        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO kiểm an toàn khối câu ghi chiều nhận (không ghi dữ liệu).
    /// Created date: 26/10/2026
    /// </summary>
    public class ValidateWriteSqlInput
    {
        public string PacketCode { get; set; } = string.Empty;

        /// <summary>Đối tác (không bắt buộc): có giá trị = kiểm / chạy theo câu ghi và danh sách trường của đối tác; trống = mẫu chung.</summary>
        public string? PartnerId { get; set; }

        public string? WriteSql { get; set; }

        public List<string>? AffectedTables { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Một kết quả kiểm câu ghi. Level: Error (chặn lưu) | Warning (cho lưu, cần người dùng xác nhận).
    /// Created date: 26/10/2026
    /// </summary>
    public class WriteSqlCheckItem
    {
        public string Level { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        /// <summary>Dòng (từ 1) trong câu ghi, null khi không xác định được vị trí.</summary>
        public int? Line { get; set; }

        /// <summary>Cột (từ 1) trong câu ghi, null khi không xác định được vị trí.</summary>
        public int? Column { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Kết quả kiểm khối câu ghi: hợp lệ khi không có lỗi (cảnh báo vẫn tính là hợp lệ), kèm danh sách bảng tự dò và khoá bản ghi.
    /// Created date: 26/10/2026
    /// </summary>
    public class ValidateWriteSqlOutput
    {
        public bool IsValid { get; set; }

        public List<WriteSqlCheckItem> Checks { get; set; } = [];

        public List<string> DetectedTables { get; set; } = [];

        public List<string> KeyFields { get; set; } = [];
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO chạy thử khối câu ghi chiều nhận: chạy thật trên CSDL nhận trong một giao dịch rồi quay lui, không ghi dữ liệu.
    /// Created date: 26/10/2026
    /// </summary>
    public class TryWriteSqlInput
    {
        public string PacketCode { get; set; } = string.Empty;

        /// <summary>Đối tác (không bắt buộc): có giá trị = kiểm / chạy theo câu ghi và danh sách trường của đối tác; trống = mẫu chung.</summary>
        public string? PartnerId { get; set; }

        public string? RecordsJson { get; set; }

        public string? WriteSql { get; set; }

        public List<string>? AffectedTables { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Kết quả chạy thử khối câu ghi của gói (đã quay lui, CSDL không đổi), tổng hợp số bản ghi thêm/sửa của mọi câu trong khối.
    /// Created date: 26/10/2026
    /// </summary>
    public class TryWriteSqlOutput
    {
        public bool IsSuccess { get; set; }

        public int RecordCount { get; set; }

        public int Inserted { get; set; }

        public int Updated { get; set; }

        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy câu ghi chiều nhận riêng của đối tác kèm câu ghi mẫu chung để đối chiếu.
    /// Created date: 30/10/2026
    /// </summary>
    public class GetPartnerWriteSqlInput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Câu ghi chiều nhận của đối tác: câu ghi riêng (CustomWriteSql) và câu ghi mẫu chung (StandardWriteSql).
    ///              Đối tác chưa có câu ghi riêng (IsCustom = false) thì Worker chạy câu ghi mẫu chung.
    ///              AffectedTables / DetectedTables / KeyFields / Remark mô tả câu ghi hiệu lực (riêng nếu có, không thì mẫu chung).
    /// Created date: 30/10/2026
    /// </summary>
    public class PartnerWriteSqlOutput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PartnerCode { get; set; } = string.Empty;

        public string PartnerName { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;

        public string? StandardWriteSql { get; set; }

        public string? CustomWriteSql { get; set; }

        public bool IsCustom { get; set; }

        public List<string> AffectedTables { get; set; } = [];

        public List<string> DetectedTables { get; set; } = [];

        public List<string> KeyFields { get; set; } = [];

        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật câu ghi chiều nhận riêng cho đối tác (câu ghi rỗng bị từ chối — muốn dùng lại mẫu chung thì gọi ResetPartnerWriteSql).
    /// Created date: 30/10/2026
    /// </summary>
    public class UpdatePartnerWriteSqlInput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;

        public string? WriteSql { get; set; }

        public List<string>? AffectedTables { get; set; }

        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO đặt lại câu ghi chiều nhận của đối tác về câu ghi mẫu chung (xoá câu ghi riêng).
    /// Created date: 30/10/2026
    /// </summary>
    public class ResetPartnerWriteSqlInput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;
    }
}
