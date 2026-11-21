using Modules.TMS.Report.Controllers.TrafficAnalysis.Dto;

namespace Modules.TMS.Report.Controllers.TrafficAnalysis
{
	/// <summary>
	/// Author: Tuan HA
	/// Created date: 07/02/2025
	/// </summary>
	 [ApiDescriptionSettings(GroupName,Order = 460)]
	public class TmsTrafficAnalysisController : BaseController
	{
		/// <summary>
		/// Get TmsTrafficAnalysis pagination list
		/// </summary>s
		/// <param name="input"></param>
		/// <returns></returns>
		[DisplayName("Get pagination list")]
		public async Task<SqlSugarPagedList<PageTrafficAnalysisOutput>> Page(PageTrafficAnalysisInput input)
		{
			return await MessBus.InvokeAsync<SqlSugarPagedList<PageTrafficAnalysisOutput>>(input);
		}

		/// <summary>
		/// Get TrafficAnalysis tree structure
		/// </summary>
		/// <returns></returns>
		[HttpGet("GetList")]
		[DisplayName("Get List")]
		public async Task<ActionResult<List<TrafficAnalysisOutput>>> GetTrafficAnalysis([FromQuery] TrafficAnalysisInput input)
		{
			return await MessBus.InvokeAsync<List<TrafficAnalysisOutput>>(input);
		}

		/// <summary>
		/// Đọc ảnh dữ liệu phân tích giao thông VDS từ ổ đĩa và trả về Base64.
		/// </summary>
		[HttpGet("GetImage")]
		[DisplayName("Get traffic analysis image")]
		public async Task<ActionResult<TrafficAnalysisImageOutput>> GetImage([FromQuery] TrafficAnalysisImageInput input)
		{
			return await MessBus.InvokeAsync<TrafficAnalysisImageOutput>(input);
		}
	}
}

	 


