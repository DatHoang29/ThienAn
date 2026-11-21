using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.VMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    [SugarTable("VmsImage")]
    public class VmsImage : EntityTenant
    {
        /* - - - - - - - - - - - - - - - - - - - - *
         * VmsImage:
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

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "")]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? Lang { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? Type { get; set; }

        /// <summary>
        /// Khóa liên kết loại thiết bị VMS hoặc LCS (TmsEquipmentType.ID).
        /// Dùng để lọc đúng thư viện ảnh theo loại thiết bị khi điều khiển.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Mã loại thiết bị VMS hoặc LCS (TmsEquipmentType)")]
        public string? EquipmentTypeId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "")]
        public string? Url { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "")]
        public string? Size { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "Display order")]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; }
    }
}
