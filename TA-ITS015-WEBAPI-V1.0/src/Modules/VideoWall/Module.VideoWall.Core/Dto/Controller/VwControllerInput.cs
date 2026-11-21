using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.Controller
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageControllerInput : BasePageInput
    {
        public string? Name { get; set; }
        public string? Model { get; set; }
        public string? Chassis { get; set; }
        public string? RefId { get; set; }

        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách VwController (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwControllerInput
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? Model { get; set; }
        public string? Chassis { get; set; }
        public string? RefId { get; set; }

        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 VwController theo ID
    /// Created date: 2026-08-01
    /// </summary>
    public class VwIdControllerInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddControllerInput : VwController { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateControllerInput : VwAddControllerInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteControllerInput : BaseIdInput { }

    /// <summary>
    /// Author: Đạt
    /// Description: DTO truy vấn danh sách kênh tín hiệu đầu vào từ thiết bị controller vật lý.
    /// Created date: 17/08/2026
    /// </summary>
    public class VwGetInputChannelsControllerInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lệnh module TMS gửi qua IMessageBus sau khi cập nhật thiết bị: chép lại IP:Port / tài khoản / mật khẩu
    ///              sang các VwController liên kết (RefId = EquipmentId). Worker đọc thông tin kết nối từ VwController.
    /// Created date: 23/10/2026
    /// </summary>
    public class VwSyncControllerFromEquipmentInput
    {
        public string? EquipmentId { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lệnh module TMS gửi qua IMessageBus trước khi xoá thiết bị: báo lỗi nếu thiết bị đang gắn với VwController.
    /// Created date: 23/10/2026
    /// </summary>
    public class VwEnsureEquipmentNotUsedInput
    {
        public List<string> EquipmentIds { get; set; } = new();
    }
}
