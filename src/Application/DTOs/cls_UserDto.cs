/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Application.DTOs
{
    /// <summary>
    /// Description: DTO phản hồi thông tin người dùng được Mapster ánh xạ từ Entity cls_User
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_UserDto
    {
        /// <summary>
        /// Description: Định danh khóa chính người dùng
        /// </summary>
        public int vId { get; set; }

        /// <summary>
        /// Description: Tên đăng nhập người dùng
        /// </summary>
        public string vUserName { get; set; } = null!;

        /// <summary>
        /// Description: Địa chỉ Email
        /// </summary>
        public string? vEmail { get; set; }

        /// <summary>
        /// Description: Ngày tạo định dạng hiển thị đẹp (dd/MM/yyyy HH:mm:ss)
        /// </summary>
        public string? vFormattedCreateDate { get; set; }
    }
}
