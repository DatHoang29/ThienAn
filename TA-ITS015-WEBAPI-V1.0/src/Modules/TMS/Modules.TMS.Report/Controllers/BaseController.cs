using Shared.Infrastructure.Controllers;

namespace Modules.TMS.Report.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public  class BaseController : CommonBaseController,ITransient
    {
        protected internal new const string BasePath = "api/tmsreport";
        protected internal new const string GroupName = "TmsReport";
    }
}
