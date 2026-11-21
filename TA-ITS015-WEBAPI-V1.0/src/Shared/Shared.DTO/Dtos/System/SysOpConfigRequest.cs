using Shared.DTO.Dtos.Base.Request;

namespace Shared.DTO.Dtos.System
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số lấy danh sách SysOpConfig
    /// </summary>
    public class SysOpConfigListRequest : BasePageReqDto
    {
        public string? GroupCode { get; set; }
    }
}
