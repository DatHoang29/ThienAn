using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.WorkDetailContact.Dto;

public class PageTmsWorkDetailContactInput : BasePageInput
{
    public string Code { get; set; }
    public string WorkDetailId { get; set; }
    public string WorkContactId { get; set; }
    public int OrderNo { get; set; }

}

public class TmsWorkDetailContactInput
{
    public int OrderNo { get; set; }

    public string WorkDetailId { get; set; }
    public string Code { get; set; }
    public string WorkContactId { get; set; }


}

public class AddTmsWorkDetailContactInput : TmsWorkDetailContacts
{
}

public class UpdateTmsWorkDetailContactInput : AddTmsWorkDetailContactInput
{

}

public class DeleteTmsWorkDetailContactInput : BaseIdInput
{

}

/// <summary>
/// Author: Ngọc Thắng
/// </summary>
public class TmsWorkDetailIdsInput
{
    public string? WorkDetailIdList { get; set; }
}