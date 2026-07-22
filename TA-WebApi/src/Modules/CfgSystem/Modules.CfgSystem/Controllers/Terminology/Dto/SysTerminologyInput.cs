using Modules.CfgSystem.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.CfgSystem.Controllers.Terminology.Dto
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO tham số phân trang
    /// Created date: 09/08/2024
    /// </summary>
    public class PageSysTerminologyInput: BasePageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Value { get; set; }
        public string? Language { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO tham số tìm kiếm
    /// Created date: 09/08/2024
    /// </summary>
    public class SysTerminologyInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Value { get; set; }
        public string? Language { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO tham số tìm kiếm
    /// </summary>
    public class SysTerminologyParaInput
    {
        public string[]? CodeList { get; set; }
        public string? Language { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Decription: DTO thông tin thêm mới
    /// Created date: 09/08/2024
    /// </summary>
    public class AddSysTerminologyInput : SysTerminology { }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cập nhật
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateSysTerminologyInput : AddSysTerminologyInput {}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin xóa một record
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteSysTerminologyInput: BaseIdInput {}
}
