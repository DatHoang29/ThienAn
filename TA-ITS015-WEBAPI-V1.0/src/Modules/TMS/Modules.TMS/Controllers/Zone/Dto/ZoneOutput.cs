using Shared.DTO.Enums;


using Modules.TMS.Controllers.ZoneStatus.Dto;
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.Zone.Dto;

public class ZoneOutput : TmsZone
{
    public bool? HasChildInList { get; set; }
    public ZoneStatusOutput ZoneStatus { get; set; }
}


public class PageZoneOutput : TmsZone
{
}

public sealed class ZoneSpeedConfigListOutput
{
    public List<ZoneDirectionDefaultOutput> DirectionDefaults { get; set; } = [];
    public List<ZoneSpeedConfigOutput> Zones { get; set; } = [];
}

public sealed class ZoneDirectionDefaultOutput
{
    public string Direction { get; set; } = "";
    public int MinSpeed { get; set; }
    public int MaxSpeed { get; set; }
}

public sealed class ZoneSpeedConfigOutput
{
    public string Id { get; set; } = "";
    public string? Pid { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? LaneId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
    public int? FromKmNumber { get; set; }
    public int? FromMetNumber { get; set; }
    public int? ToKmNumber { get; set; }
    public int? ToMetNumber { get; set; }
    public float? MinSpeed { get; set; }
    public float? MaxSpeed { get; set; }
    public List<ZoneVdsCameraOutput> VdsCameras { get; set; } = [];
}

public sealed class ZoneVdsCameraOutput
{
    public string Id { get; set; } = "";
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public string? DirectionId { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class BatchUpdateZoneSpeedOutput
{
    public int Updated { get; set; }
}
