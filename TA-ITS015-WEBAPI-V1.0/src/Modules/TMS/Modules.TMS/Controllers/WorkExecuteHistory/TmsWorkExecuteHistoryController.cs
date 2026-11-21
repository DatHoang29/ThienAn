using Modules.TMS.Controllers.WorkExecuteHistory.Dto;

namespace Modules.TMS.Controllers.WorkExecuteHistory
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsWorkExecuteHistoryController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tham số
        /// Created date: 20/11/2024
        /// </summary>
        [HttpPost("ListByConditions")]
        [DisplayName("Retrieve the work-execute-history list by the conditions")]
        public async Task<List<TmsWorkExecuteHistoryOutput>> ListByConditions(TmsWorkExecuteHistoryParaInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsWorkExecuteHistoryOutput>>(input);
        }
    }
}
