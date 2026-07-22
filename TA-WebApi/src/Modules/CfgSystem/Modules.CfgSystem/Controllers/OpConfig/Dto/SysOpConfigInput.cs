using Modules.CfgSystem.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.CfgSystem.Controllers.OpConfig.Dto
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin phân trang
    /// Created date: 09/08/2024
    /// </summary>
    public class PageSysOpConfigInput: BasePageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Value { get; set; }
        public string? GroupCode { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin tìm kiếm
    /// Created date: 09/08/2024
    /// </summary>
    public class SysOpConfigInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Value { get; set; }
        public string? GroupCode {  get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Decription: DTO thông tin thêm mới
    /// Created date: 09/08/2024
    /// </summary>
    public class AddSysOpConfigInput : SysOpConfig {}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cập nhật
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateSysOpConfigInput : AddSysOpConfigInput {}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO xóa một record
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteSysOpConfigInput: BaseIdInput {}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO chứa id hoặc code
    /// </summary>
    public class SysOpConfigSingleInput
    {
        public string? Id { get; set; }
        public string? Code { get; set; }
    }
}
