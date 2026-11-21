namespace Modules.TMS.Core.Dto.WorkExecute
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin trạng thái state
    /// Created date: 20/11/2024
    /// </summary>
    public class TmsWorkExecuteStateInput
    {
        public string? Id { get; set; }
        public string? State { get; set; }
        public string? Remark { set; get; }

        /// <summary>
        /// Actor dùng cho luồng nền (ví dụ SYSTEM_AUTOMATION). Null giữ nguyên hành vi
        /// manual: lấy account từ HttpContext/App.User.
        /// </summary>
        public string? Actor { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin các tham số
    /// Created date: 20/11/2024
    /// </summary>
    public class TmsWorkExecuteParaInput
    {
        public string? IncidentId { get; set; }
        public bool? UseForTimeline { get; set; }
    }
}
