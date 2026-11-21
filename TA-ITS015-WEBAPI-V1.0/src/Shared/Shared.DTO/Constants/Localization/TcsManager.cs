using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.DTO.Constants.Localization
{
    public class TcsManager
    {
        internal class Prefix
        {
            internal const string Message = "lz.message.tcs.";
            internal const string Validation = "lz.validation.tcs.";
            internal const string Exception = "lz.exception.tcs.";
            internal const string Entity = "lz.entity.tcs.";
        }

        public class Entity
        {
            public const string GroupCode = Prefix.Entity + "groupCode";
            public const string MaThe = Prefix.Entity + "maThe";
            public const string TenChuThe = Prefix.Entity + "tenChuThe";
        }

        public class Exception
        {
            /// <summary>
            /// Thẻ đã được sử dụng
            /// </summary>
            public const string ParkingPassIsUsed = Prefix.Exception + "parkingPassIsUsed";

            /// <summary>
            /// Lỗi xảy ra trong quá trình xử lý: {0}
            /// </summary>
            public const string ExceptionError = Prefix.Exception + "exceptionError";

            /// <summary>
            /// Không có dữ liệu trong file
            /// </summary>
            public const string NoDataInFile = Prefix.Exception + "noDataInFile";

            /// <summary>
            /// Không tìm thấy file mẫu để tải xuống
            /// </summary>
            public const string TemplateFileNotFound = Prefix.Exception + "templateFileNotFound";

            /// <summary>
            /// Khoảng thời gian tìm kiếm không được vượt quá 7 ngày
            /// </summary>
            public const string DateRangeMustNotExceed7Days = Prefix.Exception + "dateRangeMustNotExceed7Days";

            /// <summary>
            /// Khoảng thời gian tìm kiếm không được vượt quá 30 ngày
            /// </summary>
            public const string DateRangeMustNotExceed30Days = Prefix.Exception + "dateRangeMustNotExceed30Days";
        }


        public class Message
        {
            /// <summary>
            /// Mã thẻ không tồn tại
            /// </summary>
            public const string CardCodeExists = Prefix.Message + "cardCodeExists";
            /// <summary>
            /// Không có dữ liệu trong file
            /// </summary>
            public const string NotDataInFile = Prefix.Message + "notDataInFile";

            /// <summary>
            /// Biển số xe đã tồn tại
            /// </summary>
            public const string LicensePlateExists = Prefix.Message + "notDataInFile";

            /// <summary>
            /// Ngày hết hạn phải lớn hơn ngày hiện tại
            /// </summary>
            public const string ExpirationDateMustBeGreaterThanToday = Prefix.Message + "expirationDateMustBeGreaterThanToday";

            /// <summary>
            /// Ngày hết hạn phải lớn hơn ngày bắt đầu
            /// </summary>
            public const string ExpirationDateMustBeGreaterThanStartDate = Prefix.Message + "expirationDateMustBeGreaterThanStartDate";

            /// <summary>
            /// Ngày kết thúc phải lớn hơn ngày bắt đầu
            /// </summary>
            public const string EndDateMustBeGreaterThanStartDate = Prefix.Message + "endDateMustBeGreaterThanStartDate";

            /// <summary>
            /// Mã loại vé không tồn tai
            /// </summary>
            public const string TicketTypeExist = Prefix.Message + "ticketTypeExist";

            /// <summary>
            /// Đã tồn tại biển số hoặc mã thẻ
            /// </summary>
            public const string CardCodeOrTicketTypeExist = Prefix.Message + "cardCodeOrTicketTypeExist";

            /// <summary>
            /// Đã tồn tại tên trong hệ thống
            /// </summary>
            public const string NameExists = Prefix.Message + "nameExists";

            /// <summary>
            /// Mã không tồn tại 
            /// </summary>
            public const string CodeExists = Prefix.Message + "codeExists";

            /// <summary>
            /// Mã ID đã tồn tại
            /// </summary>
            public const string IdAlreadyExists = Prefix.Message + "idAlreadyExists";

            /// <summary>
            /// Ngày bắt đầu phải lớn hơn ngày hết hạn
            /// </summary>
            public const string StartDateMustBeGreaterThanEndDate = Prefix.Message + "startDateMustBeGreaterThanEndDate";

            /// <summary>
            /// Ngày hiệu lực phải lớn hơn ngày vô hiệu
            /// </summary>
            public const string StartDateMustBeGreaterThanEndDatePriority = Prefix.Message + "startDateMustBeGreaterThanEndDatePriority";

            /// <summary>
            /// Ngày vô hiệu phải lớn hơn ngày hiện tại
            /// </summary>
            public const string StartDateMustBeGreaterThanNowDatePriority = Prefix.Message + "startDateMustBeGreaterThanNowDatePriority";

        }

    }
}
