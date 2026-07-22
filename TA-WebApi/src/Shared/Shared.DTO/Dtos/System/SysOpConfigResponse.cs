using Shared.DTO.Dtos.Base;
using Shared.DTO.Enums;

namespace Shared.DTO.Dtos.System
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cơ bản SysOpConfig
    /// </summary>
    public class SysOpConfigResponseBase : BaseEntityDto
    {
        //SysOpConfig {
        // ID varchar(64) pk
        // Code string(128) null
        // GroupCode string(64) null
        // Name string(64) null
        // Value string(128) null
        // IsSysConfig boolean null def(true)
        // OrderNo integer null
        // Status integer null
        // Remark string(1024) null
        //}

        public string? GroupCode { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
        public bool? IsSysConfig { get; set; }
        public int? OrderNo { get; set; }
        public string? Remark { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin mở rộng SysOpConfig
    /// </summary>
    public class SysOpConfigResponse : SysOpConfigResponseBase
    {
    }
}
