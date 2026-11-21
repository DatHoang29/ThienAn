using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Modules.Samplev2.Core.Entities
{
    [SugarTable("SampCategory")]
    public class SampCategory:EntityTenant
    {
        [SugarColumn(Length = EntityConst.Length256, IsNullable = true)]
        public override string? Code { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Description { get; set; }

        [SugarColumn(IsNullable = true,ColumnDescription = "0: chưa kích hoạt; 1: đã kích hoạt")]
        public BaseEnums.StatusEnum? Status { get; set; }
    }
}
