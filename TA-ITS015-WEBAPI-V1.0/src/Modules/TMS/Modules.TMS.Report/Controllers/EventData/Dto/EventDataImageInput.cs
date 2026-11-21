using System.ComponentModel.DataAnnotations;

namespace Modules.TMS.Report.Controllers.EventData.Dto
{
    /// <summary>
    /// Author: Dev Team
    /// Description: Thông tin dùng để đọc ảnh của dữ liệu sự kiện VDS từ ổ đĩa.
    /// Created date: 08/08/2026
    /// </summary>
    public sealed class EventDataImageInput
    {
        /// <summary>ID của dữ liệu sự kiện.</summary>
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$")]
        public string ID { get; set; } = string.Empty;

        /// <summary>Thời gian phát hiện sự kiện (UTC+0 từ FE).</summary>
        [Required]
        public DateTime? DetectTime { get; set; }

        /// <summary>Loại ảnh: f là ảnh toàn cảnh, p là ảnh biển số.</summary>
        [Required]
        [RegularExpression("^[fFpP]$")]
        public string PictureType { get; set; } = string.Empty;

        /// <summary>Chất lượng JPEG từ 1 đến 100. Mặc định là 80.</summary>
        [Range(1, 100)]
        public int Quality { get; set; } = 80;
    }
}
