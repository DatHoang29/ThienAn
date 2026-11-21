using Shared.DTO.Constants.Localization;

namespace Modules.Mail.Core.Exceptions
{
    /// <summary>
    /// Description: Nội dung dịch thuật riêng của phân hệ Mail.
    ///              Các khoá dùng chung (Required, MaxLength, Exist, DataNotExist, Code, Name...)
    ///              đã có ở BaseLocaleManager — KHÔNG khai lại.
    /// Created date: 2026-08-14
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        public class Mail
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.rptEmail.";
                internal const string Validation = "lz.validation.rptEmail.";
                internal const string Exception = "lz.exception.rptEmail.";
                internal const string Entity = "lz.entity.rptEmail.";
            }

            /// <summary>Tên hiển thị các trường, dùng trong message lỗi.</summary>
            public class Entity
            {
                /// <summary>Báo cáo</summary>
                public const string ReportCode = Prefix.Entity + "reportCode";

                /// <summary>Kỳ báo cáo</summary>
                public const string PeriodType = Prefix.Entity + "periodType";

                /// <summary>Kỳ lấy số liệu (kỳ trước / kỳ hiện tại)</summary>
                public const string PeriodOffset = Prefix.Entity + "periodOffset";

                /// <summary>Tần suất</summary>
                public const string Frequency = Prefix.Entity + "frequency";

                /// <summary>Ngày chạy</summary>
                public const string DayOfMonth = Prefix.Entity + "dayOfMonth";

                /// <summary>Tháng chạy (tháng trong quý / trong năm)</summary>
                public const string MonthInPeriod = Prefix.Entity + "monthInPeriod";

                /// <summary>Thứ chạy trong tuần</summary>
                public const string DaysOfWeek = Prefix.Entity + "daysOfWeek";

                /// <summary>Giờ chạy</summary>
                public const string HourOfDay = Prefix.Entity + "hourOfDay";

                /// <summary>Định dạng</summary>
                public const string Format = Prefix.Entity + "format";

                /// <summary>Người nhận</summary>
                public const string Recipients = Prefix.Entity + "recipients";

                /// <summary>CC</summary>
                public const string Cc = Prefix.Entity + "cc";

                /// <summary>BCC</summary>
                public const string Bcc = Prefix.Entity + "bcc";

                /// <summary>Tiêu đề email</summary>
                public const string Subject = Prefix.Entity + "subject";

                /// <summary>Mẫu email</summary>
                public const string TemplateCode = Prefix.Entity + "templateCode";

                /// <summary>Nội dung thư</summary>
                public const string Content = Prefix.Entity + "content";

                /// <summary>Nhóm báo cáo (SysPlugin)</summary>
                public const string PluginCode = Prefix.Entity + "pluginCode";

                /// <summary>Tham số chi tiết</summary>
                public const string DetailParams = Prefix.Entity + "detailParams";
            }

            public class Validation
            {
                /// <summary>"[{PropertyName}] chỉ được từ {From} đến {To}." — dùng trong FluentValidation (InclusiveBetween).</summary>
                public const string DayOfMonthRange = Prefix.Validation + "dayOfMonthRange";

                /// <summary>"Địa chỉ email không hợp lệ: {0}."</summary>
                public const string InvalidRecipient = Prefix.Validation + "invalidRecipient";

                /// <summary>"Báo cáo [{0}] không hỗ trợ kỳ [{1}]."</summary>
                public const string PeriodNotMatchReport = Prefix.Validation + "periodNotMatchReport";

                /// <summary>"Phải chọn ít nhất một báo cáo để gửi."</summary>
                public const string ItemsRequired = Prefix.Validation + "itemsRequired";

                /// <summary>"Tổng số địa chỉ (người nhận + CC + BCC) không được vượt {0}."</summary>
                public const string TooManyRecipients = Prefix.Validation + "tooManyRecipients";

                /// <summary>"Số báo cáo trong một thư không được vượt {0}."</summary>
                public const string TooManyItems = Prefix.Validation + "tooManyItems";

                /// <summary>"Dòng {0}: báo cáo [{1}] chưa hỗ trợ đặt lịch hoặc đã ngừng sử dụng."</summary>
                public const string ItemReportNotActive = Prefix.Validation + "itemReportNotActive";

                /// <summary>"Dòng {0}: kỳ lấy số liệu [{1}] không hợp lệ."</summary>
                public const string ItemPeriodOffsetInvalid = Prefix.Validation + "itemPeriodOffsetInvalid";

                /// <summary>"[{0}] không được để trống." — dùng với Oops.Oh (tham số đã dịch bằng L.Text).
                /// BaseValidation.Required dùng {PropertyName} của FluentValidation nên KHÔNG dùng được với Oops.Oh.</summary>
                public const string FieldRequired = Prefix.Validation + "fieldRequired";

                /// <summary>"[{0}] không hợp lệ." — dùng với Oops.Oh thay BaseValidation.MustBeValidValue.</summary>
                public const string FieldInvalid = Prefix.Validation + "fieldInvalid";

                /// <summary>"Lịch gửi hàng tuần chỉ được chọn một thứ trong tuần."</summary>
                public const string WeeklyDayOfWeekSingle = Prefix.Validation + "weeklyDayOfWeekSingle";

                /// <summary>"Mẫu email [{0}] không tồn tại hoặc đã ngừng sử dụng."</summary>
                public const string TemplateNotFound = Prefix.Validation + "templateNotFound";

            }

            public class Exception
            {
                /// <summary>"Chưa hỗ trợ đặt lịch cho báo cáo [{0}]."</summary>
                public const string ReportNotSupported = Prefix.Exception + "reportNotSupported";

                /// <summary>"Kết xuất báo cáo ra tệp rỗng, không gửi."</summary>
                public const string EmptyReportFile = Prefix.Exception + "emptyReportFile";

                /// <summary>"Lịch đang tạm dừng, không thể gửi."</summary>
                public const string ScheduleDisabled = Prefix.Exception + "scheduleDisabled";

                /// <summary>"Báo cáo [{0}] đang được dùng trong {1} lịch gửi email, không thể xoá hoặc đổi mã."</summary>
                public const string ReportInUse = Prefix.Exception + "reportInUse";

                /// <summary>"Báo cáo [{0}] không có trong danh mục hoặc đã ngừng sử dụng."</summary>
                public const string ReportNotActive = Prefix.Exception + "reportNotActive";

                /// <summary>"Không tìm thấy file nội dung thư [{0}]. Kiểm tra lại thư mục wwwroot/Upload/Mail."</summary>
                public const string ContentFileNotFound = Prefix.Exception + "contentFileNotFound";
            }

            public class Message
            {
                /// <summary>"Đã gửi báo cáo qua email."</summary>
                public const string SendSuccess = Prefix.Message + "sendSuccess";

                /// <summary>"Đã đăng ký lịch gửi báo cáo."</summary>
                public const string ScheduleRegistered = Prefix.Message + "scheduleRegistered";
            }
        }
    }
}
