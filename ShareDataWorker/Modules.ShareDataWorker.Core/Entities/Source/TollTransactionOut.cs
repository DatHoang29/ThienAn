using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TollTransactionOut")]
    public class TollTransactionOut : EntityTenant
    {
        public string TransactionId { get; set; } = null!;
        public string? TagId { get; set; }
        public string? PlateEdit { get; set; }
        public string? Plate { get; set; }
        public string? PlateLpr { get; set; }
        public string? VehicleTypeId { get; set; }
        public DateTime? TransactionDateTimeIn { get; set; }
        public DateTime? TransactionDateTime { get; set; }
        public string? LaneId { get; set; }
        public string? StationId { get; set; }
        public DateTime? SyncTime { get; set; }
    }
}
