using Shared.DTO.Dtos.Base;
using Shared.DTO.Dtos.Base.Request;
using Shared.DTO.Enums;

namespace Shared.DTO.Dtos.System
{
    #region DTOs output
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cơ bản SysTerminology
    /// </summary>
    public class TerminologyDto : BaseEntityDto
    {
        //SysTerminology {
            // ID varchar(64) pk
            // Code string(256) unique
            // Name string(256) null
            // Value string(256) null
            // Lang varchar(16) def(vi-VN)
            // OrderNo integer null
            // Remark string(1024) null
            // Status integer null def(1)
        //}

        public override string? Code { get; set; }

        public string? Name { get; set; }

        public string? Value { get; set; }

        public string? Lang { get; set; }

        public int? OrderNo { get; set; }

        public string? Remark { get; set; }

        public BaseEnums.StatusEnum? Status { get; set; } 
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin mở rộng SysTerminology
    /// </summary>
    public class TerminologyOutputDto : TerminologyDto
    {
    }

    #endregion DTOs output

    #region DTOs input
    public class TerminologyListInputDto : BasePageReqDto
    {
    }
    #endregion DTOs input
}
