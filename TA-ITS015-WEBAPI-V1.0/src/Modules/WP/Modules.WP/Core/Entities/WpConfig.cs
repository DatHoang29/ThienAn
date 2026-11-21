using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Spectre.Console;
using SqlSugar;

namespace Modules.WP.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Entity cấu hình hệ thống WP (Config)
    /// Created date: 03/07/2026
    /// </summary>
    [SugarTable("WpConfig")]
    public class WpConfig: EntityBase
    {
        [SugarColumn(Length = EntityConst.Length256, IsNullable = true)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public new string? Code { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Value { get; set; }
        [SugarColumn(IsNullable = true,ColumnDescription = "0: False; 1: True")]
        public BaseEnums.StatusEnum? SysFlag { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? GroupCode { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? OrderNo { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
