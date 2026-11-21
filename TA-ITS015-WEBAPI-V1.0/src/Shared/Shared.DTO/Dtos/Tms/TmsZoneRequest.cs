using Shared.DTO.Dtos.Base.Request;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO tham số lấy danh sách TmsZone
    /// </summary>
    public class TmsZoneListRequest : BasePageReqDto {
        public string? ID { get; set; }
    }
    public class TmsZoneWithChildrenRequest : BasePageReqDto {
        public string? ID { get; set; }
    }
}
