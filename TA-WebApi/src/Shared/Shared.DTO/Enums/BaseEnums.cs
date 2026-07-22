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
            Dir = 1,

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
        /// Author: Ngọc Thắng
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\baseEnum.ts: AccountOrgCodeEnum
        /// </summary>
        public enum AccountOrgCodeEnum
        {
            TcVhh = 0, // Nhóm trưởng ca
            NvGsgt = 1, // Nhóm giám sát
            NvQlgt = 2, // Nhóm quản lý
            Leader = 3, // Tương tự TcVhh
            Monitor = 4, // Tương tự NvGsgt
            Manager = 5 // Tương tự NvQlgt
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
            WorkExecute = 1
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
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Enum code cấu hình SysOpConfig
        /// </summary>
        public static class SysOpConfigCodeEnum
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
    }
}
