using Shared.Infrastructure.Controllers;
using Wolverine;

namespace Modules.FMS.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public  class BaseController : CommonBaseController,ITransient
    {
        protected internal new const string BasePath = "api/fms";

        protected internal new const string GroupName = "FMS";
    }
}
