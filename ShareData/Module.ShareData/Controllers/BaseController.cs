using Shared.Infrastructure.Controllers;
using Wolverine;

namespace Module.ShareData.Controllers
{
    /// <summary>
    /// Description: Lớp base controller của phân hệ Chia sẻ dữ liệu (ShareData)
    /// Created date: 2026-08-04
    /// </summary>
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/sharedata";

        protected internal new const string GroupName = "ShareData";
    }
}
