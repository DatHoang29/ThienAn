#region using reference

// For: Entity

// For: BasePageInput

#endregion

using Modules.VMS.Core.Entities;

namespace Modules.VMS.Controllers.Current.Dto
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto của VmsCurrentE truyền ra phần tìm kiếm
    /// Created date: 18/10/2024
    /// </summary>
    public class VmsCurrentOutput: VmsCurrent {}
     
    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto của VmsCurrentE truyền ra phần phân trang
    /// Created date: 18/10/2024
    /// </summary>
    public class PageVmsCurrentOutput: VmsCurrent { }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Đối tượng dto truyền ra cho trang Quản lý nội dung mặc định VMS
    /// Created date: 14/07/2027
    /// </summary>
    public class PageVmsDefaultCurrentOutput : VmsCurrent { }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Đối tượng dto truyền ra cho tìm kiếm (list) nội dung mặc định VMS
    /// Created date: 14/07/2027
    /// </summary>
    public class VmsDefaultCurrentOutput : VmsCurrent { }
}
