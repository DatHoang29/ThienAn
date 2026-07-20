/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Microsoft.AspNetCore.Mvc;
using Lab.WebApi.Entities;
using Lab.WebApi.Infrastructure;

namespace Lab.WebApi.Controllers
{
    /// <summary>
    /// Description: API Controller kiểm thử tính năng chia bảng (Table Splitting) theo phút bằng SqlSugar
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    [ApiController]
    [Route("api/SplitTest")]
    public class cls_SplitTestController : ControllerBase
    {
        /// <summary>
        /// Description: Instance dịch vụ cơ sở dữ liệu SqlSugar
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        private readonly cls_SqlSugarDb vSqlDb;

        /// <summary>
        /// Description: Hàm khởi tạo Controller
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <param name="objSqlDb">Instance dịch vụ database SqlSugar</param>
        public cls_SplitTestController(cls_SqlSugarDb objSqlDb)
        {
            vSqlDb = objSqlDb;
        }

        /// <summary>
        /// Description: Thêm mới bản ghi vào bảng chia theo phút (SqlSugar sẽ tự động tạo bảng theo phút nếu chưa có)
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <param name="strName">Tên dữ liệu nhập vào</param>
        /// <returns>Bản ghi vừa được tạo cùng với ID Snowflake và tên bảng tương ứng</returns>
        [HttpPost]
        public IActionResult fn_InsertData([FromQuery] string strName)
        {
            try
            {
                var objNewRecord = new cls_SplitTestTable
                {
                    vName = strName,
                    vCreateTime = DateTime.Now
                };

                // SqlSugar tự động sinh Snowflake ID và tự động tạo bảng theo định dạng phút nếu chưa có
                var longGeneratedId = vSqlDb.vClient.Insertable(objNewRecord).SplitTable().ExecuteReturnSnowflakeId();
                objNewRecord.vId = longGeneratedId;

                return Ok(new
                {
                    strMessage = "Thêm thành công vào bảng phân chia theo ngày!",
                    objData = objNewRecord,
                    strTableName = $"tbl_SplitTestTable_{objNewRecord.vCreateTime:yyyyMMdd}"
                });
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi khi thêm dữ liệu phân chia: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Truy vấn toàn bộ dữ liệu từ tất cả các bảng phân chia theo phút đã được tạo
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <returns>Danh sách dữ liệu hợp nhất từ các bảng phân chia</returns>
        [HttpGet]
        public IActionResult fn_GetAllSplitData()
        {
            try
            {
                // Gọi .SplitTable() để SqlSugar tự động UNION ALL tất cả các bảng phân chia
                var lstData = vSqlDb.vClient.Queryable<cls_SplitTestTable>()
                                           .SplitTable()
                                           .OrderBy(it => it.vCreateTime, SqlSugar.OrderByType.Desc)
                                           .ToList();

                return Ok(lstData);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi khi truy vấn: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Truy vấn dữ liệu phân chia theo khoảng thời gian (Tối ưu hóa: chỉ quét các bảng trong khoảng thời gian chỉ định)
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <param name="dtBegin">Thời gian bắt đầu</param>
        /// <param name="dtEnd">Thời gian kết thúc</param>
        /// <returns>Danh sách bản ghi phù hợp</returns>
        [HttpGet("by-time-range")]
        public IActionResult fn_GetByTimeRange([FromQuery] DateTime dtBegin, [FromQuery] DateTime dtEnd)
        {
            try
            {
                // Truy vấn tối ưu chỉ trên các bảng tương ứng với khoảng thời gian dtBegin -> dtEnd
                var lstData = vSqlDb.vClient.Queryable<cls_SplitTestTable>()
                                           .SplitTable(dtBegin, dtEnd)
                                           .Where(it => it.vCreateTime >= dtBegin && it.vCreateTime <= dtEnd)
                                           .ToList();

                return Ok(lstData);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi truy vấn theo thời gian: {objEx.Message}");
            }
        }

        /// <summary>
        /// Description: Lấy danh sách tên các bảng phân chia đã thực tế được tạo trong cơ sở dữ liệu
        /// Author: Antigravity
        /// Create Date: 20/07/2026
        /// </summary>
        /// <returns>Danh sách tên bảng</returns>
        [HttpGet("tables")]
        public IActionResult fn_GetCreatedTables()
        {
            try
            {
                var lstAllTables = vSqlDb.vClient.DbMaintenance.GetTableInfoList();
                var lstSplitTables = lstAllTables.Where(it => it.Name.StartsWith("tbl_SplitTestTable_"))
                                                 .Select(it => it.Name)
                                                 .ToList();

                return Ok(lstSplitTables);
            }
            catch (Exception objEx)
            {
                return BadRequest($"Lỗi lấy danh sách bảng: {objEx.Message}");
            }
        }
    }
}
