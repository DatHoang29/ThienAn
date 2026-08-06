using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsVehicleRegistration")]
    public class TmsVehicleRegistration : EntityTenant
    {
        [SugarColumn(ColumnName = "LicensePlate")]
        public string Plate { get; set; } = null!;
        public string? Brand { get; set; }
        public string? Owner { get; set; }
    }
}
