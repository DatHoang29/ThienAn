using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.WallPermission
{
    /// <summary>
    /// Description: DTO phân trang danh sách VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwPageWallPermissionInput : BasePageInput
    {
        /// <summary>Tìm theo mã hoặc tên quyền (chứa chuỗi).</summary>
        public string? Keyword { get; set; }

        /// <summary>Quyền có gán trực tiếp cho người dùng này.</summary>
        public string? UserId { get; set; }

        /// <summary>Quyền có gán cho đơn vị này.</summary>
        public string? OrgId { get; set; }

        /// <summary>Quyền thuộc phiên bản bố cục này (VwWallTopology.ID); rỗng = mọi phiên bản.</summary>
        public string? WallTopologyId { get; set; }

        /// <summary>Dùng / Không dùng; rỗng = tất cả.</summary>
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Description: DTO tìm kiếm danh sách VwWallPermission (không phân trang)
    /// Created date: 11/09/2026
    /// </summary>
    public class VwWallPermissionInput
    {
        public string? ID { get; set; }
        public string? UserId { get; set; }
        public string? OrgId { get; set; }
        public string? WallTopologyId { get; set; }
    }

    /// <summary>
    /// Description: DTO lấy chi tiết 1 VwWallPermission theo ID
    /// Created date: 11/09/2026
    /// </summary>
    public class VwIdWallPermissionInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO thêm mới 1 VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwAddWallPermissionInput : VwWallPermission
    {
        /// <summary>Danh sách người dùng được gán quyền.</summary>
        public List<string>? UserIds { get; set; }

        /// <summary>Danh sách đơn vị được gán quyền.</summary>
        public List<string>? OrgIds { get; set; }
    }

    /// <summary>
    /// Description: DTO cập nhật 1 VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwUpdateWallPermissionInput : VwAddWallPermissionInput { }

    /// <summary>
    /// Description: DTO xóa 1 VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwDeleteWallPermissionInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO truy vấn vùng lưới hiệu lực của người dùng đang đăng nhập
    /// Created date: 13/09/2026
    /// </summary>
    public class VwGetMyWallPermissionInput { }
}
