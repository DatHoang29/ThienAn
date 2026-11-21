using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.VehicleFilterHistory.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang
    /// Created date: 13/11/2024
    /// </summary>
    public class PageTmsVehicleFilterHistoryInput : BasePageInput
    {
        public string? LicensePlate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Direction { get; set; }
        public string? Type { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tìm kiếm
    /// Created date: 13/11/2024
    /// </summary>
    public class TmsVehicleFilterHistoryInput
    {
        public string? LicensePlate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Direction { get; set; }
        public string? Type { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thêm mới record
    /// Created date: 13/11/2024
    /// </summary>
    public class AddTmsVehicleFilterHistoryInput : TmsVehicleFilterHistory { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO cập nhật record
    /// Created date: 13/11/2024
    /// </summary>
    public class UpdateTmsVehicleFilterHistoryInput : AddTmsVehicleFilterHistoryInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO xóa record
    /// Created date: 13/11/2024
    /// </summary>
    public class DeleteTmsVehicleFilterHistoryInput : BaseIdInput { }
}
