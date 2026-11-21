namespace Module.VideoWall.Core.Dto.WindowScene
{
    /// <summary>
    /// Author: Đạt
    /// Description: DTO đổi nguồn tín hiệu trực tiếp cho cửa sổ trên Video Wall (thao tác kéo thả nguồn vào cửa sổ).
    /// Created date: 17/08/2026
    /// </summary>
    public class VwSwitchWindowSourceInput
    {
        /// <summary>
        /// ID của bản ghi VwWindowScene trong CSDL
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// ID nguồn tín hiệu VwSource mới cần gán vào cửa sổ
        /// </summary>
        public string SourceId { get; set; } = string.Empty;
    }
}
