using Refit;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Interfaces.Api;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Utility.Apis.Tms
{
	public interface ISignalLogApi : IBaseApi
	{
		[Post(BaseEndpoint.TmsEp.SignalLog.Base + "/add")]
		Task<ApiResponse<WResult<string>>> Add([Body]TmsSignalLogAddRequest query);
	}
}
