using Shared.Infrastructure.Controllers;


namespace Modules.Samplev2.Controllers
{


    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        /// <summary>
        /// Đường dẫn gốc cho module này
        /// </summary>
        protected internal new const string BasePath = "api/sample";

        /// <summary>
        /// Group name cho module này. Sử dụng phân chia trong Swagger 
        /// </summary>
        protected internal new const string GroupName = "Sample";

    }


}
