using Modules.TMS.Core.Dto.WorkExecute;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.WorkExecute
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 20/11/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsWorkExecuteController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái state cho một record
        /// Created date: 20/11/2024
        /// </summary>
        [HttpPost("UpdateState")]
        [DisplayName("Update the state of a record")]
        public async Task<bool> UpdateTmsWorkExecuteState(TmsWorkExecuteStateInput input)
        {
            return await this.MessBus.InvokeAsync<bool>(input);
        }

        // <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái state work-execute thành chờ xử lý
        /// Created date: 20/11/2024
        /// </summary>
        [HttpPost("UpdateWorkExecuteStateToWaiting")]
        [DisplayName("Update the work-execute state to waiting")]
        public async Task<bool> UpdateWorkExecuteStateToWaiting(string workExecuteId, string? remark)
        {
            var input = new TmsWorkExecuteStateInput()
            {
                Id = workExecuteId,
                State = ((int)TmsWorkExecuteStateEnum.Waiting).ToString(),
                Remark = remark
            };

            return await this.MessBus.InvokeAsync<bool>(input);
        }

        // <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái state work-execute thành bỏ qua
        /// Created date: 20/11/2024
        /// </summary>
        [HttpPost("UpdateWorkExecuteStateToSkipped")]
        [DisplayName("Update the work-execute state to skipped")]
        public async Task<bool> UpdateWorkExecuteStateToSkipped(string workExecuteId, string? remark)
        {
            var input = new TmsWorkExecuteStateInput()
            {
                Id = workExecuteId,
                State = ((int)TmsWorkExecuteStateEnum.Skipped).ToString(),
                Remark = remark
            };

            return await this.MessBus.InvokeAsync<bool>(input);
        }

        // <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái state work-execute thành đã xử lý
        /// Created date: 20/11/2024
        /// </summary>
        [HttpPost("UpdateWorkExecuteStateToExecuted")]
        [DisplayName("Update the work-execute state to executed")]
        public async Task<bool> UpdateWorkExecuteStateToExecuted(string workExecuteId, string? remark)
        {
            var input = new TmsWorkExecuteStateInput()
            {
                Id = workExecuteId,
                State = ((int)TmsWorkExecuteStateEnum.Executed).ToString(),
                Remark = remark
            };

            return await this.MessBus.InvokeAsync<bool>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tham số
        /// Created date: 20/11/2024
        /// </summary>
        [HttpPost("ListWorkExecuteByConditions")]
        [DisplayName("Retrieve the work-execute list by the conditions")]
        public async Task<List<TmsWorkExecuteOutput>> ListByConditions(TmsWorkExecuteParaInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsWorkExecuteOutput>>(input);
        }
    }
}
