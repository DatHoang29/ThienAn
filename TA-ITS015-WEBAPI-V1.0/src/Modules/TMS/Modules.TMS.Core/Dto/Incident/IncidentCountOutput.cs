namespace Modules.TMS.Core.Dto.Incident
{
    public class IncidentCountOutput
    {
        public int? Total { get; set; }
        public int? New { get; set; }
        public int? InValid { get; set; }
        public int? Valid { get; set; }
    }
}
