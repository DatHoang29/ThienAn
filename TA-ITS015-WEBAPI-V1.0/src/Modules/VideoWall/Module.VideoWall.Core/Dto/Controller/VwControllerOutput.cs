using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.Controller
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwController (phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageControllerOutput : VwController { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwController (danh sách / chi tiết)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwControllerOutput : VwController { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddControllerOutput : VwController { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateControllerOutput : VwController { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteControllerOutput : VwController { }

    /// <summary>
    /// Author: Đạt
    /// Description: DTO thông tin kênh tín hiệu đầu vào của VwController
    /// Created date: 13/09/2026
    /// </summary>
    public class VwGetInputChannelsControllerOutput
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? VideoInputChannelAccessStatus { get; set; }
    }
}
