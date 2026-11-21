using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.WorkDetailContact.Dto;

public class TmsWorkDetailContactOutput : TmsWorkDetailContacts
{
    // Begin - Add by Ngọc Thắng
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactRefId { get; set; }
    public string? ContactType { get; set; }
    // End - Add by Ngọc Thắng
}


public class PageTmsWorkDetailContactOutput : TmsWorkDetailContacts
{
}