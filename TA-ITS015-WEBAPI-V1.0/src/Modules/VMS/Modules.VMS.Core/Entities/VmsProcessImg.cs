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
    /// Description: Bảng thông tin hình ảnh của tiến trình VMS liên quan
    /// Created date: 09/08/2024
    /// </summary>
    [SugarTable("VmsProcessImg", TableDescription = "Bảng thông tin hình ảnh của tiến trình VMS liên quan")]
    public class VmsProcessImg : EntityTenant
    {
        /* - - - - - - - - - - - - - - - - - - - - *
         * VmsProcessImg:
         * 
	       
        * - - - - - - - - - - - - - - - - - - - - */

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "")]
        public string? ProcessId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "")]
        public string? Url { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? TimeToLive { get; set; }

        /// <summary>
        /// Khóa liên kết sự cố (TmsIncident.ID) nếu ảnh thuộc về sự cố
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID sự cố liên quan")]
        public string? IncidentId { get; set; }

        /// <summary>
        /// Nguồn gốc ảnh: schedule, incident_auto, incident_manual, manual_append
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Nguồn gốc ảnh")]
        public string? SourceType { get; set; }

        /// <summary>
        /// Trạng thái ảnh có đang được kích hoạt phát trong vòng lặp hay không
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Trạng thái ảnh có đang kích hoạt phát")]
        public bool? IsActive { get; set; } = true;
    }
}
