using System.ComponentModel;

namespace Modules.ShareDataWorker.Core.Enums
{
    /// <summary>
    /// Các Enum dùng chung cho Module ShareData (ESHARE V1)
    /// Author: Đạt
    /// Created date: 27/07/2026
    /// </summary>
    public class EshEnums
    {
        public static class DataSourceKind
        {
            [Description("Chọn bảng/cột")]
            public const string FieldPicker = "FIELD_PICKER";

            [Description("Câu lệnh SQL đã duyệt")]
            public const string SavedQuery = "SAVED_QUERY";
        }

        public static class MappingDirection
        {
            [Description("Chiều Gửi đi")]
            public const string Out = "OUT";

            [Description("Chiều Nhận về")]
            public const string In = "IN";
        }

        public static class DataFormat
        {
            [Description("JSON")]
            public const string Json = "JSON";

            [Description("XML")]
            public const string Xml = "XML";

            [Description("CSV")]
            public const string Csv = "CSV";
        }

        public static class PartnerStatus
        {
            [Description("Kích hoạt")]
            public const string Enabled = "ENABLED";

            [Description("Tắt")]
            public const string Disabled = "DISABLED";
        }

        public static class EventFormat
        {
            [Description("JSON")]
            public const string Json = "JSON";

            [Description("Protobuf")]
            public const string Protobuf = "PROTOBUF";

            [Description("Dữ liệu thô")]
            public const string Raw = "RAW";
        }

        public enum DatatypeIdEnum
        {
            [Description("101 - Thông tin chung / luồng giao thông")]
            TrafficFlow = 101,

            [Description("102 - Dữ liệu hình ảnh giao thông (CCTV)")]
            CctvImage = 102,

            [Description("103 - Dữ liệu dò xe (VDS)")]
            VehicleDetection = 103,

            [Description("104 - Dữ liệu thời tiết")]
            Weather = 104,

            [Description("105 - Dữ liệu định danh phương tiện (AVI/RFID)")]
            VehicleIdentification = 105,

            [Description("106 - Dữ liệu kiểm tra tải trọng xe (WIM)")]
            WeighInMotion = 106,

            [Description("107 - Thông tin sự kiện giao thông")]
            TrafficIncident = 107,

            [Description("108 - Thông tin biển báo điện tử (VMS)")]
            VmsDisplay = 108,

            [Description("109 - Thông tin thu phí (ETC)")]
            TollCollection = 109,

            [Description("110 - Trao đổi với người tham gia giao thông")]
            PublicMessaging = 110,

            [Description("111 - Trao đổi với TT QLĐHGT tuyến")]
            InterCenterExchange = 111,
        }

        public static class SubDirection
        {
            [Description("Gửi đi")]
            public const string Outbound = "OUTBOUND";

            [Description("Nhận về")]
            public const string Inbound = "INBOUND";
        }

        public static class SubMode
        {
            [Description("Một lần")]
            public const string Single = "SINGLE";

            [Description("Theo sự kiện")]
            public const string Event = "EVENT";

            [Description("Định kỳ")]
            public const string Periodic = "PERIODIC";

            [Description("Định kỳ theo lô")]
            public const string Batch = "BATCH";

            [Description("Thời gian thực")]
            public const string Realtime = "REALTIME";

            [Description("Theo yêu cầu")]
            public const string OnDemand = "ON_DEMAND";
        }

        public static class SubState
        {
            [Description("Chờ duyệt")]
            public const string Pending = "PENDING";

            [Description("Hoạt động")]
            public const string Active = "ACTIVE";

            [Description("Tạm dừng")]
            public const string Paused = "PAUSED";

            [Description("Từ chối")]
            public const string Rejected = "REJECTED";

            [Description("Đã hủy")]
            public const string Cancelled = "CANCELLED";

            [Description("Hết hạn")]
            public const string Expired = "EXPIRED";
        }

        public static class ExportStatus
        {
            [Description("Thành công")]
            public const string Success = "SUCCESS";

            [Description("Thất bại")]
            public const string Failed = "FAILED";

            [Description("Cảnh báo")]
            public const string Warning = "WARNING";

            [Description("Chạy thử")]
            public const string DryRun = "DRY_RUN";
        }

        public static class RunStatus
        {
            [Description("Rảnh / Chờ chạy")]
            public const string Idle = "IDLE";

            [Description("Đang thực thi")]
            public const string Running = "RUNNING";

            [Description("Tắt")]
            public const string Disabled = "DISABLED";
        }

        public static class PackagingFormat
        {
            [Description("Dữ liệu thô")]
            public const string Raw = "RAW";

            [Description("Nén ZIP")]
            public const string Zip = "ZIP";

            [Description("Nén GZIP")]
            public const string Gzip = "GZIP";
        }

        public static class LogSeverity
        {
            [Description("Thông tin")]
            public const string Info = "INFO";

            [Description("Cảnh báo")]
            public const string Warning = "WARNING";

            [Description("Lỗi")]
            public const string Error = "ERROR";

            [Description("Nghiêm trọng")]
            public const string Critical = "CRITICAL";
        }

        public static class SystemConstants
        {
            [Description("Chưa xác định")]
            public const string Unknown = "UNKNOWN";
        }

        public static class Operator
        {
            [Description("Hệ thống tự động")]
            public const string System = "SYSTEM";

            [Description("Quản trị viên")]
            public const string Admin = "ADMIN";
        }
    }
}
