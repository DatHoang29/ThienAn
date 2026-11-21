namespace Modules.WP.Report.Controllers.Statistical.Dto
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin báo cáo thống kê lưu lượng truy cập theo tháng
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalMonthReportOutput
    {
        public StatisticalMonthParameter Parameter { get; set; } = new StatisticalMonthParameter();
        public List<StatisticalMonthData> ChiTiet { get; set; } = new List<StatisticalMonthData>();
        public StatisticalCong Cong { get; set; } = new StatisticalCong();
        public StatisticalSignature Signature { get; set; } = new StatisticalSignature();
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin tham số báo cáo theo tháng
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalMonthParameter
    {
        public string DonViCapTren { get; set; } = string.Empty;
        public string DonViTrucThuoc { get; set; } = string.Empty;
        public string Hinh { get; set; } = string.Empty;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public string NgayHienTai { get; set; } = string.Empty;
        public string NgayXacDinh { get; set; } = string.Empty;
        public string DiaDanh { get; set; } = string.Empty;
        public string NhanVienBaoCao { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin chi tiết lưu lượng theo ngày trong tháng
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalMonthData
    {
        public int STT { get; set; }
        public string Thang { get; set; } = string.Empty;
        public string NgayThangNam { get; set; } = string.Empty;
        public int LuuLuong { get; set; }
    }
}
