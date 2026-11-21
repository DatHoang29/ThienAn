using Modules.VMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.VMS.Controllers.ImageVms.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang
    /// Created date: 01/12/2024
    /// </summary>
    public class PageVmsImageInput: BasePageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Type {  get; set; }
        public string? Lang { get; set; }
        public string? EquipmentTypeId { get; set; } // Lọc theo loại thiết bị VMS/LCS (TmsEquipmentType.ID)
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tìm kiếm
    /// Created date: 01/12/2024
    /// </summary>
    public class VmsImageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Lang { get; set; }
        public string? EquipmentTypeId { get; set; } // Lọc theo loại thiết bị VMS/LCS (TmsEquipmentType.ID)
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chi tiết
    /// Created date: 01/12/2024
    /// </summary>
    public class VmsImageSingleInput
    {
        public string? Id { set; get; }
        public string? Code { set; get; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thêm mới
    /// Created date: 01/12/2024
    /// </summary>
    public class AddVmsImageInput : VmsImage { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO cập nhật
    /// Created date: 01/12/2024
    /// </summary>
    public class UpdateVmsImageInput : AddVmsImageInput {}

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO xóa
    /// Created date: 01/12/2024
    /// </summary>
    public class DeleteVmsImageInput: BaseIdInput {}
}
