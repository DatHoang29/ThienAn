/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Microsoft.AspNetCore.Mvc;
using Mapster;
using Lab.WebApi.Entities;
using Lab.WebApi.Infrastructure;

namespace Lab.WebApi.Controllers
{
    /// <summary>
    /// Description: API Controller xử lý các yêu cầu thao tác dữ liệu người dùng (User)
    /// Author: Antigravity
    /// Create Date: 17/07/2026
    /// </summary>
    [ApiController]
    [Route("api/User")]
    public class cls_UserController : ControllerBase
    {
        /// <summary>
        /// Description: Instance dịch vụ cơ sở dữ liệu SqlSugar
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        private readonly cls_SqlSugarDb vSqlDb;

        /// <summary>
        /// Description: Hàm khởi tạo UserController
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        /// <param name="objSqlDb">Instance dịch vụ database SqlSugar được inject vào</param>
        public cls_UserController(cls_SqlSugarDb objSqlDb)
        {
            vSqlDb = objSqlDb;
        }

        /// <summary>
        /// Description: Khởi tạo database và đồng bộ các bảng (Code First) từ Entity
        /// Author: Antigravity
        /// Create Date: 17/07/2026
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
        /// Description: Lấy toàn bộ danh sách người dùng trong hệ thống
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        /// <returns>Danh sách thực thể người dùng</returns>
        [HttpGet]
        public IActionResult fn_GetAllUsers()
        {
            try
            {
                var lstUsers = vSqlDb.vClient.Queryable<cls_User>().ToList();
                return Ok(lstUsers);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Thêm mới một người dùng vào cơ sở dữ liệu
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        /// <param name="objUser">Thông tin người dùng muốn thêm</param>
        /// <returns>Kết quả thêm mới thành công</returns>
        [HttpPost]
        public IActionResult fn_AddUser([FromBody] cls_User objUser)
        {
            try
            {
                if (objUser == null)
                {
                    return BadRequest("Dữ liệu người dùng trống!");
                }

                objUser.dtCreateDate = DateTime.Now;
                var intInsertCount = vSqlDb.vClient.Insertable(objUser).ExecuteCommand();
                return Ok($"Đã thêm thành công {intInsertCount} người dùng!");
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Xóa một người dùng theo Id định danh
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        /// <param name="intId">Định danh của người dùng cần xóa</param>
        /// <returns>Kết quả xóa thành công</returns>
        [HttpDelete("{intId}")]
        public IActionResult fn_DeleteUser(int intId)
        {
            try
            {
                var intDeleteCount = vSqlDb.vClient.Deleteable<cls_User>().In(intId).ExecuteCommand();
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
        /// Description: Truy vấn danh sách người dùng từ DB và sử dụng Mapster để ánh xạ sang DTO (cls_UserDto)
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <returns>Danh sách DTO người dùng đã được định dạng bởi Mapster</returns>
        [HttpGet("mapped-list")]
        public IActionResult fn_GetMappedUsers()
        {
            try
            {
                // 1. Truy vấn dữ liệu thực thể từ DB qua SqlSugar
                var lstUserEntities = vSqlDb.vClient.Queryable<cls_User>().ToList();

                // 2. Sử dụng Mapster để chuyển đổi (Adapt) từ List<cls_User> sang List<cls_UserDto>
                var lstUserDtos = lstUserEntities.Adapt<List<Lab.WebApi.DTOs.cls_UserDto>>();

                return Ok(lstUserDtos);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi khi ánh xạ bằng Mapster: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Endpoint thử nghiệm nhận Entity truyền vào và sử dụng Mapster ánh xạ trực tiếp sang DTO để kiểm tra kết quả trên Swagger UI
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <param name="objUserEntity">Thực thể người dùng đầu vào</param>
        /// <returns>Đối tượng DTO sau khi được Mapster ánh xạ</returns>
        [HttpPost("test-mapster")]
        public IActionResult fn_TestMapsterEntityIntoDto([FromBody] cls_User objUserEntity)
        {
            try
            {
                if (objUserEntity == null)
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ!");
                }

                if (!objUserEntity.dtCreateDate.HasValue)
                {
                    objUserEntity.dtCreateDate = DateTime.Now;
                }

                // Thực hiện ánh xạ Mapster từ Entity -> DTO
                var objMappedDto = objUserEntity.Adapt<Lab.WebApi.DTOs.cls_UserDto>();

                return Ok(new
                {
                    strNote = "Mapster đã ánh xạ thành công từ Entity (cls_User) sang DTO (cls_UserDto)",
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
