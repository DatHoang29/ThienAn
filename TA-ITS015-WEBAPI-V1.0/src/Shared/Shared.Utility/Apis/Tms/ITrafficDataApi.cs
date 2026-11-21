using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Tms
{
    public interface ITrafficDataApi : IBaseApi
    {
        [Post(BaseEndpoint.TmsEp.TrafficData.Base + "/list")]
        Task<ApiResponse<WResult<List<TmsTrafficDataResponse>>>> List(TmsTrafficDataListRequest query);
    }
}