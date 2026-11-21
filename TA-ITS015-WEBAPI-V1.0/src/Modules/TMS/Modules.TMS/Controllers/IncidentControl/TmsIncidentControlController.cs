using Microsoft.AspNetCore.Mvc;
using Modules.TMS.Core.Dto.IncidentControl;
using Modules.TMS.Core.Entities;
using System.ComponentModel;

namespace Modules.TMS.Controllers.IncidentControl
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller truy vấn thông tin kiểm soát thiết bị theo sự cố (Incident Control)
    /// Created date: 2026-10-14
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsIncidentControlController : BaseController
    {
        /// <summary>
        /// Lấy danh sách kiểm soát sự cố theo danh sách thiết bị (hỗ trợ kiểm tra thiết bị đang bị sự cố chiếm giữ)
        /// </summary>
        [HttpPost("GetByEquipment")]
        [DisplayName("Lấy kiểm soát thiết bị theo danh sách thiết bị")]
        public async Task<List<TmsIncidentControl>> GetByEquipment([FromBody] GetIncidentControlByEquipmentInput input)
        {
            return await MessBus.InvokeAsync<List<TmsIncidentControl>>(input);
        }

        /// <summary>
        /// Lấy danh sách kiểm soát thiết bị theo ID sự cố
        /// </summary>
        [HttpPost("GetByIncident")]
        [DisplayName("Lấy danh sách thiết bị theo sự cố")]
        public async Task<List<TmsIncidentControl>> GetByIncident([FromBody] GetIncidentControlInput input)
        {
            return await MessBus.InvokeAsync<List<TmsIncidentControl>>(input);
        }

        /// <summary>
        /// Lấy danh sách bản ghi Incident Control
        /// </summary>
        [HttpPost("List")]
        [DisplayName("Lấy danh sách Incident Control")]
        public async Task<List<TmsIncidentControlOutput>> List([FromBody] TmsIncidentControlInput input)
        {
            return await MessBus.InvokeAsync<List<TmsIncidentControlOutput>>(input);
        }
    }
}
