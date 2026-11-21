using Shared.Infrastructure.Controllers;
using Wolverine;

namespace Module.VideoWall.Controllers
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
        protected internal new const string BasePath = "api/videowall";

        protected internal new const string GroupName = "VideoWall";
    }
}
