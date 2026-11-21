using Shared.Infrastructure.Controllers;

namespace Modules.TOLL.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/toll";
        protected internal new const string GroupName = "Toll";
    }
}
