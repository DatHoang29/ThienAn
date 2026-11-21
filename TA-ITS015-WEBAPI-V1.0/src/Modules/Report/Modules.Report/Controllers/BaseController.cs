using Shared.Infrastructure.Controllers;

namespace Modules.Report.Controllers
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Base controller của phân hệ quản lý báo cáo.
    /// Dùng chung BasePath/GroupName với Modules.Report.Base để FE sinh client trong cùng nhóm "Report".
    /// Created date: 27/07/2026
    /// </summary>
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/report";
        protected internal new const string GroupName = "Report";
    }
}
