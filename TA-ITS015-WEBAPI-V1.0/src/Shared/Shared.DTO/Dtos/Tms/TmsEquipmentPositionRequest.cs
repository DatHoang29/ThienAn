using Shared.DTO.Dtos.Base.Request;

namespace Shared.DTO.Dtos.Tms
{
    public class TmsEquipmentPositionRequest : BasePageReqDto
    {
        public string? ZoneId { get; set; }
        public string? DirectionId { get; set; }
        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }
        public string[]? EquipmentIdList { get; set; }
        public string? EquipmentTypeId { get; set; }
    }
}
