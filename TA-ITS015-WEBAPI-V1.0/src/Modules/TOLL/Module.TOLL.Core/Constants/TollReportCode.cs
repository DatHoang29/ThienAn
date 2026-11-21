namespace Module.TOLL.Core.Constants
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Mã báo cáo thu phí — NGUỒN DUY NHẤT.
    ///              Dùng chung cho QueryHandler của Module.TOLL.Report (mã mẫu báo cáo khi render / export)
    ///              và phân hệ Mail (đặt lịch gửi báo cáo qua email). Thêm báo cáo mới khai mã ở đây,
    ///              không khai chuỗi cứng trong handler.
    /// Created date: 01/10/2026
    /// </summary>
    public static class TollReportCode
    {
        /* ── Lưu lượng ── */
        public const string LuuLuongTheoCaDauVao = "1.1_LLTCDV";
        public const string LuuLuongTheoCaDauRa = "1.1_LLTCDR";
        public const string LuuLuongTheoNgayDauVao = "1.2_LLTNDV";
        public const string LuuLuongTheoNgayDauRa = "1.2_LLTNDR";
        public const string LuuLuongTheoNamDauVao = "1.3_LLTNAMDV";
        public const string LuuLuongTheoNamDauRa = "1.3_LLTNAMDR";
        public const string LuuLuongTheoThangDauVao = "1.4_LLTTDV";
        public const string LuuLuongTheoThangDauRa = "1.4_LLTTDR";
        public const string LuuLuongTheoLoTrinh = "1.7_LLTLT";

        /* ── Doanh thu ── */
        public const string DoanhThuTheoThoiGian = "1.1_DTTTG";
        public const string DoanhThuTheoNgay = "1.2_DTTN";
        public const string DoanhThuTheoThang = "1.3_DTTT";
        public const string DoanhThuTheoQuy = "1.4_DTTQ";
        public const string DoanhThuTheoNam = "1.5_DTTNam";
    }
}
