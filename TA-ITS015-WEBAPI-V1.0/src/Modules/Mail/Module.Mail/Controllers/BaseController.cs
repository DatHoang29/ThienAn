using Shared.Infrastructure.Controllers;

namespace Modules.Mail.Controllers
{
    /// <summary>
    /// Description: Base controller của phân hệ Mail. Phân hệ gồm hai phần: hạ tầng gửi thư dùng chung
    ///              (Smtp, Templates, cấu hình) và tính năng gửi báo cáo theo lịch (RptEmail*, Reports).
    /// Created date: 2026-08-13
    /// </summary>
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/mail";
        protected internal new const string GroupName = "Mail";
    }
}
