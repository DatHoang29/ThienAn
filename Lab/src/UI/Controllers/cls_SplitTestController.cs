/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace UI.Controllers
{
    /// <summary>
    /// Description: Controller thuộc tầng UI nhận HTTP/UI Request kiểm thử chia bảng (SplitTable) theo DDD
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    [ApiController]
    [Route("api/SplitTest")]
    public class cls_SplitTestController : ControllerBase
    {
        /// <summary>
        /// Description: Application Service xử lý nghiệp vụ chia bảng
        /// </summary>
        private readonly Icls_SplitTestService vSplitService;

        /// <summary>
        /// Description: Hàm khởi tạo Controller tiếp nhận SplitTestService qua DI
        /// </summary>
        /// <param name="objSplitService">Instance Icls_SplitTestService được inject</param>
        public cls_SplitTestController(Icls_SplitTestService objSplitService)
        {
            vSplitService = objSplitService;
        }

        /// <summary>
        /// Description: Thêm mới bản ghi vào bảng chia theo ngày
        /// </summary>
        /// <param name="strName">Tên dữ liệu nhập vào</param>
        /// <returns>Bản ghi DTO vừa tạo và tên bảng tương ứng</returns>
        [HttpPost]
        public IActionResult fn_InsertData([FromQuery] string strName)
        {
            try
            {
                var (objDto, strTableName) = vSplitService.fn_InsertSplitData(strName);

                return Ok(new
                {
                    strMessage = "Thêm thành công vào bảng phân chia theo ngày (Pure POCO Domain + UI Layer)!",
                    objData = objDto,
                    strTableName = strTableName
                });
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi khi thêm dữ liệu phân chia: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Truy vấn toàn bộ dữ liệu từ tất cả các bảng phân chia
        /// </summary>
        /// <returns>Danh sách DTO hợp nhất</returns>
        [HttpGet]
        public IActionResult fn_GetAllSplitData()
        {
            try
            {
                var lstData = vSplitService.fn_GetAllSplitData();
                return Ok(lstData);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi khi truy vấn: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Truy vấn dữ liệu phân chia theo khoảng thời gian
        /// </summary>
        /// <param name="dtBegin">Thời gian bắt đầu</param>
        /// <param name="dtEnd">Thời gian kết thúc</param>
        /// <returns>Danh sách DTO phù hợp</returns>
        [HttpGet("by-time-range")]
        public IActionResult fn_GetByTimeRange([FromQuery] DateTime dtBegin, [FromQuery] DateTime dtEnd)
        {
            try
            {
                var lstData = vSplitService.fn_GetByTimeRange(dtBegin, dtEnd);
                return Ok(lstData);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi truy vấn theo thời gian: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Lấy danh sách tên các bảng phân chia thực tế đã sinh ra trong DB
        /// </summary>
        /// <returns>Danh sách tên bảng</returns>
        [HttpGet("tables")]
        public IActionResult fn_GetCreatedTables()
        {
            try
            {
                var lstSplitTables = vSplitService.fn_GetCreatedTables();
                return Ok(lstSplitTables);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi lấy danh sách bảng: {objEx.Message}");
            }
        }
    }
}
