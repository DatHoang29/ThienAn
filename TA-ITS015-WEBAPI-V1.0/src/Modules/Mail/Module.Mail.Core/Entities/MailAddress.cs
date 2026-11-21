using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.Mail.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Sổ địa chỉ email
    /// Created date: 02/10/2026
    /// </summary>
    [SugarTable("MailAddress")]
    public class MailAddress : EntityTenant
    {
        /// <summary>Địa chỉ email, chữ thường, đã cắt khoảng trắng.</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length256)]
        public string Email { get; set; } = null!;
    }
}
