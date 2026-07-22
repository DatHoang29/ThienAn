using Refit;
using Shared.DTO.Dtos.System;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.System
{
    public interface IConfigTypeApi : IBaseApi
    {
        [Get(BaseEndpoint.SystemEp.ConfigType.Base + "/list")]
        Task<ApiResponse<WResult<List<SysConfigTypeResponse>>>> List(SysConfigTypeListRequest query);
    }
}
