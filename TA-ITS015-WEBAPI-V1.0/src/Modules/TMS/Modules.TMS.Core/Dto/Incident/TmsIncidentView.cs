using Modules.TMS.Core.Dto.WorkExecute;

namespace Modules.TMS.Core.Dto.Incident
{
    public class TmsIncidentView
    {
        public TmsIncidentOutput Incident { get; set; }
        public List<TmsWorkExecuteOutput> WorkExecuteList { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Thông tin luồng TỰ ĐỘNG đang gắn với sự cố này.
        ///              NULL = sự cố thuần thủ công, frontend xử lý y như trước khi có tính năng tự động.
        ///              Khác NULL = có automation run chưa kết thúc, frontend khóa thao tác thủ công
        ///              và hiển thị đếm ngược.
        /// Created date: 24/07/2026
        /// </summary>
        public IncidentAutomationView? Automation { get; set; }
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Nhánh dữ liệu tự động hóa bám vào TmsIncidentView. Chỉ chứa những gì giao diện cần
    ///              để hiển thị banner đếm ngược và quyết định có cho thao tác hay không - không phơi
    ///              toàn bộ entity TmsIncidentAutomationRun ra ngoài.
    /// Created date: 24/07/2026
    /// </summary>
    public class IncidentAutomationView
    {
        /// <summary>
        /// ID của TmsIncidentAutomationRun, dùng cho ExecuteNow / ConvertToManual.
        /// </summary>
        public string RunId { get; set; }

        /// <summary>
        /// Trạng thái run (IncidentAutomationConst.RunStatus). Banner chỉ đếm ngược khi WarningPending.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Thời điểm bắt đầu đếm ngược. Cùng với ExecuteDueAt cho ra độ dài cửa sổ cảnh báo,
        ///              để giao diện vẽ được thanh tiến trình (không có mốc này thì chỉ hiện được số giây).
        /// Created date: 24/07/2026
        /// </summary>
        public DateTime? WarningStartedAt { get; set; }

        /// <summary>
        /// Thời điểm sẽ tự động thực thi. Frontend đếm ngược tới mốc này.
        /// </summary>
        public DateTime? ExecuteDueAt { get; set; }

        /// <summary>
        /// Thời điểm sẽ tự động kết thúc (nếu cấu hình bật tự động kết thúc).
        /// </summary>
        public DateTime? AutoFinishAt { get; set; }

        /// <summary>
        /// Run có bật tự động kết thúc theo snapshot cấu hình hay không.
        /// </summary>
        public bool IsAutoFinishEnabled { get; set; }

        /// <summary>
        /// Số giây tự động kết thúc sau khi run được kích hoạt. Có giá trị trước khi AutoFinishAt được tính.
        /// </summary>
        public int? AutoFinishAfterSeconds { get; set; }

        /// <summary>
        /// Tên kịch bản sẽ được áp dụng, để người trực biết trước điều gì sắp xảy ra.
        /// </summary>
        public string? TrafficPlanName { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Icon loại sự kiện để banner hiển thị. Đặt ở đây thay vì thêm vào TmsIncidentOutput
        ///              vì chỉ banner cần, còn TmsIncidentOutput đang được dùng ở rất nhiều endpoint khác.
        /// Created date: 24/07/2026
        /// </summary>
        public string? EventTypeUrl { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Tài khoản hiện tại có được phép thao tác (thực thi ngay / chuyển thủ công) hay không.
        ///              Tính ở backend theo nhóm tài khoản, KHÔNG để frontend tự suy luận phân quyền.
        ///              false => chỉ xem thông tin và đếm ngược.
        /// Created date: 24/07/2026
        /// </summary>
        public bool CanOperate { get; set; }

        /// <summary>
        /// Giờ server tại thời điểm trả về, để frontend hiệu chỉnh lệch giờ máy trạm khi đếm ngược.
        /// </summary>
        public DateTime ServerTime { get; set; }

        /// <summary>
        /// Author: KienLT
        /// Description: Mã lỗi khi Status = Failed (run gần nhất của sự cố Đang xử lý đã lỗi và đã bàn giao cho
        ///              người). Ví dụ AutoDeviceControlFailed, StuckTimeout, UnexpectedError. Null với run còn sống.
        /// Created date: 16/10/2026
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Author: KienLT
        /// Description: Lý do lỗi đi kèm ErrorCode để người trực biết vì sao tự động dừng (vd thiết bị bị giữ).
        /// Created date: 16/10/2026
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
