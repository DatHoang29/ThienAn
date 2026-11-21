using System.ComponentModel;

namespace ShareDataWorker.Core.Enums
{
    /// <summary>
    /// Các Enum dùng chung cho Module ShareData (ESHARE V1)
    /// Author: Đạt
    /// Created date: 27/07/2026
    /// </summary>
    public class ShareDataEnum
    {

        public enum PacketPolicy
        {
            [Description("Ảnh chụp toàn bộ (Snapshot)")]
            Snapshot = 1,

            [Description("Tăng dần liên tục (AlwaysIncremental)")]
            AlwaysIncremental = 2,

            [Description("Chưa sẵn sàng xử lý (NotReady)")]
            NotReady = 3,

            [Description("Vô hiệu hóa (Disabled)")]
            Disabled = 4
        }

        public static class FlowType
        {
            [Description("Chia sẻ đối tác ngoài")]
            public const string External = "EXTERNAL";
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
            InterCenterExchange = 111
        }

        public static class ExportStatus
        {
            [Description("Thành công")]
            public const string Success = "SUCCESS";

            [Description("Đã xếp hàng")]
            public const string Queued = "QUEUED";

            [Description("Thất bại")]
            public const string Failed = "FAILED";
        }

        public static class PduType
        {
            [Description("Gói dữ liệu")]
            public const string DataPacket = "DATA_PACKET";

            [Description("Gói tập tin")]
            public const string FilePacket = "FILE_PACKET";
        }

        public static class ExportMessage
        {
            [Description("Không có dữ liệu mới")]
            public const string NoNewData = "Không có dữ liệu mới";
            public const string CursorSqlRisk = "Câu truy vấn nối đuôi có rủi ro";
        }

        public static class SystemConstants
        {
            [Description("Chưa xác định")]
            public const string Unknown = "UNKNOWN";
        }

        public static class ProtocolProfile
        {
            [Description("Mã hoá JSON (Mở rộng)")]
            public const string Json = "JSON";
        }

        public static class Operator
        {
            [Description("Hệ thống tự động")]
            public const string System = "SYSTEM";
        }
    }
}
