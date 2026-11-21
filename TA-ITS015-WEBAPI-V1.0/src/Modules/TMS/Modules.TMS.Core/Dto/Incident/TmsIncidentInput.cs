using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Core.Dto.Incident
{
    #region Common DTOs ==================
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số phân trang
    /// Created date: 09/08/2024
    /// </summary>
    public class PageTmsIncidentInput : BasePageInput
    {
        public bool IncludeEvents { get; set; }
        public bool IncludeIncidents { get; set; } = true;
        public string? Name { get; set; }
        public string? EventId { get; set; }
        public string? EventTypeId { get; set; }
        public string? ZoneId { get; set; }
        public string? Source { get; set; }
        public string? State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? FromKm { get; set; }
        public int? FromMet { get; set; }
        public int? ToKm { get; set; }
        public int? ToMet { get; set; }
        public bool? IsFilterByOrgId { get; set; }

        /// <summary>
        /// GIỮ ĐỂ KHÔNG ĐỔI SCHEMA — server KHÔNG còn đọc cờ này nữa.
        ///
        /// <para>Trước đây cờ này vừa bỏ hai câu COUNT thống kê vừa cho vượt trần
        /// <c>TmsIncidentPageRules.MaxPageSize</c>. Vì nó là property công khai trên body,
        /// client tự đặt <c>isRequestForChart: true</c> là kéo được cả bảng. Hai tác dụng đó nay do
        /// <see cref="ChartTmsIncidentPageInput"/> mang — kiểu message NỘI BỘ, không phải tham số action nên
        /// client không đặt được.</para>
        /// </summary>
        public bool IsRequestForChart { get; set; } = false;

        public int? ClosedIncidentDaysLimit { get; set; }
        public int? OpenIncidentDaysLimit { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Message NỘI BỘ cho hai đường vẽ biểu đồ / đếm sự cố (<c>IncidentChart</c>,
    /// <c>CountIncident</c>). Hai đường đó cần TRỌN tập lọc nên FE gửi <c>pageSize = 1000000</c>; đồng thời
    /// chúng chỉ đọc <c>Records</c> nên hai câu COUNT thống kê là vô ích.
    ///
    /// <para>Vì sao phải là kiểu riêng chứ không phải một cờ trên DTO công khai: Wolverine định tuyến theo
    /// kiểu runtime, và kiểu này KHÔNG bao giờ là tham số của action nào nên không lộ ra swagger và client
    /// không dựng được. Đó là cách duy nhất phân biệt "hệ thống tự gọi" với "client gọi" sau khi message đã
    /// qua bus.</para>
    /// Created date: 21/09/2026
    /// </summary>
    public class ChartTmsIncidentPageInput : PageTmsIncidentInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số lọc
    /// Created date: 09/08/2024
    /// </summary>
    public class TmsIncidentInput
    {
        public string? Name { get; set; }
        public string? EventTypeId { get; set; }
        public string? ZoneId { get; set; }
        public string? Source { get; set; }
        public string? State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? FromKm { get; set; }
        public int? FromMet { get; set; }
        public int? ToKm { get; set; }
        public int? ToMet { get; set; }
        public bool? IsFilterByOrgId { get; set; }
        public bool IsRequestForChart { get; set; } = false;
        public int? ClosedIncidentDaysLimit { get; set; }
        public int? OpenIncidentDaysLimit { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Decription: DTO thông tin thêm mới
    /// Created date: 09/08/2024
    /// </summary>
    public class AddTmsIncidentInput : TmsIncident { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin cập nhật
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateTmsIncidentInput : AddTmsIncidentInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin xóa
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteTmsIncidentInput : BaseIdInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin id hoặc code
    /// Created Date: 09/08/2024
    /// </summary>
    public class TmsIncidentSingleInput
    {
        public string? Id { set; get; }
        public string? Code { set; get; }
    }

    #endregion Common DTOs ==================

    #region Extended DTOs ==================
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin trạng thái và kịch bản
    /// Created date: 19/08/2024
    /// </summary>
    public class TmsIncidentStateInput : TmsIncidentSingleInput
    {
        public string? State { get; set; }
        public string? TrafficPlanId { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Actor ghi vào các trường audit (ValidatedBy/ApprovedBy/FinishedBy) khi được set.
        ///              Dùng khi dispatch từ luồng tự động (không có HttpContext/App.User).
        ///              Null => lấy theo App.User hiện tại (hành vi cũ, không đổi cho luồng thủ công).
        /// Created date: 24/07/2026
        /// </summary>
        public string? Actor { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO kiểm tra có sự cố
    /// Created date: 19/08/2024
    /// </summary>
    public class TmsIncidentCheckInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO sự cố mới nhất
    /// Created date: 19/08/2024
    /// </summary>
    public class TmsIncidentLatestInput
    {
        public int Count { get; set; } = 1;
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO sự cố theo account
    /// Created date: 19/08/2024
    /// </summary>
    public class TmsIncidentByAccountInput { }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin kịch bản để update
    /// Created date: 19/08/2024
    /// </summary>
    public class TmsIncidentTrafficPlanInput : TmsIncidentSingleInput
    {
        public string? TrafficPlanId { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO trạng thái state được phép
    /// Created date: 19/08/2024
    /// </summary>
    public class TmsIncidentAllowedActionInput { }

    #endregion Extended DTOs ==================
}