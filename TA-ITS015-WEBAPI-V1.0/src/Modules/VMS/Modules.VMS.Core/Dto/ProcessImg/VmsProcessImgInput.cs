
using Modules.VMS.Core.Entities;
using Shared.Core.Utilities;


namespace Modules.VMS.Controllers.ProcessImg.Dto
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto chứa thông tin phân trang truyền vào
    /// Created date: 14/10/2024
    /// </summary>
    public class PageVmsProcessImgInput: BasePageInput
    {
        public string? ProcessId { get; set; }

    }

    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto chứa thông tin tìm kiếm truyền vào
    /// Created date: 14/10/2024
    /// </summary>
    public class VmsProcessImgInput
    {
        public string? ProcessId { get; set; }
    }

    /// <summary>
    /// Author: KhoaND
    /// Decription: Đối tượng dto của VmsProcessImg chứa thông tin cần lưu truyền vào
    /// Created date: 14/10/2024
    /// </summary>
    /// 
    public class ListUrl
    {
        public string ImageUrl { get; set; }
        public int TimeToLive { get; set; }
        public List<string> Data { get; set; }

    }
    public class AddVmsProcessImgInput : VmsProcessImg
    {
        public string OldUrl { get; set; }

        public List<ListUrl> ListUrl { get; set; }
    }

    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto của VmsProcessImg chứa thông tin cần cập nhật truyền vào
    /// Created date: 14/10/2024
    /// </summary>
    public class UpdateVmsProcessImgInput : AddVmsProcessImgInput {}

    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto chứa thông tin cần xóa khỏi csdl truyền vào
    /// Created date: 14/10/2024
    /// </summary>
    public class DeleteVmsProcessImgInput: BaseIdInput {}
}
