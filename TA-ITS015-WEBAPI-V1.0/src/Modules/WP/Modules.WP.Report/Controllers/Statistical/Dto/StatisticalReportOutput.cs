namespace Modules.WP.Report.Controllers.Statistical.Dto
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin báo cáo thống kê lưu lượng truy cập
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalReportOutput
    {
        public StatisticalParameter Parameter { get; set; } = new StatisticalParameter();
        public List<StatisticalData> ChiTiet { get; set; } = new List<StatisticalData>();
        public StatisticalCong Cong { get; set; } = new StatisticalCong();
        public StatisticalSignature Signature { get; set; } = new StatisticalSignature();
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin tham số báo cáo
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalParameter
    {
        public string DonViCapTren { get; set; } = string.Empty;
        public string DonViTrucThuoc { get; set; } = string.Empty;
        public string Hinh { get; set; } = string.Empty;
        public string Ngay { get; set; } = string.Empty;
        public string NgayHienTai { get; set; } = string.Empty;
        public string NgayXacDinh { get; set; } = string.Empty;
        public string DiaDanh { get; set; } = string.Empty;
        public string NhanVienBaoCao { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin chi tiết lưu lượng theo giờ
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalData
    {
        public int STT { get; set; }
        public string Ngay { get; set; } = string.Empty;
        public string Gio { get; set; } = string.Empty;
        public int LuuLuong { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin dòng tổng cộng
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalCong
    {
        public string Nhan { get; set; } = string.Empty;
        public int LuuLuong { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin chữ ký báo cáo
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalSignature
    {
        public string ChucDanh1 { get; set; } = string.Empty;
        public string ChuKi1 { get; set; } = string.Empty;
        public string NguoiKi1 { get; set; } = string.Empty;
        public string ChucDanh2 { get; set; } = string.Empty;
        public string ChuKi2 { get; set; } = string.Empty;
        public string NguoiKi2 { get; set; } = string.Empty;
        public string ChucDanh3 { get; set; } = string.Empty;
        public string ChuKi3 { get; set; } = string.Empty;
        public string NguoiKi3 { get; set; } = string.Empty;
    }
}
