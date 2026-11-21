using Shared.DTO.Dtos.Base.Request;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số query danh sách TmsEquipment
    /// </summary>
    public class TmsEquipmentListRequest {
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa thông tin danh sách zone và thiết bị
    /// </summary>
    public class TmsEquipmentZoneListRequest
    {
        public string[] ZoneIdList { get; set; }
        public string[] EquipmentIdList { get; set; }
        public string? ZoneType { get; set; }
        public string? EquipmentType { get; set; }
    }
}
