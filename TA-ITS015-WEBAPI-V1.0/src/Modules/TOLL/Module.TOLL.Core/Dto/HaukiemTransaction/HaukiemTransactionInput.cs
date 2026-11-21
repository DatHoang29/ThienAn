using Module.TOLL.Core.Enums;
using Shared.Core.Utilities;
using System;

namespace Module.TOLL.Core.Dto.HaukiemTransaction
{
    /// <summary>
    /// Input điều kiện lấy danh sách giao dịch cần hậu kiểm.
    /// Direction: 1 = chỉ xe vào, 2 = chỉ xe ra, null/khác = lấy cả hai
    /// Author: HieuNV
    /// Created date: 27/07/2026
    /// </summary>
    public class GetQueryPostTransactionInput : BasePageInput
    {
        public string? TransactionId { get; set; }

        public string? TagId { get; set; }

        public string? Plate { get; set; }

        public string? PlateLpr { get; set; }

        public string? VehicleTypeId { get; set; }

        public string? TicketId { get; set; }

        public string? TicketTypeId { get; set; }

        public string? StatusId { get; set; }

        public string? StatusChecked { get; set; }

        public string? StationId { get; set; }

        public string? LaneId { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string? UserId { get; set; }

        public string? ShiftId { get; set; }

        /// <summary>
        /// 1 = xe vào, 2 = xe ra, để trống = cả hai hướng
        /// </summary>
        public TollEnums.DirectionEnum? Direction { get; set; }

        public string? ReasonId { get; set; }
    }
}
