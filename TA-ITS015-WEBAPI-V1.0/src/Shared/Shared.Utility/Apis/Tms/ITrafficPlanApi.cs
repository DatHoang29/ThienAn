using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Tms
{
    public interface ITrafficPlanApi : IBaseApi
    {
        [Get(BaseEndpoint.TmsEp.TrafficPlan.Base + "/list")]
        Task<ApiResponse<WResult<List<TmsTrafficPlanResponse>>>> List(TmsTrafficPlanListRequest query);
    }
}
