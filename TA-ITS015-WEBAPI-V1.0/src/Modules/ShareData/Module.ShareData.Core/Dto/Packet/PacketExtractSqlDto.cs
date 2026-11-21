using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.Packet
{
    /// <summary>
    /// Author: DatHQ
    /// Description: DTO lấy cấu hình câu truy vấn trích xuất động của gói tin theo PacketCode.
    /// Created date: 13/10/2026
    /// </summary>
    public class GetPacketExtractSqlInput
    {
        public string PacketCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO trả về cấu hình câu truy vấn trích xuất động của gói tin.
    /// Created date: 13/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    public class PacketExtractSqlOutput
    {
        public string ID { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;

        public string ExtractSql { get; set; } = string.Empty;

        public string? ColumnNames { get; set; }

        public BaseEnums.StatusEnum? Status { get; set; } = BaseEnums.StatusEnum.Enable;

        public string? Remark { get; set; }

        /// <summary>Danh sách trường do câu SQL mẫu chung sinh ra.</summary>
        public List<SqlFieldItem> Fields { get; set; } = [];

        /// <summary>Thời điểm dò cột gần nhất của câu SQL mẫu chung.</summary>
        public DateTime? DescribedTime { get; set; }

        /// <summary>true = chưa lưu vì câu SQL mới làm mất khoá đang được hồ sơ ánh xạ dùng, hoặc đổi công thức cột mốc khi đối tác đang có mốc;
        ///          gửi lại với Force = true để xác nhận lưu.</summary>
        public bool RequiresConfirm { get; set; }

        /// <summary>Khoá vừa mất khỏi câu SQL so với bản đang lưu.</summary>
        public List<string> Lost { get; set; } = [];

        /// <summary>Hồ sơ ánh xạ đang dùng khoá vừa mất.</summary>
        public List<SqlFieldMappingUsage> AffectedMappings { get; set; } = [];

        /// <summary>Đối tác đang có mốc nối đuôi sẽ được đặt lại (gửi lại từ mốc khởi tạo) vì câu SQL mới đổi công thức cột mốc.</summary>
        public List<CursorResetPartner> CursorResetPartners { get; set; } = [];

        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO cập nhật câu truy vấn trích xuất động của gói tin (dành cho SuperAdmin).
    /// Created date: 13/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    public class UpdatePacketExtractSqlInput
    {
        public string PacketCode { get; set; } = string.Empty;

        /// <summary>Câu SQL trích xuất của gói tin.</summary>
        public string? ExtractSql { get; set; }

        public BaseEnums.StatusEnum? Status { get; set; } = BaseEnums.StatusEnum.Enable;

        public string? Remark { get; set; }

        /// <summary>Phần người dùng sửa trên các trường (nhãn, bắt buộc, bỏ qua, nhóm, ghi chú); khoá phải có trong kết quả dò câu SQL.</summary>
        public List<SqlFieldEditInput>? Fields { get; set; }

        /// <summary>
        /// Bộ biến đi kèm câu SQL (null = dùng bộ biến đang lưu): câu được kiểm theo đúng bộ biến này và cả hai ghi trong cùng một lần,
        /// câu bị từ chối thì bộ biến cũng không được lưu.
        /// </summary>
        public List<PacketSqlVariableItemInput>? Variables { get; set; }

        /// <summary>Xác nhận lưu dù câu SQL mới làm mất khoá đang được hồ sơ ánh xạ dùng, hoặc đổi công thức cột mốc (mốc của đối tác được đặt lại).</summary>
        public bool Force { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO kiểm tra cú pháp và trích xuất danh sách cột từ câu SQL trích xuất.
    /// Created date: 13/10/2026
    /// </summary>
    public class ParsePacketColumnsInput
    {
        public string ExtractSql { get; set; } = string.Empty;

        /// <summary>Gói tin của câu SQL: có thì cho dùng các biến đã khai ở bảng tham số câu truy vấn.</summary>
        public string? PacketCode { get; set; }

        /// <summary>Đối tác khi đang soạn SQL riêng: kiểm theo bộ biến của SQL riêng đó.</summary>
        public string? PartnerId { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO kết quả kiểm tra cú pháp và trích xuất cột.
    /// Created date: 13/10/2026
    /// </summary>
    public class ParsePacketColumnsOutput
    {
        public bool IsValid { get; set; }

        public string? ErrorMessage { get; set; }

        /// <summary>Dòng (từ 1) của lỗi trong câu SQL, null khi không xác định được vị trí.</summary>
        public int? ErrorLine { get; set; }

        /// <summary>Cột (từ 1) của lỗi trong câu SQL, null khi không xác định được vị trí.</summary>
        public int? ErrorColumn { get; set; }

        public List<string> Columns { get; set; } = [];
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO thực thi xem trước dữ liệu mẫu từ câu SQL trích xuất (tối đa 10 bản ghi).
    /// Created date: 13/10/2026
    /// </summary>
    public class ExecutePacketSqlSampleInput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string ExtractSql { get; set; } = string.Empty;

        public int Top { get; set; } = 10;

        /// <summary>Đối tác khi đang chạy thử SQL riêng: dùng bộ biến của SQL riêng đó.</summary>
        public string? PartnerId { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Kết quả thực thi xem trước dữ liệu mẫu.
    /// Created date: 13/10/2026
    /// </summary>
    public class ExecutePacketSqlSampleOutput
    {
        public bool IsSuccess { get; set; }

        public string? ErrorMessage { get; set; }

        public List<string> Columns { get; set; } = [];

        public List<Dictionary<string, object?>> Rows { get; set; } = [];
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO lấy danh sách đối tác có thể cấu hình câu truy vấn riêng trên một gói tin (đối tác có đăng ký gửi gói này hoặc đã có câu riêng).
    /// Created date: 22/10/2026
    /// </summary>
    public class GetPartnerSqlListInput
    {
        public string PacketCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Thông tin tóm tắt câu truy vấn trích xuất của một đối tác trên một gói tin.
    /// Created date: 22/10/2026
    /// </summary>
    public class PartnerSqlListItemOutput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PartnerCode { get; set; } = string.Empty;

        public string PartnerName { get; set; } = string.Empty;

        public bool IsCustom { get; set; }

        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO lấy chi tiết câu truy vấn trích xuất riêng của đối tác kèm thông tin đối chiếu với gói tin chuẩn.
    /// Created date: 22/10/2026
    /// </summary>
    public class GetPartnerExtractSqlInput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Chi tiết câu truy vấn trích xuất riêng đối tác và kết quả đối chiếu với câu chuẩn của gói tin.
    /// Created date: 22/10/2026
    /// </summary>
    public class PartnerExtractSqlOutput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PartnerCode { get; set; } = string.Empty;

        public string PartnerName { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;

        public string StandardSql { get; set; } = string.Empty;

        public string? CustomSql { get; set; }

        public bool IsCustom { get; set; }

        public bool HasDifferences { get; set; }

        /// <summary>Danh sách trường do câu SQL riêng của đối tác sinh ra; rỗng khi đối tác dùng câu mẫu chung.
        ///          Khoá của mẫu chung mà câu riêng không trả về có Lost = true.</summary>
        public List<SqlFieldItem> Fields { get; set; } = [];

        /// <summary>Danh sách trường của câu SQL mẫu chung, để đối chiếu.</summary>
        public List<SqlFieldItem> CommonFields { get; set; } = [];

        /// <summary>Thời điểm dò cột gần nhất của câu SQL riêng.</summary>
        public DateTime? DescribedTime { get; set; }

        /// <summary>true = chưa thực hiện vì làm mất khoá đang được hồ sơ ánh xạ của đối tác dùng, hoặc đổi công thức cột mốc khi đối tác đang có mốc;
        ///          gửi lại với Force = true để xác nhận.</summary>
        public bool RequiresConfirm { get; set; }

        /// <summary>Khoá bị mất nếu thực hiện.</summary>
        public List<string> Lost { get; set; } = [];

        /// <summary>Hồ sơ ánh xạ của đối tác đang dùng khoá bị mất.</summary>
        public List<SqlFieldMappingUsage> AffectedMappings { get; set; } = [];

        /// <summary>Đối tác đang có mốc nối đuôi sẽ được đặt lại (gửi lại từ mốc khởi tạo) vì câu SQL đổi công thức cột mốc.</summary>
        public List<CursorResetPartner> CursorResetPartners { get; set; } = [];
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO cập nhật câu truy vấn trích xuất riêng cho đối tác.
    /// Created date: 22/10/2026
    /// </summary>
    public class UpdatePartnerExtractSqlInput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;

        public string? ExtractSql { get; set; }

        /// <summary>Phần người dùng sửa trên các trường của câu SQL riêng; khoá phải có trong kết quả dò câu SQL.</summary>
        public List<SqlFieldEditInput>? Fields { get; set; }

        /// <summary>Bộ biến đi kèm câu SQL riêng (null = dùng bộ biến đang lưu): kiểm và ghi cùng câu trong một lần, câu bị từ chối thì không lưu.</summary>
        public List<PacketSqlVariableItemInput>? Variables { get; set; }

        /// <summary>Xác nhận lưu dù câu SQL riêng làm mất khoá đang được hồ sơ ánh xạ của đối tác dùng, hoặc đổi công thức cột mốc (mốc của đối tác được đặt lại).</summary>
        public bool Force { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO đặt lại câu truy vấn trích xuất riêng của đối tác về theo gói tin chuẩn.
    /// Created date: 22/10/2026
    /// </summary>
    public class ResetPartnerExtractSqlInput
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PacketCode { get; set; } = string.Empty;

        /// <summary>Xác nhận đặt lại dù hồ sơ ánh xạ của đối tác đang dùng khoá chỉ câu SQL riêng có, hoặc câu mẫu chung khác công thức cột mốc (mốc của đối tác được đặt lại).</summary>
        public bool Force { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đối tác đang có mốc nối đuôi sẽ được đặt lại khi lưu câu SQL đổi công thức cột mốc __lastKey.
    /// Created date: 29/10/2026
    /// </summary>
    public class CursorResetPartner
    {
        public string PartnerId { get; set; } = string.Empty;

        public string? PartnerCode { get; set; }

        public string? PartnerName { get; set; }
    }

}

