using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;

namespace Module.VideoWall.Core.Dto.WallTopology
{
    /// <summary>
    /// Description: DTO lấy cấu hình tường đang dùng. Không có tham số: toàn hệ thống chỉ có
    ///              MỘT tường, nên không cần khoá để tra.
    /// Created date: 09/09/2026
    /// </summary>
    public class VwCurrentWallTopologyInput { }

    /// <summary>
    /// Description: DTO lấy danh sách bố cục tường (đang dùng trước, rồi theo mã) kèm khối và số kịch bản / phân quyền.
    /// Created date: 22/10/2026 — cập nhật 26/10/2026 (quản lý nhiều bố cục)
    /// </summary>
    public class VwListWallTopologyInput
    {
        /// <summary>Lọc theo mã / tên (chứa).</summary>
        public string? Keyword { get; set; }
    }

    /// <summary>
    /// Description: DTO lấy 1 bố cục tường theo ID (VD: vẽ kịch bản thuộc bố cục khác bố cục đang dùng).
    /// Created date: 22/10/2026
    /// </summary>
    public class VwIdWallTopologyInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO thêm bố cục tường: mã, tên, kích thước ô và DANH SÁCH ĐẦY ĐỦ khối (BlockNo liên tục 0..n-1).
    ///              Bố cục thêm mới không phải bố cục đang dùng (trừ khi hệ thống chưa có bố cục nào).
    /// Created date: 26/10/2026
    /// </summary>
    public class VwAddWallTopologyInput : VwWallTopology
    {
        public List<VwWallBlockInput> Blocks { get; set; } = [];
    }

    /// <summary>
    /// Description: DTO sửa bố cục tường (theo ID). Bố cục đang dùng: thu nhỏ đè lên dữ liệu bị chặn; bố cục khác sửa tự do.
    /// Created date: 26/10/2026
    /// </summary>
    public class VwUpdateWallTopologyInput : VwAddWallTopologyInput { }

    /// <summary>
    /// Description: DTO xoá bố cục tường (không xoá được bố cục đang dùng).
    /// Created date: 26/10/2026
    /// </summary>
    public class VwDeleteWallTopologyInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO đặt 1 bố cục làm bố cục đang dùng.
    /// Created date: 22/10/2026 — cập nhật 26/10/2026
    /// </summary>
    public class VwSetCurrentWallTopologyInput : BaseIdInput { }

    /// <summary>
    /// Description: Một khối trong lệnh lưu bố cục tường. BlockNo phải liên tục 0..n-1.
    /// Created date: 22/10/2026
    /// </summary>
    public class VwWallBlockInput : VwWallBlock
    {
        /// <summary>
        /// Description: Số thứ tự CŨ của khối khi đổi chỗ / xoá khối ở trang Bố cục tường (khối mới ⇒ null).
        ///              Có ít nhất 1 khối gửi trường này ⇒ BE đánh số lại khối của màn hình, cửa sổ kịch bản, vùng phân quyền
        ///              theo vị trí mới; khối cũ không còn trong danh sách ⇒ bị xoá (chỉ được khi không còn dữ liệu trên khối).
        ///              Không khối nào gửi ⇒ khớp khối theo BlockNo như trước.
        /// Created date: 28/10/2026
        /// </summary>
        public int? SourceBlockNo { get; set; }
    }

    /// <summary>
    /// Description: Lưu bố cục tường đang dùng trong MỘT transaction: thông tin chung + màn hình mẫu của tường
    ///              và toàn bộ khối. Blocks là danh sách ĐẦY ĐỦ — khối không có trong danh sách sẽ bị xoá.
    ///              Cổng ra tự đặt lại theo khối mới (không còn gửi màn hình kèm theo).
    /// Created date: 22/10/2026 — cập nhật 06/11/2026
    /// </summary>
    public class VwSaveWallLayoutInput : VwWallTopology
    {
        public List<VwWallBlockInput> Blocks { get; set; } = [];
    }
}
