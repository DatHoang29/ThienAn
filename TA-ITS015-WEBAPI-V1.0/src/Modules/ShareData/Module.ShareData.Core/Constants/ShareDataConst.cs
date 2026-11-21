using Shared.DTO.Enums;

namespace Module.ShareData.Core.Constants
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Bộ mã nghiệp vụ phân hệ Chia sẻ dữ liệu.
    ///              CHỈ còn các bộ giá trị KHÔNG lưu thành cột enum: khóa bên trong JSON và lý do dạng
    ///              chuỗi mở. Mọi cột có tập giá trị đóng đã chuyển sang enum trong BaseEnums:
    ///
    ///              | Cột                                    | Enum                          |
    ///              |----------------------------------------|-------------------------------|
    ///              | ShareDataPartner.Status                | BaseEnums.StatusEnum          |
    ///              | ShareDataPartner.SessionState          | BaseEnums.SessionState        |
    ///              | ShareDataPartner.ProtocolProfile       | BaseEnums.ProtocolProfile     |
    ///              | ShareDataPartner.InitiatorMode         | BaseEnums.InitiatorMode       |
    ///              | ShareDataSession.Direction             | BaseEnums.SessionDirection    |
    ///              | ShareDataSession.State                 | BaseEnums.SessionFsmState     |
    ///              | ShareDataSubscription.State            | BaseEnums.SubSubscriptionState |
    ///              | ShareDataSubscription.Direction        | BaseEnums.Direction           |
    ///              | ShareDataSubscription.Mode             | BaseEnums.SubMode             |
    ///              | ShareDataSubscription.Format           | BaseEnums.PublishFormat       |
    ///              | ShareDataMapping.Direction             | BaseEnums.Direction           |
    ///              | ShareDataMapping.Format                | BaseEnums.PublishFormat       |
    ///              | ShareDataCodeSet.Scope                 | BaseEnums.ShareDataScope      |
    ///              | ShareDataTable.JoinType                | BaseEnums.PacketJoinType      |
    ///              | ShareDataAlertLog.Severity             | BaseEnums.AlertSeverity       |
    ///              | ShareDataAlertLog.AlertSource          | BaseEnums.AlertSource         |
    ///              | ShareDataActivityLog.LogType           | BaseEnums.LogTypeEnum         |
    ///              | ShareDataActivityLog.Action            | BaseEnums.ActivityAction      |
    ///              | ShareDataActivityLog.TargetType        | BaseEnums.ShareDataTargetType |
    ///              | ShareDataActivityLog.Format            | BaseEnums.PublishFormat       |
    ///              | ShareDataActivityLog.Success           | BaseEnums.SuccessEnums        |
    ///              | ShareDataActivityLog.TransferDirection | BaseEnums.TransferDirection   |
    /// Created date: 2026-08-04
    /// </summary>
    public static class ShareDataConst
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Nhóm trạng thái đăng ký dùng cho điều kiện truy vấn / chuyển tiếp.
        /// Kiểu mảng để nullable để so khớp thẳng với cột State (nullable) trong biểu thức SqlSugar.
        /// Created date: 14/08/2026
        /// </summary>
        public static class SubStateGroup
        {
            public static readonly BaseEnums.SubSubscriptionState?[] All =
            {
                BaseEnums.SubSubscriptionState.Pending,
                BaseEnums.SubSubscriptionState.Active,
                BaseEnums.SubSubscriptionState.Paused,
                BaseEnums.SubSubscriptionState.Rejected,
                BaseEnums.SubSubscriptionState.Cancelled,
                BaseEnums.SubSubscriptionState.Expired
            };

            /// <summary>Các trạng thái còn "sống" — dùng để chặn tạo trùng.</summary>
            public static readonly BaseEnums.SubSubscriptionState?[] Alive =
            {
                //BaseEnums.SubSubscriptionState.Pending,
                BaseEnums.SubSubscriptionState.Active,
                //BaseEnums.SubSubscriptionState.Paused
            };

            /// <summary>Các trạng thái kết thúc — không chuyển tiếp được nữa.</summary>
            public static readonly BaseEnums.SubSubscriptionState?[] Finished =
            {
                BaseEnums.SubSubscriptionState.Rejected,
                BaseEnums.SubSubscriptionState.Cancelled,
                BaseEnums.SubSubscriptionState.Expired
            };
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kiểu dữ liệu của field trong ShareDataTable.FieldsJson (khóa dataType).
        ///              GIỮ DẠNG CHUỖI: nằm bên trong JSON của bản chụp gói tin, vốn để người đọc và
        ///              FE đọc trực tiếp — đổi sang số sẽ làm JSON mất nghĩa.
        /// Created date: 21/08/2026
        /// </summary>
        public static class FieldDataType
        {
            public const string String = "string";
            public const string Number = "number";
            public const string DateTime = "datetime";
            public const string Bool = "bool";

            public static readonly string[] All = { String, Number, DateTime, Bool };
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kiểu lịch gửi trong ShareDataSubscription.ScheduleJson (khóa kind).
        ///              GIỮ DẠNG CHUỖI vì cùng lý do với FieldDataType.
        /// Created date: 14/08/2026
        /// </summary>
        public static class ScheduleKind
        {
            public const string Continuous = "continuous";
            public const string Daily = "daily";

            public static readonly string[] All = { Continuous, Daily };
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lý do ngắt kết nối / hủy đăng ký thường dùng.
        ///              GIỮ DẠNG CHUỖI: các cột EndReason / CancelReason / RejectReason nhận text tự do
        ///              từ client và tập reason-cd của giao thức là tập MỞ, không đóng được thành enum.
        /// Created date: 14/08/2026
        /// </summary>
        public static class Reason
        {
            public const string ClientRequested = "clientRequested";
            public const string PartnerDisabled = "partnerDisabled";
            public const string DeletedByUser = "deletedByUser";
            public const string Other = "other";
        }

        #region sharedata_config Constants

        /// <summary>Nhóm cấu hình vận hành của phân hệ ShareData trên SysOpConfig.</summary>
        public const string ConfigGroupCode = "sharedata_config";

        /// <summary>
        /// Chỉ dùng trong kiểm thử để đọc nhóm cấu hình riêng; môi trường thật không khai báo.
        /// </summary>
        public const string ConfigGroupOverrideKey = "ShareDataConfig:GroupCode";

        public sealed record ConfigSpec(string Code, int Default, int Min, int Max);

        #region Worker (19 mã)

        public const string ConfigKeyOutboundCheckIntervalSec = "outbound_check_interval_sec";
        public const int DefaultOutboundCheckIntervalSec = 5;
        public const int MinOutboundCheckIntervalSec = 1;
        public const int MaxOutboundCheckIntervalSec = 300;

        public const string ConfigKeyInboundCheckIntervalSec = "inbound_check_interval_sec";
        public const int DefaultInboundCheckIntervalSec = 5;
        public const int MinInboundCheckIntervalSec = 1;
        public const int MaxInboundCheckIntervalSec = 300;

        public const string ConfigKeyTrackingPollIntervalSec = "tracking_poll_interval_sec";
        public const int DefaultTrackingPollIntervalSec = 1;
        public const int MinTrackingPollIntervalSec = 1;
        public const int MaxTrackingPollIntervalSec = 60;

        public const string ConfigKeyOutboundLockMinSec = "outbound_lock_min_sec";
        public const int DefaultOutboundLockMinSec = 300;
        public const int MinOutboundLockMinSec = 30;
        public const int MaxOutboundLockMinSec = 3600;

        public const string ConfigKeyOutboundLockIntervalMultiplier = "outbound_lock_interval_multiplier";
        public const int DefaultOutboundLockIntervalMultiplier = 3;
        public const int MinOutboundLockIntervalMultiplier = 1;
        public const int MaxOutboundLockIntervalMultiplier = 10;

        public const string ConfigKeyOutboundDefaultIntervalSec = "outbound_default_interval_sec";
        public const int DefaultOutboundDefaultIntervalSec = 30;
        public const int MinOutboundDefaultIntervalSec = 1;
        public const int MaxOutboundDefaultIntervalSec = 86400;

        public const string ConfigKeyInboundMaxPacketsPerRun = "inbound_max_packets_per_run";
        public const int DefaultInboundMaxPacketsPerRun = 50;
        public const int MinInboundMaxPacketsPerRun = 1;
        public const int MaxInboundMaxPacketsPerRun = 1000;

        public const string ConfigKeyInboundStuckTimeoutMin = "inbound_stuck_timeout_min";
        public const int DefaultInboundStuckTimeoutMin = 10;
        public const int MinInboundStuckTimeoutMin = 1;
        public const int MaxInboundStuckTimeoutMin = 1440;

        public const string ConfigKeyInboundWarnIntervalMin = "inbound_warn_interval_min";
        public const int DefaultInboundWarnIntervalMin = 5;
        public const int MinInboundWarnIntervalMin = 1;
        public const int MaxInboundWarnIntervalMin = 1440;

        public const string ConfigKeyInboundWriteTimeoutSec = "inbound_write_timeout_sec";
        public const int DefaultInboundWriteTimeoutSec = 120;
        public const int MinInboundWriteTimeoutSec = 10;
        public const int MaxInboundWriteTimeoutSec = 3600;

        public const string ConfigKeyTrackingTableRetryMin = "tracking_table_retry_min";
        public const int DefaultTrackingTableRetryMin = 5;
        public const int MinTrackingTableRetryMin = 1;
        public const int MaxTrackingTableRetryMin = 1440;

        public const string ConfigKeyTrackingLogRetentionDays = "tracking_log_retention_days";
        public const int DefaultTrackingLogRetentionDays = 7;
        public const int MinTrackingLogRetentionDays = 1;
        public const int MaxTrackingLogRetentionDays = 365;

        public const string ConfigKeyTrackingLogPurgeBatch = "tracking_log_purge_batch";
        public const int DefaultTrackingLogPurgeBatch = 1000;
        public const int MinTrackingLogPurgeBatch = 100;
        public const int MaxTrackingLogPurgeBatch = 50000;

        public const string ConfigKeyTrackingChangeRetentionDays = "tracking_change_retention_days";
        public const int DefaultTrackingChangeRetentionDays = 2;
        public const int MinTrackingChangeRetentionDays = 1;
        public const int MaxTrackingChangeRetentionDays = 30;

        public const string ConfigKeyCleanupIntervalMin = "cleanup_interval_min";
        public const int DefaultCleanupIntervalMin = 1440;
        public const int MinCleanupIntervalMin = 1;
        public const int MaxCleanupIntervalMin = 43200;

        public const string ConfigKeyReloadIntervalSec = "reload_interval_sec";
        public const int DefaultReloadIntervalSec = 30;
        public const int MinReloadIntervalSec = 5;
        public const int MaxReloadIntervalSec = 3600;

        public static readonly ConfigSpec[] WorkerSpecs =
        [
            new(ConfigKeyOutboundCheckIntervalSec, DefaultOutboundCheckIntervalSec, MinOutboundCheckIntervalSec, MaxOutboundCheckIntervalSec),
            new(ConfigKeyInboundCheckIntervalSec, DefaultInboundCheckIntervalSec, MinInboundCheckIntervalSec, MaxInboundCheckIntervalSec),
            new(ConfigKeyTrackingPollIntervalSec, DefaultTrackingPollIntervalSec, MinTrackingPollIntervalSec, MaxTrackingPollIntervalSec),
            new(ConfigKeyOutboundLockMinSec, DefaultOutboundLockMinSec, MinOutboundLockMinSec, MaxOutboundLockMinSec),
            new(ConfigKeyOutboundLockIntervalMultiplier, DefaultOutboundLockIntervalMultiplier, MinOutboundLockIntervalMultiplier, MaxOutboundLockIntervalMultiplier),
            new(ConfigKeyOutboundDefaultIntervalSec, DefaultOutboundDefaultIntervalSec, MinOutboundDefaultIntervalSec, MaxOutboundDefaultIntervalSec),
            new(ConfigKeyInboundMaxPacketsPerRun, DefaultInboundMaxPacketsPerRun, MinInboundMaxPacketsPerRun, MaxInboundMaxPacketsPerRun),
            new(ConfigKeyInboundStuckTimeoutMin, DefaultInboundStuckTimeoutMin, MinInboundStuckTimeoutMin, MaxInboundStuckTimeoutMin),
            new(ConfigKeyInboundWarnIntervalMin, DefaultInboundWarnIntervalMin, MinInboundWarnIntervalMin, MaxInboundWarnIntervalMin),
            new(ConfigKeyInboundWriteTimeoutSec, DefaultInboundWriteTimeoutSec, MinInboundWriteTimeoutSec, MaxInboundWriteTimeoutSec),
            new(ConfigKeyTrackingTableRetryMin, DefaultTrackingTableRetryMin, MinTrackingTableRetryMin, MaxTrackingTableRetryMin),
            new(ConfigKeyTrackingLogRetentionDays, DefaultTrackingLogRetentionDays, MinTrackingLogRetentionDays, MaxTrackingLogRetentionDays),
            new(ConfigKeyTrackingLogPurgeBatch, DefaultTrackingLogPurgeBatch, MinTrackingLogPurgeBatch, MaxTrackingLogPurgeBatch),
            new(ConfigKeyTrackingChangeRetentionDays, DefaultTrackingChangeRetentionDays, MinTrackingChangeRetentionDays, MaxTrackingChangeRetentionDays),
            new(ConfigKeyCleanupIntervalMin, DefaultCleanupIntervalMin, MinCleanupIntervalMin, MaxCleanupIntervalMin),
            new(ConfigKeyReloadIntervalSec, DefaultReloadIntervalSec, MinReloadIntervalSec, MaxReloadIntervalSec)
        ];

        #endregion

        #region WebAPI (13 mã)

        public const string ConfigKeySqlSampleTopMin = "sql_sample_top_min";
        public const int DefaultSqlSampleTopMin = 1;
        public const int MinSqlSampleTopMin = 1;
        public const int MaxSqlSampleTopMin = 500;

        public const string ConfigKeySqlSampleTopMax = "sql_sample_top_max";
        public const int DefaultSqlSampleTopMax = 50;
        public const int MinSqlSampleTopMax = 1;
        public const int MaxSqlSampleTopMax = 500;

        public const string ConfigKeySqlSampleTopDefault = "sql_sample_top_default";
        public const int DefaultSqlSampleTopDefault = 10;
        public const int MinSqlSampleTopDefault = 1;
        public const int MaxSqlSampleTopDefault = 500;

        public const string ConfigKeyMappingSampleTopMin = "mapping_sample_top_min";
        public const int DefaultMappingSampleTopMin = 1;
        public const int MinMappingSampleTopMin = 1;
        public const int MaxMappingSampleTopMin = 500;

        public const string ConfigKeyMappingSampleTopMax = "mapping_sample_top_max";
        public const int DefaultMappingSampleTopMax = 50;
        public const int MinMappingSampleTopMax = 1;
        public const int MaxMappingSampleTopMax = 500;

        public const string ConfigKeyMappingSampleTopDefault = "mapping_sample_top_default";
        public const int DefaultMappingSampleTopDefault = 5;
        public const int MinMappingSampleTopDefault = 1;
        public const int MaxMappingSampleTopDefault = 500;

        public const string ConfigKeyLogListDefaultTop = "log_list_default_top";
        public const int DefaultLogListDefaultTop = 1000;
        public const int MinLogListDefaultTop = 1;
        public const int MaxLogListDefaultTop = 100000;

        public const string ConfigKeySubscriptionIntervalSecMin = "subscription_interval_sec_min";
        public const int DefaultSubscriptionIntervalSecMin = 5;
        public const int MinSubscriptionIntervalSecMin = 1;
        public const int MaxSubscriptionIntervalSecMin = 604800;

        public const string ConfigKeySubscriptionIntervalSecMax = "subscription_interval_sec_max";
        public const int DefaultSubscriptionIntervalSecMax = 86400;
        public const int MinSubscriptionIntervalSecMax = 1;
        public const int MaxSubscriptionIntervalSecMax = 604800;

        public const string ConfigKeySubscriptionPriorityMin = "subscription_priority_min";
        public const int DefaultSubscriptionPriorityMin = 0;
        public const int MinSubscriptionPriorityMin = 0;
        public const int MaxSubscriptionPriorityMin = 100;

        public const string ConfigKeySubscriptionPriorityMax = "subscription_priority_max";
        public const int DefaultSubscriptionPriorityMax = 10;
        public const int MinSubscriptionPriorityMax = 0;
        public const int MaxSubscriptionPriorityMax = 100;

        public const string ConfigKeySubscriptionDebounceSecMin = "subscription_debounce_sec_min";
        public const int DefaultSubscriptionDebounceSecMin = 0;
        public const int MinSubscriptionDebounceSecMin = 0;
        public const int MaxSubscriptionDebounceSecMin = 86400;

        public const string ConfigKeySubscriptionDebounceSecMax = "subscription_debounce_sec_max";
        public const int DefaultSubscriptionDebounceSecMax = 3600;
        public const int MinSubscriptionDebounceSecMax = 0;
        public const int MaxSubscriptionDebounceSecMax = 86400;

        public static readonly ConfigSpec[] WebApiSpecs =
        [
            new(ConfigKeySqlSampleTopMin, DefaultSqlSampleTopMin, MinSqlSampleTopMin, MaxSqlSampleTopMin),
            new(ConfigKeySqlSampleTopMax, DefaultSqlSampleTopMax, MinSqlSampleTopMax, MaxSqlSampleTopMax),
            new(ConfigKeySqlSampleTopDefault, DefaultSqlSampleTopDefault, MinSqlSampleTopDefault, MaxSqlSampleTopDefault),
            new(ConfigKeyMappingSampleTopMin, DefaultMappingSampleTopMin, MinMappingSampleTopMin, MaxMappingSampleTopMin),
            new(ConfigKeyMappingSampleTopMax, DefaultMappingSampleTopMax, MinMappingSampleTopMax, MaxMappingSampleTopMax),
            new(ConfigKeyMappingSampleTopDefault, DefaultMappingSampleTopDefault, MinMappingSampleTopDefault, MaxMappingSampleTopDefault),
            new(ConfigKeyLogListDefaultTop, DefaultLogListDefaultTop, MinLogListDefaultTop, MaxLogListDefaultTop),
            new(ConfigKeySubscriptionIntervalSecMin, DefaultSubscriptionIntervalSecMin, MinSubscriptionIntervalSecMin, MaxSubscriptionIntervalSecMin),
            new(ConfigKeySubscriptionIntervalSecMax, DefaultSubscriptionIntervalSecMax, MinSubscriptionIntervalSecMax, MaxSubscriptionIntervalSecMax),
            new(ConfigKeySubscriptionPriorityMin, DefaultSubscriptionPriorityMin, MinSubscriptionPriorityMin, MaxSubscriptionPriorityMin),
            new(ConfigKeySubscriptionPriorityMax, DefaultSubscriptionPriorityMax, MinSubscriptionPriorityMax, MaxSubscriptionPriorityMax),
            new(ConfigKeySubscriptionDebounceSecMin, DefaultSubscriptionDebounceSecMin, MinSubscriptionDebounceSecMin, MaxSubscriptionDebounceSecMin),
            new(ConfigKeySubscriptionDebounceSecMax, DefaultSubscriptionDebounceSecMax, MinSubscriptionDebounceSecMax, MaxSubscriptionDebounceSecMax)
        ];

        #endregion

        #endregion

        #region Biến SQL (ShareDataSqlVariable)

        /// <summary>Kiểu dữ liệu của biến suy từ nguồn hệ thống (SqlVariableBuiltinTypes) hoặc chọn cho hằng số (SqlVariableConstTypes).</summary>
        public const string SqlVariableTypeDateTime = "DateTime";

        public const string SqlVariableTypeString = "String";

        public const string SqlVariableTypeInt = "Int";

        public const string SqlVariableTypeDecimal = "Decimal";

        /// <summary>Kiểu dữ liệu được phép chọn khi nguồn là hằng số (Const).</summary>
        public static readonly IReadOnlyList<string> SqlVariableConstTypes =
        [
            SqlVariableTypeString,
            SqlVariableTypeInt,
            SqlVariableTypeDecimal
        ];

        /// <summary>Lấy từ nguồn hệ thống: Value là mã nguồn trong SqlVariableBuiltinTypes.</summary>
        public const string SqlVariableKindBuiltin = "Builtin";

        /// <summary>Giá trị nhập cố định (hằng số): Value là giá trị do người dùng nhập, DataType thuộc SqlVariableConstTypes.</summary>
        public const string SqlVariableKindConst = "Const";

        /// <summary>
        /// Nguồn hệ thống được phép và kiểu dữ liệu bắt buộc của từng nguồn: chỉ còn lastKey (mốc nối đuôi ở ShareDataLastSend).
        /// Biến khác là giá trị nhập (Const); cần đầu năm hay giờ hiện tại thì viết hàm ngày giờ ngay trong câu SQL.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> SqlVariableBuiltinTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SqlVariableSourceLastKey] = SqlVariableTypeString
        };

        public const string SqlVariableSourceLastKey = "lastKey";

        /// <summary>Tên quy ước của biến cỡ trang (@pageSize) trong câu SQL trích xuất.</summary>
        public const string SqlVariablePageSize = "pageSize";

        /// <summary>Phạm vi một dòng biến khi xem theo đối tác.</summary>
        public const string SqlVariableScopeCommon = "Common";

        public const string SqlVariableScopePartner = "Partner";

        #endregion

        #region Câu ghi chiều nhận (ShareDataSql.WriteSql)

        /// <summary>
        /// Tham số hệ thống Worker truyền vào mọi câu ghi chiều nhận (DataInboundService.BuildWriteParameters), kèm kiểu SQL
        /// dùng khi dò cột và chạy thử. Câu ghi chỉ được dùng các tham số này.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> WriteSqlSystemParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["records"] = "nvarchar(max)",
            ["now"] = "datetime",
            ["tenantId"] = "nvarchar(64)",
            ["operator"] = "nvarchar(64)",
            ["partnerId"] = "nvarchar(64)",
            ["partnerCode"] = "nvarchar(128)",
            ["datatypeId"] = "nvarchar(128)",
            ["serialNbr"] = "int",
            ["packetId"] = "nvarchar(64)"
        };

        /// <summary>Mức của một kết quả kiểm câu ghi: lỗi chặn lưu, cảnh báo cho lưu.</summary>
        public const string WriteSqlCheckError = "Error";

        public const string WriteSqlCheckWarning = "Warning";

        #endregion
    }
}
