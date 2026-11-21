using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Furion.DependencyInjection;
using Furion.DynamicApiController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Controllers;
using Wolverine;


namespace Modules.Chainzones.ITS.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public  class BaseController : CommonBaseController,ITransient
    {
        protected internal new const string BasePath = "api/itsVmsControl";

        protected internal new const string GroupName = "ItsVmsControl";

    }
    
  
}
