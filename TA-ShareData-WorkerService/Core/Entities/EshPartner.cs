using SqlSugar;
using TA_ShareData_WorkerService.Core.Enums;

namespace TA_ShareData_WorkerService.Core.Entities
{
    [SugarTable("EshPartner")]
    [SugarIndex("UX_EshPartner_Code", "Code", OrderByType.Asc, "IsDelete", OrderByType.Asc, true)]
    public class EshPartner : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Code { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Address { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Port { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Username { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? PasswordHash { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? Status { get; set; } = EshEnums.PartnerStatus.Enabled;
    }
}
