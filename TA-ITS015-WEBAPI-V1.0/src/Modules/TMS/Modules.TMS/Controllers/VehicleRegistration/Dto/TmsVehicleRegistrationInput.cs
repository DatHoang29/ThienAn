using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.VehicleRegistration.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang
    /// Created date: 17/12/2024
    /// </summary>
    public class PageTmsVehicleRegistrationInput : BasePageInput
    {
        public string? LicensePlate { get; set; }
        public string? VehicleType { get; set; }
        public string? Owner { get; set; }
        public string? Brand { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang - tìm kiếm
    /// Created date: 17/12/2024
    /// </summary>
    public class TmsVehicleRegistrationInput
    {
        public string? LicensePlate { get; set; }
        public string? VehicleType { get; set; }
        public string? Owner { get; set; }
        public string? Brand { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thêm mới record
    /// Created date: 17/12/2024
    /// </summary>
    public class AddTmsVehicleRegistrationInput : TmsVehicleRegistration { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO cập nhật record
    /// Created date: 17/12/2024
    /// </summary>
    public class UpdateTmsVehicleRegistrationInput : AddTmsVehicleRegistrationInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO xóa record
    /// Created date: 17/12/2024
    /// </summary>
    public class DeleteTmsVehicleRegistrationInput : BaseIdInput { }
}
