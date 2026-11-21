using Microsoft.AspNetCore.Authorization;
using Modules.Media.Controllers.ItsMedia.EquipmentType.Dto;
using Modules.Media.Controllers.ItsMedia.EventType.Dto;
using Modules.Media.Controllers.ItsMedia.Incident.Dto;
using Modules.Media.Controllers.ItsMedia.Vms.Dto;
using Modules.Media.Core.Dto.EquipmentType;
using Modules.Media.Core.Dto.EventType;
using Modules.Media.Core.Dto.Incident;
using Modules.Media.Core.Dto.Vms;
using Modules.Media.Infrastructure.Filters;

namespace Modules.Media.Controllers.ItsMedia
{
    /// <summary>
    /// Description: ITS-Media — nguồn ảnh của site ITS. CHỈ site WP (MediaPublic) gọi vào,
    /// xác thực bằng header X-Media-Key (media_api_key). Chỉ bật khi Media:Role = Provider | Both.
    /// Trả JSON thô (NonUnify) để WP đọc trực tiếp.
    /// Created date: 08/10/2026
    /// </summary>
    [AllowAnonymous]
    [MediaRole(MediaConst.Role.Provider)]
    [MediaApiKey]
    public class ItsMediaController : BaseController
    {
        /// <summary>Description: Ảnh camera của sự cố theo mốc trạng thái. Created date: 08/10/2026</summary>
        [NonUnify]
        [HttpGet("GetIncident")]
        [DisplayName("ITS-Media: incident images")]
        public async Task<List<ItsMediaIncidentOutput>> GetIncident([FromQuery] ItsMediaIncidentInput input)
        {
            return await MessBus.InvokeAsync<List<ItsMediaIncidentOutput>>(input);
        }

        /// <summary>Description: Icon loại sự kiện. Created date: 08/10/2026</summary>
        [NonUnify]
        [HttpGet("GetEventType")]
        [DisplayName("ITS-Media: event type icon")]
        public async Task<ItsMediaEventTypeOutput> GetEventType([FromQuery] ItsMediaEventTypeInput input)
        {
            return await MessBus.InvokeAsync<ItsMediaEventTypeOutput>(input);
        }

        /// <summary>Description: Icon loại thiết bị. Created date: 08/10/2026</summary>
        [NonUnify]
        [HttpGet("GetEquipmentType")]
        [DisplayName("ITS-Media: equipment type icon")]
        public async Task<ItsMediaEquipmentTypeOutput> GetEquipmentType([FromQuery] ItsMediaEquipmentTypeInput input)
        {
            return await MessBus.InvokeAsync<ItsMediaEquipmentTypeOutput>(input);
        }

        /// <summary>Description: Ảnh VMS. Created date: 08/10/2026</summary>
        [NonUnify]
        [HttpGet("GetVms")]
        [DisplayName("ITS-Media: VMS image")]
        public async Task<ItsMediaVmsOutput> GetVms([FromQuery] ItsMediaVmsInput input)
        {
            return await MessBus.InvokeAsync<ItsMediaVmsOutput>(input);
        }
    }
}
