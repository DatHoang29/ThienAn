using Shared.Infrastructure.Controllers;


namespace Modules.TMS.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/tms";
        protected internal new const string GroupName = "TMS";

    }


}
