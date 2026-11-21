
namespace Module.ShareData.Core.Dto.Metadata
{
    /// <summary>
    /// Author: DatHQ
    /// Description: DTO thông tin bảng nguồn CSDL cho phép trích xuất chia sẻ dữ liệu kèm danh sách mã gói tin liên kết.
    /// Created date: 14/10/2026
    /// Modified date: 18/10/2026
    /// </summary>
    public class MetadataTableDto
    {
        /// <summary>
        /// Mã định danh dòng danh mục bảng nguồn (ShareDataSourceTable.ID), dùng thay tên bảng khi gọi lấy danh sách cột.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Tên bảng trong CSDL (ví dụ: TmsZone, TmsTrafficData, TmsIncident).
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Tên hiển thị thân thiện tiếng Việt.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả mục đích nghiệp vụ và các gói tin liên quan (P101-P110).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Bí danh (alias) mặc định đề xuất khi sinh câu truy vấn SQL (ví dụ: z, td, i, w, v).
        /// </summary>
        public string? DefaultAlias { get; set; }

        /// <summary>
        /// Danh sách mã gói tin gắn với bảng nguồn này (lấy từ ShareDataSourceTable.PacketCodes).
        /// </summary>
        public List<string> PacketCodes { get; set; } = [];
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: DTO thông tin chi tiết từng cột của bảng nguồn CSDL.
    /// Created date: 14/10/2026
    /// </summary>
    public class MetadataFieldDto
    {
        /// <summary>
        /// Tên cột trong CSDL (ví dụ: ZoneId, AverageSpeed, DetectTime).
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Kiểu dữ liệu trong CSDL (ví dụ: nvarchar, int, datetime, decimal, float).
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Tên hiển thị tiếng Việt của trường dữ liệu.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả chi tiết hoặc ghi chú cách lấy dữ liệu (Join, tính toán, định dạng).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Tên trường đề xuất (alias camelCase) khi trả về payload JSON (ví dụ: zoneId, averageSpeed).
        /// </summary>
        public string? SuggestedAlias { get; set; }

        /// <summary>
        /// Cột có cho phép giá trị NULL không.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Cột có phải là khóa chính (Primary Key) không.
        /// </summary>
        public bool IsPrimaryKey { get; set; }
    }

    /// <summary>
    /// Input lấy danh mục bảng nguồn chia sẻ dữ liệu.
    /// </summary>
    public class GetMetadataTablesInput
    {
    }

    /// <summary>
    /// Input lấy danh sách cột của một bảng nguồn cụ thể.
    /// </summary>
    public class GetMetadataFieldsInput
    {
        /// <summary>
        /// Mã định danh bảng (MetadataTableDto.Id) cần lấy danh mục cột. Bắt buộc thuộc danh mục bảng cho phép.
        /// </summary>
        public string Id { get; set; } = string.Empty;
    }
}
