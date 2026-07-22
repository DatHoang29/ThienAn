/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace UI.Controllers
{
    /// <summary>
    /// Description: Controller thuộc tầng UI tiếp nhận HTTP/UI Request quản lý Người dùng sử dụng Wolverine Mediator
    /// Author: Antigravity
    /// Create Date: 17/07/2026
    /// </summary>
    [ApiController]
    [Route("api/User")]
    [cls_AuditLogActionFilter] // Áp dụng Action Filter AOP
    public class cls_UserController : ControllerBase
    {
        /// <summary>
        /// Description: Application Service để truy vấn dữ liệu (Queries - CQRS)
        /// </summary>
        private readonly Icls_UserService vUserService;

        /// <summary>
        /// Description: DB Context để phục vụ api khởi tạo database
        /// </summary>
        private readonly cls_SqlSugarDb vSqlDb;

        /// <summary>
        /// Description: Hàm khởi tạo UserController tiếp nhận Application Service và SqlSugarDb qua DI
        /// </summary>
        /// <param name="objUserService">Instance Icls_UserService được inject cho luồng Query</param>
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
        /// Description: Lấy toàn bộ danh sách người dùng DTO (Luồng Query)
        /// </summary>
        /// <returns>Danh sách DTO người dùng</returns>
        [HttpGet]
        public IActionResult fn_GetAllUsers()
        {
            var lstUserDtos = vUserService.fn_GetAllUsers();
            return Ok(lstUserDtos);
        }

        /// <summary>
        /// Description: Thêm mới một người dùng bằng Wolverine Mediator (Luồng Command - CQRS)
        /// </summary>
        /// <param name="objUser">Thông tin thực thể người dùng nhận từ Body</param>
        /// <param name="bus">IMessageBus của Wolverine được tiêm qua Parameter (Method Injection)</param>
        /// <returns>Kết quả thêm mới thành công</returns>
        [HttpPost]
        [ServiceFilter(typeof(cls_AuthorizationFilter))] // Bộ lọc Auth
        public async Task<IActionResult> fn_AddUser([FromBody] cls_User objUser, [FromServices] IMessageBus bus)
        {
            if (objUser == null)
            {
                return BadRequest("Dữ liệu người dùng không được để trống!");
            }

            // Đóng gói dữ liệu đầu vào thành Mệnh lệnh (Command)
            var command = new cls_CreateUserCommand(objUser.strUserName, objUser.strEmail);

            // Gửi qua Command Bus của Wolverine để tự động chuyển tiếp tới cls_CreateUserHandler xử lý
            var intInsertCount = await bus.InvokeAsync<int>(command);

            return Ok($"[Wolverine Mediator] Đã thêm thành công {intInsertCount} người dùng!");
        }

        /// <summary>
        /// Description: Xóa một người dùng bằng Wolverine Mediator (Luồng Command - CQRS)
        /// </summary>
        /// <param name="intId">Định danh người dùng cần xóa</param>
        /// <param name="bus">IMessageBus của Wolverine được tiêm qua Parameter (Method Injection)</param>
        /// <returns>Kết quả xóa thành công</returns>
        [HttpDelete("{intId}")]
        [ServiceFilter(typeof(cls_AuthorizationFilter))] // Bộ lọc Auth
        public async Task<IActionResult> fn_DeleteUser(int intId, [FromServices] IMessageBus bus)
        {
            // Đóng gói dữ liệu đầu vào thành Mệnh lệnh xóa (Command)
            var command = new cls_DeleteUserCommand(intId);

            // Gửi qua Command Bus của Wolverine để tự động chuyển tiếp tới cls_DeleteUserHandler xử lý
            var intDeleteCount = await bus.InvokeAsync<int>(command);

            if (intDeleteCount > 0)
            {
                return Ok($"[Wolverine Mediator] Đã xóa thành công người dùng có Id {intId}!");
            }
            return NotFound($"[Wolverine Mediator] Không tìm thấy người dùng có Id {intId} để xóa!");
        }

        /// <summary>
        /// Description: Endpoint kiểm thử nhận Entity truyền vào và sử dụng Mapster qua Service ánh xạ sang DTO
        /// </summary>
        /// <param name="objUserEntity">Thực thể người dùng đầu vào</param>
        /// <returns>Đối tượng DTO sau khi được ánh xạ</returns>
        [HttpPost("test-mapster")]
        public IActionResult fn_TestMapster([FromBody] cls_User objUserEntity)
        {
            if (objUserEntity == null)
            {
                return BadRequest("Dữ liệu đầu vào không hợp lệ!");
            }

            var objMappedDto = vUserService.fn_MapToDto(objUserEntity);

            return Ok(new
            {
                strNote = "Đã ánh xạ thành công từ Entity (cls_User Pure POCO) sang DTO (cls_UserDto) qua tầng Application Service!",
                objOriginalEntity = objUserEntity,
                objMappedDto = objMappedDto
            });
        }

        /// <summary>
        /// Description: Endpoint kiểm thử Exception Filter toàn cục (Cố tình tung ra ngoại lệ Exception)
        /// </summary>
        /// <returns>Sẽ được cls_GlobalExceptionFilter bắt lại và format JSON chuẩn</returns>
        [HttpGet("test-exception")]
        public IActionResult fn_TestException()
        {
            throw new InvalidOperationException("Lỗi thử nghiệm Exception Filter AOP!");
        }
    }
}
