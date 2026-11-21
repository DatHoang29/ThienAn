
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.SignalLogs.Dto;

public class SignalLogsOutput : TmsSignalLog
{
    public string GeoJsonContent { get; set; } // New property to hold file content

}


public class PageTmsSignalLogsOutput : TmsSignalLog
{
}