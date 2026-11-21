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
    /// Description: Entity statistical hệ thống WP (Thống kê trang web)
    /// Created date: 16/07/2026
    /// </summary>
    [SugarTable("WpStatistical")]
    public class WpStatistical : EntityTenant
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "StatYear")]
        public int StatYear { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "StatMonth")]
        public int StatMonth { get; set; }
        
        [SugarColumn(IsNullable = false, ColumnDescription = "StatDate" , UniqueGroupNameList = new[] { "vn_date_hour" })]
        public DateTime StatDate { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "StatHour", UniqueGroupNameList = new[] { "vn_date_hour" })]
        public int StatHour { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "Count")]
        public int Count { get; set; } = 0;
    }
}
