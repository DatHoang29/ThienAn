using Shared.DTO.Dtos.Base;
using Shared.DTO.Enums;

namespace Shared.DTO.Dtos.System
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cơ bản SysConfigType
    /// </summary>
    public class SysConfigTypeResponseBase : BaseEntityDto
    {
        //SysConfigType {
            // ID varchar(64) pk
            // Code string(128) null
            // Name string(64) null
            // OrderNo integer null
            // Remark string(256) null
            // Status integer null
        //}

        public string? Name { get; set; }
        public int? OrderNo { get; set; }
        public string? Remark { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; } 
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin mở rộng SysConfigType
    /// </summary>
    public class SysConfigTypeResponse : SysConfigTypeResponseBase
    {
    }
}
