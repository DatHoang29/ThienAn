using System.ComponentModel.DataAnnotations;

namespace Modules.TMS.Report.Controllers.TrafficAnalysis.Dto
{
    /// <summary>
    /// Author: Dev Team
    /// Description: Thông tin dùng để đọc ảnh dữ liệu phân tích giao thông VDS.
    /// Created date: 08/08/2026
    /// </summary>
    public sealed class TrafficAnalysisImageInput
    {
        /// <summary>ID của dữ liệu phân tích giao thông.</summary>
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$")]
        public string ID { get; set; } = string.Empty;

        /// <summary>Thời gian phát hiện dữ liệu (UTC+0 từ FE).</summary>
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
