using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Bảng phân quyền Tầng 3 — khu vực màn hình được phép thao tác. Mỗi quyền có Mã (Code) và Tên riêng.
    ///
    /// Một quyền gán cho NHIỀU người dùng và / hoặc NHIỀU đơn vị qua bảng con VwWallPermissionMember
    /// (cập nhật 22/10/2026 — trước đây mỗi bản ghi chỉ thuộc đúng 1 User hoặc 1 Org).
    /// Người dùng thuộc nhiều quyền ⇒ vùng hiệu lực là hợp các ô; quyền gán trực tiếp cho người dùng
    /// được ưu tiên hơn quyền gán theo đơn vị (xem VwPermissionService).
    ///
    /// Config là mảng JSON các ô lưới rời rạc được cấp, dạng List&lt;VwGridCell&gt;
    /// (Col 0..7, Row 0..3, khớp sơ đồ thietkevideowall.jpg) — theo đúng sketch
    /// DocBusinessThienAn/VideoWall-0911.txt, KHÔNG dùng bounding-box (ColStart/ColEnd/RowStart/RowEnd)
    /// để không bị giới hạn "phải gọn trong 1 hình chữ nhật".
    ///
    /// Thiết kế: không phụ thuộc WallNo / KB-00. Kiểm tra quyền chạy TRƯỚC ToCenterUniformRect,
    /// ngang hàng với VwSceneRegionService.EnsureWindowInsideSceneRegionAsync.
    /// Created date: 10/09/2026
    /// </summary>
    [SugarTable("VwWallPermission")]
    public class VwWallPermission : EntityTenant
    {
        /// <summary>
        /// Description: Tên quyền (VD: "Vùng giám sát Đội A"). Mã quyền dùng cột Code của EntityTenant.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>
        /// Description: Mảng JSON các ô lưới được cấp: List&lt;VwGridCell&gt; (Col 0..7, Row 0..3).
        /// Created date: 10/09/2026
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString)]
        public string? Config { get; set; }

        /// <summary>
        /// Description: Toàn quyền cả tường (bỏ qua Config). true ⇒ chỉ bật cờ, Config = null (không lưu danh sách ô);
        ///              chọn đủ mọi ô ⇒ BE tự bật cờ này.
        /// Created date: 27/09/2026 — cập nhật 27/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public bool? IsFullAccess { get; set; }

        /// <summary>
        /// Description: Mô tả ngắn về vùng phân quyền.
        /// Created date: 10/09/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        /// <summary>
        /// Description: Bố cục tường mà quyền được cấp trên (VwWallTopology.ID). Gán khi tạo, không đổi — giống VwScene.
        ///              Toạ độ ô (Config) chỉ có nghĩa trong bố cục này; chỉ quyền thuộc bố cục đang dùng (Status = Dùng)
        ///              mới có hiệu lực. Đổi cấu trúc tường ⇒ phiên bản mới, admin cấp lại quyền (hoặc Nhân bản quyền cũ).
        /// Created date: 23/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WallTopologyId { get; set; }

        /// <summary>
        /// Description: Dùng / Không dùng. Không dùng ⇒ quyền không có hiệu lực dù thuộc bố cục đang dùng.
        ///              Độc lập với WallTopologyId (bố cục chỉ quyết định toạ độ ô còn đúng hay không).
        ///              null (dữ liệu cũ) coi như Dùng.
        /// Created date: 23/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "0: không dùng; 1: dùng")]
        public BaseEnums.StatusEnum? Status { get; set; }
    }
}
