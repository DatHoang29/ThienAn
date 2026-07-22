using Refit;
using Shared.DTO.Dtos.System;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.System
{
    public interface ITerminologyApi : IBaseApi
    {
        [Get(BaseEndpoint.SystemEp.Terminology.Base + "/list")]
        Task<ApiResponse<WResult<List<TerminologyOutputDto>>>> List(TerminologyListInputDto query);
    }
}
