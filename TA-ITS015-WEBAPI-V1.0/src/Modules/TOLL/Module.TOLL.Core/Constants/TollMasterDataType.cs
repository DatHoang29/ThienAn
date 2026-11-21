namespace Module.TOLL.Core.Constants
{
    /// <summary>
    /// Nhóm danh mục trong bảng TollMasterData (cột DataType).
    /// Dùng để lọc khi join lấy tên hiển thị của danh mục.
    /// Author: HieuNV
    /// Created date: 27/07/2026
    /// </summary>
    public static class TollMasterDataType
    {
        /// <summary>Loại xe - nguồn Sys_VehicleType</summary>
        public const string VehicleType = "VehicleType";

        /// <summary>Loại vé - nguồn Sys_TicketType</summary>
        public const string TicketType = "TicketType";

        /// <summary>Loại giá - nguồn Sys_PriceType</summary>
        public const string PriceType = "PriceType";

        /// <summary>Lý do - nguồn Sys_Reason</summary>
        public const string Reason = "Reason";

        /// <summary>Hình thức thanh toán - nguồn Sys_Payment</summary>
        public const string Payment = "Payment";

        /// <summary>Trạng thái - nguồn Sys_Status</summary>
        public const string Status = "Status";

        /// <summary>Trạng thái giao dịch (hậu kiểm) - nguồn Sys_TransactionStatus</summary>
        public const string TransactionStatus = "TransactionStatus";
    }
}
