using Microsoft.AspNetCore.Http;

namespace Modules.Mail.Core.Dto.EmailSchedule;

/// <summary>Ảnh/logo để chèn vào nội dung HTML của lịch hoặc mẫu email.</summary>
public class MailUploadImageInput
{
    public IFormFile File { get; set; } = null!;
}

public class MailUploadImageOutput
{
    /// <summary>Đường dẫn ảnh do upload service trả về, dùng làm src của thẻ img.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Tên file ảnh người dùng chọn.</summary>
    public string Name { get; set; } = string.Empty;
}
