using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.System
{
    public interface IZoneApi : IBaseApi
    {
        [Get(BaseEndpoint.TmsEp.Zone.Base + "/list")]
        Task<ApiResponse<WResult<List<TmsZoneResponse>>>> List(TmsZoneListRequest query);

        [Get(BaseEndpoint.TmsEp.Zone.Base + "/gettreewithchild")]
        Task<ApiResponse<WResult<List<TmsZoneResponse>>>> TreeWithChild(TmsZoneWithChildrenRequest query);
    }
}
