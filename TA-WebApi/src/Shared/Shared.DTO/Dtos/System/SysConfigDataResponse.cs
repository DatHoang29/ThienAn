using Shared.DTO.Dtos.Base;
using Shared.DTO.Enums;

namespace Shared.DTO.Dtos.System
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cơ bản SysConfigData
    /// </summary>
    public class SysConfigDataResponseBase : BaseEntityDto
    {
        //SysConfigData {
            // ID varchar(64) pk
            // ConfigTypeId varchar(64) null > SysConfigType.ID
            // Name string(256) null
            // Value string(128) null
            // StyleSetting string(512) null
            // ClassSetting string(512) null
            // OrderNo integer null
            // Remark string(256) null
            // Status integer null
            // TagType string(16) null
        //}

        public string? ConfigTypeId { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? TagType { get; set; }
        public string? StyleSetting { get; set; }
        public string? ClassSetting { get; set; }
        public int? OrderNo { get; set; }
        public string? Remark { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin mở rộng SysConfigData
    /// </summary>
    public class SysConfigDataResponse : SysConfigDataResponseBase
    {
        public string? ConfigDataId { get; set; }
        public string? ConfigTypeCode { get; set; }
    }
}
