using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;
using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.VMS.Core.Entities
{
    /// <summary>
    /// Author: SonTH
    /// Description: VmsTemplate
    /// Created date: 2025/01/17
    /// </summary>
    [SugarTable("VmsTemplate", TableDescription = "")]
	public class VmsTemplate : EntityTenant
	{
		/* - - - - - - - - - - - - - - - - - - - - *
		 * VmsTemplate {
				ID varchar(64) pk unique
				Code string(64) null unique
				Name string(64) null
				Lang string(16) null def(vi-VN)
				JsonData string(256) null
				Size string(16) null
				RowNo integer null
				ImageNo integer null
				OrderNo integer null
				Preview string(256) null
				Status integer null
				Remark string(256)
			}
		* - - - - - - - - - - - - - - - - - - - - */

		[SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Name")]
		public string? Name { get; set; }

		[SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Language")]
		public string? Lang { get; set; }

		[SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Json Data")]
		public string? JsonData { get; set; }

		[SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Size")]
		public string? Size { get; set; }

		[SugarColumn(IsNullable = true, ColumnDescription = "Row number")]
		public int? RowNo { get; set; }

		[SugarColumn(IsNullable = true, ColumnDescription = "Image number")]
		public int? ImageNo { get; set; }

		[SugarColumn(IsNullable = true, ColumnDescription = "Order")]
		public int? OrderNo { get; set; }

		[SugarColumn(IsNullable = true, ColumnDescription = "Activation status => 0: disable, 1: enable")]
		public BaseEnums.StatusEnum? Status { get; set; }

		[SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Review")]
		public string? Preview { get; set; }

		[SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Remark")]
		public string? Remark { get; set; }
	}
}
