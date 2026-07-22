/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Application.Commands
{
    /// <summary>
    /// Description: Mệnh lệnh xóa người dùng theo ID (Delete User Command DTO - Wolverine Mediator)
    /// Author: Antigravity
    /// Create Date: 22/07/2026
    /// </summary>
    /// <param name="intId">Mã định danh người dùng</param>
    public record cls_DeleteUserCommand(int intId);
}
