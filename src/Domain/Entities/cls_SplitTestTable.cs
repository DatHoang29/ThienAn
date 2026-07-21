/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Domain.Entities
{
    /// <summary>
    /// Description: Thực thể kiểm thử chia bảng thuần túy (Pure POCO Domain Entity - Không dính líu ORM/DB)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_SplitTestTable
    {
        /// <summary>
        /// Description: Mã định danh khóa chính (Snowflake ID)
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        public long vId { get; set; }

        /// <summary>
        /// Description: Tên dữ liệu mẫu
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        public string? vName { get; set; }

        /// <summary>
        /// Description: Thời gian tạo (Trường dùng để phân chia bảng)
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        public DateTime vCreateTime { get; set; }
    }
}
