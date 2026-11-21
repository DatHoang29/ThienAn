using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.WorkExecute.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin đầu ra
    /// Created date: 20/11/2024
    /// </summary>
    public class TmsWorkExecuteOutput : TmsWorkExecute
    {
        public List<object> WorkContactList { get; set; }
    }
}
