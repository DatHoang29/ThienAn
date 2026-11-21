using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.Source
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwSource (phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageSourceOutput : VwSource { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwSource (danh sách / chi tiết)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSourceOutput : VwSource { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddSourceOutput : VwSource { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateSourceOutput : VwSource { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteSourceOutput : VwSource { }
}
