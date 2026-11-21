using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.VDS.Core.Entities
{
    /// <summary>Tham số vận hành VDS, lưu trong SQL Server theo tenant.</summary>
    [SugarTable("VdsConfig")]
    public class VdsConfig : EntityTenant
    {


        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? GroupCode { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Value { get; set; }

        [SugarColumn(IsNullable = true)]
        public bool? IsSysConfig { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; }
    }
}
