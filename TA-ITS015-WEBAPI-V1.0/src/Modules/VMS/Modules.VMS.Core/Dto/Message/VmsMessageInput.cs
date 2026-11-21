using Modules.VMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.VMS.Controllers.Message.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Đối tượng DTO chứa thông tin để phân trang
    /// Created date: 01/12/2024
    /// </summary>
    public class PageVmsMessageInput: BasePageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Type {  get; set; }
        public string? Lang { get; set; }
        public string? Content1 {  get; set; }
        public string? Content2 { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
        public string? State { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Đối tượng dto chứa thông tin để lọc
    /// Created date: 01/12/2024
    /// </summary>
    public class VmsMessageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Lang { get; set; }
        public string? Content1 { get; set; }
        public string? Content2 { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
        public string? State { get; set; }
        public string? SortKey { get; set; }
        public string? SortOrder { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Đối tượng DTO chứa thông tin id hoặc code
    /// Created date: 01/12/2024
    /// </summary>
    public class VmsMessageSingleInput
    {
        public string? Id { set; get; }
        public string? Code { set; get; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: Đối tượng DTO chứa thông tin để thêm mới
    /// Created date: 01/12/2024
    /// </summary>
    public class AddVmsMessageInput : VmsMessage { }

    /// <summary>
    /// Author: ThangNN
    /// Description: Đối tượng DTO chứa thông tin để cập nhật
    /// Created date: 01/12/2024
    /// </summary>
    public class UpdateVmsMessageInput : AddVmsMessageInput {}

    /// <summary>
    /// Author: ThangNN
    /// Description: Đối tượng DTO chứa thông tin để xóa
    /// Created date: 01/12/2024
    /// </summary>
    public class DeleteVmsMessageInput: BaseIdInput {}
}
