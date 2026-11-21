using Modules.CfgSystem.Core.Entities;

namespace Modules.CfgSystem.Core.Dto.ConfigData
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa thông tin sysConfigData
    /// Created date: 09/08/2024
    /// </summary>
    public class SysConfigDataOutput : SysConfigData
    {
        public string? ConfigDataId { get; set; }
        public string? ConfigTypeCode { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa thông tin sysConfigData (Phân trang)
    /// Created date: 09/08/2024
    /// </summary>
    public class PageSysConfigDataOutput : SysConfigData { }
}
