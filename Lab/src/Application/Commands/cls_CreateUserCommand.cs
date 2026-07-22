/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Application.Commands
{
    /// <summary>
    /// Description: Mệnh lệnh khởi tạo người dùng (Create User Command DTO - Wolverine Mediator)
    /// Author: Antigravity
    /// Create Date: 22/07/2026
    /// </summary>
    /// <param name="strUserName">Tên người dùng</param>
    /// <param name="strEmail">Địa chỉ Email</param>
    public record cls_CreateUserCommand(string strUserName, string? strEmail);
}
