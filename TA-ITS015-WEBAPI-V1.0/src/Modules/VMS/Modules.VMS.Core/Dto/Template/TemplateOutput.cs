#region using reference

// For: Entity
using Modules.VMS.Core.Entities;

// For: BasePageInput

#endregion

namespace Modules.VMS.Controllers.Template.Dto
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: Đối tượng dto của VmsTemplateE truyền ra phần tìm kiếm
    /// Created date: 09/08/2024
    /// </summary>
    public class TemplateOutput: VmsTemplate {}
     
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: Đối tượng dto của VmsTemplateE truyền ra phần phân trang
    /// Created date: 09/08/2024
    /// </summary>
    public class PageTemplateOutput: VmsTemplate { }
}
