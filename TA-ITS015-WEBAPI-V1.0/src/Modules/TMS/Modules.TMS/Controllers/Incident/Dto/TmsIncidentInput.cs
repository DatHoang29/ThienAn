using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.Incident.Dto
{
    #region Common DTOs ==================
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số phân trang
    /// Created date: 09/08/2024
    /// </summary>
    public class PageTmsIncidentInput : BasePageInput
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
        public int? ClosedIncidentDaysLimit {  get; set; }
        public int? OpenIncidentDaysLimit { get; set; }
    }

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
    public class DeleteTmsIncidentInput : BaseIdInput {}

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
    public class TmsIncidentStateInput: TmsIncidentSingleInput
    {
        public string? State { get; set; }
        public string? TrafficPlanId { get; set; }
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
    public class TmsIncidentTrafficPlanInput: TmsIncidentSingleInput
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