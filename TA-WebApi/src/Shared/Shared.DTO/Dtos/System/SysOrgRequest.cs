using Shared.DTO.Dtos.Base.Request;

namespace Shared.DTO.Dtos.System
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO tham số lấy danh sách SysOrg
    /// </summary>
    public class SysOrgListRequest : BasePageReqDto
    {
        public string? ID { get; set; }
    }
}
