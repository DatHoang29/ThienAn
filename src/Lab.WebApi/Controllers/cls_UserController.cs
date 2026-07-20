/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Microsoft.AspNetCore.Mvc;
using MapsterMapper;
using Lab.WebApi.Entities;
using Lab.WebApi.Infrastructure;
using Lab.WebApi.DTOs;

namespace Lab.WebApi.Controllers
{
    /// <summary>
    /// Description: API Controller xử lý các yêu cầu thao tác dữ liệu người dùng (User) tích hợp Mapster DI
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
        /// Description: Instance IMapper được tiêm (inject) từ DI Container
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        private readonly IMapper vMapper;

        /// <summary>
        /// Description: Hàm khởi tạo UserController tiếp nhận IMapper qua Dependency Injection
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        /// <param name="objSqlDb">Instance dịch vụ database SqlSugar</param>
        /// <param name="objMapper">Instance IMapper được tiêm từ DI Container</param>
        public cls_UserController(cls_SqlSugarDb objSqlDb, IMapper objMapper)
        {
            vSqlDb = objSqlDb;
            vMapper = objMapper;
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
        /// Description: Lấy toàn bộ danh sách người dùng trong hệ thống và tự động Map sang DTO qua IMapper DI
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        /// <returns>Danh sách DTO người dùng</returns>
        [HttpGet]
        public IActionResult fn_GetAllUsers()
        {
            try
            {
                var lstUserEntities = vSqlDb.vClient.Queryable<cls_User>().ToList();
                
                // Ánh xạ sang List DTO sử dụng dịch vụ IMapper được inject
                var lstUserDtos = vMapper.Map<List<cls_UserDto>>(lstUserEntities);

                return Ok(lstUserDtos);
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
        /// Description: Endpoint kiểm thử nhận Entity truyền vào và sử dụng IMapper DI ánh xạ trực tiếp sang DTO
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <param name="objUserEntity">Thực thể người dùng đầu vào</param>
        /// <returns>Đối tượng DTO sau khi được IMapper DI ánh xạ</returns>
        [HttpPost("test-mapster")]
        public IActionResult fn_TestMapster([FromBody] cls_User objUserEntity)
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

                // Cú pháp Mapster chuẩn sử dụng IMapper DI Container
                var objMappedDto = vMapper.Map<cls_UserDto>(objUserEntity);

                return Ok(new
                {
                    strNote = "Đã ánh xạ thành công từ Entity (cls_User) sang DTO (cls_UserDto) bằng IMapper qua DI!",
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
