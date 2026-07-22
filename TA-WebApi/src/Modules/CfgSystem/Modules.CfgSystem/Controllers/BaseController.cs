using Furion.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Controllers;

namespace Modules.CfgSystem.Controllers
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>

    [ApiController]
    [Route($"{BasePath}/[Controller]")]
    [ApiDescriptionSettings(GroupName = GroupName)]
    public class BaseController: CommonBaseController, ITransient
    {
        protected internal new const string GroupName = "CfgSystem";
        protected internal new const string BasePath = "Api/CfgSystem";
    }
}
