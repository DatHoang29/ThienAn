using Modules.CfgSystem.Core.Dto.Share;

namespace Modules.CfgSystem.Controllers.Share;

/// <summary>
/// System User Service 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 490)]
public class ShareController : BaseController
{
    /// <summary>
    /// Get user paginated list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get file share"), HttpGet]
    public virtual async Task<FileStreamResult> Get([FromQuery] ShareInput input)
    {
        return await MessBus.InvokeAsync<FileStreamResult>(input);
    }


}