using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.WorkUnit.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang
    /// Created date: 25/11/2024
    /// </summary>
    public class PageTmsWorkUnitInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Phone { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tìm kiếm
    /// Created date: 25/11/2024
    /// </summary>
    public class TmsWorkUnitInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Phone { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thêm mới record
    /// Created date: 25/11/2024
    /// </summary>
    public class AddTmsWorkUnitInput : TmsWorkUnit { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO cập nhật record
    /// Created date: 25/11/2024
    /// </summary>
    public class UpdateTmsWorkUnitInput : AddTmsWorkUnitInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO xóa record
    /// Created date: 25/11/2024
    /// </summary>
    public class DeleteTmsWorkUnitInput : BaseIdInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tìm kiếm theo vị trí
    /// Created date: 25/11/2024
    /// </summary>
    public class TmsWorkUnitPositionInput
    {
        public string? WorkUnitId { get; set; }
        public string? MapLocationId { get; set; }
        public string? RefType { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin chi tiết
    /// Created Date: 25/11/2024
    /// </summary>
    public class TmsWorkUnitSingleInput
    {
        public string? Id { set; get; }
        public string? Code { set; get; }
    }
}
