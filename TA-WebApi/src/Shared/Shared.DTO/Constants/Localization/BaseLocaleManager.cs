namespace Shared.DTO.Constants.Localization
{
    /// <summary>
    /// Nội dung thông báo dịch thuật dùng chung
    /// </summary>
    public class BaseLocaleManager
    {
        protected class Prefix
        {
            internal const string Entity = "lz.entity.base.";
            internal const string Validation = "lz.validation.base.";
            internal const string Exception = "lz.exception.base.";
            internal const string Message = "lz.message.base.";
        }
        /// <summary>
        /// Sử dụng cho dịch thuật dùng chung về kiểm tra dữ liệu
        /// </summary>
        public class BaseValidation
        {
            /// <summary>
            /// The {PropertyName} cannot be empty.
            /// </summary>
            public const string Required = Prefix.Validation + "required";
            /// <summary>
            /// [{PropertyName}] cannot be more than {MaxLength} characters.
            /// </summary>
            public const string MaxLength = Prefix.Validation + "maxLength";

            /// <summary>
            /// [{PropertyName}] cannot be less than {MinLength} characters.
            /// </summary>
            public const string MinLength = Prefix.Validation + "minLength";

            /// <summary>
            ///  ‘Email’ is not a valid email address
            /// </summary>
            public const string Email = Prefix.Validation + "email";

            /// <summary>
            /// [{PropertyName}] only allows numbers.
            /// </summary>
            public const string Number = Prefix.Validation + "number";

            /// <summary>
            /// [{PropertyName}] only allows characters.
            /// </summary>
            public const string Text = Prefix.Validation + "text";

            /// <summary>
            /// [{PropertyName}] only allows characters or numbers.
            /// </summary>
            public const string TextOrNumber = Prefix.Validation + "textOrNumber";

            /// <summary>
            /// The {PropertyName} cannot be empty.
            /// </summary>
            public const string IdRequired = Prefix.Validation + "idRequired";

            /// <summary>
            /// The {PropertyName} does not match any accepted value
            /// </summary>
            public const string MustBeValidValue = Prefix.Validation + "mustBeValidValue";


            /// <summary>
            /// The {PropertyName} must be greater than {ComparisonValue}
            /// </summary>
            public const string GreaterThan = Prefix.Validation + "greaterThan";

            /// <summary>
            /// The {PropertyName} must be greater than or equal to {ComparisonValue}
            /// </summary>
            public const string GreaterThanOrEqual = Prefix.Validation + "greaterThanOrEqual";

            /// <summary>
            /// The {PropertyName} must be less than {ComparisonValue}
            /// </summary>
            public const string LessThan = Prefix.Validation + "lessThan";

            /// <summary>
            /// The {PropertyName} must be less than or equal to {ComparisonValue}
            /// </summary>
            public const string LessThanOrEqual = Prefix.Validation + "lessThanOrEqual";

        }

        /// <summary>
        /// Sử dụng cho dịch thuật dùng chung về báo lỗi
        /// </summary>
        public class BaseException
        {

            /// <summary>
            /// Thông báo dịch thuật về lỗi dữ liệu
            /// </summary>
            #region Data

            /// <summary>
            /// {0} already exists
            /// </summary>
            public const string Exist = Prefix.Exception + "exist";


            /// <summary>
            /// {0} not found
            /// </summary>
            public const string NotFound = Prefix.Exception + "notFound";

            /// <summary>
            /// {0} not exist
            /// </summary>
            public const string NotExist = Prefix.Exception + "notExist";

            /// <summary>
            /// {0} cannot be deleted
            /// </summary>
            public const string CannotDelete = Prefix.Exception + "cannotDelete";

            /// <summary>
            /// Data not found.
            /// </summary>
            public const string DataNotFound = Prefix.Exception + "dataNotFound";

            /// <summary>
            /// Data does not exist.
            /// </summary>
            public const string DataNotExist = Prefix.Exception + "dataNotExist";

            /// <summary>
            /// Data already exists
            /// </summary>
            public const string DataExist = Prefix.Exception + "dataExist";


            /// <summary>
            /// This data cant be parent of itself
            /// </summary>
            public const string CannotSelfParent = Prefix.Exception + "cannotSelfParent";

            /// <summary>
            /// Parent does not exist
            /// </summary>
            public const string ParentNotExist = Prefix.Exception + "parentNotExist";

            /// <summary>
            /// Incorrect accept value
            /// </summary>
            public const string IncorrectAcceptValue = Prefix.Exception + "incorrectAcceptValue";

            #endregion

            /// <summary>
            /// Thông báo dịch thuật về quyền thực hiện
            /// </summary>

            #region Permission

            /// <summary>
            /// You do not have permission to perform this action
            /// </summary>
            public const string NoPermission = Prefix.Exception + "noPermission";

            /// <summary>
            /// You need admin permission to perform this action
            /// </summary>
            public const string RequireAdminPermission = Prefix.Exception + "requireAdminPermission";

            /// <summary>
            /// Illegal operation. Please check again
            /// </summary>
            public const string IllegalOperation = Prefix.Exception + "illegalOperation";


            #endregion

            #region Error

            /// <summary>
            /// Server error. Please try again later
            /// </summary>
            public const string ServerError = Prefix.Exception + "serverError";

            /// <summary>
            /// Data does not match. Please check again
            /// </summary>
            public const string DataNotMatch = Prefix.Exception + "dataNotMatch";



            #endregion

        }
        /// <summary>
        /// Sử dụng cho dịch thuật dùng chung về các thông báo
        /// </summary>
        public class BaseMessage
        {
            /// <summary>
            /// Đã import thành công {0} bản ghi
            /// </summary>
            public const string ImportNumberSuccess = Prefix.Message + "importNumberSuccess";

            public const string AccountNotExist = Prefix.Message + "accountNotExist";

            

        }

        /// <summary>
        /// Sử dung cho dịch thuật dùng chung về các thông tin cơ bản 
        /// </summary>
        public class BaseEntity
        {

           

            /// <summary>
            /// ID
            /// </summary>
            public const string ID = Prefix.Entity + "id";

            /// <summary>
            /// Code
            /// </summary>
            public const string Code = Prefix.Entity + "code";

            /// <summary>
            /// Name
            /// </summary>
            public const string Name = Prefix.Entity + "name";

            /// <summary>
            /// Remark
            /// </summary>
            public const string Remark = Prefix.Entity + "remark";

            /// <summary>
            /// Parent
            /// </summary>
            public const string Pid = Prefix.Entity + "pid";

            /// <summary>
            /// Code or Name
            /// </summary>
            public const string CodeOrName = Prefix.Entity + "codeOrName";



            public const string Info = Prefix.Entity + "info";
            public const string Source = Prefix.Entity + "source";
            public const string Description = Prefix.Entity + "description";
            public const string Content = Prefix.Entity + "content";
            public const string Image = Prefix.Entity + "image";
            public const string Url = Prefix.Entity + "url";
            public const string Type = Prefix.Entity + "type";
            public const string Size = Prefix.Entity + "size";
            public const string OrderNo = Prefix.Entity + "orderNo";
            public const string Status = Prefix.Entity + "status";
            public const string State = Prefix.Entity + "state";
            public const string Lang = Prefix.Entity + "lang";

            /// <summary>
            /// Account
            /// </summary>
            public const string Account = Prefix.Entity + "account";
            public const string Password = Prefix.Entity + "password";


            public const string Data = Prefix.Entity + "data";

            public const string Email = Prefix.Entity + "email";

            public const string Port = Prefix.Entity + "port";

            public const string IpAddress = Prefix.Entity + "ipAddress";
        }
    }
}
