using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.Mail.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Mẫu email.
    /// Created date: 12/10/2026
    /// </summary>
    [SugarTable("MailTemplate")]
    public class MailTemplate : EntityTenant
    {
        [SugarColumn(IsNullable = false, Length = EntityConst.Length128)]
        public string Name { get; set; } = null!;

        /// <summary>Đường dẫn file HTML tính từ wwwroot, VD "Upload/Mail/Template/{ID}.html".</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? FilePath { get; set; }

        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
