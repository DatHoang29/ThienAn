/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Application.Commands
{
    /// <summary>
    /// Description: Handler xử lý nghiệp vụ tạo người dùng (Wolverine Handler - tích hợp FluentValidation)
    /// Author: Antigravity
    /// Create Date: 22/07/2026
    /// </summary>
    public class cls_CreateUserHandler
    {
        /// <summary>
        /// Description: Phương thức Handle nhận Command, tự động xác thực và tiêm UserRepository
        /// </summary>
        /// <param name="command">Mệnh lệnh tạo người dùng</param>
        /// <param name="vUserRepository">UserRepository được tiêm tự động từ DI</param>
        /// <param name="vValidator">Validator được tiêm tự động từ DI để kiểm tra tính hợp lệ của lệnh</param>
        /// <returns>Số lượng bản ghi thành công</returns>
        public int Handle(cls_CreateUserCommand command, Icls_UserRepository vUserRepository, IValidator<cls_CreateUserCommand> vValidator)
        {
            // 1. Thực hiện xác thực dữ liệu đầu vào, tự động tung ra ValidationException nếu vi phạm luật
            vValidator.ValidateAndThrow(command);

            // 2. Chuyển đổi dữ liệu và thực thi lưu DB
            var objUser = new cls_User
            {
                strUserName = command.strUserName,
                strEmail = command.strEmail,
                dtCreateDate = DateTime.Now
            };

            return vUserRepository.fn_AddUser(objUser);
        }
    }
}
