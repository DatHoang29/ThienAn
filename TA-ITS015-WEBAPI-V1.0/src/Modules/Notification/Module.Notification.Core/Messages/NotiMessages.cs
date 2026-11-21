using Modules.Notification.Core.Enums;

namespace Modules.Notification.Core.Messages
{
    /// <summary>
    /// Author: SonTH
    /// Description: Lệnh gửi thông báo — CONTRACT CÔNG KHAI cho mọi module khác.
    ///
    ///              Cách gọi từ module khác (đã inject IMessageBus của Wolverine):
    /// <code>
    /// await _messageBus.InvokeAsync(new SendNotificationCommand {
    ///     Title = "Sự cố đã được xác minh", Content = "...",
    ///     Category = "TMS.Incident", Severity = NotiEnums.SeverityEnum.Warning.ToString(),
    ///     TargetType = NotiEnums.TargetTypeEnum.Org, OrgIds = new() { incident.OrgId },
    ///     SourceKey = incident.ID,
    ///     IdempotencyKey = $"TMS:Incident:{incident.ID}:Verified"
    /// });
    /// </code>
    ///              Hoặc inject NotificationPublisher để dùng ToUsersAsync/ToOrgAsync/ToAllAsync.
    ///
    ///              KHÔNG thêm reference internals của Module.Notification vào module gọi —
    ///              project này (Core) làdependency duy nhất cần thiết.
    /// Created date: 17/09/2026
    /// </summary>
    public class SendNotificationCommand
    {
        /// <summary>Tiêu đề đã render — module gọi tự dựng chuỗi (hoặc từ i18n) trước khi gửi.</summary>
        public string Title { get; set; } = null!;

        /// <summary>Nội dung đã render.</summary>
        public string Content { get; set; } = null!;

        /// <summary>Phân loại {Module}.{Loại} (vd "TMS.Incident") — tiền tố module nằm ngay trong Category.</summary>
        public string Category { get; set; } = null!;

        /// <summary>Mức độ: Info | Warning | Critical. Mặc định Info.</summary>
        public string Severity { get; set; } = NotiEnums.SeverityEnum.Info.ToString();

        /// <summary>Phạm vi người nhận.</summary>
        public NotiEnums.TargetTypeEnum TargetType { get; set; }

        /// <summary>Danh sách user nhận — bắt buộc khi TargetType = User.</summary>
        public List<string>? UserIds { get; set; }

        /// <summary>Danh sách org nhận — bắt buộc khi TargetType = Org.</summary>
        public List<string>? OrgIds { get; set; }

        /// <summary>TargetType = Org: có bao gồm cây org con không. Mặc định true.</summary>
        public bool IncludeChildren { get; set; } = true;

        /// <summary>Định danh bản ghi nguồn (vd IncidentId) để frontend điều hướng.</summary>
        public string? SourceKey { get; set; }

        /// <summary>
        /// Chống gửi trùng khi caller retry. Key trùng đã tồn tại thì trả kết quả cũ
        /// (Duplicated = true) và KHÔNG fan-out lại. Khuyến nghị "{Module}:{Key}:{event}".
        /// </summary>
        public string? IdempotencyKey { get; set; }
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Kết quả gửi thông báo.
    /// Created date: 17/09/2026
    /// </summary>
    public class SendNotificationResult
    {
        /// <summary>Id thông báo đã tạo (hoặc id của bản ghi trùng khi Duplicated).</summary>
        public string MessageId { get; set; } = null!;

        /// <summary>Số người nhận đã fan-out (0 khi lỗi, đúng số dòng NotiRecipient khi thành công).</summary>
        public int RecipientCount { get; set; }

        /// <summary>true nếu IdempotencyKey đã tồn tại — không fan-out lần hai.</summary>
        public bool Duplicated { get; set; }
    }
}
