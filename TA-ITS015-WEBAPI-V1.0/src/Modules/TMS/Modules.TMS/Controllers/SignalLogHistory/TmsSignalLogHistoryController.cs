using Modules.TMS.Controllers.SignalLogHistory.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.SignalLogHistory
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/02/2025
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsSignalLogHistoryController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 01/02/2025
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageSignalLogHistoryOutput>> Page(PageSignalLogHistoryInput input)
        {
            return await this.MessBus.InvokeAsync<SqlSugarPagedList<PageSignalLogHistoryOutput>>(input);
        }
    }
}
