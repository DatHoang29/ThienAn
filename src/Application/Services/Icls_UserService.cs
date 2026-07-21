/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Application.DTOs;
using Domain.Entities;

namespace Application.Services
{
    /// <summary>
    /// Description: Service Interface xử lý các Use Case nghiệp vụ liên quan đến Người dùng
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public interface Icls_UserService
    {
        /// <summary>
        /// Description: Lấy danh sách toàn bộ người dùng đã được ánh xạ sang DTO
        /// </summary>
        /// <returns>Danh sách DTO người dùng</returns>
        List<cls_UserDto> fn_GetAllUsers();

        /// <summary>
        /// Description: Lấy thông tin người dùng DTO theo Id
        /// </summary>
        /// <param name="intId">Mã người dùng</param>
        /// <returns>DTO người dùng</returns>
        cls_UserDto? fn_GetUserById(int intId);

        /// <summary>
        /// Description: Thêm mới người dùng vào hệ thống
        /// </summary>
        /// <param name="objUser">Thực thể người dùng</param>
        /// <returns>Số bản ghi thêm mới thành công</returns>
        int fn_AddUser(cls_User objUser);

        /// <summary>
        /// Description: Xóa người dùng khỏi hệ thống theo Id
        /// </summary>
        /// <param name="intId">Mã người dùng</param>
        /// <returns>Số bản ghi xóa thành công</returns>
        int fn_DeleteUser(int intId);

        /// <summary>
        /// Description: Thử nghiệm ánh xạ từ Entity sang DTO cho kiểm thử
        /// </summary>
        /// <param name="objUser">Thực thể người dùng</param>
        /// <returns>DTO người dùng</returns>
        cls_UserDto fn_MapToDto(cls_User objUser);
    }
}
