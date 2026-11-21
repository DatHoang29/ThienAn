using Shared.DTO.Dtos.Base.Request;
namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO lấy danh sách TmsWorkExecute
    /// </summary>
    public class TmsWorkExecuteHistoryListRequest : BasePageReqDto
    {
        public string? IncidentId { get; set; }
    }
}
