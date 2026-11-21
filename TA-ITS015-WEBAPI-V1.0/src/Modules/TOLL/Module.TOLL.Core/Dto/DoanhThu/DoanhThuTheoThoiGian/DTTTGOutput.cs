namespace Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoThoiGian
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO output báo cáo doanh thu theo thời gian (khớp template 1.1_DTTTG.frx)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTTGReportOutput
    {
        /// <summary>
        /// Cấu hình động lấy từ RptInfoConfig, gom theo nhóm: RptInfo.header.title, RptInfo.signature.left_name...
        /// FastReport bind trực tiếp vào node này thay vì set cứng trong file .frx.
        /// </summary>
        public Dictionary<string, Dictionary<string, string?>> RptInfo { get; set; } = new Dictionary<string, Dictionary<string, string?>>();

        public DTTTGParameter Parameter { get; set; } = new DTTTGParameter();
        public List<DTTTGData> ChiTiet { get; set; } = new List<DTTTGData>();
        public DTTTGCong Cong { get; set; } = new DTTTGCong();
        public DTTTGSignature Signature { get; set; } = new DTTTGSignature();
    }

    public class DTTTGParameter
    {
        public string? DonViCapTren { get; set; }
        public string? DonViTrucThuoc { get; set; }
        public string? Hinh { get; set; }
        public string? Tram { get; set; }
        public string? NgayBatDau { get; set; }
        public string? NgayKetThuc { get; set; }
        public string? NgayHienTai { get; set; }
        public string? NgayXacDinh { get; set; }
        public string? DiaDanh { get; set; }
        public string? NhanVienBaoCao { get; set; }
    }

    public class DTTTGData
    {
        public int ThuTuNhom { get; set; }
        public string? TenNhom { get; set; }
        public string? LoaiVe { get; set; }
        public decimal MenhGia { get; set; }
        public string? LoTrinh { get; set; }
        public long MTC_SLVe { get; set; }
        public decimal MTC_ThanhTien { get; set; }
        public long ETC_HC_SLVe { get; set; }
        public decimal ETC_HC_ThanhTien { get; set; }
        public long TongHop_SLVe { get; set; }
        public decimal TongHop_ThanhTien { get; set; }
    }

    public class DTTTGCong
    {
        public long MTC_SLVe { get; set; }
        public decimal MTC_ThanhTien { get; set; }
        public long ETC_HC_SLVe { get; set; }
        public decimal ETC_HC_ThanhTien { get; set; }
        public long TongHop_SLVe { get; set; }
        public decimal TongHop_ThanhTien { get; set; }
    }

    public class DTTTGSignature
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
    /// DTO nhận kết quả từ stored proc ITS015_Proc_ReportRevenue_Time.
    /// (Tên property = tên cột store trả về; nếu store đặt alias khác thì sửa cho khớp.)
    /// </summary>
    public class DTTTGQueryOutput
    {
        public string? TenNhom { get; set; }
        public string? LoaiXe { get; set; }              // -> ChiTiet.LoaiVe
        public decimal MenhGia { get; set; }
        public string? LoTrinh { get; set; }
        public long SoLuong { get; set; }                // -> MTC_SLVe
        public decimal ThanhTien { get; set; }           // -> MTC_ThanhTien
        public long SoLuong_DieuChinh { get; set; }      // -> ETC_HC_SLVe
        public decimal ThanhTien_DieuChinh { get; set; } // -> ETC_HC_ThanhTien
        public long SoLuong_Tong { get; set; }           // -> TongHop_SLVe
        public decimal ThanhTien_Tong { get; set; }      // -> TongHop_ThanhTien
    }
}
