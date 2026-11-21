using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.WorkContact.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang
    /// Created date: 05/11/2024
    /// </summary>
    public class PageTmsWorkContactInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Phone { get; set; }
        public string? RefId { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tìm kiếm
    /// Created date: 05/11/2024
    /// </summary>
    public class TmsWorkContactInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Phone { get; set; }
        public string? RefId { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin chi tiết
    /// Created date: 05/11/2024
    /// </summary>
    public class TmsWorkContactSingleInput
    {
        public string? Id { get; set; }
        public string? Code { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO lọc theo điều kiện
    /// Created date: 05/11/2024
    /// </summary>
    public class TmsWorkContactPara
    {
        public string? Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? RefId { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thêm mới record
    /// Created date: 05/11/2024
    /// </summary>
    public class AddTmsWorkContactInput : TmsWorkContact { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO cập nhật record
    /// Created date: 05/11/2024
    /// </summary>
    public class UpdateTmsWorkContactInput : AddTmsWorkContactInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO xóa record
    /// Created date: 05/11/2024
    /// </summary>
    public class DeleteTmsWorkContactInput : BaseIdInput { }
}
