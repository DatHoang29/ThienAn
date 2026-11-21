using System.ComponentModel;

namespace Modules.Notification.Core.Enums
{
    /// <summary>
    /// Author: SonTH
    /// Description: Các enum riêng của phân hệ Notification.
    ///              Entity lưu enum dạng STRING (NotiEnums.TargetTypeEnum.User...) để dễ đọc
    ///              và tương thích cách lưu state hiện hữu của dự án.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiEnums
    {
        /// <summary>
        /// Phạm vi người nhận của một thông báo.
        /// </summary>
        public enum TargetTypeEnum
        {
            /// <summary>Gửi riêng cho từng người dùng (UserIds bắt buộc).</summary>
            [Description("Người dùng")]
            User = 1,

            /// <summary>Gửi cho một tổ chức, mặc định bao gồm cây con (OrgIds bắt buộc).</summary>
            [Description("Tổ chức")]
            Org = 2,

            /// <summary>Gửi toàn hệ thống — fan-out theo danh sách user active tại thời điểm gửi.</summary>
            [Description("Toàn hệ thống")]
            All = 3,
        }

        /// <summary>
        /// Mức độ của thông báo — quyết định màu/âm báo phía frontend.
        /// </summary>
        public enum SeverityEnum
        {
            [Description("Thông tin")]
            Info = 1,

            [Description("Cảnh báo")]
            Warning = 2,

            [Description("Nghiêm trọng")]
            Critical = 3,
        }
    }
}
