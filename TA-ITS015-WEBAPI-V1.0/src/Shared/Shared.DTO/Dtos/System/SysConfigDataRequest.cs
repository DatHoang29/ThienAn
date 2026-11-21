using Shared.DTO.Dtos.Base.Request;

namespace Shared.DTO.Dtos.System
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số lấy danh sách SysConfigData
    /// </summary>
    public class SysConfigDataListRequest : BasePageReqDto
    {
        public string? ConfigTypeId { get; set; }
        public string? ConfigTypeCode { get; set; }
    }
}
