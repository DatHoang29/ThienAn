using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin cơ bản TmsEquipmentType
    /// </summary>
    public class TmsEquipmentTypeResponseBase : BaseEntityDto
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public string DefaultIcon { get; set; }
        public string ActiveIcon { get; set; }
        public string InactiveIcon { get; set; }
        public string FaultIcon { get; set; }
        public string Remark { get; set; }
        public int? Status { get; set; }
        public string ModuleId { get; set; }
        /// <summary>Mã modal hiển thị trên bigscreen (SysConfigData.Code của 'tms_equipment_modal_type'); null = modal mặc định.</summary>
        public string ModalCode { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin mở rộng TmsEquipmentType
    /// </summary>
    public class TmsEquipmentTypeResponse : TmsEquipmentTypeResponseBase
    {
    }
}
