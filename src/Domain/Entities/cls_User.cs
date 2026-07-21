/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using SqlSugar;

namespace Domain.Entities
{
    /// <summary>
    /// Description: Thực thể đại diện cho bảng User trong cơ sở dữ liệu
    /// Author: Antigravity
    /// Create Date: 17/07/2026
    /// </summary>
    [SugarTable("tbl_User")]
    public class cls_User
    {
        /// <summary>
        /// Description: Định danh duy nhất của người dùng (Khóa chính, tự tăng)
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "Id")]
        public int intId { get; set; }

        /// <summary>
        /// Description: Tên đăng nhập của người dùng
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        [SugarColumn(ColumnName = "UserName", Length = 50, IsNullable = false)]
        public string strUserName { get; set; } = null!;

        /// <summary>
        /// Description: Địa chỉ Email của người dùng
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        [SugarColumn(ColumnName = "Email", Length = 100, IsNullable = true)]
        public string? strEmail { get; set; }

        /// <summary>
        /// Description: Ngày tạo tài khoản
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        [SugarColumn(ColumnName = "CreateDate", IsNullable = true)]
        public DateTime? dtCreateDate { get; set; }
    }
}
