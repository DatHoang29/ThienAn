using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.WallTopology
{
    /// <summary>
    /// Description: DTO trả về 1 khối của tường
    /// Created date: 22/10/2026
    /// </summary>
    public class VwWallBlockOutput : VwWallBlock
    {
        /// <summary>
        /// Số màn của khối = Cols × Rows.
        /// </summary>
        public int ScreenCount { get; set; }

        /// <summary>
        /// Cột bắt đầu của khối khi ghép các khối thành một dải ngang = tổng Cols các khối đứng trước.
        /// </summary>
        public int OffsetCol { get; set; }
    }

    /// <summary>
    /// Description: DTO trả về cấu hình tường đang dùng
    /// Created date: 09/09/2026
    /// </summary>
    public class VwWallTopologyOutput : VwWallTopology
    {
        /// <summary>
        /// Các khối của tường, sắp theo BlockNo (trái → phải).
        /// </summary>
        public List<VwWallBlockOutput> Blocks { get; set; } = [];

        /// <summary>
        /// Tổng số ô màn của tường = tổng ScreenCount các khối.
        /// </summary>
        public int TotalScreens { get; set; }

        /// <summary>
        /// Chiều rộng canvas khi ghép các khối thành một dải ngang (pixel) = tổng Cols × ScreenWidth.
        /// </summary>
        public int CanvasWidth { get; set; }

        /// <summary>
        /// Chiều cao canvas (pixel) = Rows lớn nhất của các khối × ScreenHeight.
        /// </summary>
        public int CanvasHeight { get; set; }

        /// <summary>
        /// true = chưa có bản ghi trong CSDL, đây là giá trị mặc định suy từ VwWallProfile.
        /// Client nên nhắc người dùng lưu cấu hình tường trước khi dựng kịch bản.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Số kịch bản (chưa xoá) dựng trên bố cục này. Chỉ điền ở API danh sách / lấy theo ID.
        /// </summary>
        public int SceneCount { get; set; }

        /// <summary>
        /// Số phân quyền vùng (chưa xoá) cấp trên bố cục này. Điền cùng chỗ với SceneCount.
        /// </summary>
        public int PermissionCount { get; set; }
    }
}
