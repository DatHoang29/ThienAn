using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Tms
{
    public interface IWorkExecuteHistoryApi : IBaseApi
    {
        [Post(BaseEndpoint.TmsEp.WorkExecuteHistory.Base + "/ListByConditions")]
        Task<ApiResponse<WResult<List<TmsWorkExecuteResponse>>>> List(TmsWorkExecuteHistoryListRequest query);
    }
}
