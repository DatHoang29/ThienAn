using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Danh mục bảng nguồn của phân hệ ShareData (giám sát Change Tracking và tra cứu trên bộ dựng truy vấn).
    /// Created date: 23/09/2026
    /// Modified date: 18/10/2026
    /// </summary>
    [SugarTable("ShareDataSourceTable")]
    [SugarIndex("UX_ShareDataSourceTable_TableName", nameof(TableName), OrderByType.Asc, nameof(IsDelete), OrderByType.Asc, isUnique: true)]
    public class ShareDataSourceTable : EntityTenant
    {
        /// <summary>
        /// Tên bảng nguồn trong CSDL (duy nhất khi chưa xóa mềm).
        /// </summary>
        [SugarColumn(ColumnDescription = "Tên bảng nguồn trong CSDL", Length = EntityConst.Length128, IsNullable = true)]
        public string? TableName { get; set; }

        /// <summary>
        /// Tên hiển thị của bảng nguồn.
        /// </summary>
        [SugarColumn(ColumnDescription = "Tên hiển thị của bảng nguồn", Length = EntityConst.Length128, IsNullable = true)]
        public string? Name { get; set; }

        /// <summary>
        /// Danh sách mã gói tin kích hoạt khi bảng có thay đổi (phân tách bằng dấu phẩy).
        /// </summary>
        [SugarColumn(ColumnDescription = "Danh sách mã gói tin kích hoạt (phân tách bằng dấu phẩy)", Length = EntityConst.Length256, IsNullable = true)]
        public string? PacketCodes { get; set; }

        /// <summary>
        /// Bật giám sát SQL Server Change Tracking cho bảng này (true = Bật, false = Tắt).
        /// </summary>
        [SugarColumn(ColumnDescription = "Bật giám sát SQL Server Change Tracking (1 = Bật, 0 = Tắt)", IsNullable = true)]
        public bool? IsTracked { get; set; }

        /// <summary>
        /// Thứ tự sắp xếp hiển thị / xử lý.
        /// </summary>
        [SugarColumn(ColumnDescription = "Thứ tự sắp xếp", IsNullable = true)]
        public int? OrderNo { get; set; }

        /// <summary>
        /// Ghi chú nghiệp vụ của bảng nguồn.
        /// </summary>
        [SugarColumn(ColumnDescription = "Ghi chú nghiệp vụ", Length = EntityConst.Length512, IsNullable = true)]
        public string? Remark { get; set; }

        /// <summary>
        /// Mốc version Change Tracking đã xử lý của bảng (null hoặc âm là chưa khởi tạo).
        /// </summary>
        [SugarColumn(ColumnDescription = "Mốc version Change Tracking đã xử lý của bảng (null hoặc âm = chưa khởi tạo)", IsNullable = true)]
        public long? LastVersion { get; set; }

        /// <summary>
        /// Thời điểm gần nhất phát hiện bảng có thay đổi dữ liệu.
        /// </summary>
        [SugarColumn(ColumnDescription = "Thời điểm gần nhất phát hiện bảng có thay đổi dữ liệu", IsNullable = true)]
        public DateTime? LastChangeTime { get; set; }

        /// <summary>
        /// Mô tả ngắn vấn đề gần nhất của bảng (về null khi bình thường trở lại).
        /// </summary>
        [SugarColumn(ColumnDescription = "Mô tả ngắn vấn đề gần nhất của bảng (null khi bình thường)", Length = EntityConst.Length512, IsNullable = true)]
        public string? LastIssue { get; set; }

        /// <summary>
        /// Cho phép dùng bảng này trong bộ dựng truy vấn và trong câu lệnh SQL của gói tin.
        /// </summary>
        [SugarColumn(ColumnDescription = "Cho phép dùng bảng trong bộ dựng truy vấn và câu lệnh SQL của gói tin (1 = Cho phép, 0 = Chặn)", IsNullable = true)]
        public bool? IsQueryable { get; set; }

        /// <summary>
        /// Bí danh mặc định khi chọn bảng trên bộ dựng truy vấn.
        /// </summary>
        [SugarColumn(ColumnDescription = "Bí danh mặc định khi chọn bảng trên bộ dựng truy vấn", Length = EntityConst.Length32, IsNullable = true)]
        public string? DefaultAlias { get; set; }
    }
}