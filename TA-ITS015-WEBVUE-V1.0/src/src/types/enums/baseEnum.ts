// Author: SonTH
export enum BaseConfigTypeEnum {
    // System
    /**
    * Loại trạng thái
    */
    SysStatusType = 'status_type',

    /**
    * Loại boolean
    */
    SysBooleanType = 'boolean_type',

    /**
    * Loại ngôn ngữ
    */
    SysLanguageType = 'language_type',

    /**
    * Loại đơn vị
    */
    SysOrgType = 'org_type', // Loại đơn vị


    /**
    * Loại giới tính
    */
    SysGenderType = 'sys_gender_type',

    /**
    * Loại menu
    */
    SysMenuType = 'menu_type',


    /**
    * Loại tài khoản
    */
    SysAccountType = 'sys_account_type',

    /**
     * Cú pháp dịch thuật
     */
    SysI18nPrefix = "i18n_prefix",

    /**
     * Loại cấu hình hoạt động
     */
    SysOpConfigType = "operation_config_type",


    // TMS
    /**
    * Phân loại zone: Tuyến, đoạn, zone
    */
    TmsZoneType = 'tms_zone_type',

    /**
    * Chế độ xem của bảng ma trận giao thông: vận tốc / lưu lượng.
    * Code là hợp đồng với BE (quyết định đọc số nào của ô), Name là nhãn hiển thị,
    * Value là đơn vị đo.
    */
    TmsTrafficMatrixMetric = 'tms_traffic_matrix_metric',

    /**
    * Phân loại vị trí zone: Tuyến chính, tuyến phụ, hầm, ...
    */

    TmsZoneLocation = 'tms_zone_location',

    /**
    * Phân loại xe: 6m, 12m...
    */
    TmsVehicleTypeConfigCode = 'vehicle_type',

    /**
    * Phân loại khu vực 
    */
    TmsLocationType = 'tms_location',
    /**
    * Phân loại làn
    */
    TmsLaneType = 'tms_lane',

    /**
    * Phân loại hướng
    */
    TmsDirectionType = 'direction_config_type',

    /**
    * Phân loại hướng hoạt động
    */
    TmsActiveDirectionType = 'tms_active_direction_type',

    TmsActionType = 'tms_action_type',

    /**
    * Phân loại trạng thái thiết bị
    */
    TmsEquipmentState = 'equipment_state',

    /**
    * Kích thước icon thiết bị
    */
    TmsEquipIconSize = 'tms_equip_icon_size',

    /**
    * Mã modal xem/điều khiển thiết bị trên bigscreen (gán cho loại thiết bị qua TmsEquipmentType.modalCode)
    */
    TmsEquipmentModalType = 'tms_equipment_modal_type',

    // Chu kỳ báo cáo theo đoạn tuyến
    TmsTrafficStatisticSectionCycle = 'traffic_statistic_section_cycle',

    // Chu kỳ báo cáo theo zone
    TmsTrafficStatisticZoneCycle = 'traffic_statistic_zone_cycle',

    // Trạng thái của sự cố
    TmsIncidentStatus = 'tms_incident_status',

    TmsIncidentAutomationTriggerState = 'tms_incident_automation_trigger_state',
    TmsIncidentAutomationFailurePolicy = 'tms_incident_automation_failure_policy',
    TmsIncidentAutomationResetPolicy = 'tms_incident_automation_reset_policy',

    // Trạng thái xử lý công việc sự cố
    TmsWorkExecuteStatus = 'work_execute_state',

    // Nguồn phát hiện sự cố
    TmsSourceDetectIncident = 'tms_source_detect',

    // Danh sách màu sắc trạng thái
    TmsEquipmentStateColor = 'equipment_state_color',

    TmsVehicleFilterType = 'vehicle_filter_type',

    TmsVehicleFilterWarning = 'vehicle_filter_warning',

    // Phân loại khu vực
    TmsLocationConfigType = 'location_config_type',

    // Phân loại phương tiện
    TmsVehicleRegistrationType = 'vehicle_registration_type',

    // Phân loại liên hệ
    TmsWorkContactType = 'work_contact_type',

    // Phân loại đơn vị
    TmsWorkUnitType = 'work_unit_type',

    /**
    * Trạng thái event lịch trực
    */
    TmsDutyState = 'tms_duty_state',

    /**
    * Vai trò nhân sự trong lịch trực
    */
    TmsDutyPatronRole = 'tms_duty_patron_role',

    /**
    * Trạng thái nhân sự trong lịch trực
    */
    TmsDutyPatronState = 'tms_duty_patron_state',

    /**
    * Trạng thái yêu cầu thay đổi lịch trực
    */
    TmsDutyRequestState = 'tms_duty_request_state',

    /**
    * Loại hành động trong lịch sử lịch trực
    */
    TmsDutyAction = 'tms_duty_action',

    /**
    * Loại yêu cầu thay đổi lịch trực
    */
    TmsDutyRequestType = 'tms_duty_request_type',

    /**
     * Trạng thái lịch bảo trì
     */
    TmsMaintenanceScheduleStatus = 'tms_maintenance_schedule_status',

    /**
     * Mức ưu tiên bảo trì
     */
    TmsMaintenancePriority = 'tms_maintenance_priority',

    /**
     * Tần suất bảo trì
     */
    TmsMaintenanceFrequency = 'tms_maintenance_frequency',

    /**
     * Trạng thái thiết bị trong lịch bảo trì
     */
    TmsMaintenanceEquipmentStatus = 'tms_maintenance_equipment_status',

    /**
     * Trạng thái công việc bảo trì
     */
    TmsMaintenanceTaskStatus = 'tms_maintenance_task_status',

    /**
     * Trạng thái cảnh báo bảo trì
     */
    TmsMaintenanceAlertStatus = 'tms_maintenance_alert_status',

    /**
     * Loại cảnh báo bảo trì
     */
    TmsMaintenanceAlertType = 'tms_maintenance_alert_type',

    // VMS      
    /**
    * Phân loại thông điệp vms
    */
    VmsMessageType = 'vms_message_type', // Phân loại thông điệp vms
    /**
    * Phân loại hình ảnh vms
    */
    VmsImageType = 'vms_image_type', // Phân loại hình ảnh vms
    /**
    * Phân loại thao tác vms
    */
    VmsActionType = 'vms_action_type', // Phân loại thao tác vms

    /**
    * Phân loại cỡ chữ vms
    */
    VmsFontSize = 'vms_font_size', // Phân loại cỡ chữ vms

    /**
    * Phân loại kiểu chữ vms
    */
    VmsFontName = 'vms_font_name', // Phân loại kiểu chữ vms
    /**
    * Phân loại màu chữ vms
    */
    VmsFontColor = 'vms_font_color', // Phân loại màu chữ vms
    /**
    * Phân loại màu nền vms
    */
    VmsBackgroundColor = 'vms_background_color', // Phân loại màu nền vms

    /**
    * Nội dung động VMS (token tự động: {VehiclePlate}, {VehicleSpeed}, {VehicleLenght}, {VehicleHeight})
    */
    VmsContentDefault = 'vms_content_default', // Nội dung động VMS

    /**
    * Phân loại kích thước hình ảnh vms
    */
    VmsImageSize = 'vms_size_image', // Phân loại kích thước hình ảnh vms

    /**
    * Phân loại kích thước icon vms
    */
    VmsIconSize = 'vms_size_icon', // Phân loại kích thước icon vms


    //Report
    RptReportType = 'report_type', // Phân loại loại báo cáo

    //Định dạng xuất báo cáo
    RptExportFormat = "rpt_export_format",

    // Nhóm cấu hình thông tin báo cáo (header/footer/signature/company)
    RptInfoConfigType = 'rpt_info_config_type',

    // Kiểu giá trị của cấu hình thông tin báo cáo (text/image/date/html) - Code khớp RptInfoValueTypeEnum
    RptInfoValueType = 'rpt_info_value_type',

    // Loại thiết bị được giám sát ở màn Telemetry FMS (Code = Value = ID loại thiết bị)
    FmsTelemetryEquipmentType = 'fms_telemetry_equipment_type',

    // Loại báo cáo lưu lượng giao thông
    TmsTrafficVolumeReportType = 'traffic_volume_report_type',

    // Màu sắc trạng thái khu vực 
    // code = condition của TmsZoneStatus (normal / slow / conges), value = mã màu
    TmsZoneStatusColor = 'tms_zone_status_color',

    // CCTV
    CctvStreamOptions = 'cctv_stream_options', // Danh sách đường truyền camera

    // VideoWall
    /**
    * Phân loại nguồn tín hiệu VideoWall (VwSource.SourceType) — Value = màu nhận diện
    */
    VideoWallSourceType = 'videowall_source_type',

    /**
    * Phân loại tín hiệu VideoWall (VwSource.SignalType)
    */
    VideoWallSignalType = 'videowall_signal_type',

    /**
    * Độ phân giải tín hiệu VideoWall (VwSource.MaxResolution) — Code = giá trị lưu, VD "1920x1080@60Hz"
    */
    VideoWallResolution = 'videowall_resolution',

    /**
    * Model bộ điều khiển VideoWall (VwController.Model)
    */
    VideoWallControllerModel = 'videowall_controller_model',

    /**
    * Vai trò bộ điều khiển VideoWall (VwController.Role: distribution / display / standalone)
    */
    VideoWallControllerRole = 'videowall_controller_role',

    /**
    * Màu nhận diện bộ điều khiển VideoWall (VwController.colorHex)
    */
    VideoWallControllerColor = 'videowall_controller_color',

    /**
    * Nguồn sự kiện của quy tắc VideoWall (VwEventRule.EventSource)
    */
    VideoWallEventSource = 'videowall_event_source',

    /**
    * Mức ưu tiên quy tắc sự kiện VideoWall (VwEventRule.Priority: CRITICAL / HIGH / NORMAL / LOW)
    */
    VideoWallEventPriority = 'videowall_event_priority',

    SharedataSessionState = 'sharedata_session_state',
    SharedataSubMode = 'sharedata_sub_mode',
    SharedataSubState = 'sharedata_sub_state',
    SharedataPubState = 'sharedata_pub_state',
    SharedataPartnerStatus = 'sharedata_partner_status',
    SharedataProtocolProfile = 'sharedata_protocol_profile',
    SharedataInitiatorMode = 'sharedata_initiator_mode',
    SharedataRejectReason = 'sharedata_reject_reason',
    SharedataLogoutReason = 'sharedata_logout_reason',
    SharedataAlertSeverity = 'sharedata_alert_severity',
    SharedataAlertSource = 'sharedata_alert_source',
    SharedataDirection = 'sharedata_direction',
    SharedataValueType = 'sharedata_value_type',
    SharedataErrorCode = 'sharedata_error_code',
    ShareScope = 'sharedata_scope',
    SharedataactionType = 'sharedata_action',
    TollDirectionList = 'toll_direction_list',
    TollStationList = 'toll_station_list',
    TollStationType = 'toll_station_type',
    TollMailAuto = 'toll_report_auto_mail',
    TollReportList = 'toll_report',
    TollPeriod = 'toll_period',
    TollFrequencies = 'toll_frequencies',
    MailTriggerType = 'mail_trigger_type',
    MailPeriod = 'mail_period' // Kỳ lấy số liệu của báo cáo đính kèm: 0 = kỳ hiện tại, 1 = kỳ trước
}

export type BaseConfigTypeType = keyof typeof BaseConfigTypeEnum;

// Author: Ngọc Thắng
// Description: Enum một số cấu hình đặc biệt
export enum SpecialConfigEnum {
    AllDirection = '0', // Tất cả các hướng
}

// Description: Hướng giao dịch của xe qua trạm thu phí (khớp TollEnums.DirectionCode phía backend)
export enum TollDirectionEnum {
    In = '1', // Xe vào
    Out = '2', // Xe ra
}

// Author: Ngọc Thắng
// Description: Danh sách code dùng chung bảng SysConfigType
export enum SysConfigTypeCodeEnum {
    StatusType = 'status_type', // Loại trạng thái
    OperationConfigType = 'operation_config_type', // Loại cấu hình hoạt động
    LanguageType = 'language_type', // Phân loại ngôn ngữ
    I18nPrefix = 'i18n_prefix', // Phân loại dịch thuật

    TmsZoneTypeConfigCode = 'tms_zone_type', // Phân loại zone: Tuyến, đoạn, zone
    TmsVehicleTypeConfigCode = 'vehicle_type', // Phân loại xe: 6m, 12m...
    TmsDirectionConfigCode = 'direction_config_type', // Phân loại hướng: B-N, N-B...

    TmsSourceDetectIncident = 'tms_source_detect', // Nguồn phát hiện sự cố
    TmsLocationType = 'tms_location', // Phân loại location

    TmsWorkUnitType = 'work_unit_type', // Phân loại đơn vị


    TmsWorkexecuteStatus = 'tms_workexecute_status', // Trạng thái xử lý công việc sự cố
    TmsIncidentStatus = 'tms_incident_status', // Trạng thái của sự cố

    TmsEquipmentState = 'equipment_state', // Trạng thái hoạt động của thiết bị
    TmsEquipmentStateInputType = 'equipment_history_type', // Phân loại nguồn cung cấp thông tin trạng thái thiết bị

}

export enum BaseOpConfigGroupCodeEnum {
    ItsOperationConfig = 'its_operation_config',
    // Nhóm cấu hình vận hành: phạm vi gợi ý thiết bị, vị trí đầu/cuối tuyến, giới hạn ký tự VMS...
    OperationConfig = 'operation_config',
    // Nhóm cấu hình phân hệ gửi mail báo cáo
    MailConfig = 'mail_config',
    // Nhóm cấu hình giới hạn và giá trị mặc định của phân hệ ShareData
    SharedataConfig = 'sharedata_config'
}

// Description: Mã cấu hình SysOpConfig của nhóm mail_config (BaseOpConfigGroupCodeEnum.MailConfig)
export enum MailOpConfigCodeEnum {
    MaxItems = 'mail_max_items', // Số báo cáo tối đa trong một thư
    MaxRecipients = 'mail_max_recipients' // Số người nhận tối đa mỗi thư
}


// Author: Ngọc Thắng
// Description: Danh sách code dùng chung bảng SysOpConfig
export enum BaseOpConfigCodeEnum {
    DayWorkShift = 'shift_operation', // Ca làm việc trong ngày
    DayWorkTime = 'day_operation', // Thời gian làm việc trong ngày
    CctvConfig = 'cctv_config', // Cấu hình vận hành CCTV (grid dashboard, vendor...),
    EventQuickRegister = 'event_quick_register' // Cấu hình danh sách sự cố cho phép tạo nhanh tạo giao diện bản đồ
}

// Trạng thái
// Author: Ngọc Thắng
export enum StatusTypeEnum {
    Disable = 0, // Không kích hoạt
    Enable = 1 // Kích hoạt
}

// Định dạng thời gian
// Author: Ngọc Thắng
export enum DateTimeFormatEnum {
    DateDefault = 'DD/MM/YYYY', // Định đạng hiển thị ngày mặc định
    TimeDefault = 'HH:mm:ss', // Định dạng hiển thị thời gian mặc định
    DateTimeDefault = 'DD/MM/YYYY HH:mm:ss', // Định dạng hiển thị thời gian mặc định
    DateTimeDefaultRevert = 'HH:mm:ss DD/MM/YYYY', // Định dạng hiển thị thời gian mặc định

    DateValidInput = 'YYYY-MM-DDT00:00:00', // Định dạng ngày tháng hợp lệ tương ứng backend
    DateTimeValidInput = 'YYYY-MM-DDTHH:mm:ss' // Định dạng ngày tháng hợp lệ tương ứng backend
}

// Phân loại thông báo
// Author: Ngọc Thắng
export enum AlertTypeEnum {
    Info = 'info',
    Error = 'error',
    Warning = 'warning',
    Confirm = 'confirm'
}

// Đường dẫn thư mục upload
// Author: Ngọc Thắng
export enum UploadFoldersEnum {
    Root = '/Upload/',
    Images = '/Upload/Images/',
    Files = '/Upload/Files/',
    Icons = '/Upload/Icons/',
    Weather = '/Upload/Tms/Weather/',
    Incident = '/Upload/Tms/Incident/'
}

/* ─────────────────────────────────────────────────────────
 * ShareData — enum GIÁ TRỊ dùng để so sánh nghiệp vụ.
 * Khớp 1:1 với Shared.DTO/Enums/BaseEnums.cs phía backend.
 * Lưu ý: các `Sharedata*` ở BaseConfigTypeEnum bên trên là MÃ NHÓM CẤU HÌNH (dùng để
 * hiển thị nhãn/màu tag), khác hoàn toàn với các enum dưới đây.
 * ───────────────────────────────────────────────────────── */

/** Trạng thái đăng ký chia sẻ — BaseEnums.SubSubscriptionState. */
export enum SharedataSubStateEnum {
    Pending = 0,
    Active = 1,
    Paused = 2,
    Rejected = 3,
    Cancelled = 4,
    Expired = 5,
}

/** Chiều dữ liệu — BaseEnums.Direction. */
export enum SharedataDirectionEnum {
    Outbound = 0,
    Inbound = 1,
    /**
     * Một phễu lọc dùng chung cho cả hai chiều.
     * CHỈ hợp lệ với Phễu lọc (ShareDataMapping) — KHÔNG dùng cho Đăng ký,
     * vì một đăng ký chỉ chạy được một chiều.
     */
    Both = 2,
}

/**
 * Tình trạng hồ sơ ánh xạ của một đăng ký — BaseEnums.SubMappingState.
 *
 * Backend SUY RA giá trị này lúc truy vấn theo (đối tác × gói tin × chiều), không lưu cột nào
 * trong CSDL. Đăng ký chỉ bật được khi ở trạng thái Ready.
 */
export enum SharedataSubMappingStateEnum {
	/** Chưa có hồ sơ ánh xạ nào — phải sang màn Ánh xạ tạo mới. */
	NoMapping = 0,
	/** Có hồ sơ và đang bật — bật đăng ký được. */
	Ready = 1,
	/** Có hồ sơ nhưng đang tắt — chỉ cần bật hồ sơ lên, không phải tạo mới. */
	Disabled = 2,
}

/** Chế độ đăng ký — BaseEnums.SubMode. */
export enum SharedataSubModeEnum {
    Single = 0,
    Event = 1,
    Periodic = 2,
}

/** Định dạng xuất bản — BaseEnums.PublishFormat. */
export enum SharedataPublishFormatEnum {
    Data = 0,
    File = 1,
}

/** Trạng thái kết nối đối tác — BaseEnums.SessionState. */
export enum SharedataSessionStateEnum {
    Disconnected = 0,
    Connected = 1,
}

/** Trạng thái máy trạng thái phiên — BaseEnums.SessionFsmState. */
export enum SharedataSessionFsmStateEnum {
    Idle = 0,
    Connecting = 1,
    LoginWait = 2,
    Active = 3,
    Closing = 4,
    Closed = 5,
    ReconnectWait = 6,
    Rejected = 7,
    Timeout = 8,
}

/** Loại nhật ký — BaseEnums.LogTypeEnum. */
export enum SharedataLogTypeEnum {
    Config = 0,
    Transfer = 1,
}

/** Chiều truyền gói tin — BaseEnums.TransferDirection. */
export enum SharedataTransferDirectionEnum {
    Snd = 0,
    Rcv = 1,
}

/**
 * Mã dữ liệu trong nhóm cấu hình `sharedata_config` (SysOpConfig): min/max/default của các ô nhập số ShareData.
 */
export enum SharedataFeConfigCodeEnum {
    SubscriptionIntervalSecMin = 'subscription_interval_sec_min',
    SubscriptionIntervalSecMax = 'subscription_interval_sec_max',
    SubscriptionIntervalSecDefault = 'subscription_interval_sec_default',
    SubscriptionDebounceSecMin = 'subscription_debounce_sec_min',
    SubscriptionDebounceSecMax = 'subscription_debounce_sec_max',
    SubscriptionDebounceSecDefault = 'subscription_debounce_sec_default',
    SubscriptionPriorityMin = 'subscription_priority_min',
    SubscriptionPriorityMax = 'subscription_priority_max',
    SubscriptionPriorityDefault = 'subscription_priority_default',
    PartnerHeartbeatSecMin = 'partner_heartbeat_sec_min',
    PartnerHeartbeatSecMax = 'partner_heartbeat_sec_max',
    PartnerHeartbeatSecDefault = 'partner_heartbeat_sec_default',
    PartnerResponseTimeoutSecMin = 'partner_response_timeout_sec_min',
    PartnerResponseTimeoutSecMax = 'partner_response_timeout_sec_max',
    PartnerResponseTimeoutSecDefault = 'partner_response_timeout_sec_default',
    PartnerDatagramSizeMin = 'partner_datagram_size_min',
    PartnerDatagramSizeMax = 'partner_datagram_size_max',
    PartnerDatagramSizeDefault = 'partner_datagram_size_default',
    MappingSampleTopMin = 'mapping_sample_top_min',
    MappingSampleTopMax = 'mapping_sample_top_max',
    MappingSampleTopDefault = 'mapping_sample_top_default',
    HistoryRangeDefaultDays = 'history_range_default_days',
    HistoryRangeMaxDays = 'history_range_max_days',
    SqlSampleTopMin = 'sql_sample_top_min',
    SqlSampleTopMax = 'sql_sample_top_max',
    SqlSampleTopDefault = 'sql_sample_top_default',
    LogListDefaultTop = 'log_list_default_top',
}

// Hình ảnh icon có sẵn trong thư mục upload
// Author: Ngọc Thắng
export enum UploadAssetItemsEnum {
    Logo = UploadFoldersEnum.Root + 'logo.png',
    Default = UploadFoldersEnum.Root + 'icon.png',
    LoadingIcon = UploadFoldersEnum.Root + 'loading.gif',
    ProcessingIcon = UploadFoldersEnum.Root + 'processing.gif',
    WarningIcon = UploadFoldersEnum.Root + 'warning.png',
    WeatherIcon = UploadFoldersEnum.Root + 'weather.png',
    AttentionIcon = UploadFoldersEnum.Root + 'attention.png',
    EquipmentIcon = UploadFoldersEnum.Root + 'equipment.svg',
    WorkUnitIcon = UploadFoldersEnum.Root + 'workunit.svg',
}

// Giá trị z-index trong tầng cao nhất
// Author: Ngọc Thắng
export enum ZIndexHighestEnum {
    Max = 999999999, // Cao nhất
    Mid = 888888888, // Cao nhì
    Min = 777777777 // Cao 3
}

// Kích thước trường dữ liệu phổ biến
// Author: Ngọc Thắng
export enum EntityFieldLength {
    ConfigFieldLength = 32,
    KeyFieldLength = 64,
    RemarkFieldLength = 256,
    Length8 = 8,
    Length16 = 16,
    Length32 = 32,
    Length64 = 64,
    Length128 = 128,
    Length256 = 256,
    Length512 = 512,
    Length1024 = 1024,
    Length2048 = 2048
};

// Cấu hình thông tin báo cáo - RptInfoConfig
// Author: ChuongLP
export enum RptInfoConfigEnum {
    /** ReportCode đại diện cấu hình dùng chung cho mọi báo cáo */
    GlobalReportCode = '*'
}

// Kiểu giá trị của cấu hình thông tin báo cáo
export enum RptInfoValueTypeEnum {
    Text = 'text',
    Image = 'image',
    Date = 'date',
    Html = 'html'
}


// Enum cấu hình về bản đồ
export enum MapTypeConfigEnum {
    Online = 'server',
    Image = 'image',
    SelfHost = 'selfhost'
}

// Loại nguồn tín hiệu VideoWall (VwSource.SourceType)
export enum VideoWallSourceTypeEnum {
    LocalSignal = 'local_signal', 
    IpStream = 'ip_stream', 
    CascadeLink = 'cascade_link', 
}

export enum  VideoWallEnums {    
    WALL_ROWS = 4,
    WALL_COLS = 8,
    PANEL_WIDTH_PX = 3840,
    PANEL_HEIGHT_PX = 2160,
    OUTPUT_PORTS_PER_CONTROLLER = 8,
    INPUT_PORTS_PER_CONTROLLER = 8,
}

// Chế độ đồng bộ dữ liệu ShareData
export const SyncMode = {
    Snapshot: 1,
    Incremental: 2,
} as const;

// Loại hình kết nối đối tác ShareData (ShareDataPartner.ConnectionType)
export enum ConnectionTypeEnum {
    Api = 1,
    File = 2,
    Ftp = 3,
    Ftps = 4,
    Kafka = 5,
}

export type SyncMode = (typeof SyncMode)[keyof typeof SyncMode];
