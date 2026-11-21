namespace Modules.TMS.Report.Controllers.EventData.Dto
{
    /// <summary>
    /// Author: Dev Team
    /// Description: Ảnh sự kiện VDS được trả về dưới dạng Base64.
    /// Created date: 08/08/2026
    /// </summary>
    public sealed class EventDataImageOutput
    {
        /// <summary>Dữ liệu Base64 thuần, không bao gồm tiền tố data URI.</summary>
        public string Base64 { get; set; } = string.Empty;

        /// <summary>Loại ảnh: full là ảnh toàn cảnh, plate là ảnh biển số.</summary>
        public string PictureType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
