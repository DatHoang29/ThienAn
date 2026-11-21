using Shared.Infrastructure.Controllers;

namespace Modules.TOLL.Report.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/tollreport";
        protected internal new const string GroupName = "TollReport";
    }
}
