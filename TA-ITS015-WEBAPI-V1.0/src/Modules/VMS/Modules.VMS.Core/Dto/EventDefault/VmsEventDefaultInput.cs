using Shared.Core.Utilities;

namespace Modules.VMS.Controllers.EventDefault.Dto
{
    /// <summary>
    /// Author: 
    /// Description: DTO thêm mới nội dung mặc định VMS theo sự kiện sự cố.
    ///              Kế thừa entity để nhận đủ field hiển thị; bổ sung cờ UseForEvent và FontSize (lưu vào DefaultConfig).
    /// Created date: 15/07/2026
    /// </summary>
    public class AddVmsEventDefaultInput : Core.Entities.VmsEventDefault
    {
        /// <summary>
        /// Cờ đánh dấu "Sử dụng nội dung này cho sự kiện" (chuẩn bị cho nút Đặt mặc định sau này)
        /// </summary>
        public bool UseForEvent { get; set; }

        /// <summary>
        /// Cỡ chữ (FE gửi lên, lưu trong DefaultConfig)
        /// </summary>
        public string? FontSize { get; set; }
    }

    /// <summary>
    /// Author: 
    /// Description: DTO cập nhật nội dung mặc định VMS theo sự kiện sự cố.
    /// Created date: 15/07/2026
    /// </summary>
    public class UpdateVmsEventDefaultInput : AddVmsEventDefaultInput
    {
    }

    /// <summary>
    /// Author: 
    /// Description: DTO xóa nội dung mặc định VMS theo sự kiện sự cố.
    /// Created date: 15/07/2026
    /// </summary>
    public class DeleteVmsEventDefaultInput : BaseIdInput
    {
    }

    /// <summary>
    /// Author: 
    /// Description: DTO phân trang danh sách nội dung mặc định VMS theo sự kiện sự cố.
    /// Created date: 15/07/2026
    /// </summary>
    public class PageVmsEventDefaultInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? EventTypeId { get; set; }
    }

    /// <summary>
    /// Author: 
    /// Description: DTO tìm kiếm (không phân trang) nội dung mặc định VMS theo sự kiện sự cố.
    /// Created date: 15/07/2026
    /// </summary>
    public class VmsEventDefaultListInput
    {
        public string? ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? EventTypeId { get; set; }
    }
}
