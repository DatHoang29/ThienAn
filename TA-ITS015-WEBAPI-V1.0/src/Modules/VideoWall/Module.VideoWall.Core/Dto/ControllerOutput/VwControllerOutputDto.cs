using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.ControllerOutput
{
    /// <summary>
    /// Description: Lọc danh sách cổng ra (không phân trang).
    /// Created date: 06/11/2026
    /// </summary>
    public class VwControllerOutputInput
    {
        public string? ControllerId { get; set; }

        /// <summary>true: chỉ cổng đang nằm trong lưới của bố cục đang áp dụng.</summary>
        public bool? OnlyPlaced { get; set; }
    }

    /// <summary>Lấy chi tiết 1 cổng ra.</summary>
    public class VwIdControllerOutputInput : BaseIdInput { }

    /// <summary>
    /// Description: Sửa các trường web quản lý của cổng ra. Trường thiết bị (tên cổng, ô, vị trí) chỉ đọc.
    /// Created date: 06/11/2026
    /// </summary>
    public class VwUpdateControllerOutputInput : BaseIdInput
    {
        public string? DisplayName { get; set; }
        public string? Remark { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Description: Khai báo cổng của bộ phân phối cấp tín hiệu cho bộ hiển thị nào (null = chưa nối).
    /// Created date: 07/11/2026
    /// </summary>
    public class VwSetFeedsControllerOutputInput : BaseIdInput
    {
        public string? FeedsControllerId { get; set; }
    }

    /// <summary>
    /// Description: Cổng ra kèm bộ điều khiển và màn đang cắm (chỉ đọc).
    /// Created date: 06/11/2026
    /// </summary>
    public class VwControllerOutputOutput : VwControllerOutput
    {
        public string? ControllerCode { get; set; }
        public string? ControllerName { get; set; }
        /// <summary>Vai trò bộ điều khiển của cổng (VwControllerRole).</summary>
        public string? ControllerRole { get; set; }
        /// <summary>true: cổng của bộ phân phối (không nằm trên lưới tường).</summary>
        public bool IsMaster { get; set; }

        public string? ScreenId { get; set; }
        public string? ScreenCode { get; set; }
        public string? ScreenName { get; set; }
        public string? SerialNumber { get; set; }
        public string? Temperature { get; set; }
        public string? RunTimeHours { get; set; }
        public string? ScreenState { get; set; }
        public DateTime? LastCheckedAt { get; set; }
    }
}
