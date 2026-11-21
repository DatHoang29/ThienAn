using Shared.Infrastructure.Controllers;

namespace Modules.Template.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public  class BaseController : CommonBaseController,ITransient
    {
        protected internal new const string BasePath = "api/sample";
       
        protected internal new const string GroupName = "Sample";

    }
    
  
}
