/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Infrastructure.Persistence;

namespace WebApi.Controllers
{
    /// <summary>
    /// Description: API Controller tiếp nhận request quản lý Người dùng và điều phối xuống Application Service
    /// Author: Antigravity
    /// Create Date: 17/07/2026
    /// </summary>
    [ApiController]
    [Route("api/User")]
    public class cls_UserController : ControllerBase
    {
        /// <summary>
        /// Description: Application Service xử lý nghiệp vụ Người dùng
        /// </summary>
        private readonly Icls_UserService vUserService;

        /// <summary>
        /// Description: DB Context để phục vụ api khởi tạo database
        /// </summary>
        private readonly cls_SqlSugarDb vSqlDb;

        /// <summary>
        /// Description: Hàm khởi tạo UserController tiếp nhận Application Service và SqlSugarDb qua DI
        /// </summary>
        /// <param name="objUserService">Instance Icls_UserService được inject</param>
        /// <param name="objSqlDb">Instance DB Context được inject</param>
        public cls_UserController(Icls_UserService objUserService, cls_SqlSugarDb objSqlDb)
        {
            vUserService = objUserService;
            vSqlDb = objSqlDb;
        }

        /// <summary>
        /// Description: Khởi tạo database và đồng bộ các bảng (Code First) từ Entity
        /// </summary>
        /// <returns>Thông báo kết quả thực thi</returns>
        [HttpPost("init-db")]
        public IActionResult fn_InitDb()
        {
            try
            {
                vSqlDb.fn_InitDb();
                return Ok("Khởi tạo Database và các Bảng thành công!");
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi khi khởi tạo: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Lấy toàn bộ danh sách người dùng DTO
        /// </summary>
        /// <returns>Danh sách DTO người dùng</returns>
        [HttpGet]
        public IActionResult fn_GetAllUsers()
        {
            try
            {
                var lstUserDtos = vUserService.fn_GetAllUsers();
                return Ok(lstUserDtos);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Thêm mới một người dùng vào hệ thống
        /// </summary>
        /// <param name="objUser">Thông tin thực thể người dùng</param>
        /// <returns>Kết quả thêm mới thành công</returns>
        [HttpPost]
        public IActionResult fn_AddUser([FromBody] cls_User objUser)
        {
            try
            {
                if (objUser == null)
                {
                    return BadRequest("Dữ liệu người dùng không được để trống!");
                }

                var intInsertCount = vUserService.fn_AddUser(objUser);
                return Ok($"Đã thêm thành công {intInsertCount} người dùng!");
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Xóa một người dùng theo Id định danh
        /// </summary>
        /// <param name="intId">Định danh người dùng cần xóa</param>
        /// <returns>Kết quả xóa thành công</returns>
        [HttpDelete("{intId}")]
        public IActionResult fn_DeleteUser(int intId)
        {
            try
            {
                var intDeleteCount = vUserService.fn_DeleteUser(intId);
                if (intDeleteCount > 0)
                {
                    return Ok($"Đã xóa thành công người dùng có Id {intId}!");
                }
                return NotFound($"Không tìm thấy người dùng có Id {intId} để xóa!");
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Endpoint kiểm thử nhận Entity truyền vào và sử dụng Mapster qua Service ánh xạ sang DTO
        /// </summary>
        /// <param name="objUserEntity">Thực thể người dùng đầu vào</param>
        /// <returns>Đối tượng DTO sau khi được ánh xạ</returns>
        [HttpPost("test-mapster")]
        public IActionResult fn_TestMapster([FromBody] cls_User objUserEntity)
        {
            try
            {
                if (objUserEntity == null)
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ!");
                }

                var objMappedDto = vUserService.fn_MapToDto(objUserEntity);

                return Ok(new
                {
                    strNote = "Đã ánh xạ thành công từ Entity (cls_User) sang DTO (cls_UserDto) qua tầng Application Service!",
                    objOriginalEntity = objUserEntity,
                    objMappedDto = objMappedDto
                });
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi ánh xạ Mapster: {objEx.Message}");
            }
        }
    }
}
