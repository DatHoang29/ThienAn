using Shared.Infrastructure.Controllers;

namespace Modules.FMS.Report.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public  class BaseController : CommonBaseController,ITransient
    {
        protected internal new const string BasePath = "api/fmsreport";
        protected internal new const string GroupName = "FmsReport";
    }
}
