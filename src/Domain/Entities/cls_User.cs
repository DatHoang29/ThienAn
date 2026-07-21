/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Domain.Entities
{
    /// <summary>
    /// Description: Thực thể người dùng thuần túy (Pure POCO Domain Entity - Không dính líu ORM/DB)
    /// Author: Antigravity
    /// Create Date: 17/07/2026
    /// </summary>
    public class cls_User
    {
        /// <summary>
        /// Description: Định danh duy nhất của người dùng
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        public int intId { get; set; }

        /// <summary>
        /// Description: Tên đăng nhập của người dùng
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        public string strUserName { get; set; } = null!;

        /// <summary>
        /// Description: Địa chỉ Email của người dùng
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        public string? strEmail { get; set; }

        /// <summary>
        /// Description: Ngày tạo tài khoản
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        public DateTime? dtCreateDate { get; set; }
    }
}
