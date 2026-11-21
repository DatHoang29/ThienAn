namespace Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNgay
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO output báo cáo doanh thu theo ngày (khớp template 1.2_DTTN.frx)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTNReportOutput
    {
        /// <summary>
        /// Cấu hình động lấy từ RptInfoConfig, gom theo nhóm: RptInfo.header.title, RptInfo.signature.left_name...
        /// FastReport bind trực tiếp vào node này thay vì set cứng trong file .frx.
        /// </summary>
        public Dictionary<string, Dictionary<string, string?>> RptInfo { get; set; } = new Dictionary<string, Dictionary<string, string?>>();

        public DTTNParameter Parameter { get; set; } = new DTTNParameter();
        public List<DTTNData> ChiTiet { get; set; } = new List<DTTNData>();
        public DTTNCong Cong { get; set; } = new DTTNCong();
        public DTTNSignature Signature { get; set; } = new DTTNSignature();
    }

    public class DTTNParameter
    {
        public string? DonViCapTren { get; set; }
        public string? DonViTrucThuoc { get; set; }
        public string? Hinh { get; set; }
        public string? Tram { get; set; }
        public string? Ngay { get; set; }
        public string? NgayHienTai { get; set; }
        public string? NgayXacDinh { get; set; }
        public string? DiaDanh { get; set; }
        public string? NhanVienBaoCao { get; set; }
    }

    public class DTTNData
    {
        public int STT { get; set; }
        public string? LoaiVe { get; set; }
        public decimal MenhGiaVe { get; set; }
        public long SoLuot { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class DTTNCong
    {
        public long SoLuot { get; set; }
        public decimal ThanhTien { get; set; }
        public decimal TongDoanhThu { get; set; }
        public string? BangChu { get; set; }
    }

    public class DTTNSignature
    {
        public string? ChucDanh1 { get; set; }
        public string? ChuKi1 { get; set; }
        public string? NguoiKi1 { get; set; }
        public string? ChucDanh2 { get; set; }
        public string? ChuKi2 { get; set; }
        public string? NguoiKi2 { get; set; }
        public string? ChucDanh3 { get; set; }
        public string? ChuKi3 { get; set; }
        public string? NguoiKi3 { get; set; }
    }

    /// <summary>
    /// DTO nhận kết quả từ stored proc ITS015_Proc_ReportRevenue_Daily.
    /// (Tên property = tên cột store trả về; nếu store đặt alias khác thì sửa cho khớp.)
    /// </summary>
    public class DTTNQueryOutput
    {
        public string? LoaiXe { get; set; }       // mã loại xe
        public string? TenLoaiXe { get; set; }    // -> ChiTiet.LoaiVe
        public decimal MenhGia { get; set; }      // -> MenhGiaVe
        public long SoLuong { get; set; }         // -> SoLuot
        public decimal ThanhTien { get; set; }    // -> ThanhTien
    }
}
