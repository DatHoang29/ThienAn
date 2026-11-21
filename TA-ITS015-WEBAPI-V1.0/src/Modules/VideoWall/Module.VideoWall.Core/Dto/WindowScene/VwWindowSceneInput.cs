using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.WindowScene
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageWindowSceneInput : BasePageInput
    {
        public string? Name { get; set; }
        public string? SceneId { get; set; }
        public string? SourceId { get; set; }
        public BaseEnums.SceneWindowVisible? Visible { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách VwWindowScene (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwWindowSceneInput
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? SceneId { get; set; }
        public string? SourceId { get; set; }
        public BaseEnums.SceneWindowVisible? Visible { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 VwWindowScene theo ID
    /// Created date: 2026-08-01
    /// </summary>
    public class VwIdWindowSceneInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddWindowSceneInput : VwWindowScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateWindowSceneInput : VwAddWindowSceneInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteWindowSceneInput : BaseIdInput { }
}
