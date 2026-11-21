using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Tms
{
    public interface IIncidentApi : IBaseApi
    {
        [Post(BaseEndpoint.TmsEp.Incident.Base + "/list")]
        Task<ApiResponse<WResult<List<TmsIncidentResponse>>>> List(TmsIncidentListRequest query);

        [Post(BaseEndpoint.TmsEp.Incident.Base + "/single")]
        Task<ApiResponse<WResult<TmsIncidentResponse>>> Single(TmsIncidentSingleRequest query);
    }
}
