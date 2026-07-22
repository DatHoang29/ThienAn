using Refit;
using Shared.DTO.Dtos.System;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.System
{
    public interface IOrgApi : IBaseApi
    {
        [Get(BaseEndpoint.SystemEp.Org.Base + "/list")]
        Task<ApiResponse<WResult<List<SysOrgResponse>>>> List(SysOrgListRequest query);
    }
}
