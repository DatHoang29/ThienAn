using Modules.Media.Controllers.MediaPublic.Vms.Dto;
using Modules.Media.Controllers.MediaPublic.EquipmentType.Dto;
using Modules.Media.Controllers.MediaPublic.EventType.Dto;
using Modules.Media.Controllers.MediaPublic.Incident.Dto;
using Microsoft.AspNetCore.Authorization;
using Modules.Media.Core.Dto.EquipmentType;
using Modules.Media.Core.Dto.EventType;
using Modules.Media.Core.Dto.Incident;
using Modules.Media.Core.Dto.Vms;
using Modules.Media.Infrastructure.Filters;

namespace Modules.Media.Controllers.MediaPublic
{
    /// <summary>
    /// Description: WP-MediaPublic — cổng ảnh cho FE Web Portal. Chỉ bật khi Media:Role = Public | Both.
    ///  - Get*     : trả JSON (Base64) — dùng cho panel (ảnh sự cố...).
    ///  - Content* : trả thẳng file ảnh — dùng cho thẻ &lt;img src&gt; (icon, VMS), cho phép ẩn danh.
    /// Created date: 08/10/2026
    /// </summary>
    [MediaRole(MediaConst.Role.Public)]
    public class MediaPublicController : BaseController
    {
        /// <summary>Thời gian trình duyệt được giữ ảnh (FE đổi tham số v khi dữ liệu đổi).</summary>
        private const int BrowserCacheSeconds = 86400;

        // ============================ Incident ============================

        /// <summary>Description: Ảnh camera của sự cố (chỉ sự cố đủ điều kiện). Created date: 08/10/2026</summary>
        [HttpGet("GetIncident")]
        [DisplayName("MediaPublic: incident images")]
        public async Task<List<MediaPublicIncidentOutput>> GetIncident([FromQuery] MediaPublicIncidentInput input)
        {
            return await MessBus.InvokeAsync<List<MediaPublicIncidentOutput>>(input);
        }

        // ============================ EventType ============================

        /// <summary>Description: Icon loại sự kiện dạng JSON. Created date: 08/10/2026</summary>
        [HttpGet("GetEventType")]
        [DisplayName("MediaPublic: event type icon")]
        public async Task<MediaPublicEventTypeOutput> GetEventType([FromQuery] MediaPublicEventTypeInput input)
        {
            return await MessBus.InvokeAsync<MediaPublicEventTypeOutput>(input);
        }

        /// <summary>Description: Icon loại sự kiện dạng file (dùng cho img src). Created date: 08/10/2026</summary>
        [AllowAnonymous]
        [NonUnify]
        [HttpGet("ContentEventType")]
        [DisplayName("MediaPublic: event type icon content")]
        public async Task<IActionResult> ContentEventType([FromQuery] MediaPublicEventTypeInput input)
        {
            return ToFile(await MessBus.InvokeAsync<MediaPublicEventTypeOutput>(input));
        }

        // ============================ EquipmentType ============================

        /// <summary>Description: Icon loại thiết bị dạng JSON. Created date: 08/10/2026</summary>
        [HttpGet("GetEquipmentType")]
        [DisplayName("MediaPublic: equipment type icon")]
        public async Task<MediaPublicEquipmentTypeOutput> GetEquipmentType([FromQuery] MediaPublicEquipmentTypeInput input)
        {
            return await MessBus.InvokeAsync<MediaPublicEquipmentTypeOutput>(input);
        }

        /// <summary>Description: Icon loại thiết bị dạng file (dùng cho img src). Created date: 08/10/2026</summary>
        [AllowAnonymous]
        [NonUnify]
        [HttpGet("ContentEquipmentType")]
        [DisplayName("MediaPublic: equipment type icon content")]
        public async Task<IActionResult> ContentEquipmentType([FromQuery] MediaPublicEquipmentTypeInput input)
        {
            return ToFile(await MessBus.InvokeAsync<MediaPublicEquipmentTypeOutput>(input));
        }

        // ============================ Vms ============================

        /// <summary>Description: Ảnh VMS dạng JSON. Created date: 08/10/2026</summary>
        [HttpGet("GetVms")]
        [DisplayName("MediaPublic: VMS image")]
        public async Task<MediaPublicVmsOutput> GetVms([FromQuery] MediaPublicVmsInput input)
        {
            return await MessBus.InvokeAsync<MediaPublicVmsOutput>(input);
        }

        /// <summary>Description: Ảnh VMS dạng file (dùng cho img src). Created date: 08/10/2026</summary>
        [AllowAnonymous]
        [NonUnify]
        [HttpGet("ContentVms")]
        [DisplayName("MediaPublic: VMS image content")]
        public async Task<IActionResult> ContentVms([FromQuery] MediaPublicVmsInput input)
        {
            return ToFile(await MessBus.InvokeAsync<MediaPublicVmsOutput>(input));
        }

        // ============================ helper ============================

        /// <summary>Ready -> file; External -> redirect; còn lại -> 404.</summary>
        private static IActionResult ToFile(MediaBaseOutput output)
        {
            if (output.Status == MediaConst.Status.External && !string.IsNullOrWhiteSpace(output.Url))
                return new RedirectResult(output.Url);

            if (!output.Exists || string.IsNullOrEmpty(output.Base64))
                return new NotFoundResult();

            byte[] bytes;
            try { bytes = Convert.FromBase64String(output.Base64); }
            catch (FormatException) { return new NotFoundResult(); }

            var response = App.HttpContext?.Response;
            if (response != null)
            {
                response.Headers["Cache-Control"] = $"public, max-age={BrowserCacheSeconds}";
                if (!string.IsNullOrEmpty(output.Version))
                    response.Headers["ETag"] = $"\"{output.Version}\"";
            }

            return new FileContentResult(bytes, output.ContentType);
        }
    }
}
