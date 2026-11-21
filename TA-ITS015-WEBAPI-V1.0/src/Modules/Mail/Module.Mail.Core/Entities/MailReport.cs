using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.Mail.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Danh mục báo cáo được phép đặt lịch gửi qua email, gom nhóm theo hệ (SysPlugin).
    /// Created date: 01/10/2026
    /// </summary>
    [SugarTable("MailReport")]
    public class MailReport : EntityTenant
    {
        [SugarColumn(IsNullable = false, Length = EntityConst.Length128)]
        public string Name { get; set; } = null!;
        [SugarColumn(IsNullable = false, Length = EntityConst.Length64)]
        public string PluginCode { get; set; } = null!;

        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
        public string? DetailParams { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
