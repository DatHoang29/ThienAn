using System;

namespace Module.TOLL.Core.Dto.HaukiemTransaction
{
    /// <summary>
    /// Output hiển thị danh sách giao dịch cần hậu kiểm.
    /// Các field map theo TollTransactionIn / TollTransactionOut và bảng tra cứu
    /// TollStation / TollLane / TollMasterData
    /// Author: HieuNV
    /// Created date: 27/07/2026
    /// </summary>
    public class ListTransactionOutPut
    {
        public int? Seq { get; set; }

        /// <summary>
        /// ID bản ghi trong ITS (dùng cho thao tác hậu kiểm)
        /// </summary>
        public string? ID { get; set; }

        public string? TransactionId { get; set; }

        public string? TagId { get; set; }

        // ===== BIỂN SỐ =====
        public string? Plate { get; set; }

        public string? PlateLpr { get; set; }

        public string? PlateEdit { get; set; }

        // ===== LOẠI XE =====
        public string? VehicleTypeId { get; set; }

        public string? VehicleTypeName { get; set; }

        // ===== VÉ =====
        public string? TicketId { get; set; }

        public string? TicketTypeId { get; set; }

        public string? TicketTypeName { get; set; }

        public string? Serial { get; set; }

        /// <summary>
        /// Mức giá thu - chỉ có ở giao dịch xe ra (TollTransactionOut.Price)
        /// </summary>
        public decimal? Price { get; set; }

        // ===== TRẠNG THÁI =====
        public string? StatusId { get; set; }

        public string? StatusName { get; set; }

        public string? StatusChecked { get; set; }

        public string? StatusCheckedName { get; set; }

        // ===== TRẠM / LÀN (RA) =====
        public string? StationId { get; set; }

        public string? StationName { get; set; }

        public string? LaneId { get; set; }

        public string? LaneName { get; set; }

        // ===== TRẠM / LÀN (VÀO) =====
        public string? StationIdIn { get; set; }

        public string? StationInName { get; set; }

        public string? LaneInId { get; set; }

        public string? LaneInName { get; set; }

        // ===== THỜI GIAN =====
        public DateTime? TransactionDatetime { get; set; }

        public DateTime? TransactionDatetimeIn { get; set; }

        public DateTime? ProccessDate { get; set; }

        public DateTime? SyncTime { get; set; }

        // ===== ẢNH (đường dẫn gốc lưu trong DB) =====
        public string? ImageLane { get; set; }

        public string? ImagePlate { get; set; }

        public string? ImageLaneIn { get; set; }

        public string? ImagePlateIn { get; set; }

        // ===== NHÂN VIÊN / CA =====
        public string? UserId { get; set; }

        /// <summary>
        /// Tên nhân viên vận hành - lấy từ OperatorName của entity
        /// </summary>
        public string? UserName { get; set; }

        public string? ShiftId { get; set; }

        // ===== LÝ DO =====
        public string? ReasonId { get; set; }

        public string? ReasonName { get; set; }

        /// <summary>
        /// Hướng giao dịch: 1 = vào, 2 = ra
        /// </summary>
        public string? Direction { get; set; }
    }
}
