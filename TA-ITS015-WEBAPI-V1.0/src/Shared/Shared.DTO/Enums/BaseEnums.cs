using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTO.Enums
{
    public class BaseEnums
    {
        /// <summary>
        /// Status Enum
        /// </summary>
        public enum StatusEnum
        {
            [Description("Enable")]
            Enable = 1,
            [Description("Disable")]
            Disable = 0
        }
        public enum DefaultEnum
        {
            [Description("Default")]
            Default = 1,
            [Description("None")]
            None = 0
        }

        /// <summary>
        /// Enum sharedata
        /// Author: HieuNV
        /// </summary>
        public enum SessionState
        {
            [Description("Connected")]
            Connected = 1,
            [Description("Disconnected")]
            Disconnected = 0
        }

        public enum SubSubscriptionState
        {
            [Description("Pending")]
            Pending = 0,
            [Description("Active")]
            Active = 1,
            [Description("Paused")]
            Paused = 2,
            [Description("Rejected")]
            Rejected = 3,
            [Description("Cancelled")]
            Cancelled = 4,
            [Description("Expired")]
            Expired = 5,
        }
        public enum Direction
        {
            [Description("Inbound")]
            Inbound = 1,
            [Description("Outbound")]
            Outbound = 0,
            /// <summary>
            /// Dùng chung cho cả hai chiều — CHỈ hợp lệ với ShareDataMapping.
            /// Đặt cho ShareDataSubscription là SAI: một đăng ký chỉ có thể một chiều,
            /// và hai worker sẽ tranh nhau cùng một dòng trạng thái (SerialNbr, NextTimeRun).
            /// Cả hai worker đều phát hiện và cảnh báo trường hợp này.
            /// </summary>
            [Description("Both")]
            Both = 2
        }

        public enum LogTypeEnum
        {
            [Description("Transfer")]
            Transfer = 1,
            [Description("CONFIG")]
            CONFIG = 0
        }

        public enum ShareDataScope
        {
            [Description("TCVN")]
            TCVN = 1,
            [Description("Partner")]
            Partner = 0
        }

        public enum AlertSource
        {
            /// <summary>Phiên kết nối C2C: mất kết nối, timeout, login bị từ chối.</summary>
            [Description("Session")]
            Session = 0,
            /// <summary>Gói tin: thiếu field bắt buộc, không có dữ liệu, bản chụp lệch mã nguồn.</summary>
            [Description("Packet")]
            Packet = 1,
            /// <summary>Đăng ký chia sẻ: quá hạn, bị từ chối, lịch gửi sai.</summary>
            [Description("Subscription")]
            Subscription = 2,
            /// <summary>Giao thức: sai định dạng PDU, mã hóa/giải mã lỗi.</summary>
            [Description("Protocol")]
            Protocol = 3,
            /// <summary>Phễu lọc: thiếu bộ mã, quy đổi đơn vị thất bại, biểu thức lỗi.</summary>
            [Description("Funnel")]
            Funnel = 4,
        }

        public enum TransferDirection
        {
            //SND (gửi đi)
            [Description("SND")]
            SND = 0,
            /// RCV (nhận về)
            [Description("RCV")]
            RCV = 1,
        }

        /// <summary>
        /// Trạng thái xử lý gói tin thô nhận về — ShareDataInboundPacket.ProcessState.
        /// Author: HieuNV
        /// </summary>
        public enum InboundProcessState
        {
            [Description("PENDING")]
            Pending = 0,
            [Description("PROCESSING")]
            Processing = 1,
            [Description("DONE")]
            Done = 2,
            [Description("FAILED")]
            Failed = 3,
            /// <summary>Gói đã lưu nhưng chưa có đăng ký nhận hợp lệ — worker không xử lý.</summary>
            [Description("NO_SUBSCRIPTION")]
            NoSubscription = 4
        }

        public enum IsRequired
        {
            [Description("No-Requỉed")]
            NoRequired = 0,
            [Description("IsRequired")]
            IsRequired = 1,
        }

        /// <summary>
        /// Hồ sơ mã hóa dữ liệu — ShareDataPartner.ProtocolProfile.
        /// TCVN 13600-2 (ASN.1) / 13600-3 (XML Profile A).
        /// Author: HieuNV
        /// </summary>
        public enum ProtocolProfile
        {
            [Description("ASN")]
            Asn = 0,
            [Description("XML_A")]
            XmlA = 1
        }

        /// <summary>
        /// Bên khởi tạo kết nối — ShareDataPartner.InitiatorMode (ISO 14827 datexLogin-Initiator-cd).
        /// Author: HieuNV
        /// </summary>
        public enum InitiatorMode
        {
            [Description("clientInitiated")]
            ClientInitiated = 0,
            [Description("serverInitiated")]
            ServerInitiated = 1
        }

        /// <summary>
        /// Chiều phiên kết nối C2C — ShareDataSession.Direction.
        /// KHÁC Direction (chiều đăng ký chia sẻ Inbound/Outbound).
        /// Author: HieuNV
        /// </summary>
        public enum SessionDirection
        {
            [Description("OUT")]
            Out = 0,
            [Description("IN")]
            In = 1
        }

        /// <summary>
        /// Trạng thái máy trạng thái phiên kết nối — ShareDataSession.State.
        /// KHÁC SessionState (chỉ 2 giá trị đã/chưa kết nối, dùng cho ShareDataPartner.SessionState).
        /// Author: HieuNV
        /// </summary>
        public enum SessionFsmState
        {
            [Description("0. Idle (Khởi tạo)")]
            Idle = 0,

            [Description("1. Connecting (Đang kết nối TCP)")]
            Connecting = 1,

            [Description("2. Connected (Đã nối TCP)")]
            Connected = 2,

            [Description("3. Authenticating (Đang xác thực Login)")]
            Authenticating = 3,

            [Description("4. Authenticated (Đã xác thực)")]
            Authenticated = 4,

            [Description("5. Active (Phiên đang hoạt động)")]
            Active = 5,

            [Description("6. Suspended (Tạm dừng - đứt nhịp tim)")]
            Suspended = 6,

            [Description("7. Disconnecting (Đang ngắt kết nối)")]
            Disconnecting = 7,

            [Description("8. Closed (Đã đóng kết nối)")]
            Closed = 8
        }

        /// <summary>
        /// Chế độ đăng ký chia sẻ — ShareDataSubscription.Mode (SubscriptionMode ISO 14827-2).
        /// Author: HieuNV
        /// </summary>
        public enum SubMode
        {
            [Description("SINGLE")]
            Single = 0,
            [Description("EVENT")]
            Event = 1,
            [Description("PERIODIC")]
            Periodic = 2
        }

        /// <summary>
        /// Tình trạng hồ sơ ánh xạ của một đăng ký chia sẻ.
        ///
        /// SUY RA lúc truy vấn theo (đối tác × gói tin × chiều), KHÔNG lưu cột trong CSDL:
        /// hồ sơ có thể bị bật, tắt, xoá bất kỳ lúc nào mà đăng ký không hề biết, nên mọi bản
        /// chụp đều sẽ lệch. Luật suy phải khớp đúng luật worker dùng lúc chạy.
        ///
        /// Author: HieuNV
        /// </summary>
        public enum SubMappingState
        {
            /// <summary>Chưa có hồ sơ ánh xạ nào cho bộ ba này — phải sang màn Ánh xạ tạo mới.</summary>
            [Description("NO_MAPPING")]
            NoMapping = 0,

            /// <summary>Có hồ sơ và đang bật — đăng ký được phép bật.</summary>
            [Description("READY")]
            Ready = 1,

            /// <summary>Có hồ sơ nhưng đang tắt — chỉ cần bật hồ sơ lên, không phải tạo mới.</summary>
            [Description("DISABLED")]
            Disabled = 2
        }

        /// <summary>
        /// Định dạng dữ liệu xuất bản — cột Format của Subscription / Mapping / ActivityLog.
        /// Author: HieuNV
        /// </summary>
        public enum PublishFormat
        {
            [Description("DATA")]
            Data = 0,
            [Description("FILE")]
            File = 1
        }

        /// <summary>
        /// Mức độ cảnh báo — ShareDataAlertLog.Severity.
        /// Author: HieuNV
        /// </summary>
        public enum AlertSeverity
        {
            [Description("warning")]
            Warning = 0,
            [Description("error")]
            Error = 1
        }

        /// <summary>
        /// Hành động ghi vào nhật ký hoạt động — ShareDataActivityLog.Action.
        /// Nhóm CONFIG: Create..Reject. Nhóm TRANSFER: Send..Export.
        /// Author: HieuNV
        /// </summary>
        public enum ActivityAction
        {
            [Description("CREATE")]
            Create = 0,
            [Description("UPDATE")]
            Update = 1,
            [Description("DELETE")]
            Delete = 2,
            [Description("CONNECT")]
            Connect = 3,
            [Description("DISCONNECT")]
            Disconnect = 4,
            [Description("PAUSE")]
            Pause = 5,
            [Description("RESUME")]
            Resume = 6,
            [Description("CANCEL")]
            Cancel = 7,
            [Description("APPROVE")]
            Approve = 8,
            [Description("REJECT")]
            Reject = 9,
            [Description("SEND")]
            Send = 10,
            [Description("RECEIVE")]
            Receive = 11,
            [Description("EXPORT")]
            Export = 12
        }

        /// <summary>
        /// Loại đối tượng bị tác động — ShareDataActivityLog.TargetType.
        /// Author: HieuNV
        /// </summary>
        public enum ShareDataTargetType
        {
            [Description("Partner")]
            Partner = 0,
            [Description("Subscription")]
            Subscription = 1,
            [Description("Session")]
            Session = 2,
            [Description("EventSource")]
            EventSource = 3,
            [Description("Packet")]
            Packet = 4,
            [Description("Mapping")]
            Mapping = 5,
            [Description("CodeSet")]
            CodeSet = 6,
            [Description("PacketField")]
            PacketField = 7
        }

        /// <summary>
        /// Loại đối tượng CSDL mà gói tin đọc dữ liệu từ đó — ShareDataTable.ObjectType.
        /// Author: HieuNV
        /// </summary>
        public enum PacketObjectType
        {
            [Description("TABLE")]
            Table = 1,
            [Description("VIEW")]
            View = 2
        }

        /// <summary>
        /// Kiểu join giữa các bảng của gói tin — ShareDataTable.JoinType.
        /// Author: HieuNV
        /// </summary>
        public enum PacketJoinType
        {
            [Description("INNER")]
            Inner = 0,
            [Description("LEFT")]
            Left = 1
        }

        /// <summary>
        /// Chế độ đồng bộ dữ liệu ShareData (1-Snapshot, 2-Incremental).
        /// </summary>
        public enum SyncMode
        {
            [Description("Gửi toàn bộ (Snapshot)")]
            Snapshot = 1,

            [Description("Gửi nối đuôi (Incremental)")]
            Incremental = 2
        }

        /// <summary>
        /// Author: HieuNV
        /// Date: 31/07/2026
        /// HeroEnum -- Wp
        /// </summary>
        public enum HeroEnum
        {
            [Description("Enable")]
            Enable = 1,
            [Description("Disable")]
            Disable = 0
        }


        /// <summary>
        /// Author: HieuNV
        /// Date: 31/07/2026
        /// GenLockIn -- Vw
        /// </summary>
        public enum GenlockConnectEnum
        {
            [Description("Connect")]
            Connect = 1,
            [Description("DisConnect")]
            DisConnect = 0
        }

        /// <summary>
        /// Author: HieuNV
        /// Date: 31/07/2026
        /// VwSceneWindow -- Vw
        /// </summary>
        public enum SceneWindowVisible
        {
            [Description("InVisible")]
            InVisible = 0,
            [Description("Visible")]
            Visible = 1
        }

        /// <summary>
        /// Author: HieuNV
        /// Date: 03/08/2026
        /// VwSceneWindow -- Vw
        /// </summary>
        public enum ActiveScene
        {
            [Description("DeActivate")]
            DeActivate = 0,
            [Description("Activate")]
            Activate = 1
        }

        /// <summary>
        /// Author: HieuNV
        /// Date: 31/07/2026
        /// ScreenState -- Vw
        /// </summary>
        public class ScreenState
        {
            [Description("Online")]
            public const string Online = "Online";

            [Description("Offline")]
            public const string Offline = "Offline";
            [Description("Warning")]
            public const string Warning = "Warning";
        }

        /// <summary>
        /// Author: HieuNV
        /// Date: 31/07/2026
        /// SignalStatus -- Vw
        /// </summary>
        public class SignalStatus
        {
            [Description("Signal")]
            public const string Signal = "Signal";
            [Description("NoSignal")]
            public const string NoSignal = "NoSignal";

            [Description("abnormal")]
            public const string Abnormal = "Abnormal";
        }

        /// <summary>
        /// Author: HieuNV
        /// Date: 31/07/2026
        /// SuccessEnum -- Vw
        /// </summary>
        public enum SuccessEnums
        {
            [Description("Success")]
            Success = 1,
            [Description("Fail")]
            Fail = 0
        }

        public class Statistical
        {
            public const string Year = "Year";
            public const string Month = "Month";
            public const string Date = "Date";
            public const string Hour = "Hour";

        }

        public class SysGenderTypeEnum
        {
            public const string Unknown = "unknown";
            public const string Male = "male";
            public const string Female = "female";
        }

        /// <summary>
        /// Account Type Enum
        /// </summary>
        [Description("AccountType")]
        public enum AccountTypeEnum
        {
            /// <summary>
            /// SuperAdmin
            /// </summary>
            [Description("SuperAdmin")]
            [Display(Name = "superadmin")]
            SuperAdmin = 111,

            /// <summary>
            /// SystemAdmin
            /// </summary>
            [Description("SystemAdmin")]
            [Display(Name = "sysadmin")]
            SysAdmin = 222,

            /// <summary>
            /// User
            /// </summary>
            [Description("User")]
            [Display(Name = "user")]
            NormalUser = 333,

            /// <summary>
            /// Guest
            /// </summary>
            [Description("Guest")]
            [Display(Name = "guest")]
            Guest = 444,

            /// <summary>
            /// Viewer
            /// </summary>
            [Description("Viewer")]
            [Display(Name = "viewer")]
            Viewer = 555
        }


        [Description("TenantType")]
        public enum TenantTypeEnum
        {
            /// <summary>
            /// ID
            /// </summary>
            [Description("Id")]
            Id = 0,

            /// <summary>
            /// DB
            /// </summary>
            [Description("Db")]
            Db = 1,
        }

        [Description("System Menu Type Enum")]
        public enum MenuTypeEnum
        {
            /// <summary>
            /// Directory
            /// </summary>
            [Description("Directory")]
            Dir = '1',

            /// <summary>
            /// Menu
            /// </summary>
            [Description("Menu")]
            Menu = 2,

            /// <summary>
            /// Button
            /// </summary>
            [Description("Button")]
            Btn = 3
        }

        public class MenuTypeEnumConst
        {
            public const string Dir = "dir";
            public const string Menu = "menu";
            public const string Btn = "btn";
        }
        /// <summary>
        /// Yes or No Enum
        /// </summary>
        [Description("Yes or No Enum")]
        public enum YesNoEnum
        {
            /// <summary>
            /// Yes
            /// </summary>
            [Description("Yes")]
            Y = 1,

            /// <summary>
            /// No
            /// </summary>
            [Description("No")]
            N = 0
        }

        [Description("Role Data Scope Enum")]
        public enum DataScopeEnum
        {
            /// <summary>
            /// All Data
            /// </summary>
            [Description("All Data")]
            All = 1,

            /// <summary>
            /// Current Organization and Below Data
            /// </summary>
            [Description("Current Organization and Below Data")]
            OrgChild = 2,

            /// <summary>
            /// Current Organization Data
            /// </summary>
            [Description("Current Organization Data")]
            Org = 3,

            /// <summary>
            /// Only My Data
            /// </summary
            [Description("Seft Data")]
            Self = 4,

            /// <summary>
            /// Manual Define
            /// </summary>
            [Description("Manual Define")]
            Define = 5
        }

        public class SysAccessTypeEnum
        {
            public const string Jwt = "jwt";
        }


        /// <summary>
        /// Author: Ngọc Thắng
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: TmsIncidentStateEnum
        /// </summary>
        public enum TmsIncidentStateEnum
        {
            Skipped = 0, // Đã bỏ qua
            Registration = 1, // Đăng ký mới
            Verified = 2, // Đã xác thực
            InProgress = 3, // Đang được xử lý
            Finished = 4, // Đã xử lý xong
            Canceled = 5, // Đã hủy bỏ
            Updated = 99, // Đã cập nhật
            Deleted = 100 // Đã được xóa
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: TmsIncidentActionEnum
        /// </summary>
        public enum TmsIncidentActionEnum
        {
            Skip = 0, // Bỏ qua
            Registration = 1, // Đăng ký
            Verify = 2, // Xác thực
            Execute = 3, // Xử lý
            Finish = 4, // Kết thúc
            Cancel = 5, // Hủy bỏ
            LiveCam = 96, // Live cam
            Detail = 97, // Chi tiết
            Report = 98, // Báo cáo
            Update = 99, // Cập nhật
            Delete = 100 // Xóa bỏ
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: TmsWorkExecuteStateEnum
        /// </summary>
        public enum TmsWorkExecuteStateEnum
        {
            Waiting = 0, // Chưa thực hiện
            Executed = 1, // Đã thực hiện
            Skipped = 2 // Đã bỏ qua
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Enum trạng thái visible của thiết bị
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: TmsEquipmentVisibleStateEnum
        /// </summary>
        public enum TmsEquipmentVisibleStateEnum
        {
            Visible = 1, // Hiển thị
            Invisible = 0 // Ẩn
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum nhóm thao tác xử lý incident
        public static class AccountOrgCodeEnum
        {
            public const string GroupLeader = "group_leader"; // Nhóm trưởng ca
            public const string GroupSupervisor = "group_supervisor"; // Nhóm giám sát
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum đối tượng phát thông báo phân hệ sự cố
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: IncidentBroadcastObjectEnum
        /// </summary>
        public enum IncidentBroadcastObjectEnum
        {
            Incident = 0,
            WorkExecute = 1,

            /// <summary>
            /// Author: SonTH - 24/07/2026
            /// Biến động của một lần tự động xử lý sự cố (TmsIncidentAutomationRun): tạo lịch cảnh báo,
            /// bắt đầu chạy, kết thúc, hủy. Frontend dùng để cập nhật banner cảnh báo tức thì.
            /// </summary>
            AutomationRun = 2
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum trạng thái hoat động của thiết bị
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: TmsEquipmentStateEnum
        /// </summary>
        public enum TmsEquipmentStateEnum
        {
            Inactive = 0, // Không kích hoạt
            Active = 1, // Đang kích hoạt
            Error = 2, // Lỗi
            Fault = 3, // Hỏng
            CannotConnected = 4 // Không thể kết nối
        }

        /// <summary>
        /// Author: Hieu NV
        /// Description: Enum định dạng trạng thái sử dụng của bảng giá
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\baseEnum.ts: PriceListUseStateEnum
        /// </summary>
        public enum PriceListUseStateEnum
        {
            Old = 0,     // Bảng giá cũ
            InUse = 1,   // Đang sử dụng
            UpComming = 2, // Chưa áp dụng / chờ xác nhận
        }


        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum định dạng ngày tháng
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\baseEnum.ts: DateTimeFormatEnum
        /// </summary>
        public static class DateTimeFormatEnum
        {
            public const string DateDefault = "dd/MM/yyyy"; // Định đạng hiển thị ngày mặc định
            public const string TimeDefault = "HH:mm:ss"; // Định dạng hiển thị thời gian mặc định
            public const string DateTimeDefault = "dd/MM/yyyy HH:mm:ss"; // Định dạng hiển thị thời gian mặc định
            public const string DateTimeDefaultRevert = "HH:mm:ss dd/MM/yyyy"; // Định dạng hiển thị thời gian mặc định
            public const string DateTimeValidInput = "yyyy-MM-ddTHH:mm:ss"; // Định dạng ngày tháng hợp lệ tương ứng backend

            public const string MonthYearDefault = "MM/yyyy"; // Định đạng hiển thị tháng và năm mặc định
            public const string DayMonthDefault = "dd/MM"; // Định đạng hiển thị ngày và tháng mặc định
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum code cấu hình SysConfigType | SysConfigData
        /// </summary>
        public static class SysConfigTypeCodeEnum
        {
            public const string TmsZoneTypeConfigCode = "tms_zone_type"; // Phân loại zone: Tuyến, đoạn, zone
            public const string TmsVehicleTypeConfigCode = "vehicle_type"; // Phân loại xe: 6m, 12m...
            public const string TmsDirectionConfigCode = "direction_config_type"; // Phân loại hướng: B-N, N-B...
            public const string TmsDirectionAllCode = "direction_all_value"; // Giá trị tương ứng "Tất Cả" hướng...
            public const string TmsSourceDetectIncident = "tms_source_detect"; // Nguồn phát hiện sự cố
            public const string TmsWorkexecuteStatus = "tms_workexecute_status"; // Trạng thái thực thi công việc sự cố
            public const string TmsIncidentStatus = "tms_incident_status"; // Trạng thái sự cố

            public const string TmsEquipmentState = "equipment_state"; // Trạng thái hoạt động của thiết bị
            public const string TmsEquipmentStateInputType = "equipment_history_type"; // Đối tượng cập nhật trạng thái hoạt động thiết bị

            // Bảng ma trận vận tốc / lưu lượng theo giờ. Dải màu quy ước: Code = khoảng giá trị
            // ("-30" | "30-40" | "90-"), Name = nhãn chú giải, Value = mã màu, OrderNo = thứ tự dải.
            public const string TmsTrafficMatrixMetric = "tms_traffic_matrix_metric"; // Chế độ xem: vận tốc / lưu lượng
            public const string TmsTrafficMatrixSpeedBand = "tms_traffic_matrix_speed_band"; // Dải màu theo vận tốc
            public const string TmsTrafficMatrixVolumeBand = "tms_traffic_matrix_volume_band"; // Dải màu theo lưu lượng
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum code cấu hình SysOpConfig
        /// </summary>
        public static class BaseOpConfigCodeEnum
        {
            public const string DayWorkTime = "day_operation";
            public const string DayWorkShift = "shift_operation";
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum loại báo cáo lưu lượng giao thông
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: TmsTrafficDataReportTypeEnum
        /// </summary>
        public static class TmsTrafficDataReportTypeEnum
        {
            public const string TrafficDayReportByShift = "traffic_day_report_by_shift"; // Báo cáo theo ca trong ngày
            public const string TraffiDayReportByTime = "traffic_day_report_by_time"; // Báo cáo theo thời gian trong ngày
            public const string TrafficMonthReportByTime = "traffic_month_report_by_time"; // Báo cáo theo ca trong tháng
            public const string TrafficMonthReportByShift = "traffic_month_report_by_shift"; // Báo cáo theo giờ trong tháng
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum ca làm việc trong ngày
        /// </summary>
        public static class WorkShiftEnum
        {
            public const string WorkShift = "work_shift";
            public const string WorkTime = "work_time";

            public const string WorkShift1 = "work_shift_1";
            public const string WorkShift2 = "work_shift_2";
            public const string WorkShift3 = "work_shift_3";

            public const string WorkTime1 = "work_time_1";
            public const string WorkTime2 = "work_time_2";
            public const string WorkTime3 = "work_time_3";
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum loại kích thước xe
        /// </summary>
        public static class TmsVehicleLengthEnum
        {
            public const string Short = "short";
            public const string Medium = "medium";
            public const string Long = "long";
            public const string VeryLong = "very_long";
            public const string ExtraLong = "extra_long";
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum một số cấu hình đặc biệt
        /// </summary>
        public static class SpecialConfigEnum
        {
            public const string AllDirection = "0";
        }

        public enum TriggerType
        {
            [Description("Schedule")]
            Schedule = 1,

            [Description("Manual")]
            Manual = 2,

            [Description("Test")]
            Test = 3,
        }
    }
}
