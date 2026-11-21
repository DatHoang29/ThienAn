using Modules.VDS.Infrastructure.Services.Filters.Mvc;
using Shared.Infrastructure.Controllers;


namespace Modules.VDS.Controllers
{
    [Route(BasePath + "/[controller]")]
    [TypeFilter(typeof(VdsExceptionFilter))]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/vds";
        protected internal new const string GroupName = "VDS";

    }


}
