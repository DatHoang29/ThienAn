using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.Scene
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwScene (phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageSceneOutput : VwScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwScene (danh sách / chi tiết)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSceneOutput : VwScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddSceneOutput : VwScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateSceneOutput : VwScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteSceneOutput : VwScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi kích hoạt kịch bản.
    /// Created date: 2026-08-03
    /// </summary>
    public class VwActiveSceneOutput : VwScene
    {
        /// <summary>ID luật sự kiện đã dẫn tới kịch bản này (null nếu kích hoạt trực tiếp).</summary>
        public string? RuleId { get; set; }

        /// <summary>Loại sự kiện đã kích hoạt (null nếu kích hoạt trực tiếp).</summary>
        public string? EventTypeId { get; set; }

        /// <summary>ID bản ghi log thực thi trong VwEventTriggerLog.</summary>
        public string? TriggerLogId { get; set; }

        /// <summary>Thời điểm kích hoạt.</summary>
        public DateTime? TriggeredAt { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về kịch bản đang được kích hoạt.
    /// Created date: 2026-08-03
    /// </summary>
    public class VwGetActiveSceneOutput : VwScene { }

    /// <summary>
    /// Author: HieuNV
    /// Description: Kịch bản đang chiếu trên một vùng màn hình (một bộ điều khiển).
    ///              Trả kèm tên bộ điều khiển và tên kịch bản để FE dựng thanh trạng thái mà
    ///              không phải gọi thêm API.
    /// Created date: 2026-08-13
    /// </summary>
    public class VwActiveRegionOutput
    {
        public string ControllerId { get; set; } = null!;
        public string? ControllerCode { get; set; }
        public string? ControllerName { get; set; }

        /// <summary>Rỗng nghĩa là vùng này chưa được đẩy kịch bản nào.</summary>
        public string? SceneId { get; set; }
        public string? SceneCode { get; set; }
        public string? SceneName { get; set; }

        public DateTime? ActivatedAt { get; set; }
    }
}
