using Shared.Infrastructure.Controllers;

namespace Modules.WP.Report.Controllers
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Bộ điều khiển API cho Base
    /// Created date: 14/08/2026
    /// </summary>
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/wpreport";
        protected internal new const string GroupName = "WpReport";
    }
}
