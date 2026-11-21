using Shared.Infrastructure.Controllers;

namespace Modules.CfgSystem.Controllers
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// Description: Base controller
    /// </summary>

    [ApiController]
    [Route($"{BasePath}/[Controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string GroupName = "CfgSystem";
        protected internal new const string BasePath = "Api/CfgSystem";
    }
}
