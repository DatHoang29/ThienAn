using Refit;
using Shared.DTO.Dtos.System;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.System
{
    public interface IOpConfigApi : IBaseApi
    {
        [Get(BaseEndpoint.SystemEp.OpConfig.Base + "/list")]
        Task<ApiResponse<WResult<List<SysOpConfigResponse>>>> List(SysOpConfigListRequest query);
    }
}
