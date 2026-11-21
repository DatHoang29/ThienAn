using Refit;
using Shared.DTO.Dtos.CfgSystem.CfgSysTerminology;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.CfgSystem
{
    public interface ICfgSysTerminologyApi : IBaseApi
    {
        [Get(BaseEndpoint.CfgSystemEp.Terminology.Base + "/list")]
        public Task<ApiResponse<WResult<List<CfgSysTerminologyDto>>>> List([Query] CfgSysTerminologyDto query);

    }
}
