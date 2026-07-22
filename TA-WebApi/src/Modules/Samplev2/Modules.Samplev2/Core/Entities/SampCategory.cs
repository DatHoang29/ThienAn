using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.Samplev2.Core.Entities
{
    [SugarTable("SampCategory")]
    public class SampCategory : EntityTenant
    {

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Description { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0: chưa kích hoạt; 1: đã kích hoạt")]
        public BaseEnums.StatusEnum? Status { get; set; } = BaseEnums.StatusEnum.Enable;
    }
}
