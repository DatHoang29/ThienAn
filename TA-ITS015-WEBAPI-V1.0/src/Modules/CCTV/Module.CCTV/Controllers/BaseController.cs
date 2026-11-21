using Shared.Infrastructure.Controllers;
using Wolverine;

namespace Modules.CCTV.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public  class BaseController : CommonBaseController,ITransient
    {
        protected internal new const string BasePath = "api/cctv";

        protected internal new const string GroupName = "CCTV";
    }
}
