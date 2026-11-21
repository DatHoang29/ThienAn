using Modules.CCTV.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.CCTV.Controllers.Dashboard.Dto;


public class DashboardInput
{
    public string? ID { get; set; }
    public string? Name { get; set; }
}

// DTO nhận ID từ endpoint truyền xuống
public class DashboardIdInput
{
    public string ID { get; set; }
}

public class DashboardTagInput
{
}

public class AddDashboardInput : CctvDashboard
{
    public List<string>? Tags { get; set; }
}

public class UpdateDashboardInput : AddDashboardInput
{

}

public class DeleteDashboardInput
{
     public string? ID { get; set; }
}

public class CopyDashboardInput
{
    public string? ID { get; set; }
    public string? Name { get; set; }
}