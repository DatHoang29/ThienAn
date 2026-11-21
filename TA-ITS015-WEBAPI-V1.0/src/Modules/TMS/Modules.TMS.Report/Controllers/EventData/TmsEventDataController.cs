using Modules.TMS.Report.Controllers.EventData.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Report.Controllers.EventData
{
    /// <summary>
    /// API tra cứu dữ liệu sự kiện VDS.
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 461)]
    public class TmsEventDataController : BaseController
    {
        /// <summary>
        /// Lấy danh sách sự kiện từ bảng TmsEventData theo phân trang.
        /// </summary>
        [DisplayName("Get event data pagination list")]
        public async Task<SqlSugarPagedList<PageEventDataOutput>> Page(PageEventDataInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageEventDataOutput>>(input);
        }

        /// <summary>
        /// Lấy danh sách sự kiện từ bảng TmsEventData.
        /// </summary>
        [HttpGet("GetList")]
        [DisplayName("Get event data list")]
        public async Task<ActionResult<List<EventDataOutput>>> GetList([FromQuery] EventDataInput input)
        {
            return await MessBus.InvokeAsync<List<EventDataOutput>>(input);
        }

        /// <summary>
        /// Đọc ảnh sự kiện VDS từ ổ đĩa và trả về Base64 theo chất lượng yêu cầu.
        /// </summary>
        [HttpGet("GetImage")]
        [DisplayName("Get event data image")]
        public async Task<ActionResult<EventDataImageOutput>> GetImage([FromQuery] EventDataImageInput input)
        {
            return await MessBus.InvokeAsync<EventDataImageOutput>(input);
        }
    }
}
