using Shared.Infrastructure.Controllers;


namespace Modules.Report.Demo.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/reportdemo";

        protected internal new const string GroupName = "ReportDemo";

    }


}
