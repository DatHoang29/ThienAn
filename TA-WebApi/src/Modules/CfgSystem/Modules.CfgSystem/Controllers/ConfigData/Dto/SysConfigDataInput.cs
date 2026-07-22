using Modules.CfgSystem.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.CfgSystem.Controllers.ConfigData.Dto
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO phân trang
    /// Created date: 09/08/2024
    /// </summary>
    public class PageSysConfigDataInput: BasePageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Value { get; set; }
        public string? ConfigTypeId { get; set; }
        public string? ConfigTypeCode { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO tìm kiếm
    /// Created date: 09/08/2024
    /// </summary>
    public class SysConfigDataInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Value { get; set; }
        public string? ConfigTypeId { get; set; }
        public string? ConfigTypeCode { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thêm mới 1 record
    /// Created date: 09/08/2024
    /// </summary>
    public class AddSysConfigDataInput : SysConfigData {}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO cập nhật 1 record
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateSysConfigDataInput : AddSysConfigDataInput {}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO xóa 1 record
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteSysConfigDataInput: BaseIdInput {}
}
