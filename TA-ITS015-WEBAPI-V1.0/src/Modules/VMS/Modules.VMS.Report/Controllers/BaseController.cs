using Shared.Infrastructure.Controllers;

namespace Modules.VMS.Report.Controllers
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: BaseController cho phân hệ báo cáo VMS (VmsReport)
    /// Created date: 07/10/2026
    /// </summary>
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/vmsreport";
        protected internal const string GroupName = "VmsReport";
    }
}
