using Microsoft.AspNetCore.Http;

namespace Shared.DTO.Constants.Application;

/// <summary>
/// Cache related constants
/// </summary>
public class CacheConst
{
    /// <summary>
    /// User permission cache (button collection)
    /// </summary>
    public const string KeyUserButton = "sys_user_button:";

    /// <summary>
    /// User API cache (API collection)
    /// </summary>
    public const string KeyUserApi = "sys_user_api:";

    /// <summary>
    /// Base API cache (API collection)
    /// </summary>
    public const string KeyBaseApi = "sys_base_api:";

    /// <summary>
    /// User organization cache
    /// </summary>
    public const string KeyUserOrg = "sys_user_org:";

    /// <summary>
    /// Role maximum data scope cache
    /// </summary>
    public const string KeyRoleMaxDataScope = "sys_role_maxDataScope:";

    /// <summary>
    /// Online user cache
    /// </summary>
    public const string KeyUserOnline = "sys_user_online:";

    /// <summary>
    /// Graphic verification code cache
    /// </summary>
    public const string KeyVerCode = "sys_verCode:";

    /// <summary>
    /// Mobile verification code cache
    /// </summary>
    public const string KeyPhoneVerCode = "sys_phoneVerCode:";



    /// <summary>
    /// Tenant cache
    /// </summary>
    public const string KeyTenant = "sys_tenant";

    /// <summary>
    /// Constant dropdown cache
    /// </summary>
    public const string KeyConst = "sys_const:";

    /// <summary>
    /// All cache key collection
    /// </summary>
    public const string KeyAll = "sys_keys";

    /// <summary>
    /// SqlSugar secondary cache
    /// </summary>
    public const string SqlSugar = "sys_sqlSugar:";

    /// <summary>
    /// Open API identity cache
    /// </summary>
    public const string KeyOpenAccess = "sys_open_access:";

    /// <summary>
    /// Open API identity nonce cache
    /// </summary>
    public const string KeyOpenAccessNonce = "sys_open_access_nonce:";

    /// <summary>
    /// Login blacklist
    /// </summary>
    public const string KeyBlacklist = "sys_blacklist:";

    /// <summary>
    /// System configuration cache
    /// </summary>
    public const string KeyConfig = "sys_config:";


    public const string KeyOpConfig = "sys_op_config:";

    /// <summary>
    /// System dictionary cache
    /// </summary>
    public const string KeyDict = "sys_dict:";

    /// <summary>
    /// Excel temporary file cache
    /// </summary>
    public const string KeyExcelTemp = "sys_excel_temp:";


    public class App
    {
        /// <summary>
        /// Password error count cache
        /// </summary>
        public const string KeyPasswordErrorTimes = "sysPasswordErrorTimes";
    }

    public class System
    {
        public const string SysPos = "sysPos";
        public const string SysRole = "sysRole";
        public const string SysUser = "sysUser";

        public const string SysOrg = "sysOrg";
        public const string SysMenu = "sysMenu";
        public const string SysDict = "sysDict";
        public const string SysConfig = "sysConfig";
        public const string SysTenant = "sysTenant";
        public const string SysPlugin = "sysPlugin";
        public const string SysApp = "sysApp";

    }

    public class Tms
    {
        public const string TmsEquipment = "tms_equipment";
        public const string TmsEquipmentType = "tms_equipment_type";
        public const string TmsZone = "tms_zone";
        public const string TmsZoneEquipment = "tms_zone_equipment";
        public const string TmsPatrolTeam = "tms_patrol_team";
        public const string TmsDutySchedule = "tms_duty_schedule";
        public const string TmsEventType = "tms_event_type";
        public const string TmsIncident = "tms_incident";
        public const string TmsMap = "tms_map";
        public const string TmsMapDetail = "tms_map_detail";
        public const string TmsMapLocation = "tms_map_location";
        public const string TmsModule = "tms_module";
        public const string TmsSignalLog = "tms_signal_log";
        public const string TmsTrafficPlan = "tms_traffic_plan";
        public const string TmsTrafficPlanWorkDetail = "tms_traffic_plan_work_detail";
        public const string TmsTrafficStatistic = "tms_traffic_statistic";

        /// <summary>
        /// Bảng ma trận vận tốc / lưu lượng theo giờ. Chỉ cache NGÀY ĐÃ QUA (dữ liệu bất biến);
        /// khoá ghép thêm ngày và bộ lọc, ví dụ ":20260810:section::1::False".
        /// </summary>
        public const string TmsTrafficMatrix = "tms_traffic_matrix";
        public const string TmsVehicleFilter = "tms_vehicle_filter";
        public const string TmsVehicleFilterHistory = "tms_vehicle_filter_history";
        public const string TmsVehicleRegistration = "tms_vehicle_registration";
        public const string TmsWorkContact = "tms_workcontact";
        public const string TmsWorkUnit = "tms_workunit";
        public const string TmsWorkDetail = "tms_work_detail";
        // Maintenance Schedule
        public const string TmsMaintenanceGuide = "tms_maintenance_guide";
        public const string TmsMaintenanceSchedule = "tms_maintenance_schedule";
        public const string TmsMaintenanceAlert = "tms_maintenance_alert";
        public const string TmsMaintenanceAlertRule = "tms_maintenance_alert_rule";
        public const string TmsIncidentControl = "tms_incident_control";
        public const string TmsEquipmentEventRule = "tms_equipment_event_rule";
        public const string TmsIncidentAutomationConfig = "tms_incident_automation_config";

        /// <summary>
        /// SonTH - 24/07/2026: nhóm cấu hình SysOpConfig của phân hệ sự cố (GroupCode thật trong DB).
        /// Dùng làm tiền tố cache; khoá cụ thể ghép thêm Code, ví dụ ":automation_banner".
        /// </summary>
        public const string TmsIncidentActionGroup = "tms_incident_action_group";
    }

    public class Vms
    {
        public const string VmsImage = "vms_image";
        public const string VmsCurrent = "vms_current";
        public const string VmsProcess = "vms_process";
        public const string VmsMessage = "vms_message";
        public const string VmseventDefault = "vms_event_default";
    }

    public class Cctv
    {
        public const string CctvDashboard = "cctv_dashboard";
        public const string CctvDashboardItem = "cctv_dashboard_item";
        public const string CctvTemplate = "cctv_template";
    }


    /// <summary>
    /// Description: Cache key phân hệ Video Wall
    /// </summary>
    public class Vw
    {
        public const string VwController = "vw_controller";
        public const string VwControllerOutput = "vw_controller_output";
        public const string VwScene = "vw_scene";
        public const string VwScreen = "vw_screen";
        public const string VwSource = "vw_source";
        public const string VwWindowScene = "vw_window_scene";
        public const string VwWallTopology = "vw_wall_topology";
        public const string VwSchedule = "vw_schedule";
        public const string VwEventRule = "vw_event_rule";
        public const string VwEventTriggerLog = "vw_event_trigger_log";
        public const string VwDeviceAuthFailure = "vw_device_auth_failure";
        public const string VwWallPermission = "vw_wall_permission";
    }


    /// <summary>
    /// Description: Cache key phân hệ Chia sẻ dữ liệu (ShareData)
    /// </summary>
    public class ShareData
    {
        public const string ShareDataPartner = "sharedata_partner";
        public const string ShareDataSession = "sharedata_session";
        public const string ShareDataSubscription = "sharedata_subscription";
        public const string ShareDataDataSource = "sharedata_data_source";
        public const string ShareDataMapping = "sharedata_mapping";
        public const string ShareDataCodeSet = "sharedata_code_set";
        public const string ShareDataPacket = "sharedata_packet";
        public const string ShareDataTable = "sharedata_table";
        public const string ShareDataAlertLog = "sharedata_alert_log";
    }

    public class WP
    {
        public const string WpPost = "wp_post";
        public const string WpMenu = "wp_menu";
        public const string WpCategory = "wp_category";
        public const string WpConfig = "wp_config";
        public const string WpTrafficLaw = "wp_traffic_law";
        public const string WpPriceList = "wp_price_list";
        public const string WpPriceListDetail = "wp_price_list_detail";
        public const string WpToll = "wp_toll";
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cache key phân hệ Mail (lịch gửi báo cáo qua email)
    /// Created date: 14/08/2026
    /// </summary>
    public class Mail
    {
        public const string RptEmailSchedule = "rpt_email_schedule";
        public const string RptEmailLog = "rpt_email_log";
        public const string RptEmailReport = "rpt_email_report";
        public const string RptEmailTemplate = "rpt_email_template";
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Cache key phân hệ Thu phí (TOLL) — danh mục đồng bộ từ TCS/ETC.
    ///     Dữ liệu do dịch vụ ITS.Toll.Worker ghi thẳng vào DB, nên cache được xoá khi bấm
    ///     "Lấy dữ liệu" (ManualFetch) và khi dịch vụ báo kết quả về kênh ta.its.control.toll.
    /// Created date: 28/09/2026
    /// </summary>
    public class Toll
    {
        public const string TollStation = "toll_station";
        public const string TollLane = "toll_lane";
        public const string TollShift = "toll_shift";
        public const string TollVehicleType = "toll_vehicle_type";
        public const string TollTicketType = "toll_ticket_type";
        public const string TollPriceList = "toll_price_list";
        public const string TollPriceListDetail = "toll_price_list_detail";
    }

    public class Sample
    {
        public const string SampCategory = "samp_category";
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Cache key phân hệ quản lý báo cáo
    /// </summary>
    public class Rpt
    {
        public const string RptInfoConfig = "rpt_info_config";
    }

    /// <summary>
    /// Description: Cache key phân hệ VDS (dữ liệu giao thông time-series trên QuestDB).
    /// Chỉ cache khoảng thời gian ĐÃ ĐÓNG (giờ/ngày đã qua); không cache cửa sổ chứa hiện tại.
    /// </summary>
    /// <summary>
    /// Description: Cache ảnh của phân hệ Media (WP-MediaPublic). Khoá ghép thêm ID/cột/version,
    /// ví dụ "media_event_type:{id}:{updateTicks}" — dữ liệu đổi thì version đổi nên không cần xoá chủ động.
    /// Created date: 08/10/2026
    /// </summary>
    public class Media
    {
        public const string MediaIncident = "media_incident";
        public const string MediaEventType = "media_event_type";
        public const string MediaEquipmentType = "media_equipment_type";
        public const string MediaVms = "media_vms";
    }

    public class Vds
    {
        public const string VdsTrafficData = "vds_traffic_data";
        public const string VdsTrafficStatistic = "vds_traffic_statistic";
        public const string VdsTrafficMatrix = "vds_traffic_matrix";

        // Danh mục dựng snapshot sự kiện (loại sự kiện, thời gian hiển thị, camera, tên vị trí/hướng); hậu tố tenant|loại tài khoản.
        public const string VdsEventSnapshot = "vds_event_snapshot";

        // VDS cấu hình qua bảng dùng chung CfgSystem; giá trị phải khớp đúng prefix
        // đang được SysConfigDataQueryHandler/SysOpConfigQueryHandler sử dụng.
        public const string ConfigData = "sysConfigData";
        public const string OpConfig = "sysOpConfig";
    }
}
