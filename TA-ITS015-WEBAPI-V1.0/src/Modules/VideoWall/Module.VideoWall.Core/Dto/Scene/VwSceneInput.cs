using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.Scene
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageSceneInput : BasePageInput
    {
        public string? Name { get; set; }
        public string? OutputId { get; set; }

        public BaseEnums.DefaultEnum? IsDefault { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>Lọc theo bố cục tường (VwWallTopology.ID); bỏ trống là mọi bố cục.</summary>
        public string? WallTopologyId { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách VwScene (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSceneInput
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? OutputId { get; set; }

        public BaseEnums.DefaultEnum? IsDefault { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>Lọc theo bố cục tường (VwWallTopology.ID); bỏ trống là mọi bố cục.</summary>
        public string? WallTopologyId { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 VwScene theo ID
    /// Created date: 2026-08-01
    /// </summary>
    public class VwIdSceneInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddSceneInput : VwScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateSceneInput : VwAddSceneInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteSceneInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO đặt 1 VwScene làm kịch bản mặc định (chỉ SuperAdmin)
    /// Created date: 28/09/2026
    /// </summary>
    public class VwSetDefaultSceneInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: Active 1 scene theo eventType hoặc SceneId
    /// Created date: 2026-08-01
    /// </summary>
    public class VwActiveSceneInput {
        public string? Code { get; set; }
        public string? EventTypeId { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy kịch bản đang được kích hoạt.
    ///              FE gọi khi tải lại trang hoặc khi client mới kết nối để dựng lại giao diện.
    /// Created date: 2026-08-03
    /// </summary>
    public class VwGetActiveSceneInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy kịch bản đang chiếu của TỪNG vùng (mỗi bộ điều khiển một dòng).
    ///              Đây là nguồn sự thật về trạng thái tường khi các vùng chạy kịch bản khác nhau;
    ///              <see cref="VwGetActiveSceneInput"/> chỉ trả một kịch bản nên không diễn tả được.
    /// Created date: 2026-08-13
    /// </summary>
    public class VwGetActiveRegionInput
    {
        /// <summary>Lọc theo một bộ điều khiển; bỏ trống là lấy tất cả.</summary>
        public string? ControllerId { get; set; }
    }
}
