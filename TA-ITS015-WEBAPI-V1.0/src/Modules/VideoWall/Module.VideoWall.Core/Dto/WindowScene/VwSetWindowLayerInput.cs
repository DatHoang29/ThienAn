namespace Module.VideoWall.Core.Dto.WindowScene
{
    /// <summary>
    /// Author: Đạt
    /// Description: Hành động điều chỉnh lớp hiển thị Z-Order (Top / Bottom) của cửa sổ trên Video Wall.
    /// Created date: 17/08/2026
    /// </summary>
    public enum VwWindowLayerAction
    {
        /// <summary>Đưa cửa sổ lên trên cùng (Top Layer - PUT .../windows/{id}/top)</summary>
        Top = 1,

        /// <summary>Đưa cửa sổ xuống dưới cùng (Bottom Layer - PUT .../windows/{id}/bottom)</summary>
        Bottom = 2
    }

    /// <summary>
    /// Author: Đạt
    /// Description: DTO điều chỉnh thứ tự xếp lớp Z-Order cho cửa sổ hiển thị trên Video Wall.
    /// Created date: 17/08/2026
    /// </summary>
    public class VwSetWindowLayerInput
    {
        /// <summary>
        /// ID của bản ghi VwWindowScene trong CSDL
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// Hành động sắp xếp: 1 = Top (lên trên cùng), 2 = Bottom (xuống dưới cùng)
        /// </summary>
        public VwWindowLayerAction Action { get; set; } = VwWindowLayerAction.Top;
    }
}
