using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SqlSugar;
using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Core.Entities
{
    /// <summary>
    /// Author: 
    /// Description: Bảng hiển thị thông tin hiện tại của VMS
    /// Created date: 09/08/2024
    /// </summary>
    [SugarTable("VmsCurrent", TableDescription = "Hiển thị thông tin hiện tại của VMS")]
    public class VmsCurrent : EntityTenant
    {
        /* - - - - - - - - - - - - - - - - - - - - *
         * 
        * - - - - - - - - - - - - - - - - - - - - */

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "")]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "")]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024, ColumnDescription = "")]
        public string? RowData { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024, ColumnDescription = "")]
        public string? DefaultRowData { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "")]
        public string? Url { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "")]
        public string? DefaultUrl { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? Size { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? Font { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? DefaultFont { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? FontSize { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? Color { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? DefaultColor { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? BackColor { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? DefaultBackColor { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "")]
        public string? ExecutedBy { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public DateTime? ExecutedDate { get; set; }

        [SugarColumn(IsNullable = true,  ColumnDescription = "")]
        public int? Priority { get; set; }

		[SugarColumn(IsNullable = true, ColumnDescription = "")]
		public int? Brightness { get; set; }


	}
}
