/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Lab.Domain.Entities;

namespace Lab.Domain.Repositories
{
    /// <summary>
    /// Description: Interface thao tác dữ liệu người dùng (User Repository Contract)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public interface Icls_UserRepository
    {
        /// <summary>
        /// Description: Lấy toàn bộ danh sách người dùng từ DB
        /// </summary>
        /// <returns>Danh sách thực thể cls_User</returns>
        List<cls_User> fn_GetAllUsers();

        /// <summary>
        /// Description: Lấy thông tin người dùng theo Id
        /// </summary>
        /// <param name="intId">Mã định danh người dùng</param>
        /// <returns>Thực thể cls_User hoặc null nếu không tìm thấy</returns>
        cls_User? fn_GetUserById(int intId);

        /// <summary>
        /// Description: Thêm mới một người dùng vào DB
        /// </summary>
        /// <param name="objUser">Thực thể người dùng</param>
        /// <returns>Số dòng bị tác động</returns>
        int fn_AddUser(cls_User objUser);

        /// <summary>
        /// Description: Xóa một người dùng theo Id
        /// </summary>
        /// <param name="intId">Mã định danh người dùng</param>
        /// <returns>Số dòng bị tác động</returns>
        int fn_DeleteUser(int intId);
    }
}
