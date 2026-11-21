using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.VehicleFilter.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang
    /// Created date: 13/11/2024
    /// </summary>
    public class PageTmsVehicleFilterInput : BasePageInput
    {
        public string? LicensePlate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Type { get; set; }
        public string? Alarm { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tìm kiếm
    /// Created date: 13/11/2024
    /// </summary>
    public class TmsVehicleFilterInput
    {
        public string? LicensePlate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Type { get; set; }
        public string? Alarm { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thêm mới
    /// Created date: 13/11/2024
    /// </summary>
    public class AddTmsVehicleFilterInput : TmsVehicleFilter { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO cập nhật
    /// Created date: 13/11/2024
    /// </summary>
    public class UpdateTmsVehicleFilterInput : AddTmsVehicleFilterInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO xóa
    /// Created date: 13/11/2024
    /// </summary>
    public class DeleteTmsVehicleFilterInput : BaseIdInput { }
}
