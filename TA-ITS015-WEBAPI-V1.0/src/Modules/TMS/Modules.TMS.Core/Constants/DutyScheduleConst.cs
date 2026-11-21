namespace Modules.TMS.Core.Constants
{
    /// <summary>
    /// Constants cho Duty Schedule
    /// Author: System
    /// Created date: 20/11/2025
    /// </summary>
    public static class DutyScheduleConst
    {
        /// <summary>
        /// Loại yêu cầu thay đổi lịch trực
        /// </summary>
        public static class ChangeRequestType
        {
            /// <summary>
            /// Đổi ca với người khác
            /// </summary>
            public const string Swap = "Swap";

            /// <summary>
            /// Xin nghỉ (vắng mặt)
            /// </summary>
            public const string Absent = "Absent";

            /// <summary>
            /// Thay thế người khác
            /// </summary>
            public const string Replace = "Replace";

            /// <summary>
            /// Chuyển sang lịch trực khác
            /// </summary>
            public const string Transfer = "Transfer";
        }

        /// <summary>
        /// Trạng thái yêu cầu thay đổi
        /// </summary>
        public static class ChangeRequestStatus
        {
            /// <summary>
            /// Chờ phê duyệt
            /// </summary>
            public const string Pending = "pending";

            /// <summary>
            /// Đã phê duyệt
            /// </summary>
            public const string Approved = "approve";

            /// <summary>
            /// Từ chối
            /// </summary>
            public const string Rejected = "reject";

            /// <summary>
            /// Đã hủy
            /// </summary>
            public const string Cancelled = "cancel";
        }

        /// <summary>
        /// Trạng thái chi tiết lịch trực (người tham gia)
        /// </summary>
        public static class DetailStatus
        {
            /// <summary>
            /// Tham gia bình thường
            /// </summary>
            public const string Join = "join";

            /// <summary>
            /// Đã đổi ca (đổi đi)
            /// </summary>
            public const string Swap = "swap";

            /// <summary>
            /// Vắng mặt
            /// </summary>
            public const string Absent = "absent";
        }

        /// <summary>
        /// Vai trò trong lịch trực
        /// </summary>
        public static class Role
        {
            /// <summary>
            /// Đội trưởng
            /// </summary>
            public const string Leader = "leader";

            /// <summary>
            /// Thành viên
            /// </summary>
            public const string Member = "member";
        }

        /// <summary>
        /// Trạng thái lịch trực (event)
        /// </summary>
        public static class ScheduleStatus
        {
            /// <summary>
            /// Đăng ký mới
            /// </summary>
            public const string Register = "register";

            /// <summary>
            /// Đã xác nhận
            /// </summary>
            public const string Confirm = "confirm";

            /// <summary>
            /// Đang xử lý/Đang diễn ra
            /// </summary>
            public const string Process = "process";

            /// <summary>
            /// Hoàn thành
            /// </summary>
            public const string Finish = "finish";

            /// <summary>
            /// Đã hủy
            /// </summary>
            public const string Cancel = "cancel";
        }

        /// <summary>
        /// Loại hành động lịch sử
        /// </summary>
        public static class HistoryActionType
        {
            /// <summary>
            /// Tạo mới lịch trực
            /// </summary>
            public const string Create = "create";

            /// <summary>
            /// Cập nhật lịch trực
            /// </summary>
            public const string Update = "update";

            /// <summary>
            /// Xóa lịch trực
            /// </summary>
            public const string Delete = "delete";

            /// <summary>
            /// Thêm thành viên
            /// </summary>
            public const string AddMember = "add_member";

            /// <summary>
            /// Xóa thành viên
            /// </summary>
            public const string RemoveMember = "remove_member";

            /// <summary>
            /// Chuyển lịch trực
            /// </summary>
            public const string Transfer = "transfer";

            /// <summary>
            /// Đổi ca
            /// </summary>
            public const string Swap = "swap";

            /// <summary>
            /// Vắng mặt
            /// </summary>
            public const string Absent = "absent";

            /// <summary>
            /// Thay thế
            /// </summary>
            public const string Replace = "replace";

            /// <summary>
            /// Phê duyệt yêu cầu
            /// </summary>
            public const string Approve = "approve";

            /// <summary>
            /// Từ chối yêu cầu
            /// </summary>
            public const string Reject = "reject";

            /// <summary>
            /// Hủy yêu cầu
            /// </summary>
            public const string Cancel = "cancel";

            /// <summary>
            /// Thay đổi vai trò
            /// </summary>
            public const string ChangeRole = "change_role";

            /// <summary>
            /// Thay đổi trạng thái
            /// </summary>
            public const string ChangeStatus = "change_status";

            /// <summary>
            /// Cập nhật tên lịch trực
            /// </summary>
            public const string UpdateName = "update_name";

            /// <summary>
            /// Cập nhật trạng thái lịch trực
            /// </summary>
            public const string UpdateStatus = "update_status";

            /// <summary>
            /// Cập nhật thời gian lịch trực
            /// </summary>
            public const string UpdateTime = "update_time";

            /// <summary>
            /// Cập nhật đội tuần tra
            /// </summary>
            public const string UpdateTeam = "update_team";

            /// <summary>
            /// Cập nhật ghi chú
            /// </summary>
            public const string UpdateRemark = "update_remark";

            /// <summary>
            /// Cập nhật danh sách thành viên
            /// </summary>
            public const string UpdateMembers = "update_members";
        }
    }

    /// <summary>
    /// Enum cho Duty Schedule (cho frontend tương thích)
    /// </summary>
    public class DutyScheduleEnums
    {
        /// <summary>
        /// Enum loại yêu cầu thay đổi
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: DutyScheduleChangeRequestTypeEnum
        /// </summary>
        [Description("Duty Schedule Change Request Type")]
        public enum ChangeRequestType
        {
            /// <summary>
            /// Đổi ca
            /// </summary>
            [Description("Swap")]
            Swap = 0,

            /// <summary>
            /// Xin nghỉ
            /// </summary>
            [Description("Absent")]
            Absent = 1,

            /// <summary>
            /// Thay thế
            /// </summary>
            [Description("Replace")]
            Replace = 2,

            /// <summary>
            /// Chuyển lịch
            /// </summary>
            [Description("Transfer")]
            Transfer = 3
        }

        /// <summary>
        /// Enum trạng thái yêu cầu
        /// Note: This enum must match the enum in the frontend
        /// Path: ...\src\types\enums\tmsEnum.ts: DutyScheduleChangeRequestStatusEnum
        /// </summary>
        [Description("Duty Schedule Change Request Status")]
        public enum ChangeRequestStatus
        {
            /// <summary>
            /// Chờ duyệt
            /// </summary>
            [Description("Pending")]
            Pending = 0,

            /// <summary>
            /// Đã duyệt
            /// </summary>
            [Description("Approved")]
            Approved = 1,

            /// <summary>
            /// Từ chối
            /// </summary>
            [Description("Rejected")]
            Rejected = 2,

            /// <summary>
            /// Đã hủy
            /// </summary>
            [Description("Cancelled")]
            Cancelled = 3
        }
    }
}
