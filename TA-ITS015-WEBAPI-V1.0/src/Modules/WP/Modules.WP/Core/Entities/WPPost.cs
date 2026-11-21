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
    /// Description: Entity bài viết hệ thống WP (Post)
    /// Created date: 03/07/2026
    /// </summary>
    [SugarTable("WpPost")]
    public class WpPost:EntityTenant
    {
        [SugarColumn(Length = -1, IsNullable = true)]
        public string? Title { get; set; }

        [SugarColumn(Length = -1, IsNullable = true)] //TODO:  Check Length for category
        public string? Category { get; set; }

        [SugarColumn(IsNullable = true, Length = -1)]
        public string? Excerpt { get; set; }
        [SugarColumn(IsNullable = true, Length = -1)]
        public string? Content { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Author { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Thumbnail { get; set; }


        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true,ColumnDescription = "0: Draft; 1: Public")]
        public BaseEnums.StatusEnum?  IsDraft{ get; set; }

        [SugarColumn(IsNullable = true)]
        public BaseEnums.HeroEnum? IsHero { get; set; }
    }
}
