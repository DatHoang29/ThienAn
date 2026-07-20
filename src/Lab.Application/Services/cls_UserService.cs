/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Mapster;
using Lab.Application.DTOs;
using Lab.Domain.Entities;
using Lab.Domain.Repositories;

namespace Lab.Application.Services
{
    /// <summary>
    /// Description: Lớp cài đặt dịch vụ nghiệp vụ Người dùng (User Application Service)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_UserService : Icls_UserService
    {
        /// <summary>
        /// Description: Instance Repository truy cập dữ liệu người dùng
        /// </summary>
        private readonly Icls_UserRepository vUserRepository;

        /// <summary>
        /// Description: Hàm khởi tạo UserService tiếp nhận Repository qua DI
        /// </summary>
        /// <param name="objUserRepository">Instance Icls_UserRepository được inject</param>
        public cls_UserService(Icls_UserRepository objUserRepository)
        {
            vUserRepository = objUserRepository;
        }

        /// <summary>
        /// Description: Lấy toàn bộ người dùng và sử dụng Mapster để chuyển đổi sang DTO
        /// </summary>
        /// <returns>Danh sách DTO người dùng</returns>
        public List<cls_UserDto> fn_GetAllUsers()
        {
            var lstEntities = vUserRepository.fn_GetAllUsers();
            return lstEntities.Adapt<List<cls_UserDto>>();
        }

        /// <summary>
        /// Description: Lấy DTO người dùng theo Id
        /// </summary>
        /// <param name="intId">Mã người dùng</param>
        /// <returns>DTO người dùng hoặc null</returns>
        public cls_UserDto? fn_GetUserById(int intId)
        {
            var objEntity = vUserRepository.fn_GetUserById(intId);
            return objEntity == null ? null : objEntity.Adapt<cls_UserDto>();
        }

        /// <summary>
        /// Description: Xử lý nghiệp vụ thêm người dùng
        /// </summary>
        /// <param name="objUser">Thực thể người dùng</param>
        /// <returns>Số bản ghi thành công</returns>
        public int fn_AddUser(cls_User objUser)
        {
            if (objUser == null)
            {
                throw new ArgumentNullException(nameof(objUser), "Dữ liệu người dùng không được để trống!");
            }

            objUser.dtCreateDate = DateTime.Now;
            return vUserRepository.fn_AddUser(objUser);
        }

        /// <summary>
        /// Description: Xử lý nghiệp vụ xóa người dùng
        /// </summary>
        /// <param name="intId">Mã người dùng</param>
        /// <returns>Số bản ghi thành công</returns>
        public int fn_DeleteUser(int intId)
        {
            return vUserRepository.fn_DeleteUser(intId);
        }

        /// <summary>
        /// Description: Thử nghiệm chuyển đổi Entity sang DTO bằng Mapster
        /// </summary>
        /// <param name="objUser">Thực thể người dùng</param>
        /// <returns>DTO người dùng</returns>
        public cls_UserDto fn_MapToDto(cls_User objUser)
        {
            if (!objUser.dtCreateDate.HasValue)
            {
                objUser.dtCreateDate = DateTime.Now;
            }

            return objUser.Adapt<cls_UserDto>();
        }
    }
}
