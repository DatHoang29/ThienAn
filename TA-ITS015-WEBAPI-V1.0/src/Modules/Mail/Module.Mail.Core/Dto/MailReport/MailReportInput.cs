using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.Mail.Core.Dto.EmailReport
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh mục báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class MailRptPageReportInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? PluginCode { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO danh sách báo cáo gửi email (không phân trang)
    /// Created date: 01/10/2026
    /// </summary>
    public class MailRptReportInput
    {
        public string? Code { get; set; }
        public string? PluginCode { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 báo cáo theo ID
    /// Created date: 01/10/2026
    /// </summary>
    public class MailRptIdReportInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class MailRptAddReportInput
    {
        /// <summary>Mã mẫu báo cáo (mã file frx).</summary>
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;

        /// <summary>Nhóm = SysPlugin.Code.</summary>
        public string PluginCode { get; set; } = null!;

        /// <summary>JSON tham số chi tiết — chưa dùng.</summary>
        public string? DetailParams { get; set; }

        public BaseEnums.StatusEnum? Status { get; set; }
        public int? OrderNo { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class MailRptUpdateReportInput : MailRptAddReportInput
    {
        public string ID { get; set; } = null!;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class MailRptDeleteReportInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO bật / tắt 1 báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class MailRptToggleReportInput : BaseIdInput
    {
        public BaseEnums.StatusEnum Status { get; set; }
    }
}
