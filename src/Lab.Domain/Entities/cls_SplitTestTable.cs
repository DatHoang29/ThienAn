/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using SqlSugar;

namespace Lab.Domain.Entities
{
    /// <summary>
    /// Description: Thực thể kiểm thử chia bảng tự động (Table Splitting) theo ngày
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    [SplitTable(SplitType.Day)]
    [SugarTable("tbl_SplitTestTable_{year}{month}{day}")]
    public class cls_SplitTestTable
    {
        /// <summary>
        /// Description: Mã định danh khóa chính (Dùng Snowflake ID, không dùng tự tăng int)
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, ColumnName = "Id")]
        public long vId { get; set; }

        /// <summary>
        /// Description: Tên dữ liệu mẫu
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        [SugarColumn(ColumnName = "Name", Length = 100, IsNullable = true)]
        public string? vName { get; set; }

        /// <summary>
        /// Description: Thời gian tạo (Trường phân chia bảng - SplitField)
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        [SplitField]
        [SugarColumn(ColumnName = "CreateTime")]
        public DateTime vCreateTime { get; set; }
    }
}
