using Modules.TMS.Core.Entities;
using System.Text.Json.Serialization;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.VDS.Controllers.SSEDemo.Dto;

public class SseDemoSnapshotOutput
{
    public DateTimeOffset Timestamp { get; set; }

    public int Total { get; set; }

    public List<SseDemoEquipmentOutput> Data { get; set; } = new();
}


public class SseDemoTickOutput
{
    public DateTimeOffset At { get; set; }

    public int Total { get; set; }
}

public class SseDemoEquipmentOutput
{
    public string? ID { get; set; }
    public string? Name { get; set; }
    public string? EquipmentTypeId { get; set; }
    public int? EquipmentState { get; set; }
}
