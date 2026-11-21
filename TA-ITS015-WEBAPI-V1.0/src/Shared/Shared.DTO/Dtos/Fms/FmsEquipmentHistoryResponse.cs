using Shared.DTO.Dtos.Base;
using Shared.DTO.Dtos.Base.Request;
using System;

namespace Shared.DTO.Dtos.Fms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin cơ bản FmsEquipmentHistory
    /// </summary>
    public class FmsEquipmentHistoryResponseBase : BaseEntityDto
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

        public string? EquipmentId { get; set; }
        public string? OldState { get; set; }
        public string? NewState { get; set; }
        public string? Remark { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string? Type { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin mở rộng FmsEquipmentHistory
    /// </summary>
    public class FmsEquipmentHistoryResponse : FmsEquipmentHistoryResponseBase
    {
    }
}
