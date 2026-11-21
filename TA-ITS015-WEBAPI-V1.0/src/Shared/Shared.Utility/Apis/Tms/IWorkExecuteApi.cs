using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Tms
{
    public interface IWorkExecuteApi : IBaseApi
    {
        [Post(BaseEndpoint.TmsEp.WorkExecute.Base + "/listworkexecutebyconditions")]
        Task<ApiResponse<WResult<List<TmsWorkExecuteResponse>>>> List(TmsWorkExecuteListRequest query);
    }
}
