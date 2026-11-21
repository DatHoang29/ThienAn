using Refit;
using Shared.DTO.Dtos.System;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.System
{
    public interface ISysAuthApi : IBaseApi
    {
        [Get(BaseEndpoint.SystemEp.Auth.Base + "/userinfo")]
        Task<ApiResponse<WResult<SysAuthDto>>> UserInfo();
    }
}
