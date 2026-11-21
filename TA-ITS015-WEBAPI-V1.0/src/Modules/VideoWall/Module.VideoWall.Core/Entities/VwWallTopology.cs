using Module.VideoWall.Core.Constants;
using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Wall Topology
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("VwWallTopology")]
    public class VwWallTopology : EntityTenant
    {
        /// <summary>
        /// Description: Tên cấu hình sơ đồ ma trận tường ghép.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>
        /// Description: Tỉ lệ ô màn hình (rộng:cao) — chỉ quyết định hình dạng ô khi hiển thị, VD "16:9", "3:4".
        ///              Giá trị hợp lệ: VwWallProfile.CellRatios. Lưới cột × hàng nằm ở VwWallBlock.
        /// Created date: 27/10/2026 — thay cột ScreenWidth / ScreenHeight
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? CellRatio { get; set; }

        /// <summary>
        /// Description: Số ô của bố cục (tổng cột × hàng của mọi khối). Có giá trị ⇒ lưới khối phải đúng bằng số ô này khi sửa lưới;
        ///              null = chưa giới hạn (bố cục cũ).
        /// Created date: 28/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? CellCount { get; set; }

        /// <summary>
        /// Description: Chiều rộng một panel (pixel logic) — đơn vị toạ độ cửa sổ kịch bản, cố định theo profile
        ///              cho mọi tỉ lệ ô (thiết bị quy đổi theo từng panel nên không phụ thuộc hình dạng ô). Không lưu CSDL.
        /// Created date: 14/08/2026 — cập nhật 27/10/2026
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public int? ScreenWidth => VwWallProfile.PanelWidthPx;

        /// <summary>
        /// Description: Chiều cao một panel (pixel logic) — xem ScreenWidth. Không lưu CSDL.
        /// Created date: 14/08/2026 — cập nhật 27/10/2026
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public int? ScreenHeight => VwWallProfile.PanelHeightPx;

        /// <summary>
        /// Description: Model panel của màn hình mẫu — điền sẵn khi tạo màn từ ô trống.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? DefaultPanelModel { get; set; }

        /// <summary>
        /// Description: Loại panel của màn hình mẫu (LCD, LED...).
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? DefaultPanelType { get; set; }

        /// <summary>
        /// Description: Kích thước panel (inch) của màn hình mẫu.
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? DefaultPanelSize { get; set; }

        /// <summary>
        /// Description: Độ phân giải của màn hình mẫu, VD "3840x2160@30Hz".
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? DefaultResolution { get; set; }

        /// <summary>
        /// Description: Ghi chú Remark bổ sung cho sơ đồ topology.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        /// <summary>
        /// Description: Trạng thái bản ghi bố cục: Dùng / Không dùng (bật / tắt như các danh mục khác).
        ///              Bố cục đang chạy trên tường xem IsApply; bố cục đang áp dụng luôn ở trạng thái Dùng.
        /// Created date: 26/10/2026 — cập nhật 28/10/2026 (tách "đang áp dụng" sang IsApply)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "0: không dùng; 1: dùng")]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>
        /// Description: Bố cục đang áp dụng lên tường (mỗi tenant đúng 1 bản ghi IsApply = 1).
        ///              Đổi bằng API SetCurrent (nút "Áp dụng") — kịch bản / lập lịch / liên kết sự kiện / phân quyền
        ///              của các bố cục còn lại chuyển Không dùng.
        /// Created date: 28/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "0: không áp dụng; 1: áp dụng (bố cục đang chạy trên tường)")]
        public BaseEnums.YesNoEnum? IsApply { get; set; }
    }
}