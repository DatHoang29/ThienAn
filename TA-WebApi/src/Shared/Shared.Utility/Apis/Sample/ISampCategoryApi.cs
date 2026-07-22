using Refit;
using Shared.DTO.Dtos.Sample;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;

namespace Shared.Utility.Apis.Sample
{
    /// <summary>
    /// Sample Category Api 
    /// </summary>
    public interface ISampCategoryApi : IBaseApi
    {
        [Get(BaseEndpoint.SampleEp.Category.Base + "/list")]
        Task<ApiResponse<WResult<List<SampCategoryDto>>>> List([Query] SampCategoryDto query);

    }
}
