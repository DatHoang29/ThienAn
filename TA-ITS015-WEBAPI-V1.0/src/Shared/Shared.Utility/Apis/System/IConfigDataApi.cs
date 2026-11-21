using Refit;
using Shared.DTO.Dtos.System;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.System
{
    public interface IConfigDataApi : IBaseApi
    {
        [Get(BaseEndpoint.SystemEp.ConfigData.Base + "/list")]
        Task<ApiResponse<WResult<List<SysConfigDataResponse>>>> List(SysConfigDataListRequest query);
    }
}
