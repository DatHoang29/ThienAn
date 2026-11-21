// Phân loại đối tượng MapLocation
// Author: Ngọc Thắng
export enum TmsMapLocationRefType {
    Equipment = 'device', // Thiết bị
    Zone = 'zone', // Zone
    WorkUnit = 'unit' // Đơn vị
}

// Author: Ngọc Thắng
// Description: Danh sách config-code của incident
export enum TmsIncidentConfigCodeEnum {
    ClosedIncidentDaysLimit = 'closed_incident_days_limit',
    OpenIncidentDaysLimit = 'open_incident_days_limit',
    EquipmentSuggestionSmallRange = 'incident_equipment_suggestion_small_range',
    EquipmentSuggestionLargeRange = 'incident_equipment_suggestion_large_range',
    RouteLength = 'incident_route_length',
    RouteStartPosition = 'incident_route_start_position',
    RouteEndPosition = 'incident_route_end_position'
}

// Ngưỡng SỐ ZONE (nhóm its_operation_config) để tổng hợp trạng thái tuyến ở tiêu đề màn hình traffic info:
// đủ số zone nghẽn ⇒ Tắc nghẽn; đủ số zone chậm hoặc nghẽn ⇒ Chậm. Thiếu cấu hình ⇒ 3.
export enum TmsTrafficInfoConfigCodeEnum {
    SlowZoneThreshold = 'traffic_info_slow_zone_threshold',
    CongestionZoneThreshold = 'traffic_info_congestion_zone_threshold'
}

// Mã trạng thái giao thông của zone (TmsZoneStatus.Condition). Khớp danh mục tms_zone_status_color và cấu hình
// worker VDS (ZoneStatus: NormalCondition / SlowCondition / CongestedCondition).
export enum TmsZoneConditionEnum {
    Normal = 'normal',
    Slow = 'slow',
    Congested = 'conges'
}

// Danh sách trạng thái sự kiện sự cố
// Author: Ngọc Thắng
export enum TmsIncidentStateEnum {
    Skipped = '0', // Đã bỏ qua
    Registration = '1', // Đăng ký mới
    Verified = '2', // Đã xác thực
    InProgress = '3', // Đang được xử lý
    Finished = '4', // Đã xử lý xong
    Canceled = '5', // Đã hủy bỏ
    Execute = '95', // Thực thi
    LiveCam = '96', // Live camera
    Detail = '97', // Chi tiết timeline
    Report = '98', // Báo cáo
    Updated = '99', // Đã cập nhật
    Deleted = '100' // Đã được xóa
}

export enum TmsIncidentActionEnum {
    Skip = 0, // Bỏ qua
    Registration = 1, // Đăng ký
    Verify = 2, // Xác thực
    Execute = 3, // Xử lý
    Finish = 4, // Kết thúc
    Cancel = 5, // Hủy bỏ
    LiveCam = 96, // Live camera
    Detail = 97, // Chi tiết
    Report = 98, // Báo cáo
    Update = 99, // Cập nhật
    Delete = 100 // Xóa bỏ
}

// Danh sách trạng thái xử lý công việc
// Author: Ngọc Thắng
export enum TmsWorkExecuteStateEnum {
    Waiting = '0', // Chưa thực hiện
    Executed = '1', // Đã thực hiện
    Skipped = '2' // Đã bỏ qua
}

// Danh sách chế độ thực hiện công việc của sự cố
// Author: Ngọc Thắng
export enum TmsWorkModeEnum {
    Manual = '0', // Chế độ thực hiện bằng tay công việc xử lý sự cố
    Auto = '1' // Chế độ tự động thực hiện công việc xử lý sự cố
}

// Danh sách phân loại điều khiển thiết bị
// Author: Ngọc Thắng
export enum TmsControlTypeEnum {
    Vms = 'vms',
    Lcs = 'lcs'
}

// Danh sách phân loại nhóm liên lạc
// Author: Ngọc Thắng
export enum TmsWorkContactType {
    Employee = '1', // Nhân sự
    WorkUnit = '2', // Đơn vị
    Other = '3' // Khác
}

// Danh sách phân loại đơn vị
// Author: Ngọc Thắng
export enum TmsWorkUnitType {
    WeightStation = '6', // Trạm cân
    TollStation = '7', // Trạm thu phí
}

// Đường dẫn route menu báo cáo
// Author: Ngọc Thắng
export enum TmsReportRouteEnum {
    IncidentTimelineDetail = '#/tms/incident/timeline?incidentId=',
    IncidentReportIndex = '#/report/tms/incident/index',
    IncidentReportDetail = '#/report/tms/incident/detail?incidentId=',
}

// Author: Ngọc Thắng
// Description: Danh sách code dùng chung bảng SysConfigType
export enum TmsTrafficDataReportTypeEnum {
    TrafficDayReportByShift = "traffic_day_report_by_shift", // Báo cáo theo ca trong ngày
    TraffiDayReportByTime = "traffic_day_report_by_time", // Báo cáo theo thời gian trong ngày
    TrafficMonthReportByTime = "traffic_month_report_by_time", // Báo cáo theo ca trong tháng
    TrafficMonthReportByShift = "traffic_month_report_by_shift", // Báo cáo theo giờ trong tháng
}

// Author: Ngọc Thắng
// Description: Danh sách code ca làm việc
export enum TmsWorkShiftEnum {
    WorkShift1 = 'work_shift_1', // Ca 1
    WorkShift2 = 'work_shift_2', // Ca 2
    WorkShift3 = 'work_shift_3', // Ca 3
    WorkTime1 = 'work_time_1', // Buổi 1
    WorkTime2 = 'work_time_2', // Buổi 2
    WorkTime3 = 'work_time_3', // Buổi 3
}

// Enum đối tượng phát thông báo phân hệ sự cố
// Author: Ngọc Thắng
export enum IncidentBroadcastObjectEnum {
    Incident = 'Incident',
    WorkExecute = 'WorkExecute',
    /** SonTH - 24/07/2026: biến động của một lần tự động xử lý sự cố (TmsIncidentAutomationRun) */
    AutomationRun = 'AutomationRun'
}

// Enum nguồn dữ liệu thông tin đăng ký xe
// Author: Ngọc Thắng
export enum TmsVehicleRegistrationTypeEnum {
    Manual = '0',
    Import = '1',
    Api = '2'
}

// Enum cảnh báo phương tiện
// Author: Ngọc Thắng
export enum TmsVehicleFilterWarningEnum {
    NoAlarm = '0',
    Alarm = '1',
}

// Enum phân loại cảnh báo xe
// Author: Ngọc Thắng
export enum TmsVehicleFilterTypeEnum {
    AllowList = '0',
    DenyList = '1',
}

// Author: Ngọc Thắng
// Description: Danh sách trạng thái hoạt động của thiết bị
export enum TmsEquipmentStateEnum {
    Inactive = 0,
    Active = 1,
    Error = 2,
    Fault = 3,
    CannotConnected = 4
}

// Author: SonTH
// Description: Danh sách trạng thái hoạt động của thiết bị
export enum TmsEquipmentStateValueEnum {
    Inactive = 'inactive',
    Active = 'active',
    Fault = 'fault',
    Default = 'default'
}

// TODO: ReCheck
// loại thiết bị
export enum TmsEquipmentType {
    Vms = 'VMS', // thiết bị vms
    FCam = 'Equipment_Type_FCam', // Fcam
    TCam = 'Equipment_Type_TCam',// Tcam
    PCam = 'Equipment_Type_PCam',// Pcam
    LCS = "LCS",
    VideoWallController = 'VideoWall_Controller',
}

// Author: KienLT
// Created date: 11/11/2026
// Description: Mã modal thiết bị trên bigscreen = SysConfigData.Code của danh mục
//   BaseConfigTypeEnum.TmsEquipmentModalType, gán cho loại thiết bị tại TmsEquipmentType.modalCode.
//   Chỉ liệt kê các mã FE đã có component; mã khai thêm trong cấu hình mà chưa có ở đây ⇒ mở modal Device.
export enum TmsEquipmentModalCode {
    Device = 'device', // Thông tin thiết bị chung (mặc định)
    Vms = 'vms',
    Lcs = 'lcs',
    Cctv = 'cctv', // Camera CCTV/PTZ/FIX
    Vds = 'vds',
    Barrier = 'barrier', // Hệ barrier - thanh chắn
    BarrierLight = 'barrier_light', // Hệ barrier - đèn tín hiệu đi kèm
    TrafficLight = 'traffic_light', // Đèn tín hiệu giao thông SS (điều khiển độc lập, không ghép với barrier)
    Fan = 'fan',
}


// =============================================

export enum TmsEventType {
    Incident = 'incident', // Sự cố chung
    Fire = 'fire', // Cháy
    TrafficJam = 'traffic_jam', // Kẹt xe
    Accident = 'accident', // Tai nạn
    FA = 'FA', // Báo cháy
}

// Loại xử lý hiển thị thiết bị theo sự kiện sự cố
// Note: Giá trị PHẢI khớp const phía BE (EquipmentEventRuleConst.ProcessType)
// Author: ChuongLP
export enum TmsEquipmentEventRuleProcessType {
    Before = 'Before', // Hiển thị TRƯỚC vị trí sự cố (upstream)
    After = 'After', // Hiển thị SAU vị trí sự cố (downstream)
}

//loại bản đồ
export enum TmsTrafficInfoMap {
    Vms = 'vms', // Bản đồ VMS
    Full = 'full', // Full Tuyến
    CCTV = 'cctv', // Bản đồ CCTV
    Block = 'block', //Chế độ block
    BlockVms = 'block-vms'

}
//loại bản đồ
export enum TmsMapType {
    Image = 'image', // Bản đồ hình ảnh
    Online = 'online', // Bản đồ trực tuyến
    Offline = 'offline', // Bản đồ server offline

}

//TMS - Trạng thái công việc tự động
export enum TmsWorkDetail {
    Manual = 1,
    Auto = 2,
    Disable = 0,

}
//Loại khu vực
export enum TmsZoneTypes {
    Zone = "zone",
    Route = "route",
    Section = "section",


}

// Hướng tính toán chiều Km của polygon (zone)
// Description: Xác định đầu nào của polygon (centerline) ứng với Km thấp (from) và Km cao (to).
//  - Auto: tự động suy ra theo vị trí thực tế của các zone cùng làn (laneId)
//  - Forward: điểm đầu centerline = Km thấp (from Km/met) → điểm cuối = Km cao (to Km/met)
//  - Reverse: điểm đầu centerline = Km cao (to Km/met) → điểm cuối = Km thấp (from Km/met)
export enum TmsZoneKmDirection {
    Auto = 'auto',
    Forward = 'forward',
    Reverse = 'reverse',
}
