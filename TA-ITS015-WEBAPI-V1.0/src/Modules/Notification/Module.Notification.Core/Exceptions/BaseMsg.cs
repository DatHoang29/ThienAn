using Shared.DTO.Constants.Localization;

namespace Modules.Notification.Core.Exceptions
{
    /// <summary>
    /// Author: SonTH
    /// Description: Nội dung dịch thuật riêng của phân hệ Notification.
    ///              Các khoá dùng chung (Required, MaxLength, Exist, DataNotExist...)
    ///              đã có ở BaseLocaleManager — KHÔNG khai lại.
    /// Created date: 17/09/2026
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        public class Noti
        {
            protected class Prefix
            {
                internal const string Validation = "lz.validation.noti.";
                internal const string Exception = "lz.exception.noti.";
            }

            public class Validation
            {
                /// <summary>TargetType = User nhưng UserIds rỗng.</summary>
                public const string UserIdsRequired = Prefix.Validation + "userIdsRequired";

                /// <summary>TargetType = Org nhưng OrgIds rỗng.</summary>
                public const string OrgIdsRequired = Prefix.Validation + "orgIdsRequired";
            }

            public class Exception
            {
                /// <summary>Không xác định được user hiện tại để đọc thông báo.</summary>
                public const string UserMissing = Prefix.Exception + "userMissing";

                /// <summary>Số người nhận vượt giới hạn cấu hình.</summary>
                public const string TooManyRecipients = Prefix.Exception + "tooManyRecipients";

                /// <summary>Không resolve được người dùng nào cho target Org/All.</summary>
                public const string RecipientsEmpty = Prefix.Exception + "recipientsEmpty";
            }
        }
    }
}
