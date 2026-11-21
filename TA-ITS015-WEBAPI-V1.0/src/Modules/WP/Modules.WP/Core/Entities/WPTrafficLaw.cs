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
    /// Description: Entity quy định giao thông hệ thống WP (TrafficLaw)
    /// Created date: 03/07/2026
    /// </summary>
    [SugarTable("WpTrafficLaw")]
    public class WpTrafficLaw:EntityTenant
    {
        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? Title { get; set; }

        [SugarColumn(IsNullable = true, Length = -1)]
        public string? Content { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Author { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0: Draft; 1: Publish")]
        public BaseEnums.StatusEnum? IsPublish { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? PdfUrl { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? PublishTime { get; set; }
    }
}
