using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.FMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [SugarTable("FmsEquipmentHistory")]
    public class FmsEquipmentHistory : EntityTenant
    {
        //FmsEquipmentHistory {
	       // ID varchar(64) pk unique
	       // EquipmentId string(64) null
	       // OldState string(16) null
	       // NewState string(16) null
	       // Remark string(512) null
	       // StartTime datetime null
	       // UpdateTime datetime null
	       // Type string(16) null def(manual)
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? OldState { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? NewState { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? StartTime { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? UpdateTime { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Type { get; set; } = "manual";
    }
}
