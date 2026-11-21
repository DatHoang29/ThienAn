using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.SignalLogs.Dto;

public class PageTmsSignalLogsInput : BasePageInput
{
    public string Code { get; set; }
    public string EquipmentId { get; set; }
    public string ID { get; set; }
}

public class SignalLogsInput
{

    public string Code { get; set; }
    public string EquipmentId { get; set; }
    public string ID { get; set; }
}
public class SignalLog
{
    public string incidentId { get; set; }
    public string vmsId { get; set; }
    public string imageUrl { get; set; }
    public string data { get; set; }
    public string type { get; set; }
    public string OldUrl { get; set; }
    public int timetolive { get; set; }
}
public class AddTmsSignalLogInput : TmsSignalLog
{
}

public class UpdateTmsSignalLogInput : AddTmsSignalLogInput
{

}

public class DeleteTmsSignalLogInput : BaseIdInput
{

}