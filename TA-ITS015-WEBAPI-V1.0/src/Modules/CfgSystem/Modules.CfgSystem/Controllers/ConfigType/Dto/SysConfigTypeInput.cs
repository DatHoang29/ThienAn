using Modules.CfgSystem.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.CfgSystem.Controllers.ConfigType.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO phân trang
    /// Created date: 09/08/2024
    /// </summary>
    public class PageSysConfigTypeInput: BasePageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tìm kiếm
    /// Created date: 09/08/2024
    /// </summary>
    public class SysConfigTypeInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thêm 1 record
    /// Created date: 09/08/2024
    /// </summary>
    public class AddSysConfigTypeInput : SysConfigType {}

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO cập nhật 1 record
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateSysConfigTypeInput : AddSysConfigTypeInput {}

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO xóa 1 record
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteSysConfigTypeInput: BaseIdInput {}

    public class AllConfigListInput
    {

    }
}
