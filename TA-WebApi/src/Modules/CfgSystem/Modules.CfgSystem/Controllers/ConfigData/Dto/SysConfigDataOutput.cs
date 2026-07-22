using Modules.CfgSystem.Core.Entities;

namespace Modules.CfgSystem.Controllers.ConfigData.Dto
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO chứa thông tin sysConfigData
    /// Created date: 09/08/2024
    /// </summary>
    public class SysConfigDataOutput : SysConfigData
    {
        public string? ConfigDataId { get; set; }
        public string? ConfigTypeCode { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO chứa thông tin sysConfigData của phân trang
    /// Created date: 09/08/2024
    /// </summary>
    public class PageSysConfigDataOutput: SysConfigData { }
}
