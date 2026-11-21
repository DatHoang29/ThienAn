using Modules.TMS.Core.Dto.IncidentControl;
using Modules.VMS.Infrastructure.Services.Process;

namespace Modules.VMS.Controllers.Current.Commands;

/// <summary>
/// Adapter message-bus cho automation. Luồng manual vẫn đi thẳng vào
/// VmsProcessService.ResetIncidentToDefault; adapter này chỉ cho phép TMS auto
/// gọi lại đúng method đó mà không gọi vòng qua HTTP/controller.
/// </summary>
public sealed class ResetIncidentToDefaultCommandHandler
{
    private readonly VmsProcessService _vmsProcessService;

    public ResetIncidentToDefaultCommandHandler(VmsProcessService vmsProcessService)
    {
        _vmsProcessService = vmsProcessService;
    }

    public Task HandleAsync(ResetIncidentToDefaultInput command)
        => _vmsProcessService.ResetIncidentToDefault(command.IncidentId);
}
