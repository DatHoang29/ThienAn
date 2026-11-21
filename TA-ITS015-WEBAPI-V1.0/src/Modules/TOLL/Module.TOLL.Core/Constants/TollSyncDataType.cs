namespace Module.TOLL.Core.Constants
{
    /// <summary>
    /// Danh sách loại dữ liệu thu phí có thể lấy lại (đồng bộ) từ hệ thống TCS/ETC.
    /// Giá trị chuỗi PHẢI khớp tuyệt đối với catalog trong dịch vụ ITS.Toll.Worker
    /// (TollOpConfigReader._catalog) để dịch vụ tra đúng endpoint khi nhận lệnh qua NATS.
    /// Author: ChuongLP
    /// Created date: 14/09/2026
    /// </summary>
    public static class TollSyncDataType
    {
        /// <summary>Danh mục dùng chung (loại xe, loại vé, loại giá, bảng giá chi tiết...). Không cần khoảng thời gian.</summary>
        public const string Master = "Master";

        /// <summary>Trạm thu phí. Không cần khoảng thời gian.</summary>
        public const string Station = "Station";

        /// <summary>Làn thu phí. Không cần khoảng thời gian.</summary>
        public const string Lane = "Lane";

        /// <summary>Ca làm việc. Không cần khoảng thời gian.</summary>
        public const string Shift = "Shift";

        /// <summary>Giao dịch xe vào. Cần khoảng thời gian (from/to).</summary>
        public const string TransactionIn = "TransactionIn";

        /// <summary>Giao dịch xe ra. Cần khoảng thời gian (from/to).</summary>
        public const string TransactionOut = "TransactionOut";

        /// <summary>Tổng hợp vé ra (doanh thu). Không cần khoảng thời gian.</summary>
        public const string OutSummary = "OutSummary";

        /// <summary>Nhật ký truy cập/thao tác. Cần khoảng thời gian (from/to).</summary>
        public const string AccessLog = "AccessLog";
    }
}
