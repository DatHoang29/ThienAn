
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.WorkDetail.Dto;

public class WorkDetailOutput : TmsWorkDetail
{
}


public class PageWorkDetailOutput : TmsWorkDetail
{
    public bool IsInUse { get; set; }
}