using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Modules.Notification.Core.Entities
{
    /// <summary>
    /// Author: SonTH
    /// Description: Một người nhận của thông báo — FAN-OUT ROW-PER-RECIPIENT.
    ///
    ///              Mỗi user một dòng riêng cho nên badge unread, mark-read, xoá của từng người
    ///              chỉ đụng đúng dòng của mình: an toàn đa instance, không lock row dùng chung.
    ///
    ///              Category/Severity/SendTime được DENORMALIZE từ NotiMessage lúc fan-out để
    ///              truy vấn/pagination/sắp xếp của một user chỉ chạy trên đúng bảng này,
    ///              không join. Title/Content vẫn nằm ở NotiMessage — chỉ nạp cho trang hiện tại.
    ///              Không snapshot OrgId/UserName/OrgName: không có truy vấn nào dùng, quản lý
    ///              user/org là việc của phân hệ System.
    ///
    ///              BẢNG TỰ GENERATE bằng SqlSugar CodeFirst (Database.json: ConfigId NotiDefault
    ///              bật EnableInitDb + EnableInitTable); index khai báo ngay trên entity.
    /// Created date: 17/09/2026
    /// </summary>
    [SugarTable("NotiRecipient")]
    [Tenant("NotiDefault")]
    [SugarIndex("UX_NotiRecipient_Message_User", nameof(MessageId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, true)]
    [SugarIndex("IX_NotiRecipient_User_ReadTime", nameof(UserId), OrderByType.Asc, nameof(ReadTime), OrderByType.Asc)]
    [SugarIndex("IX_NotiRecipient_User_SendTime", nameof(UserId), OrderByType.Asc, nameof(SendTime), OrderByType.Asc)]
    public class NotiRecipient : EntityTenant
    {
        /// <summary>Thông báo tương ứng (FK logic → NotiMessage.ID).</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length64)]
        public string MessageId { get; set; } = null!;

        /// <summary>Người nhận.</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length64)]
        public string UserId { get; set; } = null!;

        /// <summary>Denormalize từ NotiMessage.Category.</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length64)]
        public string Category { get; set; } = null!;

        /// <summary>Denormalize từ NotiMessage.Severity.</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length16)]
        public string Severity { get; set; } = null!;

        /// <summary>Denormalize từ NotiMessage.SendTime — cột sắp xếp chính của lưới thông báo.</summary>
        [SugarColumn(IsNullable = false)]
        public DateTime SendTime { get; set; }

        /// <summary>Người dùng đánh dấu đã đọc. NULL = chưa đọc (nguồn đếm badge).</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? ReadTime { get; set; }
    }
}
