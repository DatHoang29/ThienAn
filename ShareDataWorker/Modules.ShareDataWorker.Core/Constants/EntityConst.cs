namespace Modules.ShareDataWorker.Core.Constants
{
    /// <summary>
    /// Các hằng số độ dài trường dữ liệu (Entity Constants) dùng cho Module ShareData
    /// Author: Đạt
    /// Created date: 02/08/2026
    /// </summary>
    public static class EntityConst
    {
        /// <summary>
        /// Độ dài trường sử dụng trong bảng Tham số cấu hình.
        /// Kích thước hiện tại: 16
        /// </summary>
        public const int ConfigFieldLength = 16;

        /// <summary>
        /// Độ dài trường khoá chính/ khoá ngoại.
        /// Kích thước hiện tại: 64
        /// </summary>
        public const int KeyFieldLength = 64;

        /// <summary>
        /// Độ dài trường Remark ghi chú.
        /// Kích thước hiện tại: 256
        /// </summary>
        public const int RemarkFieldLength = 256;

        /// <summary>
        /// Kích thước hiện tại: 8
        /// </summary>
        public const int Length8 = 8;

        /// <summary>
        /// Kích thước hiện tại: 16
        /// </summary>
        public const int Length16 = 16;

        /// <summary>
        /// Kích thước hiện tại: 32
        /// </summary>
        public const int Length32 = 32;

        /// <summary>
        /// Kích thước hiện tại: 64
        /// </summary>
        public const int Length64 = 64;

        /// <summary>
        /// Kích thước hiện tại: 128
        /// </summary>
        public const int Length128 = 128;

        /// <summary>
        /// Kích thước hiện tại: 256
        /// </summary>
        public const int Length256 = 256;

        /// <summary>
        /// Kích thước hiện tại: 512
        /// </summary>
        public const int Length512 = 512;

        /// <summary>
        /// Kích thước hiện tại: 1024
        /// </summary>
        public const int Length1024 = 1024;

        /// <summary>
        /// Độ dài tổng (precision) cho trường số thực (decimal).
        /// Kích thước hiện tại: 18
        /// </summary>
        public const int DecimalLength = 18;

        /// <summary>
        /// Số chữ số thập phân (scale) cho trường số thực (decimal).
        /// Kích thước hiện tại: 2
        /// </summary>
        public const int DecimalDigits = 2;
    }
}
