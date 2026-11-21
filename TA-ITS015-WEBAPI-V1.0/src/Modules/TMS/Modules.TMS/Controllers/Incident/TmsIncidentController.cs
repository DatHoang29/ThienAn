using Modules.TMS.Core.Dto.Incident;
using Shared.Infrastructure.Persistence.SqlSugar;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Incident
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsIncidentController : BaseController
    {
        #region CRUD ================
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<TmsIncidentPageOutput> Page(PageTmsIncidentInput input)
        {
            return await MessBus.InvokeAsync<TmsIncidentPageOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tham số
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("List")]
        [DisplayName("List based on parameters")]
        public async Task<List<TmsIncidentOutput>> GetList(TmsIncidentInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsIncidentOutput>>(input);
        }

        /// <summary>Unpaged main-screen list, shared with SSE.</summary>
        [HttpPost("MonitorList")]
        [DisplayName("Incident monitor list")]
        public Task<TmsIncidentMonitorOutput> MonitorList(TmsIncidentMonitorInput input, CancellationToken cancellationToken)
            => MessBus.InvokeAsync<TmsIncidentMonitorOutput>(input, cancellationToken);

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin theo id hoặc code
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Single")]
        [DisplayName("Get single based on id or code")]
        public async Task<TmsIncidentOutput> Single(TmsIncidentSingleInput input)
        {
            return await this.MessBus.InvokeAsync<TmsIncidentOutput>(input);
        }

        /// <summary>
        /// Đọc toàn bộ ảnh camera đã lưu tại một mốc trạng thái của sự cố.
        /// </summary>
        [HttpGet("GetImages")]
        [DisplayName("Get incident camera images")]
        public async Task<List<TmsIncidentImageOutput>> GetImages([FromQuery] TmsIncidentImageInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsIncidentImageOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a record to the table")]
        public async Task<bool> AddTmsIncident(AddTmsIncidentInput input)
        {
            return await this.MessBus.InvokeAsync<bool>(input);
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Tạo sự cố từ một bản tin VDS đã có (tab Sự kiện). Bản tin đã có sự cố ⇒ trả sự cố đó;
        ///              đang có sự cố mở cùng loại, cùng vị trí ⇒ trả thông tin xung đột, chưa tạo (gửi lại với Force).
        /// Created date: 23/10/2026
        /// </summary>
        [HttpPost("CreateFromVdsEvent")]
        [DisplayName("Create an incident from a VDS event")]
        public async Task<IncidentFromVdsEventOutput> CreateFromVdsEvent(CreateIncidentFromVdsEventInput input)
        {
            return await this.MessBus.InvokeAsync<IncidentFromVdsEventOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record in the table")]
        public async Task<bool> UpdateTmsIncident(UpdateTmsIncidentInput input)
        {
            return await this.MessBus.InvokeAsync<bool>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record in the table")]
        public async Task<bool> DeleteTmsIncident(DeleteTmsIncidentInput input)
        {
            return await this.MessBus.InvokeAsync<bool>(input);
        }

        #endregion CRUD ==============

        #region Workflow ===============

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin sự cố mới nhất
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("LatestIncident")]
        [DisplayName("Get the latest incident")]
        public async Task<List<TmsIncidentOutput>> LatestIncident(TmsIncidentLatestInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsIncidentOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Kiểm tra có sự cố theo Account
        /// </summary>
        [HttpPost("CheckIncident")]
        [DisplayName("Check incident by account")]
        public async Task<bool> CheckIncident(TmsIncidentCheckInput input)
        {
            return await this.MessBus.InvokeAsync<bool>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo trạng thái và account
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("ListByAccount")]
        [DisplayName("Get list based on state and account")]
        public async Task<Dictionary<string, List<TmsIncidentView>>> ListByAccount(TmsIncidentByAccountInput input)
        {
            return await this.MessBus.InvokeAsync<Dictionary<string, List<TmsIncidentView>>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái kèm kịch bản (nếu có) cho một record
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("UpdateState")]
        [DisplayName("Update state for a record ")]
        public async Task<bool> UpdateTmsIncidentState(TmsIncidentStateInput input)
        {
            return await this.MessBus.InvokeAsync<bool>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái state cho nhiều record
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("BatchUpdateState")]
        [DisplayName("Update state for multiple records")]
        public async Task<bool> BatchUpdateTmsIncidentState(List<TmsIncidentStateInput> input)
        {
            return await this.MessBus.InvokeAsync<bool>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Danh sách các thao tác tương ứng với account
        /// </summary>
        [HttpPost("ListAllowedAction")]
        [DisplayName("Get allowed actions by account")]
        public async Task<List<int>> ListIncidentAllowedAction(TmsIncidentAllowedActionInput input)
        {
            return await this.MessBus.InvokeAsync<List<int>>(input);
        }

        #endregion Workflow =============

        #region Statistics & Helpers ==================
        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái là "Kết thúc"
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("UpdateIncidentStateToFinished")]
        [DisplayName("Update the incident state to finished")]
        public async Task UpdateIncidentStateToFinished(string incidentId)
        {
            var input = new TmsIncidentStateInput()
            {
                Id = incidentId,
                State = ((int)TmsIncidentStateEnum.Finished).ToString()
            };

            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái là "Đăng ký mới"
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("UpdateIncidentStateToRegistration")]
        [DisplayName("Update the incident state to registration")]
        public async Task UpdateIncidentStateToRegistration(string incidentId)
        {
            var input = new TmsIncidentStateInput()
            {
                Id = incidentId,
                State = ((int)TmsIncidentStateEnum.Registration).ToString()
            };

            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái là "Xác thực"
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("UpdateIncidentStateToVerified")]
        [DisplayName("Update the incident state to verified")]
        public async Task UpdateIncidentStateToVerified(string incidentId)
        {
            var input = new TmsIncidentStateInput()
            {
                Id = incidentId,
                State = ((int)TmsIncidentStateEnum.Verified).ToString()
            };

            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái là "Xác nhận" kèm kịch bản
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("UpdateIncidentStateToInProgress")]
        [DisplayName("Update the incident state to inProgress")]
        public async Task UpdateIncidentStateToInProgress(string incidentId, string trafficPlanId)
        {
            var input = new TmsIncidentStateInput()
            {
                Id = incidentId,
                State = ((int)TmsIncidentStateEnum.InProgress).ToString(),
                TrafficPlanId = trafficPlanId
            };

            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái là "Hủy bỏ"
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("UpdateIncidentStateToCanceled")]
        [DisplayName("Update the incident state to canceled")]
        public async Task UpdateIncidentStateToCanceled(string incidentId)
        {
            var input = new TmsIncidentStateInput()
            {
                Id = incidentId,
                State = ((int)TmsIncidentStateEnum.Canceled).ToString()
            };

            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Biểu đồ sự cố
        /// </summary>
        [HttpPost("IncidentChart")]
        [DisplayName("Incident chart")]
        public async Task<IncidentChartOutput> IncidentChart(IncidentChartInput input)
        {
            return await this.MessBus.InvokeAsync<IncidentChartOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Đếm số lượng sự cố
        /// </summary>
        [HttpPost("CountIncident")]
        [DisplayName("Count incident")]
        public async Task<IncidentCountOutput> CountIncident(IncidentCountInput input)
        {
            return await this.MessBus.InvokeAsync<IncidentCountOutput>(input);
        }

        #endregion Statistics & Helpers
    }
}
