using Newtonsoft.Json;

namespace Module.ShareData.Core.Dto.Packet
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Một trường của gói tin, là một phần tử của ShareDataSql.FieldsJson.
    ///              Key, Type, DbType, Nullable, Source, Lost do bộ dò cột ghi theo câu SQL; Label, Required, Skip, Group, Remark do người dùng khai.
    /// Created date: 24/10/2026
    /// </summary>
    public class SqlFieldItem
    {
        /// <summary>Khoá trường = tên cột trong kết quả câu SQL.</summary>
        [JsonProperty("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>Kiểu quy đổi từ kiểu CSDL: string | number | datetime | bool.</summary>
        [JsonProperty("type")]
        public string Type { get; set; } = SqlFieldType.String;

        /// <summary>Kiểu gốc trên CSDL, VD nvarchar(128), decimal(18,2).</summary>
        [JsonProperty("dbType")]
        public string? DbType { get; set; }

        /// <summary>CSDL báo cột có thể NULL.</summary>
        [JsonProperty("nullable")]
        public bool? Nullable { get; set; }

        /// <summary>Bảng.cột nguồn; null khi cột là biểu thức.</summary>
        [JsonProperty("source")]
        public string? Source { get; set; }

        /// <summary>Cột không còn trong câu SQL ở lần dò gần nhất (giữ lại, không xoá).</summary>
        [JsonProperty("lost")]
        public bool Lost { get; set; }

        /// <summary>Nhãn hiển thị.</summary>
        [JsonProperty("label")]
        public string? Label { get; set; }

        /// <summary>Bắt buộc có giá trị (mặc định cho lá ở hồ sơ ánh xạ).</summary>
        [JsonProperty("required")]
        public bool Required { get; set; }

        /// <summary>Người dùng bỏ qua: không cho chọn ở màn ánh xạ.</summary>
        [JsonProperty("skip")]
        public bool Skip { get; set; }

        /// <summary>Nhóm hiển thị.</summary>
        [JsonProperty("group")]
        public string? Group { get; set; }

        [JsonProperty("remark")]
        public string? Remark { get; set; }

        /// <summary>Kiểu người dùng tự chọn; có giá trị thì Type luôn theo kiểu này, bộ dò không ghi đè. Null = theo kiểu dò được.</summary>
        [JsonProperty("typeOverride")]
        public string? TypeOverride { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Kiểu trường quy đổi từ kiểu CSDL.
    /// Created date: 24/10/2026
    /// </summary>
    public static class SqlFieldType
    {
        public const string String = "string";
        public const string Number = "number";
        public const string DateTime = "datetime";
        public const string Bool = "bool";
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Phần người dùng được sửa trên một trường; khoá phải có sẵn trong danh sách trường do bộ dò sinh ra.
    /// Created date: 24/10/2026
    /// </summary>
    public class SqlFieldEditInput
    {
        public string Key { get; set; } = string.Empty;

        public string? Label { get; set; }

        public bool Required { get; set; }

        public bool Skip { get; set; }

        public string? Group { get; set; }

        public string? Remark { get; set; }

        /// <summary>Kiểu người dùng chọn (string | number | datetime | bool); trống hoặc trùng kiểu dò được = theo kiểu dò được.</summary>
        public string? Type { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Hồ sơ ánh xạ chiều gửi đang dùng các khoá trường bị ảnh hưởng.
    /// Created date: 24/10/2026
    /// </summary>
    public class SqlFieldMappingUsage
    {
        public string MappingId { get; set; } = string.Empty;

        public string? MappingCode { get; set; }

        public string? MappingName { get; set; }

        public string? PartnerId { get; set; }

        /// <summary>Các khoá trường bị ảnh hưởng mà hồ sơ đang dùng.</summary>
        public List<string> Keys { get; set; } = [];
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách trường dùng được của gói tin cho màn ánh xạ. PartnerId có giá trị và đối tác có câu SQL riêng
    ///              thì lấy trường của câu riêng, không thì lấy trường của câu mẫu chung.
    /// Created date: 26/10/2026
    /// </summary>
    public class GetPacketFieldsInput
    {
        /// <summary>ID hoặc mã gói tin (ShareDataMapping.DatatypeId).</summary>
        public string DatatypeId { get; set; } = string.Empty;

        public string? PartnerId { get; set; }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO dò cột trả về của câu SQL đang soạn (chưa lưu) và đối chiếu với danh sách trường đang lưu.
    ///              PartnerId trống = câu mẫu chung; có giá trị = câu SQL riêng của đối tác.
    /// Created date: 24/10/2026
    /// </summary>
    public class DescribeColumnsInput
    {
        public string PacketCode { get; set; } = string.Empty;

        public string? PartnerId { get; set; }
        public string ExtractSql { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Kết quả dò cột: danh sách trường sau khi gộp với bản đang lưu (chưa ghi) và các thay đổi so với bản đang lưu.
    /// Created date: 24/10/2026
    /// </summary>
    public class DescribeColumnsOutput
    {
        public bool IsValid { get; set; }

        public string? ErrorMessage { get; set; }

        /// <summary>Dòng (từ 1) của lỗi trong câu SQL, null khi không xác định được vị trí.</summary>
        public int? ErrorLine { get; set; }

        /// <summary>Cột (từ 1) của lỗi trong câu SQL, null khi không xác định được vị trí.</summary>
        public int? ErrorColumn { get; set; }

        public List<SqlFieldItem> Fields { get; set; } = [];

        /// <summary>Khoá mới so với bản đang lưu.</summary>
        public List<string> Added { get; set; } = [];

        /// <summary>Khoá vừa mất khỏi câu SQL so với bản đang lưu.</summary>
        public List<string> Lost { get; set; } = [];

        /// <summary>Khoá đổi kiểu, dạng "khoá: kiểu cũ → kiểu mới".</summary>
        public List<string> TypeChanged { get; set; } = [];

        /// <summary>Hồ sơ ánh xạ chiều gửi đang dùng khoá vừa mất.</summary>
        public List<SqlFieldMappingUsage> AffectedMappings { get; set; } = [];
    }
}
