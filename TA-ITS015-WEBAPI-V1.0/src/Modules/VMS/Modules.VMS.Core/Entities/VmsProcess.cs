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
    /// Description: Thông tin tiến trình xử lý của VMS (Hẹn giờ, lập lịch)
    /// Created date: 09/08/2024
    /// Note: Tạo bảng thông tin cấu hình của hệ thống
    /// </summary>
    [SugarTable("VmsProcess", TableDescription = "Thông tin tiến trình xử lý của VMS (Hẹn giờ, lập lịch)")]
    public class VmsProcess : EntityTenant
    {
        /* - - - - - - - - - - - - - - - - - - - - *
         * VmsProcess:
         * 
	        ID varchar(64) pk unique
	        Code string(64) null
	        Name string(64) null
	        Lang string(16) null
	        Type string(16) null
	        Content1 string(256) null
	        Content2 string(256) null
	        Content3 string(256) null
	        OrderNo integer null
	        Status integer null
        * - - - - - - - - - - - - - - - - - - - - */

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "")]
        public string? VmsId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "")]
        public string? ThreadId { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? Brightness { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? State { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public DateTime? TimeStart { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public DateTime? TimeEnd { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? Priority { get; set; }
        
    }
}
