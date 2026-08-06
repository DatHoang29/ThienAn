namespace Module.ShareData.Core.Constants
{
    /// <summary>
    /// Description: Bộ mã nghiệp vụ phân hệ Chia sẻ dữ liệu.
    ///              Lưu DB dạng chuỗi, khớp 1:1 với giá trị FE gửi lên (xem services/sharedata/common.types.ts).
    ///              Nhãn hiển thị lấy từ danh mục SysConfigType/SysConfigData (nhóm sharedata_*).
    /// Created date: 2026-08-04
    /// </summary>
    public static class ShareDataConst
    {
        /// <summary>Hồ sơ mã hóa dữ liệu (TCVN 13600-2 ASN / 13600-3 XML Profile A).</summary>
        public static class ProtocolProfile
        {
            public const string Asn = "ASN";
            public const string XmlA = "XML_A";

            public static readonly string[] All = { Asn, XmlA };
        }

        /// <summary>Trạng thái cấu hình đối tác.</summary>
        public static class PartnerStatus
        {
            public const string Configured = "CONFIGURED";
            public const string Enabled = "ENABLED";
            public const string Disabled = "DISABLED";

            public static readonly string[] All = { Configured, Enabled, Disabled };
        }

        /// <summary>Bên khởi tạo kết nối (ISO 14827 datexLogin-Initiator-cd).</summary>
        public static class InitiatorMode
        {
            public const string ClientInitiated = "clientInitiated";
            public const string ServerInitiated = "serverInitiated";

            public static readonly string[] All = { ClientInitiated, ServerInitiated };
        }

        /// <summary>Trạng thái phiên kết nối C2C (máy trạng thái SessionFsm).</summary>
        public static class SessionState
        {
            public const string Idle = "IDLE";
            public const string Connecting = "CONNECTING";
            public const string LoginWait = "LOGIN_WAIT";
            public const string Active = "ACTIVE";
            public const string Closing = "CLOSING";
            public const string Closed = "CLOSED";
            public const string ReconnectWait = "RECONNECT_WAIT";
            public const string Rejected = "REJECTED";
            public const string Timeout = "TIMEOUT";

            public static readonly string[] All =
            {
                Idle, Connecting, LoginWait, Active, Closing, Closed, ReconnectWait, Rejected, Timeout
            };
        }

        /// <summary>Chiều phiên kết nối.</summary>
        public static class SessionDirection
        {
            public const string Out = "OUT";
            public const string In = "IN";

            public static readonly string[] All = { Out, In };
        }

        /// <summary>Chiều đăng ký chia sẻ.</summary>
        public static class SubDirection
        {
            /// <summary>Gửi đi — mình cấp dữ liệu cho đối tác.</summary>
            public const string Outbound = "OUTBOUND";

            /// <summary>Nhận về — đối tác cấp dữ liệu cho mình.</summary>
            public const string Inbound = "INBOUND";

            public static readonly string[] All = { Outbound, Inbound };
        }

        /// <summary>Chế độ đăng ký (SubscriptionMode ISO 14827-2).</summary>
        public static class SubMode
        {
            public const string Single = "SINGLE";
            public const string Event = "EVENT";
            public const string Periodic = "PERIODIC";

            public static readonly string[] All = { Single, Event, Periodic };
        }

        /// <summary>Trạng thái đăng ký. PAUSED = tắt tạm thời, bật lại được.</summary>
        public static class SubState
        {
            public const string Pending = "PENDING";
            public const string Active = "ACTIVE";
            public const string Paused = "PAUSED";
            public const string Rejected = "REJECTED";
            public const string Cancelled = "CANCELLED";
            public const string Expired = "EXPIRED";

            public static readonly string[] All = { Pending, Active, Paused, Rejected, Cancelled, Expired };

            /// <summary>Các trạng thái còn "sống" — dùng để chặn tạo trùng.</summary>
            public static readonly string[] Alive = { Pending, Active, Paused };

            /// <summary>Các trạng thái kết thúc — không chuyển tiếp được nữa.</summary>
            public static readonly string[] Finished = { Rejected, Cancelled, Expired };
        }

        /// <summary>Định dạng dữ liệu xuất bản.</summary>
        public static class PublishFormat
        {
            public const string Data = "DATA";
            public const string File = "FILE";

            public static readonly string[] All = { Data, File };
        }

        /// <summary>Chiều ánh xạ dữ liệu.</summary>
        public static class MappingDirection
        {
            public const string Out = "OUT";
            public const string In = "IN";

            public static readonly string[] All = { Out, In };
        }

        /// <summary>Loại nguồn dữ liệu.</summary>
        public static class DataSourceKind
        {
            public const string FieldPicker = "FIELD_PICKER";
            public const string SavedQuery = "SAVED_QUERY";

            public static readonly string[] All = { FieldPicker, SavedQuery };
        }

        /// <summary>Kiểu lịch gửi trong ScheduleJson.</summary>
        public static class ScheduleKind
        {
            public const string Continuous = "continuous";
            public const string Daily = "daily";

            public static readonly string[] All = { Continuous, Daily };
        }

        /// <summary>Nhóm nhật ký hoạt động — tương ứng 2 tab của màn Lịch sử.</summary>
        public static class LogType
        {
            /// <summary>Tab "Cấu hình": thêm/sửa/xóa đối tác, đăng ký, nguồn dữ liệu, ánh xạ...</summary>
            public const string Config = "CONFIG";

            /// <summary>Tab "Truyền nhận": gửi/nhận gói tin, xuất file.</summary>
            public const string Transfer = "TRANSFER";

            public static readonly string[] All = { Config, Transfer };
        }

        /// <summary>Hành động được ghi vào nhật ký hoạt động.</summary>
        public static class ActivityAction
        {
            // Nhóm CONFIG
            public const string Create = "CREATE";
            public const string Update = "UPDATE";
            public const string Delete = "DELETE";
            public const string Connect = "CONNECT";
            public const string Disconnect = "DISCONNECT";
            public const string Pause = "PAUSE";
            public const string Resume = "RESUME";
            public const string Cancel = "CANCEL";
            public const string Approve = "APPROVE";
            public const string Reject = "REJECT";

            // Nhóm TRANSFER
            public const string Send = "SEND";
            public const string Receive = "RECEIVE";
            public const string Export = "EXPORT";

            public static readonly string[] ConfigActions =
            {
                Create, Update, Delete, Connect, Disconnect, Pause, Resume, Cancel, Approve, Reject
            };

            public static readonly string[] TransferActions = { Send, Receive, Export };
        }

        /// <summary>Loại đối tượng bị tác động, ghi vào ShareDataActivityLog.TargetType.</summary>
        public static class TargetType
        {
            public const string Partner = "Partner";
            public const string Subscription = "Subscription";
            public const string Session = "Session";
            public const string DataSource = "DataSource";
            public const string MappingProfile = "MappingProfile";
            public const string EventSource = "EventSource";

            public static readonly string[] All =
            {
                Partner, Subscription, Session, DataSource, MappingProfile, EventSource
            };
        }

        /// <summary>Chiều truyền gói tin trong nhật ký truyền nhận.</summary>
        public static class TransferDirection
        {
            /// <summary>Gửi đi.</summary>
            public const string Snd = "SND";

            /// <summary>Nhận về.</summary>
            public const string Rcv = "RCV";

            public static readonly string[] All = { Snd, Rcv };
        }

        /// <summary>Kết quả của một thao tác ghi trong nhật ký.</summary>
        public static class ActivityStatus
        {
            public const string Success = "SUCCESS";
            public const string Failed = "FAILED";

            public static readonly string[] All = { Success, Failed };
        }

        /// <summary>Lý do ngắt kết nối / hủy đăng ký thường dùng.</summary>
        public static class Reason
        {
            public const string ClientRequested = "clientRequested";
            public const string PartnerDisabled = "partnerDisabled";
            public const string DeletedByUser = "deletedByUser";
            public const string Other = "other";
        }

        /// <summary>11 loại dữ liệu chia sẻ (Bảng 1 mục 1419).</summary>
        public static class Datatype
        {
            public const int Min = 101;
            public const int Max = 111;

            /// <summary>101 - Thông tin giao thông</summary>
            public const int TrafficInfo = 101;

            /// <summary>102 - Thông tin VMS</summary>
            public const int VmsInfo = 102;

            /// <summary>103 - Sự cố xảy ra</summary>
            public const int Incident = 103;

            /// <summary>104 - Sự cố (bổ sung)</summary>
            public const int IncidentExtra = 104;

            /// <summary>105 - Tình trạng đường</summary>
            public const int RoadCondition = 105;

            /// <summary>106 - Thời tiết</summary>
            public const int Weather = 106;

            /// <summary>107 - Quản lý đường</summary>
            public const int RoadManagement = 107;

            /// <summary>108 - Đỗ xe</summary>
            public const int Parking = 108;

            /// <summary>109 - Phát hiện xe</summary>
            public const int VehicleDetection = 109;

            /// <summary>110 - Hình ảnh/video (bắt buộc phê duyệt)</summary>
            public const int Media = 110;

            /// <summary>111 - Khác</summary>
            public const int Other = 111;

            /// <summary>Các loại dữ liệu bắt buộc phê duyệt khi đối tác đăng ký.</summary>
            public static readonly int[] RequireApproval = { Media };

            public static bool IsValid(int? id) => id.HasValue && id.Value >= Min && id.Value <= Max;
        }
    }
}
