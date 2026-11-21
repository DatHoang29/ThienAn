using Modules.CCTV.Core.Entities;

namespace Modules.CCTV.Controllers.Dashboard.Dto;

public class DashboardOutput : CctvDashboard
{
    public List<string>? TagList { get; set; }
}

public class DashboardTagOutput
{
    public string? Name { get; set; }
}