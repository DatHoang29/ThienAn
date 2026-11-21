using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Tms
{
    public interface IEquipmentTypeApi : IBaseApi
    {
        [Get(BaseEndpoint.TmsEp.EquipmentType.Base + "/list")]
        Task<ApiResponse<WResult<List<TmsEquipmentTypeResponse>>>> List(TmsEquipmentTypeListRequest query);
    }
}
