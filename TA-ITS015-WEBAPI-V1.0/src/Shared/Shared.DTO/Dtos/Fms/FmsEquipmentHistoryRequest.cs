using Shared.DTO.Dtos.Base;
using Shared.DTO.Dtos.Base.Request;
using System;

namespace Shared.DTO.Dtos.Fms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO lấy danh sách FmsEquipmentHistory
    /// </summary>
    public class FmsEquipmentHistoryListRequest : BasePageReqDto
    {
        public string? EquipmentId { get; set; }
        public string? EquipmentTypeId { get; set; }
        public string? NewState { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
