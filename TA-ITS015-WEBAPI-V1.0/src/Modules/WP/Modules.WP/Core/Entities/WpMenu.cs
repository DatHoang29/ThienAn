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
    /// Description: Entity menu hệ thống WP (cấu trúc cây qua Pid/Children)
    /// Created date: 25/06/2026
    /// </summary>
    [SugarTable("WpMenu")]
    public class WpMenu:EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "ParentID")]
        public string? Pid { get; set; } = "0";
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "dir: nhóm menu ; menu: trang")]
        public string? Type { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Route FE site WP, vd: /home, /news")]
        public string? Path { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Component { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Redirect { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Permission { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Title { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Icon { get; set; }
        [SugarColumn(IsNullable = true)]
        public bool? IsIFrame { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? OutLink { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public bool? IsHide { get; set; }
        [SugarColumn(IsNullable = true)]
        public bool? IsFullScreen { get; set; }
        [SugarColumn(IsNullable = true)]
        public bool? IsKeepAlive { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Remark { get; set; }
        [SugarColumn(IsNullable = true)]
        public bool? IsAffix { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "0: Disable ; 1: Enable")]
        public BaseEnums.StatusEnum? Status { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<WpMenu> Children { get; set; }

    }
}
