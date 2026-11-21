namespace Modules.TMS.Report.Controllers.TrafficStatistic.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticItemOutput
    {
        public float AvgSpeed { get; set; }
        public int TotalVehicle { get; set; }
        public float SpaceOccupy { get; set; }
        public string Time { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticReportOutput
    {
        public string Section { get; set; }
        public string Zone { get; set; }
        public string Camera { get; set; }
        public float AvgSpeed { get; set; }
        public int TotalVehicle { get; set; }
        public float SpaceOccupy { get; set; }

        public List<TrafficStatisticItemOutput> Items { get; set; }
    }
}
