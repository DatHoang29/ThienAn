using Refit;
using Shared.DTO.Dtos.Fms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Fms
{
    public interface IEquipmentHistoryApi : IBaseApi
    {
        [Post(BaseEndpoint.FmsEp.EquipmentHistory.Base + "/list")]
        Task<ApiResponse<WResult<List<FmsEquipmentHistoryResponse>>>> List(FmsEquipmentHistoryListRequest query);
    }
}
