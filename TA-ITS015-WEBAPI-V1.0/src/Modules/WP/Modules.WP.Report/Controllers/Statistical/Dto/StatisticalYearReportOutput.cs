namespace Modules.WP.Report.Controllers.Statistical.Dto
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin báo cáo thống kê lưu lượng truy cập theo năm
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalYearReportOutput
    {
        public StatisticalYearParameter Parameter { get; set; } = new StatisticalYearParameter();
        public List<StatisticalYearData> ChiTiet { get; set; } = new List<StatisticalYearData>();
        public StatisticalCong Cong { get; set; } = new StatisticalCong();
        public StatisticalSignature Signature { get; set; } = new StatisticalSignature();
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin tham số báo cáo theo năm
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalYearParameter
    {
        public string DonViCapTren { get; set; } = string.Empty;
        public string DonViTrucThuoc { get; set; } = string.Empty;
        public string Hinh { get; set; } = string.Empty;
        public int Nam { get; set; }
        public string NgayHienTai { get; set; } = string.Empty;
        public string NgayXacDinh { get; set; } = string.Empty;
        public string DiaDanh { get; set; } = string.Empty;
        public string NhanVienBaoCao { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin chi tiết lưu lượng theo tháng trong năm
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalYearData
    {
        public int STT { get; set; }
        public int Nam { get; set; }
        public string ThangNam { get; set; } = string.Empty;
        public int LuuLuong { get; set; }
    }
}
