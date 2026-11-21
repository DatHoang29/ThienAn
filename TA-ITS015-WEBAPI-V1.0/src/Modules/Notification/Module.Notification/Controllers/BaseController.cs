using Shared.Infrastructure.Controllers;

namespace Modules.Notification.Controllers
{
    /// <summary>
    /// Author: SonTH
    /// Description: Base controller của phân hệ Notification.
    ///              Phân hệ chỉ lo hạ tầng thông báo — không biết nghiệp vụ của module nào.
    /// Created date: 17/09/2026
    /// </summary>
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/notify";
        protected internal new const string GroupName = "Notification";
    }
}
