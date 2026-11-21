namespace Modules.TMS.Controllers.WorkExecute.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin trạng thái state
    /// Created date: 20/11/2024
    /// </summary>
    public class TmsWorkExecuteStateInput
    {
        public string? Id { get; set; }
        public string? State { get; set; }
        public string? Remark { set; get; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin các tham số
    /// Created date: 20/11/2024
    /// </summary>
    public class TmsWorkExecuteParaInput
    {
        public string? IncidentId { get; set; }
        public bool? UseForTimeline { get; set; }
    }
}
