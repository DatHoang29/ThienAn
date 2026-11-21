using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.WindowScene
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwWindowScene (phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageWindowSceneOutput : VwWindowScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwWindowScene (danh sách / chi tiết)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwWindowSceneOutput : VwWindowScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddWindowSceneOutput : VwWindowScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateWindowSceneOutput : VwWindowScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteWindowSceneOutput : VwWindowScene { }
}
