using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.WallPermission
{
    /// <summary>
    /// Description: DTO trả về 1 VwWallPermission (phân trang)
    /// Created date: 11/09/2026
    /// </summary>
    public class VwPageWallPermissionOutput : VwWallPermissionOutput { }

    /// <summary>
    /// Description: DTO trả về 1 VwWallPermission (danh sách / chi tiết) kèm các đối tượng được gán
    /// Created date: 11/09/2026
    /// </summary>
    public class VwWallPermissionOutput : VwWallPermission
    {
        /// <summary>Danh sách người dùng được gán quyền.</summary>
        public List<string> UserIds { get; set; } = [];

        /// <summary>Danh sách đơn vị được gán quyền.</summary>
        public List<string> OrgIds { get; set; } = [];
    }

    /// <summary>
    /// Description: DTO trả về sau khi thêm mới 1 VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwAddWallPermissionOutput : VwWallPermission { }

    /// <summary>
    /// Description: DTO trả về sau khi cập nhật 1 VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwUpdateWallPermissionOutput : VwWallPermission { }

    /// <summary>
    /// Description: DTO trả về sau khi xóa 1 VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwDeleteWallPermissionOutput : VwWallPermission { }

    /// <summary>
    /// Description: DTO trả về thông tin vùng lưới hiệu lực của người dùng đang đăng nhập (phục vụ FE vẽ ô lưới)
    /// Created date: 13/09/2026
    /// </summary>
    public class VwMyWallPermissionOutput
    {
        /// <summary>Kiểu quyền: full / area / none (xem VwWallAccessType).</summary>
        public string AccessType { get; set; } = string.Empty;

        /// <summary>true khi AccessType = full (giữ lại cho FE cũ).</summary>
        public bool IsFullAccess { get; set; }

        /// <summary>Tài khoản là SuperAdmin (FE dùng để hiện các thao tác cấp cả tường, VD Đặt mặc định).</summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>Các ô được cấp khi AccessType = area; null khi full; rỗng khi none.</summary>
        public List<VwGridCell>? AllowedCells { get; set; }

        /// <summary>Nguồn quyền: SuperAdmin / User / Org (null khi none).</summary>
        public string? Source { get; set; }
    }
}
