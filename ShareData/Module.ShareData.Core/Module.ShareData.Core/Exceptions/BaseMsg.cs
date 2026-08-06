using Shared.DTO.Constants.Localization;

namespace Module.ShareData.Core.Exceptions
{
    /// <summary>
    /// Description: Nội dung dịch thuật riêng của phân hệ Chia sẻ dữ liệu (ShareData)
    /// Created date: 2026-08-04
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        public class ShareData
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.sharedata.";
                internal const string Validation = "lz.validation.sharedata.";
                internal const string Exception = "lz.exception.sharedata.";
                internal const string Entity = "lz.entity.sharedata.";
            }

            /// <summary>
            /// Tên hiển thị các trường thực thể, dùng trong message lỗi khóa ngoại.
            /// </summary>
            public class Entity
            {
                /// <summary>Đối tác chia sẻ (ShareDataPartner)</summary>
                public const string PartnerId = Prefix.Entity + "partnerId";

                /// <summary>Phiên kết nối (ShareDataSession)</summary>
                public const string SessionId = Prefix.Entity + "sessionId";

                /// <summary>Đăng ký chia sẻ (ShareDataSubscription)</summary>
                public const string SubscriptionId = Prefix.Entity + "subscriptionId";

                /// <summary>Nguồn dữ liệu (ShareDataDataSource)</summary>
                public const string DataSourceId = Prefix.Entity + "dataSourceId";

                /// <summary>Hồ sơ ánh xạ (ShareDataMappingProfile)</summary>
                public const string MappingProfileId = Prefix.Entity + "mappingProfileId";

                /// <summary>Nguồn sự kiện (ShareDataEventSource)</summary>
                public const string EventSourceId = Prefix.Entity + "eventSourceId";

                /// <summary>Loại dữ liệu chia sẻ (101–111)</summary>
                public const string DatatypeId = Prefix.Entity + "datatypeId";

                /// <summary>Chiều chia sẻ</summary>
                public const string Direction = Prefix.Entity + "direction";

                /// <summary>Chế độ đăng ký</summary>
                public const string Mode = Prefix.Entity + "mode";

                /// <summary>Lịch gửi</summary>
                public const string Schedule = Prefix.Entity + "schedule";
            }

            public class Exception
            {
                /// <summary>"Đối tác đang có phiên kết nối, vui lòng ngắt kết nối trước."</summary>
                public const string PartnerSessionActive = Prefix.Exception + "partnerSessionActive";

                /// <summary>"Đối tác còn đăng ký đang chạy, vui lòng hủy trước khi xóa."</summary>
                public const string PartnerHasActiveSubscription = Prefix.Exception + "partnerHasActiveSubscription";

                /// <summary>"Đối tác đang ở trạng thái không sử dụng."</summary>
                public const string PartnerDisabled = Prefix.Exception + "partnerDisabled";

                /// <summary>"Đối tác đã có đăng ký cho gói tin này."</summary>
                public const string SubscriptionDuplicated = Prefix.Exception + "subscriptionDuplicated";

                /// <summary>"Không thể chuyển trạng thái đăng ký từ [{0}] sang [{1}]."</summary>
                public const string SubscriptionStateInvalid = Prefix.Exception + "subscriptionStateInvalid";

                /// <summary>"Đăng ký đã kết thúc, không thể sửa."</summary>
                public const string SubscriptionFinished = Prefix.Exception + "subscriptionFinished";

                /// <summary>"Vui lòng tắt đăng ký trước khi sửa."</summary>
                public const string SubscriptionMustPauseBeforeUpdate = Prefix.Exception + "subscriptionMustPauseBeforeUpdate";

                /// <summary>"Chưa có ánh xạ đang áp dụng cho đối tác và gói tin này."</summary>
                public const string MappingNotResolved = Prefix.Exception + "mappingNotResolved";
            }

            public class Validation
            {
                /// <summary>"Chế độ theo sự kiện bắt buộc chọn nguồn sự kiện."</summary>
                public const string EventSourceRequired = Prefix.Validation + "eventSourceRequired";

                /// <summary>"Chế độ định kỳ bắt buộc cấu hình lịch gửi."</summary>
                public const string ScheduleRequired = Prefix.Validation + "scheduleRequired";

                /// <summary>"Loại dữ liệu chia sẻ phải nằm trong khoảng 101–111."</summary>
                public const string DatatypeIdOutOfRange = Prefix.Validation + "datatypeIdOutOfRange";
            }

            public class Message
            {
                /// <summary>"Đã thiết lập phiên kết nối với đối tác."</summary>
                public const string PartnerConnected = Prefix.Message + "partnerConnected";

                /// <summary>"Đã ngắt kết nối với đối tác."</summary>
                public const string PartnerDisconnected = Prefix.Message + "partnerDisconnected";
            }
        }
    }
}
