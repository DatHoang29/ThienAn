#region using reference

// For: Entity

// For: BasePageInput

#endregion

using Modules.VMS.Core.Entities;

namespace Modules.VMS.Controllers.Process.Dto
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto của VmsProcessE truyền ra phần tìm kiếm
    /// Created date: 14/10/2024
    /// </summary>
    public class ProcessOutput: VmsProcess {}
     
    /// <summary>
    /// Author: KhoaND
    /// Description: Đối tượng dto của VmsProcessE truyền ra phần phân trang
    /// Created date: 14/10/2024
    /// </summary>
    public class PageVmsProcessOutput: VmsProcess { }
}
