using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.Screen
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageScreenInput : BasePageInput
    {
        public string? Name { get; set; }
        public string? ControllerOutputId { get; set; }
        public string? SerialNumber { get; set; }
        /// <summary>true: đang cắm vào cổng; false: trong kho / đã tháo; null: tất cả.</summary>
        public bool? Installed { get; set; }
        public string? PanelType { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách VwScreen (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwScreenInput
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? ControllerOutputId { get; set; }
        public string? SerialNumber { get; set; }
        /// <summary>true: đang cắm vào cổng; false: trong kho / đã tháo; null: tất cả.</summary>
        public bool? Installed { get; set; }
        public string? PanelType { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 VwScreen theo ID
    /// Created date: 2026-08-01
    /// </summary>
    public class VwIdScreenInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddScreenInput : VwScreen { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateScreenInput : VwAddScreenInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteScreenInput : BaseIdInput { }

    /// <summary>
    /// Description: Lưu sơ đồ màn trong 1 lần (kéo thả trên trang rồi bấm Lưu). Mỗi phần tử là trạng thái cuối của 1 màn đã đổi:
    ///              vị trí (WallBlockId trống ⇒ về kho) + cổng gán cho màn (ControllerOutputId trống ⇒ không có cổng).
    ///              FE đã kiểm ô / cổng thuộc bộ phụ trách và không trùng; BE ghi trong 1 transaction rồi chép vị trí màn sang cổng.
    /// Created date: 07/11/2026
    /// </summary>
    public class VwSaveWallMapInput
    {
        public List<VwWallMapScreenItem> Items { get; set; } = new();
    }

    public class VwWallMapScreenItem : BaseIdInput
    {
        /// <summary>false ⇒ giữ vị trí đang lưu (trang Bố cục gán cổng chỉ gửi cổng).</summary>
        public bool SetPlacement { get; set; } = true;
        public string? WallBlockId { get; set; }
        public int? GridCol { get; set; }
        public int? GridRow { get; set; }

        /// <summary>false ⇒ giữ cổng đang lưu (trang Bố cục màn chỉ gửi vị trí; màn về kho vẫn tự bỏ cổng).</summary>
        public bool SetOutput { get; set; } = true;
        public string? ControllerOutputId { get; set; }
    }
}
