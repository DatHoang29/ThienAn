using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Tms
{
    public interface IEquipmentApi : IBaseApi
    {
        [Get(BaseEndpoint.TmsEp.Equipment.Base + "/list")]
        Task<ApiResponse<WResult<List<TmsEquipmentResponse>>>> List(TmsEquipmentListRequest query);

        [Post(BaseEndpoint.TmsEp.Equipment.Base + "/listByPosition")]
        Task<ApiResponse<WResult<List<TmsEquipmentResponse>>>> ListByPosition(TmsEquipmentPositionRequest query);

        [Post(BaseEndpoint.TmsEp.Equipment.Base + "/listByEquipmentZoneList")]
        Task<ApiResponse<WResult<List<TmsEquipmentResponse>>>> ListByEquipmentZoneList(TmsEquipmentZoneListRequest query);

        
    }
}
