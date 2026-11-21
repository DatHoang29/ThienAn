using Shared.Infrastructure.Controllers;
using Furion;
namespace Modules.VMS.Controllers
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Lớp base controller của module VMS
    /// Created date: 09/08/2024
    /// </summary>

    [ApiController]
    [Route($"{BasePath}/[Controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController: CommonBaseController, ITransient
    {
        protected internal new const string GroupName = "VMS";
        protected internal new const string BasePath = "api/vms";
    }
}
