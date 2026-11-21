using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Modules.WP.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Entity danh mục hệ thống WP (Category)
    /// Created date: 03/07/2026
    /// </summary>
    [SugarTable("WpCategory")]
    public class WpCategory:EntityTenant
    {
        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Author { get; set; }

        [SugarColumn(IsNullable = true)]
        public BaseEnums.HeroEnum? IsHero { get; set; }

    }
}
