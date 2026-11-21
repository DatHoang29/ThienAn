using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.Mail.Core.Dto.TemplateMail
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class MailPageTemplateInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO danh sách mẫu email (không phân trang) — ô chọn mẫu ở form lịch gửi
    /// Created date: 12/10/2026
    /// </summary>
    public class MailTemplateInput
    {
        public string? Code { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 mẫu email theo ID (kèm nội dung HTML)
    /// Created date: 12/10/2026
    /// </summary>
    public class MailIdTemplateInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 mẫu email theo mã (kèm nội dung HTML) — form lịch nạp nội dung mẫu vừa chọn
    /// Created date: 12/10/2026
    /// </summary>
    public class MailCodeTemplateInput
    {
        public string Code { get; set; } = null!;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class MailAddTemplateInput
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;

        /// <summary>Nội dung HTML soạn bằng wangEditor. Backend ghi ra file wwwroot/Upload/Mail/Template/{ID}.html.</summary>
        public string? Content { get; set; }

        public BaseEnums.StatusEnum? Status { get; set; }
        public int? OrderNo { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class MailUpdateTemplateInput : MailAddTemplateInput
    {
        public string ID { get; set; } = null!;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class MailDeleteTemplateInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO bật / tắt 1 mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class MailToggleTemplateInput : BaseIdInput
    {
        public BaseEnums.StatusEnum Status { get; set; }
    }
}
