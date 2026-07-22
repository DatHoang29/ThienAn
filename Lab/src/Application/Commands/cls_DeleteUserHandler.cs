/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Application.Commands
{
    /// <summary>
    /// Description: Handler xử lý nghiệp vụ xóa người dùng (Wolverine Handler - 0 boilerplate code)
    /// Author: Antigravity
    /// Create Date: 22/07/2026
    /// </summary>
    public class cls_DeleteUserHandler
    {
        /// <summary>
        /// Description: Phương thức Handle nhận Command xóa và tự động tiêm UserRepository thông qua Method Injection
        /// </summary>
        /// <param name="command">Mệnh lệnh xóa người dùng</param>
        /// <param name="vUserRepository">UserRepository được tiêm tự động từ DI</param>
        /// <returns>Số lượng bản ghi thành công</returns>
        public int Handle(cls_DeleteUserCommand command, Icls_UserRepository vUserRepository)
        {
            return vUserRepository.fn_DeleteUser(command.intId);
        }
    }
}
