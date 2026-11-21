using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.WorkExecuteHistory.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin đầu ra
    /// Created date: 20/11/2024
    /// </summary>
    public class TmsWorkExecuteHistoryOutput : TmsWorkExecuteHistory
    {
        public List<object> WorkContactList { get; set; }
    }
}
