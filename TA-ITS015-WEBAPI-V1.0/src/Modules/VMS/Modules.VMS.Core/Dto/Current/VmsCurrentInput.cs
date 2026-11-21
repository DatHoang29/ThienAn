
using Shared.Core.Utilities;

namespace Modules.VMS.Controllers.Current.Dto
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto chứa thông tin phân trang truyền vào
    /// Created date: 18/10/2024
    /// </summary>
    public class PageVmsCurrentInput : BasePageInput
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? EquipmentId { get; set; }
    }

    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto chứa thông tin tìm kiếm truyền vào
    /// Created date: 18/10/2024
    /// </summary>
    public class VmsCurrentInput
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? EquipmentId { get; set; }
    }

    /// <summary>
    /// Author: KhoaND
    /// Decription: Đối tượng dto của VmsCurrent chứa thông tin cần lưu truyền vào
    /// Created date: 18/10/2024
    /// </summary>
    /// 
    public class ListEquipment
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string EquipmentId { get; set; }
    }

    public class AddVmsCurrentInput : Core.Entities.VmsCurrent
    {
        public string IncidentId { get; set; }
        public List<ListEquipment> ListEquipment { get; set; }
        public string OldUrl { get; set; }
        public string OldRowData { get; set; }
        public string ActionType { get; set; }
    }

    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto của VmsCurrent chứa thông tin cần cập nhật truyền vào
    /// Created date: 18/10/2024
    /// </summary>
    public class UpdateVmsCurrentInput : AddVmsCurrentInput
    {
    }

    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto chứa thông tin cần xóa khỏi csdl truyền vào
    /// Created date: 18/10/2024
    /// </summary>
    public class DeleteVmsCurrentInput : BaseIdInput
    {
    }



    /// <summary>
    /// Author: TuanHA
    /// Description: Đối tượng dto chứa thông tin cần xóa khỏi csdl truyền vào
    /// Created date: 18/10/2024
    /// </summary>
    public class UpdateVmsCurrentInputByProcess : Core.Entities.VmsCurrent
    {
    }

    /// <summary>
    /// Author: TuanHA
    /// Description: Đối tượng dto chứa thông tin cần xóa khỏi csdl truyền vào
    /// Created date: 18/10/2024
    /// </summary>
    public class VmsCurrentByEquipment
    {
        public string EquipmentId { get; set; }
    }

    /// <summary>
    /// Thêm hoặc cập nhật nội dung mặc định của 1 dòng VmsCurrent.
    /// Author : ChuongLP
    /// Date: 2027-07-14
    /// </summary>
    public class VmsDefaultCurrentInput
    {
        public string EquipmentId { get; set; }      // bắt buộc
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? DefaultRowData { get; set; }   // JSON { Row1, Row2 } cho text/message/phrase
        public string? DefaultUrl { get; set; }        // base64 (từ preview) hoặc path ảnh đã lưu
        public string? DefaultColor { get; set; }
        public string? DefaultBackColor { get; set; }
        public string? DefaultFont { get; set; }
        public string? Size { get; set; }
        public string? ActionType { get; set; }        // 'text' | 'image' ... (để BE biết cách xử lý DefaultUrl)
    }

    /// <summary>
    /// Description: Xóa (clear) nội dung mặc định của 1 dòng VmsCurrent.
    /// Author : ChuongLP
    /// Date: 2027-07-14
    /// </summary>
    public class DeleteVmsDefaultCurrentInput : BaseIdInput
    {
        // ID = VmsCurrent.ID. (Hoặc dùng EquipmentId nếu muốn — chọn 1 kiểu và nhất quán FE.)
    }

    /// <summary>
    /// Description: Đối tượng phân trang cho trang Quản lý nội dung mặc định VMS.
    /// Author : ChuongLP
    /// Date: 2027-07-14
    /// </summary>
    public class PageVmsDefaultCurrentInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? EquipmentId { get; set; }
        public List<string>? EquipmentIds { get; set; }
    }

    /// <summary>
    /// Description: Đối tượng chứa điều kiện tìm kiếm (không phân trang) nội dung mặc định VMS.
    /// Author : ChuongLP
    /// Date: 2027-07-14
    /// </summary>
    public class VmsDefaultCurrentListInput
    {
        public string? ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? EquipmentId { get; set; }
        public List<string>? EquipmentIds { get; set; }

    }
}
