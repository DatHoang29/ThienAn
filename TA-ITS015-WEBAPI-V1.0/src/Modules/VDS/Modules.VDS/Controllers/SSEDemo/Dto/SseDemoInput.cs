using Shared.Infrastructure.Sse;

namespace Modules.VDS.Controllers.SSEDemo.Dto;

public class SseDemoInput
{
    [System.ComponentModel.DataAnnotations.Range(1, 60)]
    public int Refresh { get; set; } = 5;
    public string? Name { get; set; }
}

public class SseDemoTickInput
{
    [System.ComponentModel.DataAnnotations.Range(1, 60)]
    public int Refresh { get; set; } = 3;
}
