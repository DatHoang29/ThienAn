/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Description: Lớp cài đặt cụ thể Repository xử lý dữ liệu Người dùng (Kế thừa IScoped để tự động đăng ký Scoped DI)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_UserRepository : Icls_UserRepository, IScoped
    {
        /// <summary>
        /// Description: Instance SqlSugar Database Context
        /// </summary>
        private readonly cls_SqlSugarDb vSqlDb;

        /// <summary>
        /// Description: Hàm khởi tạo UserRepository tiếp nhận DB Context qua DI
        /// </summary>
        /// <param name="objSqlDb">Instance dịch vụ database SqlSugar</param>
        public cls_UserRepository(cls_SqlSugarDb objSqlDb)
        {
            vSqlDb = objSqlDb;
        }

        /// <summary>
        /// Description: Truy vấn toàn bộ danh sách người dùng trong bảng tbl_User
        /// </summary>
        /// <returns>Danh sách thực thể cls_User</returns>
        public List<cls_User> fn_GetAllUsers()
        {
            return vSqlDb.vClient.Queryable<cls_User>().ToList();
        }

        /// <summary>
        /// Description: Truy vấn người dùng theo khóa chính Id
        /// </summary>
        /// <param name="intId">Mã người dùng</param>
        /// <returns>Thực thể cls_User hoặc null</returns>
        public cls_User? fn_GetUserById(int intId)
        {
            return vSqlDb.vClient.Queryable<cls_User>().InSingle(intId);
        }

        /// <summary>
        /// Description: Thêm mới người dùng vào DB
        /// </summary>
        /// <param name="objUser">Thực thể người dùng</param>
        /// <returns>Số bản ghi thành công</returns>
        public int fn_AddUser(cls_User objUser)
        {
            return vSqlDb.vClient.Insertable(objUser).ExecuteCommand();
        }

        /// <summary>
        /// Description: Xóa người dùng theo khóa chính Id
        /// </summary>
        /// <param name="intId">Mã người dùng</param>
        /// <returns>Số bản ghi xóa thành công</returns>
        public int fn_DeleteUser(int intId)
        {
            return vSqlDb.vClient.Deleteable<cls_User>().In(intId).ExecuteCommand();
        }
    }
}
