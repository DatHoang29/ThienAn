using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.Screen
{
    /// <summary>
    /// Description: Thông tin cổng ra gán cho màn, ghép vào màn hình khi trả về (chỉ đọc). Vị trí là của chính màn (VwScreen).
    /// Created date: 06/11/2026 — cập nhật 07/11/2026
    /// </summary>
    public class VwScreenWithOutput : VwScreen
    {
        /// <summary>Bộ điều khiển của cổng đang gán.</summary>
        public string? ControllerId { get; set; }
        public string? OutputName { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwScreen (phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageScreenOutput : VwScreenWithOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwScreen (danh sách / chi tiết)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwScreenOutput : VwScreenWithOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddScreenOutput : VwScreen { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateScreenOutput : VwScreen { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteScreenOutput : VwScreen { }
}
