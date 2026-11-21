using Modules.Report.Core.Dto.InfoConfig;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Report.Controllers.InfoConfig
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Quản lý cấu hình thông tin hiển thị trên báo cáo
    /// Created date: 27/07/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 100)]
    public class RptInfoConfigController : BaseController
    {
        /// <summary>
        /// Danh sách theo phân trang
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageRptInfoConfigOutput>> Page(PageRptInfoConfigInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageRptInfoConfigOutput>>(input);
        }

        /// <summary>
        /// Danh sách theo điều kiện tìm kiếm
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<RptInfoConfigOutput>> GetList([FromQuery] RptInfoConfigInput input)
        {
            return await MessBus.InvokeAsync<List<RptInfoConfigOutput>>(input);
        }

        /// <summary>
        /// Lấy một record theo id hoặc theo bộ khóa ReportCode + GroupCode + Code
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        [HttpPost("Single")]
        [DisplayName("Get single record")]
        public async Task<RptInfoConfigOutput> Single(RptInfoConfigSingleInput input)
        {
            return await MessBus.InvokeAsync<RptInfoConfigOutput>(input);
        }

        /// <summary>
        /// Lấy cấu hình đã gom nhóm để truyền vào file báo cáo.
        /// Kết quả = cấu hình dùng chung ('*') hợp với cấu hình riêng của báo cáo (riêng ghi đè chung).
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        [HttpGet("Data")]
        [DisplayName("Get grouped config data for report render")]
        public async Task<RptInfoConfigDataOutput> GetData([FromQuery] RptInfoConfigDataInput input)
        {
            return await MessBus.InvokeAsync<RptInfoConfigDataOutput>(input);
        }

        /// <summary>
        /// Thêm mới
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddRptInfoConfig(AddRptInfoConfigInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Cập nhật
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateRptInfoConfig(UpdateRptInfoConfigInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Xóa một record
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record")]
        public async Task DeleteRptInfoConfig(DeleteRptInfoConfigInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Xóa nhiều record
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteRptInfoConfig(List<DeleteRptInfoConfigInput> input)
        {
            await MessBus.InvokeAsync(input);
        }
    }
}
