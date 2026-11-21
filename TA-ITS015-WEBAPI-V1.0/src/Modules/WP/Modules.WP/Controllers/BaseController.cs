using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Furion;
using Furion.DependencyInjection;
using Furion.DynamicApiController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Controllers;
using Wolverine;


namespace Modules.WP.Controllers
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller cơ sở của module WP (WebPortal), định nghĩa BasePath và GroupName chung
    /// Created date: 03/07/2026
    /// </summary>
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public  class BaseController : CommonBaseController,ITransient
    {
        protected internal new const string BasePath = "api/wp";
       
        protected internal new const string GroupName = "WP";

    }
    
  
}
