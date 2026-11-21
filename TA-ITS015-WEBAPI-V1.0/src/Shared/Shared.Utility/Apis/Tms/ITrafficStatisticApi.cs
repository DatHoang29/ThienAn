using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Tms
{
    public interface ITrafficStatisticApi : IBaseApi
    {
        [Post(BaseEndpoint.TmsEp.TrafficStatistic.Base + "/listByDate")]
        Task<ApiResponse<WResult<List<TmsTrafficStatisticResponse>>>> ListByDate(TmsTrafficStatisticRequest query);
    }
}
