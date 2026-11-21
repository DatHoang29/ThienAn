using Shared.Infrastructure.Controllers;

namespace Modules.Media.Controllers
{
    /// <summary>
    /// Description: Controller cơ sở của phân hệ Media (ITS-Media + WP-MediaPublic).
    /// Created date: 08/10/2026
    /// </summary>
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/media";

        protected internal new const string GroupName = "Media";
    }
}
